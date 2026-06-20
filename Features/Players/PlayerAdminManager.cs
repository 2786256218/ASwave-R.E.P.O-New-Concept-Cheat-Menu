using System;
using System.Collections.Generic;
using System.Reflection;
using Cheat.Utils;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cheat.Features.Players
{
    public static class PlayerAdminManager
    {
        public sealed class PlayerRecord
        {
            public PlayerAvatar Avatar;
            public string DisplayName;
            public bool IsLocal;
            public bool IsAlive;
            public int CurrentHealth;
            public int MaxHealth;
        }

        private sealed class PlayerAdminNetHandler : MonoBehaviourPun
        {
            public bool RequestRevive(PlayerAvatar targetAvatar)
            {
                if (targetAvatar == null || targetAvatar.photonView == null)
                {
                    return false;
                }

                if (photonView == null || !photonView.IsMine)
                {
                    return false;
                }

                PlayerDeathHead deathHead = ResolveDeathHead(targetAvatar);
                int headViewId = -1;
                if (deathHead != null)
                {
                    PhotonView headPhotonView = deathHead.GetComponent<PhotonView>();
                    if (headPhotonView != null)
                    {
                        headViewId = headPhotonView.ViewID;
                    }
                }

                photonView.RPC(nameof(RequestReviveRPC), RpcTarget.MasterClient, targetAvatar.photonView.ViewID, headViewId);
                return true;
            }

            [PunRPC]
            private void RequestReviveRPC(int targetAvatarViewId, int headViewId, PhotonMessageInfo _info = default)
            {
                if (!PhotonNetwork.IsMasterClient || targetAvatarViewId <= 0)
                {
                    return;
                }

                PhotonView targetView = PhotonView.Find(targetAvatarViewId);
                if (targetView == null)
                {
                    return;
                }

                PlayerAvatar targetAvatar = targetView.GetComponent<PlayerAvatar>();
                if (targetAvatar == null)
                {
                    return;
                }

                PlayerDeathHead deathHead = null;
                if (headViewId > 0)
                {
                    PhotonView headView = PhotonView.Find(headViewId);
                    if (headView != null)
                    {
                        deathHead = headView.GetComponent<PlayerDeathHead>();
                    }
                }

                if (deathHead == null || deathHead.playerAvatar != targetAvatar)
                {
                    deathHead = ResolveDeathHead(targetAvatar);
                }

                TryDirectRevive(targetAvatar, deathHead);
            }
        }

        private static readonly FieldInfo InExtractionPointField = typeof(PlayerDeathHead).GetField("inExtractionPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static string LastStatus { get; set; } = "可查看当前房间在线玩家，并对指定玩家加满血或复活。";

        public static void Update()
        {
            EnsureNetHandlers();
        }

        public static void Cleanup()
        {
            LastStatus = string.Empty;
        }

        public static List<PlayerRecord> GetOnlinePlayers()
        {
            List<PlayerRecord> records = new List<PlayerRecord>();
            HashSet<int> seenIds = new HashSet<int>();

            if (GameDirector.instance != null && GameDirector.instance.PlayerList != null)
            {
                for (int i = 0; i < GameDirector.instance.PlayerList.Count; i++)
                {
                    AddPlayerRecord(records, seenIds, GameDirector.instance.PlayerList[i]);
                }
            }

            PlayerAvatar[] avatars = UnityEngine.Object.FindObjectsByType<PlayerAvatar>((FindObjectsSortMode)0);
            for (int i = 0; i < avatars.Length; i++)
            {
                AddPlayerRecord(records, seenIds, avatars[i]);
            }

            records.Sort((left, right) =>
            {
                int result = CompareDescending(left.IsLocal, right.IsLocal);
                if (result != 0)
                {
                    return result;
                }

                result = CompareDescending(left.IsAlive, right.IsAlive);
                return result != 0
                    ? result
                    : string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            return records;
        }

        public static bool HealPlayer(PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                LastStatus = "目标玩家不存在，无法加血。";
                return false;
            }

            if (IsPlayerDead(avatar))
            {
                LastStatus = $"`{ResolvePlayerName(avatar)}` 当前已死亡，请先复活。";
                return false;
            }

            PlayerHealth playerHealth = avatar.playerHealth;
            if (playerHealth == null)
            {
                LastStatus = $"未找到 `{ResolvePlayerName(avatar)}` 的血量组件。";
                return false;
            }

            int currentHealth = ResolveCurrentHealth(playerHealth);
            int maxHealth = ResolveMaxHealth(playerHealth);
            int missingHealth = Mathf.Max(0, maxHealth - currentHealth);
            if (missingHealth <= 0)
            {
                LastStatus = $"`{ResolvePlayerName(avatar)}` 当前已经满血。";
                return false;
            }

            playerHealth.HealOther(missingHealth, true);
            LastStatus = $"已将 `{ResolvePlayerName(avatar)}` 回满到 {maxHealth}/{maxHealth} HP。";
            return true;
        }

        public static bool KillPlayer(PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                LastStatus = "目标玩家不存在，无法击杀。";
                return false;
            }

            if (IsPlayerDead(avatar))
            {
                LastStatus = $"`{ResolvePlayerName(avatar)}` 已经死亡。";
                return false;
            }

            PlayerHealth playerHealth = avatar.playerHealth;
            if (playerHealth == null)
            {
                LastStatus = $"未找到 `{ResolvePlayerName(avatar)}` 的血量组件。";
                return false;
            }

            playerHealth.HurtOther(9999, Vector3.zero, false, -1, false);
            LastStatus = $"已击杀 `{ResolvePlayerName(avatar)}`。";
            return true;
        }

        public static bool AddCosmeticTokenForPlayer(SemiFunc.Rarity rarity, PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                LastStatus = "目标玩家不存在，无法添加代币。";
                return false;
            }

            // In REPO, MetaManager controls cosmetics. Usually this affects the local player's save data.
            // If the user wants to add tokens "to a player", we can only add it to the local save.
            // There's no known RPC to add tokens to remote players in MetaManager, 
            // but we can try to find an RPC or just add locally if they are the local player.
            bool isLocal = ResolveIsLocal(avatar);
            if (!isLocal)
            {
                LastStatus = "无法给其他玩家添加代币，代币属于本地存档数据。";
                return false;
            }

            if (MetaManager.instance != null)
            {
                MetaManager.instance.CosmeticTokenAdd(rarity);
                LastStatus = $"已为本地玩家添加代币: {rarity}";
                return true;
            }

            LastStatus = "MetaManager 未就绪，无法添加代币。";
            return false;
        }

        public static int HealAllPlayers()
        {
            List<PlayerRecord> onlinePlayers = GetOnlinePlayers();
            int healedCount = 0;

            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                PlayerRecord record = onlinePlayers[i];
                if (record.Avatar == null || !record.IsAlive)
                {
                    continue;
                }

                PlayerHealth playerHealth = record.Avatar.playerHealth;
                if (playerHealth == null)
                {
                    continue;
                }

                int currentHealth = ResolveCurrentHealth(playerHealth);
                int maxHealth = ResolveMaxHealth(playerHealth);
                int missingHealth = Mathf.Max(0, maxHealth - currentHealth);
                if (missingHealth <= 0)
                {
                    continue;
                }

                playerHealth.HealOther(missingHealth, true);
                healedCount++;
            }

            LastStatus = healedCount > 0
                ? $"已为 {healedCount} 名玩家加满血。"
                : "当前没有需要加血的存活玩家。";
            return healedCount;
        }

        public static bool RevivePlayer(PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                LastStatus = "目标玩家不存在，无法复活。";
                return false;
            }

            if (!IsPlayerDead(avatar))
            {
                LastStatus = $"`{ResolvePlayerName(avatar)}` 当前处于存活状态。";
                return false;
            }

            PlayerDeathHead deathHead = ResolveDeathHead(avatar);
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                if (!TryDirectRevive(avatar, deathHead))
                {
                    LastStatus = $"`{ResolvePlayerName(avatar)}` 复活失败，未找到可用的死亡头或复活入口。";
                    return false;
                }

                LastStatus = $"已复活 `{ResolvePlayerName(avatar)}`。";
                return true;
            }

            PlayerAdminNetHandler localHandler = GetLocalHandler();
            if (localHandler == null)
            {
                LastStatus = "本地玩家网络处理器未就绪，无法发送复活请求。";
                return false;
            }

            if (!localHandler.RequestRevive(avatar))
            {
                LastStatus = $"无法向主机发送 `{ResolvePlayerName(avatar)}` 的复活请求。";
                return false;
            }

            LastStatus = $"已向主机发送 `{ResolvePlayerName(avatar)}` 的复活请求。";
            return true;
        }

        public static int ReviveAllDeadPlayers()
        {
            List<PlayerRecord> onlinePlayers = GetOnlinePlayers();
            int revivedCount = 0;

            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                PlayerRecord record = onlinePlayers[i];
                if (record.Avatar != null && !record.IsAlive && RevivePlayer(record.Avatar))
                {
                    revivedCount++;
                }
            }

            LastStatus = revivedCount > 0
                ? $"已处理 {revivedCount} 名死亡玩家的复活。"
                : "当前没有可复活的死亡玩家。";
            return revivedCount;
        }

        private static void EnsureNetHandlers()
        {
            PlayerAvatar[] avatars = UnityEngine.Object.FindObjectsByType<PlayerAvatar>((FindObjectsSortMode)0);
            for (int i = 0; i < avatars.Length; i++)
            {
                PlayerAvatar avatar = avatars[i];
                if (avatar == null)
                {
                    continue;
                }

                GameObject gameObject = avatar.gameObject;
                if (gameObject.GetComponent<PlayerAdminNetHandler>() == null)
                {
                    gameObject.AddComponent<PlayerAdminNetHandler>();
                }
            }
        }

        private static PlayerAdminNetHandler GetLocalHandler()
        {
            PlayerAvatar avatar = PlayerController.instance != null ? PlayerController.instance.playerAvatarScript : null;
            if (avatar == null)
            {
                return null;
            }

            PlayerAdminNetHandler handler = avatar.GetComponent<PlayerAdminNetHandler>();
            if (handler == null)
            {
                handler = avatar.gameObject.AddComponent<PlayerAdminNetHandler>();
            }

            return handler;
        }

        private static bool TryDirectRevive(PlayerAvatar avatar, PlayerDeathHead deathHead)
        {
            if (deathHead != null)
            {
                bool wasInExtractionPoint = InExtractionPointField != null && (bool)InExtractionPointField.GetValue(deathHead);
                try
                {
                    if (InExtractionPointField != null)
                    {
                        InExtractionPointField.SetValue(deathHead, true);
                    }

                    deathHead.Revive();
                }
                finally
                {
                    if (InExtractionPointField != null)
                    {
                        InExtractionPointField.SetValue(deathHead, wasInExtractionPoint);
                    }
                }

                return true;
            }

            if (avatar != null)
            {
                avatar.Revive(false);
                return true;
            }

            return false;
        }

        private static PlayerDeathHead ResolveDeathHead(PlayerAvatar avatar)
        {
            PlayerDeathHead deathHead = ReflectionUtils.GetFieldValue<PlayerDeathHead>(avatar, "playerDeathHead");
            if (deathHead != null)
            {
                return deathHead;
            }

            PlayerDeathHead[] deathHeads = UnityEngine.Object.FindObjectsByType<PlayerDeathHead>((FindObjectsSortMode)0);
            for (int i = 0; i < deathHeads.Length; i++)
            {
                PlayerDeathHead candidate = deathHeads[i];
                if (candidate != null && candidate.playerAvatar == avatar)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void AddPlayerRecord(List<PlayerRecord> records, HashSet<int> seenIds, PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                return;
            }

            Scene scene = avatar.gameObject.scene;
            if (!scene.IsValid())
            {
                return;
            }

            int instanceId = avatar.GetInstanceID();
            if (!seenIds.Add(instanceId))
            {
                return;
            }

            PlayerHealth playerHealth = avatar.playerHealth;
            int currentHealth = playerHealth != null ? ResolveCurrentHealth(playerHealth) : 0;
            int maxHealth = playerHealth != null ? ResolveMaxHealth(playerHealth) : 0;
            records.Add(new PlayerRecord
            {
                Avatar = avatar,
                DisplayName = ResolvePlayerName(avatar),
                IsLocal = ResolveIsLocal(avatar),
                IsAlive = !IsPlayerDead(avatar),
                CurrentHealth = currentHealth,
                MaxHealth = maxHealth
            });
        }

        private static string ResolvePlayerName(PlayerAvatar avatar)
        {
            string playerName = ReflectionUtils.GetFieldValue<string>(avatar, "playerName");
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                return playerName;
            }

            return avatar.name.Replace("(Clone)", string.Empty).Trim();
        }

        private static bool ResolveIsLocal(PlayerAvatar avatar)
        {
            return ReflectionUtils.GetFieldValue<bool>(avatar, "isLocal");
        }

        private static bool IsPlayerDead(PlayerAvatar avatar)
        {
            return ReflectionUtils.GetFieldValue<bool>(avatar, "deadSet");
        }

        private static int ResolveCurrentHealth(PlayerHealth playerHealth)
        {
            return Mathf.Max(0, ReflectionUtils.GetFieldValue<int>(playerHealth, "health"));
        }

        private static int ResolveMaxHealth(PlayerHealth playerHealth)
        {
            int maxHealth = ReflectionUtils.GetFieldValue<int>(playerHealth, "maxHealth");
            return Mathf.Max(maxHealth, 0);
        }

        private static int CompareDescending(bool left, bool right)
        {
            return right.CompareTo(left);
        }
    }
}
