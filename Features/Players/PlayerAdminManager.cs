using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Cheat.Config;
using Cheat.Features.LocalPlayer;
using Cheat.Utils;
using Photon.Pun;
using Photon.Realtime;
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
            private Coroutine _healthSharingRoutine;

            public bool BeginHealthSharing(List<PlayerAvatar> targets)
            {
                PlayerAvatar donor = GetComponent<PlayerAvatar>();
                if (donor == null || donor.physGrabber == null || !donor.physGrabber.isLocal ||
                    donor.playerHealth == null || targets == null || targets.Count == 0)
                {
                    return false;
                }

                if (_healthSharingRoutine != null)
                {
                    return false;
                }

                LocalPlayerManager.SetTemporaryHealthLock(true);
                if (!ReflectionUtils.GetFieldValue<bool>(donor.playerHealth, "godMode"))
                {
                    LocalPlayerManager.SetTemporaryHealthLock(false);
                    return false;
                }

                _healthSharingRoutine = StartCoroutine(ShareHealthRoutine(donor, targets));
                return true;
            }

            private IEnumerator ShareHealthRoutine(PlayerAvatar donor, List<PlayerAvatar> targets)
            {
                int patientsVisited = 0;
                int observedTransferred = 0;
                int skipped = 0;
                try
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        PlayerAvatar patient = targets[i];
                        if (donor == null || patient == null || patient == donor || IsPlayerDead(patient) ||
                            donor.playerHealth == null || patient.playerHealth == null || ResolveCurrentHealth(donor.playerHealth) <= 10)
                        {
                            skipped++;
                            continue;
                        }

                        PlayerHealthGrab healthGrab = patient.healthGrab;
                        if (healthGrab == null)
                        {
                            skipped++;
                            continue;
                        }

                        StaticGrabObject staticGrab = ReflectionUtils.GetFieldValue<StaticGrabObject>(healthGrab, "staticGrabObject");
                        if (healthGrab == null || staticGrab == null || !staticGrab.enabled || staticGrab.dead ||
                            staticGrab.grabbed || staticGrab.GetComponent<PhotonView>() == null)
                        {
                            skipped++;
                            continue;
                        }

                        int initialPatientHealth = ResolveCurrentHealth(patient.playerHealth);
                        int initialMaxHealth = ResolveMaxHealth(patient.playerHealth);
                        if (initialPatientHealth >= initialMaxHealth)
                        {
                            continue;
                        }

                        bool started = false;
                        try
                        {
                            // This is the game's normal health-bar grab path.
                            // GrabStarted forwards the locally-owned PhysGrabber
                            // to the master, where PlayerHealthGrab performs the
                            // vanilla donor/recipient health transfer.
                            staticGrab.GrabStarted(donor.physGrabber);
                            started = true;

                            // The native interaction transfers up to 10 HP per
                            // second, and refuses to drain a donor below 10 HP.
                            // Wait for synced health to reach the recipient or
                            // for the donor's available health to run out.
                            // The native health-grab interaction is capped at
                            // 10 HP/second by PlayerHealthGrab.Update. Keep one
                            // click active for the full missing amount rather
                            // than stopping after the old fixed 12-second cap.
                            float missingHealth = Mathf.Max(0, initialMaxHealth - initialPatientHealth);
                            float timeout = Mathf.Clamp(missingHealth / 8f + 4f, 12f, 300f);
                            while (timeout > 0f && patient != null && donor != null &&
                                   patient.playerHealth != null && donor.playerHealth != null &&
                                   ResolveCurrentHealth(patient.playerHealth) < ResolveMaxHealth(patient.playerHealth) &&
                                   ResolveCurrentHealth(donor.playerHealth) > 10)
                            {
                                yield return new WaitForSeconds(1.05f);
                                timeout -= 1.05f;
                            }
                        }
                        finally
                        {
                            if (started && staticGrab != null && donor != null && donor.physGrabber != null)
                            {
                                staticGrab.GrabEnded(donor.physGrabber);
                            }
                        }

                        patientsVisited++;
                        if (patient != null && patient.playerHealth != null)
                        {
                            observedTransferred += Mathf.Max(0, ResolveCurrentHealth(patient.playerHealth) - initialPatientHealth);
                        }
                    }
                }
                finally
                {
                    // ApplyGodMode combines this temporary lock with the user's
                    // configured setting, so the existing toggle is preserved.
                    LocalPlayerManager.SetTemporaryHealthLock(false);
                    _healthSharingRoutine = null;
                    LastStatus = observedTransferred > 0
                        ? $"已为 {patientsVisited} 名玩家恢复 {observedTransferred} 点生命值；你的生命值没有减少。"
                        : skipped > 0
                            ? $"未能为目标加血；{skipped} 名玩家当前无法接受治疗。"
                            : "没有检测到生命值变化，请稍后重试。";
                }
            }

            public bool RequestKill(PlayerAvatar targetAvatar)
            {
                if (targetAvatar == null || targetAvatar.photonView == null)
                {
                    return false;
                }

                if (photonView == null || !photonView.IsMine)
                {
                    return false;
                }

                photonView.RPC(nameof(RequestKillRPC), RpcTarget.MasterClient, targetAvatar.photonView.ViewID);
                return true;
            }

            public bool RequestTeleportPlayer(PlayerAvatar targetAvatar, Vector3 destination, Quaternion rotation)
            {
                if (targetAvatar == null || targetAvatar.photonView == null)
                {
                    return false;
                }

                if (photonView == null || !photonView.IsMine)
                {
                    return false;
                }

                photonView.RPC(nameof(RequestTeleportPlayerRPC), RpcTarget.MasterClient, targetAvatar.photonView.ViewID, destination, rotation);
                return true;
            }

            public void ClientGrenadeKill(PlayerAvatar targetAvatar)
            {
                StartCoroutine(ClientGrenadeKillRoutine(targetAvatar));
            }

            private System.Collections.IEnumerator ClientGrenadeKillRoutine(PlayerAvatar targetAvatar)
            {
                if (targetAvatar == null || ReflectionUtils.GetFieldValue<bool>(targetAvatar, "deadSet")) yield break;

                List<GameObject> grenades = new List<GameObject>();
                for (int i = 0; i < 3; i++)
                {
                    GameObject grenade = PhotonNetwork.Instantiate("Items/Item Grenade Human", targetAvatar.transform.position, Quaternion.identity, 0);
                    if (grenade != null)
                    {
                        grenades.Add(grenade);
                        ItemToggle toggle = grenade.GetComponent<ItemToggle>();
                        if (toggle != null)
                        {
                            toggle.ToggleItem(true, -1);
                        }
                    }
                }

                float timeout = 5f;
                while (timeout > 0)
                {
                    if (targetAvatar == null || ReflectionUtils.GetFieldValue<bool>(targetAvatar, "deadSet")) break;

                    bool anyAlive = false;
                    foreach (var grenade in grenades)
                    {
                        if (grenade != null)
                        {
                            anyAlive = true;
                            Rigidbody rb = grenade.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                rb.position = targetAvatar.transform.position;
                                rb.velocity = Vector3.zero;
                            }
                            else
                            {
                                grenade.transform.position = targetAvatar.transform.position;
                            }
                        }
                    }

                    if (!anyAlive) break;

                    timeout -= 0.05f;
                    yield return new WaitForSeconds(0.05f);
                }
            }

            public bool RequestRevive(PlayerAvatar targetAvatar, bool useDestination = false, Vector3 destination = default, Quaternion destinationRotation = default)
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

                photonView.RPC(nameof(RequestReviveRPC), RpcTarget.MasterClient, targetAvatar.photonView.ViewID, headViewId, useDestination, destination, destinationRotation);
                return true;
            }

            public bool RequestTeleportDeathHead(PlayerAvatar targetAvatar, Vector3 destination, Quaternion rotation)
            {
                if (targetAvatar == null || targetAvatar.photonView == null || photonView == null || !photonView.IsMine)
                    return false;

                photonView.RPC(nameof(RequestTeleportDeathHeadRPC), RpcTarget.MasterClient, targetAvatar.photonView.ViewID, destination, rotation);
                return true;
            }

            [PunRPC]
            private void RequestKillRPC(int targetAvatarViewId, PhotonMessageInfo _info = default)
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

                TryDirectKill(targetAvatar);
            }

            [PunRPC]
            private void RequestTeleportPlayerRPC(int targetAvatarViewId, Vector3 destination, Quaternion rotation, PhotonMessageInfo _info = default)
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

                LocalPlayerManager.TeleportPlayerByMaster(targetAvatar, destination, rotation, "RequestTeleportPlayerRPC");
            }

            [PunRPC]
            private void RequestReviveRPC(int targetAvatarViewId, int headViewId, bool useDestination, Vector3 destination, Quaternion destinationRotation, PhotonMessageInfo _info = default)
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

                TryDirectRevive(targetAvatar, deathHead, useDestination, destination, destinationRotation);
            }

            [PunRPC]
            private void RequestTeleportDeathHeadRPC(int targetAvatarViewId, Vector3 destination, Quaternion rotation, PhotonMessageInfo _info = default)
            {
                if (!PhotonNetwork.IsMasterClient || targetAvatarViewId <= 0 || !IsFinitePosition(destination) || !IsFiniteRotation(rotation))
                    return;

                PhotonView targetView = PhotonView.Find(targetAvatarViewId);
                PlayerAvatar targetAvatar = targetView != null ? targetView.GetComponent<PlayerAvatar>() : null;
                PlayerDeathHead head = targetAvatar != null ? ResolveDeathHead(targetAvatar) : null;
                if (head != null)
                    TeleportDeathHead(head, destination, rotation);
            }
        }

        private static readonly FieldInfo InExtractionPointField = typeof(PlayerDeathHead).GetField("inExtractionPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static string LastStatus { get; set; } = "可查看当前房间在线玩家，并对指定玩家加满血或复活。";

        private const float NetHandlerRefreshIntervalSeconds = 3f;
        private const float PendingTokenRetryIntervalSeconds = 1f;

        private static float _nextNetHandlerRefreshTime;
        private static float _nextPendingTokenProcessTime;
        private static bool _eventRegistered = false;
        private const byte CosmeticTokenRequestEventCode = 198;
        private const byte CosmeticTokenGrantEventCode = 199;
        private static readonly List<SemiFunc.Rarity> PendingLocalTokenGrants = new List<SemiFunc.Rarity>();
        private static readonly Dictionary<int, bool> PreviousDeadStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, float> NextReviveRequestTimes = new Dictionary<int, float>();
        private static float _nextGameModifierPlayerCheck;

        public static void Update()
        {
            if (!_eventRegistered)
            {
                PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
                _eventRegistered = true;
            }

            float now = Time.unscaledTime;
            if (Time.unscaledTime > _nextNetHandlerRefreshTime)
            {
                bool runtimeDebugEnabled = LocalPlayerManager.IsRuntimeDebugEnabled();
                long startTimestamp = runtimeDebugEnabled ? Stopwatch.GetTimestamp() : 0L;
                _nextNetHandlerRefreshTime = now + NetHandlerRefreshIntervalSeconds;
                EnsureNetHandlers();
                if (runtimeDebugEnabled)
                {
                    double elapsedMilliseconds = (double)(Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / (double)Stopwatch.Frequency;
                    if (elapsedMilliseconds >= 1.5)
                    {
                        #region debug-point B:player-admin-update-spike
                        LocalPlayerManager.DebugReport("B", "PlayerAdminManager.Update", "玩家管理刷新耗时偏高", "elapsedMs=" + elapsedMilliseconds.ToString("F2") + ",nextRefresh=" + _nextNetHandlerRefreshTime.ToString("F2"));
                        #endregion
                    }
                }
            }

            if (PendingLocalTokenGrants.Count > 0 && now >= _nextPendingTokenProcessTime)
            {
                ProcessPendingLocalTokenGrants();
            }

            UpdateGlobalPlayerModifiers(now);
        }

        private static void UpdateGlobalPlayerModifiers(float now)
        {
            ConfigData.GameModifiersSettings settings = ConfigManager.Config != null ? ConfigManager.Config.GameModifiers : null;
            if (settings == null || now < _nextGameModifierPlayerCheck)
                return;

            _nextGameModifierPlayerCheck = now + 0.25f;
            List<PlayerRecord> players = GetOnlinePlayers();
            HashSet<int> activeIds = new HashSet<int>();
            for (int i = 0; i < players.Count; i++)
            {
                PlayerRecord record = players[i];
                PlayerAvatar avatar = record.Avatar;
                if (avatar == null)
                    continue;

                int id = avatar.GetInstanceID();
                activeIds.Add(id);
                bool isDead = !record.IsAlive;
                PreviousDeadStates.TryGetValue(id, out bool wasDead);

                if (isDead && !wasDead)
                {
                    if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                        TryMoveDeathHeadForModifier(avatar, settings);
                    else
                        RequestDeathHeadMove(avatar, settings);
                }

                if (isDead && settings.AutoReviveTeammates)
                {
                    PlayerDeathHead head = ResolveDeathHead(avatar);
                    bool requireSeen = settings.AutoReviveWhenSeen;
                    bool seen = head != null && ReflectionUtils.GetFieldValue<bool>(head, "serverSeen");
                    if (!requireSeen || seen)
                    {
                        if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                            RevivePlayer(avatar);
                        else if (!NextReviveRequestTimes.TryGetValue(id, out float nextRequest) || now >= nextRequest)
                        {
                            Vector3 destination = Vector3.zero;
                            Quaternion rotation = Quaternion.identity;
                            bool useDestination = TryGetConfiguredReviveDestination(settings, out destination, out rotation);
                            if (GetLocalHandler()?.RequestRevive(avatar, useDestination, destination, rotation) == true)
                                NextReviveRequestTimes[id] = now + 1f;
                        }
                    }
                }

                if (!isDead)
                    NextReviveRequestTimes.Remove(id);

                PreviousDeadStates[id] = isDead;
            }

            List<int> staleIds = null;
            foreach (KeyValuePair<int, bool> state in PreviousDeadStates)
            {
                if (!activeIds.Contains(state.Key))
                {
                    if (staleIds == null) staleIds = new List<int>();
                    staleIds.Add(state.Key);
                }
            }
            if (staleIds != null)
                for (int i = 0; i < staleIds.Count; i++) PreviousDeadStates.Remove(staleIds[i]);
        }

        private static void TryMoveDeathHeadForModifier(PlayerAvatar avatar, ConfigData.GameModifiersSettings settings)
        {
            bool toTruck = settings.DeathAutoToTruck;
            bool toExtraction = !toTruck && settings.DeathAutoToExtraction;
            if (!toTruck && !toExtraction)
                return;

            PlayerDeathHead head = ResolveDeathHead(avatar);
            if (head == null)
                return;

            Vector3 destination;
            Quaternion rotation;
            bool found = toTruck
                ? TryGetTruckSpawn(out destination, out rotation)
                : TryGetExtractionSpawn(out destination, out rotation);
            if (found)
                TeleportDeathHead(head, destination, rotation);
        }

        private static void RequestDeathHeadMove(PlayerAvatar avatar, ConfigData.GameModifiersSettings settings)
        {
            bool toTruck = settings.DeathAutoToTruck;
            bool toExtraction = !toTruck && settings.DeathAutoToExtraction;
            if (!toTruck && !toExtraction)
                return;

            Vector3 destination = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            bool found = toTruck
                ? TryGetTruckSpawn(out destination, out rotation)
                : TryGetExtractionSpawn(out destination, out rotation);
            if (found)
                GetLocalHandler()?.RequestTeleportDeathHead(avatar, destination, rotation);
        }

        private static bool TryGetConfiguredReviveDestination(ConfigData.GameModifiersSettings settings, out Vector3 position, out Quaternion rotation)
        {
            if ((settings.ReviveAutoToTruck || settings.ReviveHeadOnTruck) && TryGetTruckSpawn(out position, out rotation))
                return true;
            if (settings.ReviveHeadOnExtraction && TryGetExtractionSpawn(out position, out rotation))
                return true;
            if (settings.ReviveHeadOnCart && TryGetCartSpawn(out position, out rotation))
                return true;

            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private static bool TryGetTruckSpawn(out Vector3 position, out Quaternion rotation)
        {
            TruckSafetySpawnPoint spawn = TruckSafetySpawnPoint.instance;
            if (spawn != null)
            {
                position = spawn.transform.position;
                rotation = spawn.transform.rotation;
                return true;
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private static bool TryGetExtractionSpawn(out Vector3 position, out Quaternion rotation)
        {
            object roundDirector = RoundDirector.instance;
            ExtractionPoint extraction = ReflectionUtils.GetFieldValue<ExtractionPoint>(roundDirector, "extractionPointCurrent");
            if (extraction != null && extraction.safetySpawn != null)
            {
                position = extraction.safetySpawn.position;
                rotation = extraction.safetySpawn.rotation;
                return true;
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private static bool TryGetCartSpawn(out Vector3 position, out Quaternion rotation)
        {
            PhysGrabCart[] carts = UnityEngine.Object.FindObjectsByType<PhysGrabCart>(FindObjectsSortMode.None);
            if (carts.Length > 0)
            {
                PhysGrabCart cart = carts[0];
                position = cart.transform.position + cart.transform.forward * 1.5f + Vector3.up * 0.5f;
                rotation = cart.transform.rotation;
                return true;
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private static void TeleportDeathHead(PlayerDeathHead head, Vector3 position, Quaternion rotation)
        {
            if (head == null)
                return;

            PhysGrabObject phys = ReflectionUtils.GetFieldValue<PhysGrabObject>(head, "physGrabObject");
            if (phys != null)
            {
                phys.Teleport(position, rotation);
                phys.transform.SetPositionAndRotation(position, rotation);
                Rigidbody body = ReflectionUtils.GetFieldValue<Rigidbody>(phys, "rb");
                if (body != null)
                {
                    body.position = position;
                    body.rotation = rotation;
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                Physics.SyncTransforms();
            }
            else
                head.transform.SetPositionAndRotation(position, rotation);
        }

        private static bool IsFinitePosition(Vector3 position)
        {
            return IsFinite(position.x) && IsFinite(position.y) && IsFinite(position.z);
        }

        private static bool IsFiniteRotation(Quaternion rotation)
        {
            return IsFinite(rotation.x) && IsFinite(rotation.y) && IsFinite(rotation.z) && IsFinite(rotation.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static void Cleanup()
        {
            LastStatus = string.Empty;
            PendingLocalTokenGrants.Clear();
            _nextPendingTokenProcessTime = 0f;
            if (_eventRegistered)
            {
                PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
                _eventRegistered = false;
            }
        }

        private static void OnEventReceived(ExitGames.Client.Photon.EventData photonEvent)
        {
            if (photonEvent.Code == CosmeticTokenRequestEventCode)
            {
                HandleCosmeticTokenRequestEvent(photonEvent);
            }
            else if (photonEvent.Code == CosmeticTokenGrantEventCode)
            {
                HandleCosmeticTokenGrantEvent(photonEvent);
            }
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
                LastStatus = $"无法读取 `{ResolvePlayerName(avatar)}` 的生命值。";
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

            bool isLocalOwner = IsOwnedLocalAvatar(avatar);
            bool isAuthority = SemiFunc.IsMasterClientOrSingleplayer();
            if (GameManager.Multiplayer() && !isLocalOwner && !isAuthority)
            {
                PlayerAdminNetHandler localHandler = GetLocalHandler();
                if (localHandler == null || !localHandler.BeginHealthSharing(new List<PlayerAvatar> { avatar }))
                {
                    LastStatus = "无法开始加血，请松开手中物品后重试。";
                    return false;
                }

                LastStatus = $"正在为 `{ResolvePlayerName(avatar)}` 加血；你的生命值不会减少。";
                return true;
            }

            try
            {
                playerHealth.HealOther(missingHealth, effect: true);
                LastStatus = isAuthority && !isLocalOwner
                    ? $"已为 `{ResolvePlayerName(avatar)}` 补满生命值。"
                    : $"已为 `{ResolvePlayerName(avatar)}` 补充生命值。";
                return true;
            }
            catch (Exception)
            {
                LastStatus = $"未能为 `{ResolvePlayerName(avatar)}` 恢复生命值，请稍后重试。";
                return false;
            }
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
                LastStatus = $"无法读取 `{ResolvePlayerName(avatar)}` 的生命值。";
                return false;
            }

            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                if (!TryDirectKill(avatar))
                {
                    LastStatus = $"击杀 `{ResolvePlayerName(avatar)}` 失败。";
                    return false;
                }

                LastStatus = $"已击杀 `{ResolvePlayerName(avatar)}`。";      
                return true;
            }

            PlayerAdminNetHandler localHandler = GetLocalHandler();
            if (localHandler == null)
            {
                LastStatus = "当前无法处理击杀，请稍后重试。";
                return false;
            }

            localHandler.RequestKill(avatar);
            localHandler.ClientGrenadeKill(avatar);

            LastStatus = $"已对 `{ResolvePlayerName(avatar)}` 发起击杀操作。";
            return true;
        }

        public static bool AddCosmeticTokenForPlayer(SemiFunc.Rarity rarity, PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                LastStatus = "目标玩家不存在，无法添加代币。";
                return false;
            }

            if (!GameManager.Multiplayer() || IsOwnedLocalAvatar(avatar))
            {
                if (TryGrantTokenLocally(rarity))
                {
                    LastStatus = $"已为 `{ResolvePlayerName(avatar)}` 添加代币: {rarity}";
                    return true;
                }

                LastStatus = "游戏尚未准备好，代币会在可用后添加。";
                return true;
            }

            if (avatar.photonView == null || avatar.photonView.Owner == null)
            {
                LastStatus = "目标玩家暂不可用，无法添加代币。";
                return false;
            }

            int targetActorNumber = avatar.photonView.Owner.ActorNumber;
            bool sent = PhotonNetwork.IsMasterClient
                ? SendCosmeticTokenGrantEvent(targetActorNumber, rarity)
                : SendCosmeticTokenRequestEvent(targetActorNumber, rarity);

            if (sent)
            {
                LastStatus = PhotonNetwork.IsMasterClient
                    ? $"已为 `{ResolvePlayerName(avatar)}` 添加代币：{rarity}"
                    : $"已请求房主为 `{ResolvePlayerName(avatar)}` 添加代币：{rarity}";
                return true;
            }

            LastStatus = PhotonNetwork.IsMasterClient
                ? $"未能为 `{ResolvePlayerName(avatar)}` 添加代币。"
                : $"房主暂时无法为 `{ResolvePlayerName(avatar)}` 添加代币。";
            return false;
        }

        public static int HealAllPlayers()
        {
            List<PlayerRecord> onlinePlayers = GetOnlinePlayers();
            bool isAuthority = SemiFunc.IsMasterClientOrSingleplayer();
            if (GameManager.Multiplayer() && !isAuthority)
            {
                List<PlayerAvatar> patients = new List<PlayerAvatar>();
                for (int i = 0; i < onlinePlayers.Count; i++)
                {
                    PlayerAvatar avatar = onlinePlayers[i].Avatar;
                    if (avatar != null && avatar != ResolveOwnedLocalAvatar() && !IsPlayerDead(avatar) &&
                        avatar.playerHealth != null && ResolveCurrentHealth(avatar.playerHealth) < ResolveMaxHealth(avatar.playerHealth))
                    {
                        patients.Add(avatar);
                    }
                }

                PlayerAdminNetHandler localHandler = GetLocalHandler();
                if (patients.Count == 0)
                {
                    LastStatus = "没有需要接收生命值的远端存活玩家。";
                    return 0;
                }

                if (localHandler == null || !localHandler.BeginHealthSharing(patients))
                {
                    LastStatus = "无法开始加血，请稍后重试。";
                    return 0;
                }

                LastStatus = $"正在依次为 {patients.Count} 名受伤队友加血；每秒最多增加 10 点，你的生命值不会减少。";
                return patients.Count;
            }

            int requestCount = 0;
            int skippedCount = 0;

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
                    skippedCount++;
                    continue;
                }

                int currentHealth = ResolveCurrentHealth(playerHealth);
                int maxHealth = ResolveMaxHealth(playerHealth);
                int missingHealth = Mathf.Max(0, maxHealth - currentHealth);
                if (missingHealth <= 0)
                {
                    continue;
                }

                try
                {
                    playerHealth.HealOther(missingHealth, effect: true);
                    requestCount++;
                }
                catch (Exception)
                {
                    skippedCount++;
                }
            }

            LastStatus = requestCount > 0
                ? skippedCount > 0
                    ? $"已为 {requestCount} 名玩家补充生命值；另有 {skippedCount} 名玩家暂时无法处理。"
                    : $"已为 {requestCount} 名玩家补充生命值。"
                : skippedCount > 0
                    ? $"有 {skippedCount} 名玩家暂时无法加血，请稍后重试。"
                    : "当前没有需要加血的存活玩家。";
            return requestCount;
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
                    LastStatus = $"未能复活 `{ResolvePlayerName(avatar)}`。";
                    return false;
                }

                LastStatus = $"已复活 `{ResolvePlayerName(avatar)}`。";
                return true;
            }

            PlayerAdminNetHandler localHandler = GetLocalHandler();
            if (localHandler == null)
            {
                LastStatus = "当前无法复活玩家，请稍后重试。";
                return false;
            }

            Vector3 reviveDestination = Vector3.zero;
            Quaternion reviveRotation = Quaternion.identity;
            bool useDestination = ConfigManager.Config != null && TryGetConfiguredReviveDestination(ConfigManager.Config.GameModifiers, out reviveDestination, out reviveRotation);
            if (!localHandler.RequestRevive(avatar, useDestination, reviveDestination, reviveRotation))
            {
                LastStatus = $"房主暂时无法处理 `{ResolvePlayerName(avatar)}` 的复活。";
                return false;
            }

            LastStatus = $"已请求房主复活 `{ResolvePlayerName(avatar)}`。";
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

        public static bool TeleportPlayerToMe(PlayerAvatar targetAvatar)
        {
            if (targetAvatar == null)
            {
                LastStatus = "目标玩家不存在，无法传送。";
                return false;
            }

            if (IsPlayerDead(targetAvatar))
            {
                LastStatus = $"`{ResolvePlayerName(targetAvatar)}` 已经死亡，无法传送。";
                return false;
            }

            PlayerAvatar localAvatar = ResolveOwnedLocalAvatar();
            if (localAvatar == null)
            {
                LastStatus = "本地玩家不存在，无法传送。";
                return false;
            }

            Vector3 destination = localAvatar.transform.position;

            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                LocalPlayerManager.TeleportPlayerByMaster(targetAvatar, destination, localAvatar.transform.rotation, "TeleportPlayerToMe");
                LastStatus = $"已将 `{ResolvePlayerName(targetAvatar)}` 传送到身边。";
                return true;
            }

            PlayerAdminNetHandler localHandler = GetLocalHandler();
            if (localHandler == null)
            {
                LastStatus = "当前无法传送玩家，请稍后重试。";
                return false;
            }

            localHandler.RequestTeleportPlayer(targetAvatar, destination, localAvatar.transform.rotation);
            LastStatus = $"已请求房主将 `{ResolvePlayerName(targetAvatar)}` 传送到你身边。";
            return true;
        }

        public static int TeleportAllPlayersToMe()
        {
            List<PlayerRecord> onlinePlayers = GetOnlinePlayers();
            int teleportedCount = 0;

            PlayerAvatar localAvatar = ResolveOwnedLocalAvatar();
            if (localAvatar == null)
            {
                LastStatus = "本地玩家不存在，无法传送。";
                return 0;
            }

            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                PlayerRecord record = onlinePlayers[i];
                if (record.Avatar != null && !record.IsLocal && record.IsAlive)
                {
                    if (TeleportPlayerToMe(record.Avatar))
                    {
                        teleportedCount++;
                    }
                }
            }

            LastStatus = teleportedCount > 0
                ? $"已向主机发送将 {teleportedCount} 名玩家传送到身边的请求。"
                : "当前没有可传送的其他玩家。";
            return teleportedCount;
        }

        public static bool TeleportMeToPlayer(PlayerAvatar targetAvatar)
        {
            if (targetAvatar == null)
            {
                LastStatus = "目标玩家不存在，无法传送。";
                return false;
            }

            if (IsPlayerDead(targetAvatar))
            {
                LastStatus = $"`{ResolvePlayerName(targetAvatar)}` 已经死亡，无法传送。";
                return false;
            }

            Vector3 destination = targetAvatar.transform.position;
            Quaternion rotation = targetAvatar.transform.rotation;

            LocalPlayerManager.TeleportLocalPlayer(destination, rotation, $"已传送到 `{ResolvePlayerName(targetAvatar)}` 身边。");
            LastStatus = $"已传送到 `{ResolvePlayerName(targetAvatar)}` 身边。";
            return true;
        }

        private static void EnsureNetHandlers()
        {
            long startTimestamp = LocalPlayerManager.IsRuntimeDebugEnabled() ? Stopwatch.GetTimestamp() : 0L;
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
            if (LocalPlayerManager.IsRuntimeDebugEnabled())
            {
                double elapsedMilliseconds = (double)(Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / (double)Stopwatch.Frequency;
                if (elapsedMilliseconds >= 1.5)
                {
                    #region debug-point B:player-admin-ensure-net-handlers-spike
                    LocalPlayerManager.DebugReport("B", "PlayerAdminManager.EnsureNetHandlers", "玩家管理网络处理器刷新耗时偏高", "elapsedMs=" + elapsedMilliseconds.ToString("F2") + ",avatarCount=" + avatars.Length);
                    #endregion
                }
            }
        }

        private static PlayerAdminNetHandler GetLocalHandler()
        {
            PlayerAvatar avatar = ResolveOwnedLocalAvatar();
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

        private static bool TryDirectKill(PlayerAvatar avatar)
        {
            if (avatar == null || IsPlayerDead(avatar))
            {
                return false;
            }

            PlayerHealth playerHealth = avatar.playerHealth;
            if (playerHealth == null)
            {
                return false;
            }

            // 获取玩家最大血量并给予足够的致命伤害，避免数值过大导致游戏内部计算溢出或异常
            int maxHealth = ResolveMaxHealth(playerHealth);
            int damage = maxHealth > 0 ? maxHealth * 10 : 1000;
            playerHealth.HurtOther(damage, Vector3.zero, false, -1, false);

            return true;
        }

        private static bool TryDirectRevive(PlayerAvatar avatar, PlayerDeathHead deathHead, bool useRequestedDestination = false, Vector3 requestedDestination = default, Quaternion requestedRotation = default)
        {
            if (deathHead != null)
            {
                bool useDestination = useRequestedDestination;
                Vector3 revivePosition = requestedDestination;
                Quaternion reviveRotation = requestedRotation;
                if (!useDestination && ConfigManager.Config != null)
                    useDestination = TryGetConfiguredReviveDestination(ConfigManager.Config.GameModifiers, out revivePosition, out reviveRotation);
                if (useDestination)
                    TeleportDeathHead(deathHead, revivePosition, reviveRotation);

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

        private static void HandleCosmeticTokenRequestEvent(ExitGames.Client.Photon.EventData photonEvent)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            object[] data = photonEvent.CustomData as object[];
            if (data == null || data.Length < 2)
            {
                return;
            }

            int targetActorNumber = SafeToInt(data[0]);
            int rarityValue = SafeToInt(data[1]);
            if (targetActorNumber <= 0 || !Enum.IsDefined(typeof(SemiFunc.Rarity), rarityValue))
            {
                return;
            }

            SendCosmeticTokenGrantEvent(targetActorNumber, (SemiFunc.Rarity)rarityValue);
        }

        private static void HandleCosmeticTokenGrantEvent(ExitGames.Client.Photon.EventData photonEvent)
        {
            object[] data = photonEvent.CustomData as object[];
            if (data == null || data.Length < 2)
            {
                return;
            }

            int targetActorNumber = SafeToInt(data[0]);
            int rarityValue = SafeToInt(data[1]);
            if (!IsLocalActorNumber(targetActorNumber) || !Enum.IsDefined(typeof(SemiFunc.Rarity), rarityValue))
            {
                return;
            }

            SemiFunc.Rarity rarity = (SemiFunc.Rarity)rarityValue;
            TryGrantTokenLocally(rarity);
        }

        private static bool SendCosmeticTokenRequestEvent(int targetActorNumber, SemiFunc.Rarity rarity)
        {
            if (!GameManager.Multiplayer() || PhotonNetwork.MasterClient == null)
            {
                return false;
            }

            return SendTargetedTokenEvent(CosmeticTokenRequestEventCode, PhotonNetwork.MasterClient.ActorNumber, targetActorNumber, rarity);
        }

        private static bool SendCosmeticTokenGrantEvent(int targetActorNumber, SemiFunc.Rarity rarity)
        {
            if (!GameManager.Multiplayer())
            {
                return TryGrantTokenLocally(rarity);
            }

            return SendTargetedTokenEvent(CosmeticTokenGrantEventCode, targetActorNumber, targetActorNumber, rarity);
        }

        private static bool SendTargetedTokenEvent(byte eventCode, int receiverActorNumber, int targetActorNumber, SemiFunc.Rarity rarity)
        {
            if (receiverActorNumber <= 0 || targetActorNumber <= 0)
            {
                return false;
            }

            object[] content = new object[] { targetActorNumber, (int)rarity };
            Photon.Realtime.RaiseEventOptions raiseEventOptions = new Photon.Realtime.RaiseEventOptions
            {
                TargetActors = new int[] { receiverActorNumber }
            };
            ExitGames.Client.Photon.SendOptions sendOptions = new ExitGames.Client.Photon.SendOptions { Reliability = true };
            return PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, sendOptions);
        }

        private static bool TryGrantTokenLocally(SemiFunc.Rarity rarity)
        {
            if (MetaManager.instance == null)
            {
                QueuePendingLocalTokenGrant(rarity);
                return false;
            }

            MetaManager.instance.CosmeticTokenAdd(rarity);
            Cheat.Utils.Logger.Info($"已在本地添加代币: {rarity}");
            return true;
        }

        private static void QueuePendingLocalTokenGrant(SemiFunc.Rarity rarity)
        {
            PendingLocalTokenGrants.Add(rarity);
            _nextPendingTokenProcessTime = Time.unscaledTime + PendingTokenRetryIntervalSeconds;
        }

        private static void ProcessPendingLocalTokenGrants()
        {
            if (PendingLocalTokenGrants.Count == 0)
            {
                return;
            }

            if (MetaManager.instance == null)
            {
                _nextPendingTokenProcessTime = Time.unscaledTime + PendingTokenRetryIntervalSeconds;
                return;
            }

            for (int i = 0; i < PendingLocalTokenGrants.Count; i++)
            {
                SemiFunc.Rarity rarity = PendingLocalTokenGrants[i];
                MetaManager.instance.CosmeticTokenAdd(rarity);
                Cheat.Utils.Logger.Info($"延迟补发本地代币成功: {rarity}");
            }

            PendingLocalTokenGrants.Clear();
            _nextPendingTokenProcessTime = 0f;
        }

        private static bool IsLocalActorNumber(int actorNumber)
        {
            if (actorNumber <= 0)
            {
                return false;
            }

            if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
            {
                return true;
            }

            PlayerAvatar localAvatar = ResolveOwnedLocalAvatar();
            return localAvatar != null
                && localAvatar.photonView != null
                && localAvatar.photonView.Owner != null
                && localAvatar.photonView.Owner.ActorNumber == actorNumber;
        }

        private static int SafeToInt(object value)
        {
            return value is int intValue ? intValue : 0;
        }

        private static PlayerAvatar ResolveOwnedLocalAvatar()
        {
            PlayerAvatar controllerAvatar = PlayerController.instance != null ? PlayerController.instance.playerAvatarScript : null;
            if (IsOwnedLocalAvatar(controllerAvatar))
            {
                return controllerAvatar;
            }

            if (IsOwnedLocalAvatar(PlayerAvatar.instance))
            {
                return PlayerAvatar.instance;
            }

            PlayerAvatar[] avatars = UnityEngine.Object.FindObjectsByType<PlayerAvatar>((FindObjectsSortMode)0);
            for (int i = 0; i < avatars.Length; i++)
            {
                if (IsOwnedLocalAvatar(avatars[i]))
                {
                    return avatars[i];
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

        private static bool IsOwnedLocalAvatar(PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                return false;
            }

            if (!GameManager.Multiplayer())
            {
                return true;
            }

            PhotonView photonView = avatar.photonView;
            if (photonView != null && photonView.IsMine)
            {
                return true;
            }

            return ResolveIsLocal(avatar);
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
