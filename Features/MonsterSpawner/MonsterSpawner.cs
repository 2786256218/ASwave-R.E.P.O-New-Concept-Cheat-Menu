using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cheat.Features.LocalPlayer;
using Cheat.Utils;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

namespace Cheat.Features.MonsterSpawner;

public class MonsterSpawner : MonoBehaviour
{
	public static MonsterSpawner Instance;

	private readonly List<EnemySetup> _spawnableEnemies = new List<EnemySetup>();

	private readonly List<Enemy> _activeEnemies = new List<Enemy>();

	private GameObject _previewInstance;

	private Camera _previewCamera;

	private RenderTexture _previewTexture;

	private EnemySetup _currentPreviewEnemy;

	private float _activeEnemyUpdateTimer = 0f;

	private float _libraryUpdateTimer = 0f;

	private const float ActiveEnemyUpdateInterval = 1f;

	private const float LibraryUpdateInterval = 5f;

	private Coroutine _networkKillRoutine;

	private string _lastActionStatus = "主机/单机下可直接击杀；非主机会自动尝试网络辅助击杀。";

	private bool _backgroundRefreshEnabled;

	public List<EnemySetup> SpawnableEnemies => _spawnableEnemies;

	public EnemySetup SelectedEnemy { get; private set; }

	public List<Enemy> ActiveEnemies => _activeEnemies;

	public RenderTexture PreviewTexture => _previewTexture;

	public string LastActionStatus => _lastActionStatus;

	private void Awake()
	{
		Instance = this;
		SetupPreviewRendering();
		UpdateActiveEnemies();
		RefreshLibrary();
	}

	private void Update()
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if (_backgroundRefreshEnabled)
		{
			_activeEnemyUpdateTimer += Time.deltaTime;
			if (_activeEnemyUpdateTimer >= ActiveEnemyUpdateInterval)
			{
				UpdateActiveEnemies();
				_activeEnemyUpdateTimer = 0f;
			}
			_libraryUpdateTimer += Time.deltaTime;
			if (_libraryUpdateTimer >= LibraryUpdateInterval)
			{
				RefreshLibrary();
				_libraryUpdateTimer = 0f;
			}
		}
		if ((UnityEngine.Object)(object)_previewInstance != (UnityEngine.Object)null)
		{
			_previewInstance.transform.Rotate(Vector3.up, 30f * Time.deltaTime);
		}
	}

	private void OnDestroy()
	{
		if (_networkKillRoutine != null)
		{
			StopCoroutine(_networkKillRoutine);
			_networkKillRoutine = null;
		}
		if ((UnityEngine.Object)(object)_previewInstance != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_previewInstance);
			_previewInstance = null;
		}
		if ((UnityEngine.Object)(object)_previewCamera != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)_previewCamera).gameObject);
			_previewCamera = null;
		}
		if ((UnityEngine.Object)(object)_previewTexture != (UnityEngine.Object)null)
		{
			if (_previewTexture.IsCreated())
			{
				_previewTexture.Release();
			}
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_previewTexture);
			_previewTexture = null;
		}
		if ((UnityEngine.Object)(object)Instance == (UnityEngine.Object)(object)this)
		{
			Instance = null;
		}
	}

	private void RefreshLibrary()
	{
		if ((UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		List<EnemySetup> list = new List<EnemySetup>();
		if (EnemyDirector.instance.enemiesDifficulty1 != null)
		{
			list.AddRange(EnemyDirector.instance.enemiesDifficulty1);
		}
		if (EnemyDirector.instance.enemiesDifficulty2 != null)
		{
			list.AddRange(EnemyDirector.instance.enemiesDifficulty2);
		}
		if (EnemyDirector.instance.enemiesDifficulty3 != null)
		{
			list.AddRange(EnemyDirector.instance.enemiesDifficulty3);
		}
		try
		{
			FieldInfo field = typeof(EnemyDirector).GetField("debugEnemy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.GetValue(EnemyDirector.instance) is EnemySetup[] collection)
			{
				list.AddRange(collection);
			}
		}
		catch
		{
		}
		_spawnableEnemies.Clear();
		_spawnableEnemies.AddRange((from e in list.Distinct()
			orderby ((UnityEngine.Object)e).name
			select e).ToList());
		if ((UnityEngine.Object)(object)SelectedEnemy == (UnityEngine.Object)null && _spawnableEnemies.Count > 0)
		{
			SelectEnemy(_spawnableEnemies[0]);
		}
	}

	private void UpdateActiveEnemies()
	{
		Dictionary<int, Enemy> dictionary = new Dictionary<int, Enemy>();
		if ((UnityEngine.Object)(object)EnemyDirector.instance != (UnityEngine.Object)null && EnemyDirector.instance.enemiesSpawned != null)
		{
			foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
			{
				TryAddEnemy(dictionary, ResolveEnemyFromParent(item));
			}
		}
		foreach (Enemy item2 in UnityEngine.Object.FindObjectsOfType<Enemy>())
		{
			TryAddEnemy(dictionary, item2);
		}
		_activeEnemies.Clear();
		foreach (Enemy value in dictionary.Values)
		{
			_activeEnemies.Add(value);
		}
		_activeEnemies.Sort(delegate(Enemy left, Enemy right)
		{
			return string.Compare(GetEnemyName(left), GetEnemyName(right), System.StringComparison.OrdinalIgnoreCase);
		});
	}

	public List<Enemy> GetActiveEnemiesSnapshot(bool forceRefresh = false)
	{
		if (forceRefresh)
		{
			UpdateActiveEnemies();
			_activeEnemyUpdateTimer = 0f;
		}
		return _activeEnemies;
	}

	public void SetBackgroundRefreshEnabled(bool enabled)
	{
		if (_backgroundRefreshEnabled == enabled)
		{
			return;
		}

		_backgroundRefreshEnabled = enabled;
		_activeEnemyUpdateTimer = 0f;
		_libraryUpdateTimer = 0f;
		if (enabled)
		{
			UpdateActiveEnemies();
			RefreshLibrary();
		}
	}

	public EnemyParent GetEnemyParent(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return null;
		}
		EnemyParent componentInParent = ((Component)enemy).GetComponentInParent<EnemyParent>();
		if ((UnityEngine.Object)(object)componentInParent != (UnityEngine.Object)null)
		{
			return componentInParent;
		}
		if ((UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null || EnemyDirector.instance.enemiesSpawned == null)
		{
			return null;
		}
		foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
		{
			if (!((UnityEngine.Object)(object)item == (UnityEngine.Object)null) && (UnityEngine.Object)(object)ResolveEnemyFromParent(item) == (UnityEngine.Object)(object)enemy)
			{
				return item;
			}
		}
		return null;
	}

	private static void TryAddEnemy(Dictionary<int, Enemy> enemies, Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return;
		}
		GameObject gameObject = ((Component)enemy).gameObject;
		if (!gameObject.scene.IsValid())
		{
			return;
		}
		if (!IsAliveEnemy(enemy))
		{
			return;
		}
		int instanceID = ((UnityEngine.Object)enemy).GetInstanceID();
		if (!enemies.ContainsKey(instanceID))
		{
			enemies.Add(instanceID, enemy);
		}
	}

	private static Enemy ResolveEnemyFromParent(EnemyParent parent)
	{
		if ((UnityEngine.Object)(object)parent == (UnityEngine.Object)null)
		{
			return null;
		}
		Enemy fieldValue = ReflectionUtils.GetFieldValue<Enemy>(parent, "Enemy");
		if ((UnityEngine.Object)(object)fieldValue != (UnityEngine.Object)null)
		{
			return fieldValue;
		}
		return ((Component)parent).GetComponentInChildren<Enemy>(true);
	}

	private static bool IsAliveEnemy(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || !((Component)enemy).gameObject.activeInHierarchy || !((Behaviour)enemy).isActiveAndEnabled)
		{
			return false;
		}

		EnemyHealth component = ((Component)enemy).GetComponent<EnemyHealth>();
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return true;
		}
		try
		{
			FieldInfo field = typeof(EnemyHealth).GetField("healthCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field2 = typeof(EnemyHealth).GetField("dead", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field3 = typeof(EnemyHealth).GetField("deadImpulse", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			int num = ((field != null) ? ((int)field.GetValue(component)) : component.health);
			bool flag = field2 != null && (bool)field2.GetValue(component);
			bool flag2 = field3 != null && (bool)field3.GetValue(component);
			return num > 0 && !flag && !flag2;
		}
		catch
		{
			return true;
		}
	}

	private static string GetEnemyName(Enemy enemy)
	{
		return EnemyNameResolver.GetEnemyDisplayName(enemy);
	}

	private void ForgetEnemy(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return;
		}

		_activeEnemies.RemoveAll((Enemy candidate) => (UnityEngine.Object)(object)candidate == (UnityEngine.Object)(object)enemy || (UnityEngine.Object)(object)candidate == (UnityEngine.Object)null);
	}

	public void SelectEnemy(EnemySetup enemy)
	{
		SelectedEnemy = enemy;
		UpdatePreviewModel(enemy);
	}

	public void SpawnSelectedEnemy()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		if (!((UnityEngine.Object)(object)SelectedEnemy == (UnityEngine.Object)null) && (PhotonNetwork.IsMasterClient || GameManager.instance.gameMode == 0) && !((UnityEngine.Object)(object)LevelGenerator.Instance == (UnityEngine.Object)null))
		{
			Vector3 val = Vector3.zero;
			if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
			{
				val = ((Component)PlayerController.instance).transform.position + ((Component)PlayerController.instance).transform.forward * 3f + Vector3.up * 0.5f;
			}
			LevelGenerator.Instance.EnemySpawn(SelectedEnemy, val);
		}
	}

	public void KillEnemy(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			_lastActionStatus = "目标怪物已失效，击杀请求已取消。";
			return;
		}
		if (CanKillDirectly())
		{
			DirectKillEnemy(enemy);
			ForgetEnemy(enemy);
			_lastActionStatus = "已通过主机权限执行原生击杀。";
			UpdateActiveEnemies();
			_activeEnemyUpdateTimer = 0f;
			return;
		}
		if (TryStartNetworkAssistKill(enemy, out var status))
		{
			_lastActionStatus = status;
			return;
		}
		_lastActionStatus = status;
	}

	public void KillAllEnemies()
	{
		Enemy[] array = GetActiveEnemiesSnapshot(forceRefresh: true).ToArray();
		if (array.Length == 0)
		{
			_lastActionStatus = "当前没有可击杀的活跃怪物。";
			return;
		}
		if (CanKillDirectly())
		{
			foreach (Enemy item in array)
			{
				DirectKillEnemy(item);
				ForgetEnemy(item);
			}
			_lastActionStatus = string.Format("已通过主机权限执行清怪，共处理 {0} 只怪物。", array.Length);
			UpdateActiveEnemies();
			_activeEnemyUpdateTimer = 0f;
			return;
		}
		if (_networkKillRoutine != null)
		{
			StopCoroutine(_networkKillRoutine);
		}
		_networkKillRoutine = StartCoroutine(NetworkAssistKillAllRoutine(array));
		_lastActionStatus = string.Format("非主机模式：已开始按顺序尝试网络辅助清怪，共 {0} 只。", array.Length);
	}

	private static bool CanKillDirectly()
	{
		return !GameManager.Multiplayer() || PhotonNetwork.IsMasterClient;
	}

	private void DirectKillEnemy(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return;
		}
		PrepareEnemyForKill(enemy);
		EnemyHealth component = ((Component)enemy).GetComponent<EnemyHealth>();
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return;
		}
		int damage = GetDirectKillDamage(component);
		if (damage <= 0)
		{
			damage = Mathf.Max(component.health, 1);
		}
		component.Hurt(damage, GetKillDirection(enemy));
	}

	private void PrepareEnemyForKill(Enemy enemy)
	{
		EnemyParent enemyParent = GetEnemyParent(enemy);
		if ((UnityEngine.Object)(object)enemyParent != (UnityEngine.Object)null)
		{
			ReflectionUtils.SetFieldValue(enemyParent, "valuableSpawnTimer", 10f);
			ReflectionUtils.SetFieldValue(enemyParent, "playerClose", true);
		}
	}

	private static Vector3 GetKillDirection(Enemy enemy)
	{
		Vector3 normalized = ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null) ? (((Component)enemy).transform.position - ((Component)PlayerController.instance).transform.position).normalized : Vector3.up;
		if (normalized.sqrMagnitude <= 0.001f)
		{
			normalized = Vector3.up;
		}
		return normalized;
	}

	private static int GetDirectKillDamage(EnemyHealth enemyHealth)
	{
		if ((UnityEngine.Object)(object)enemyHealth == (UnityEngine.Object)null)
		{
			return 0;
		}

		int currentHealth = ReflectionUtils.GetFieldValue<int>(enemyHealth, "healthCurrent");
		if (currentHealth <= 0)
		{
			currentHealth = enemyHealth.health;
		}

		return Mathf.Max(currentHealth, 1);
	}

	private bool TryStartNetworkAssistKill(Enemy enemy, out string status)
	{
		status = "非主机网络辅助击杀启动失败。";
		if (!GameManager.Multiplayer())
		{
			return false;
		}
		if (!TryFindNetworkKillProxy(enemy, out var proxyObject, out var hurtCollider, out var fallbackToEnemyBody, out status))
		{
			return false;
		}
		if (_networkKillRoutine != null)
		{
			StopCoroutine(_networkKillRoutine);
		}
		_networkKillRoutine = StartCoroutine(NetworkAssistKillRoutine(enemy, proxyObject, hurtCollider, fallbackToEnemyBody));
		status = fallbackToEnemyBody ? "非主机模式：未找到可用伤害代理，已回退为敌人刚体同步处置。结果取决于主机物理同步。" : string.Format("非主机模式：已通过 `{0}` 发起网络辅助击杀。", ((UnityEngine.Object)proxyObject).name);
		return true;
	}

	private IEnumerator NetworkAssistKillAllRoutine(IEnumerable<Enemy> enemies)
	{
		foreach (Enemy enemy in enemies)
		{
			if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
			{
				continue;
			}
			if (!TryFindNetworkKillProxy(enemy, out var proxyObject, out var hurtCollider, out var fallbackToEnemyBody, out _))
			{
				continue;
			}
			yield return StartCoroutine(NetworkAssistKillRoutine(enemy, proxyObject, hurtCollider, fallbackToEnemyBody));
			yield return new WaitForSeconds(0.1f);
			UpdateActiveEnemies();
		}
		_networkKillRoutine = null;
		_activeEnemyUpdateTimer = 0f;
	}

	private IEnumerator NetworkAssistKillRoutine(Enemy enemy, PhysGrabObject proxyObject, HurtCollider hurtCollider, bool fallbackToEnemyBody)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || (UnityEngine.Object)(object)proxyObject == (UnityEngine.Object)null)
		{
			_networkKillRoutine = null;
			yield break;
		}
		if (fallbackToEnemyBody)
		{
			Vector3 vector = ((Component)enemy).transform.position + Vector3.down * 80f;
			Quaternion rotation = ((Component)proxyObject).transform.rotation;
			proxyObject.Teleport(vector, rotation);
			yield return new WaitForSeconds(0.08f);
			proxyObject.Teleport(vector + Vector3.forward * 0.4f, rotation);
			_networkKillRoutine = null;
			yield break;
		}
		ItemMelee component = ((Component)proxyObject).GetComponent<ItemMelee>();
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			component = ((Component)proxyObject).GetComponentInChildren<ItemMelee>(true);
		}
		Vector3[] array2 = BuildNetworkKillOffsets(enemy);
		Quaternion rotation2 = ((Component)proxyObject).transform.rotation;
		for (int i = 0; i < array2.Length; i++)
		{
			if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || (UnityEngine.Object)(object)proxyObject == (UnityEngine.Object)null)
			{
				break;
			}
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
			{
				component.ActivateHitbox();
			}
			proxyObject.Teleport(array2[i], rotation2);
			yield return new WaitForSeconds(0.06f);
		}
		if ((UnityEngine.Object)(object)hurtCollider != (UnityEngine.Object)null && hurtCollider.enemyKill)
		{
			yield return new WaitForSeconds(0.04f);
		}
		_networkKillRoutine = null;
	}

	private static Vector3[] BuildNetworkKillOffsets(Enemy enemy)
	{
		Transform val = ((UnityEngine.Object)(object)enemy.CenterTransform != (UnityEngine.Object)null) ? enemy.CenterTransform : ((Component)enemy).transform;
		Vector3 position = val.position;
		Vector3 forward = val.forward;
		Vector3 right = val.right;
		return new Vector3[5] { position, position + Vector3.up * 0.35f, position + forward * 0.25f, position - forward * 0.25f, position + right * 0.25f };
	}

	private bool TryFindLocalPlayerDamageProxy(Enemy enemy, out PhysGrabObject proxyObject, out HurtCollider hurtCollider, out string status)
	{
		proxyObject = null;
		hurtCollider = null;
		status = "当前玩家没有正在使用的可伤害物体。";
		List<PhysGrabObject> list = new List<PhysGrabObject>();
		AddDamageProxyCandidate(list, (UnityEngine.Object)(object)PhysGrabber.instance != (UnityEngine.Object)null ? ReflectionUtils.GetFieldValue<PhysGrabObject>(PhysGrabber.instance, "grabbedPhysGrabObject") : null);
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null && (UnityEngine.Object)(object)PlayerController.instance.physGrabObject != (UnityEngine.Object)null)
		{
			AddDamageProxyCandidate(list, PlayerController.instance.physGrabObject.GetComponent<PhysGrabObject>());
			AddDamageProxyCandidate(list, PlayerController.instance.physGrabObject.GetComponentInParent<PhysGrabObject>());
		}
		foreach (PhysGrabObject item in UnityEngine.Object.FindObjectsOfType<PhysGrabObject>(true))
		{
			if (IsHeldByLocalPlayer(item))
			{
				AddDamageProxyCandidate(list, item);
			}
		}
		float num = float.MinValue;
		foreach (PhysGrabObject item2 in list)
		{
			if (!TryResolveDamageProxy(item2, enemy, out var candidateHurtCollider))
			{
				continue;
			}
			float num2 = ScoreNetworkKillProxy(item2, candidateHurtCollider) + 350f;
			if ((UnityEngine.Object)(object)item2 == (UnityEngine.Object)(object)ReflectionUtils.GetFieldValue<PhysGrabObject>(PhysGrabber.instance, "grabbedPhysGrabObject"))
			{
				num2 += 300f;
			}
			if ((UnityEngine.Object)(object)((Component)item2).GetComponent<ItemMelee>() != (UnityEngine.Object)null)
			{
				num2 += 180f;
			}
			if (num2 > num)
			{
				num = num2;
				proxyObject = item2;
				hurtCollider = candidateHurtCollider;
			}
		}
		if ((UnityEngine.Object)(object)proxyObject == (UnityEngine.Object)null)
		{
			return false;
		}
		status = string.Format("已锁定当前玩家伤害源 `{0}`。", ((UnityEngine.Object)proxyObject).name);
		return true;
	}

	private static void AddDamageProxyCandidate(List<PhysGrabObject> candidates, PhysGrabObject candidate)
	{
		if ((UnityEngine.Object)(object)candidate != (UnityEngine.Object)null && !candidates.Contains(candidate))
		{
			candidates.Add(candidate);
		}
	}

	private static bool IsHeldByLocalPlayer(PhysGrabObject candidate)
	{
		if ((UnityEngine.Object)(object)candidate == (UnityEngine.Object)null)
		{
			return false;
		}
		if (candidate.grabbedLocal)
		{
			return true;
		}
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null && (UnityEngine.Object)(object)PlayerController.instance.physGrabObject == (UnityEngine.Object)(object)((Component)candidate).gameObject)
		{
			return true;
		}
		foreach (PhysGrabber item in candidate.playerGrabbing)
		{
			if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null && (item.photonView == null || item.photonView.IsMine))
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryResolveDamageProxy(PhysGrabObject candidate, Enemy enemy, out HurtCollider hurtCollider)
	{
		hurtCollider = null;
		if ((UnityEngine.Object)(object)candidate == (UnityEngine.Object)null)
		{
			return false;
		}
		PhotonView component = ((Component)candidate).GetComponent<PhotonView>();
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null || component.ViewID == 0)
		{
			return false;
		}
		foreach (HurtCollider item in ((Component)candidate).GetComponentsInChildren<HurtCollider>(true))
		{
			if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null && ((Behaviour)item).enabled && item.enemyLogic && (item.enemyKill || item.enemyDamage > 0 || item.deathPit) && (UnityEngine.Object)(object)item.enemyHost != (UnityEngine.Object)(object)enemy)
			{
				hurtCollider = item;
				return true;
			}
		}
		return false;
	}

	private bool TryFindNetworkKillProxy(Enemy enemy, out PhysGrabObject proxyObject, out HurtCollider hurtCollider, out bool fallbackToEnemyBody, out string status)
	{
		proxyObject = null;
		hurtCollider = null;
		fallbackToEnemyBody = false;
		status = "未找到可用于网络辅助击杀的物理代理对象。";
		if (TryFindLocalPlayerDamageProxy(enemy, out proxyObject, out hurtCollider, out status))
		{
			return true;
		}
		float num = float.MinValue;
		foreach (HurtCollider item in UnityEngine.Object.FindObjectsOfType<HurtCollider>(true))
		{
			if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null || !((Behaviour)item).enabled || !item.enemyLogic || (!item.enemyKill && item.enemyDamage <= 0 && !item.deathPit))
			{
				continue;
			}
			PhysGrabObject component2 = ((Component)item).GetComponentInParent<PhysGrabObject>();
			if ((UnityEngine.Object)(object)component2 == (UnityEngine.Object)null)
			{
				continue;
			}
			PhotonView component3 = ((Component)component2).GetComponent<PhotonView>();
			if ((UnityEngine.Object)(object)component3 == (UnityEngine.Object)null || component3.ViewID == 0)
			{
				continue;
			}
			if ((UnityEngine.Object)(object)item.enemyHost == (UnityEngine.Object)(object)enemy)
			{
				continue;
			}
			float num2 = ScoreNetworkKillProxy(component2, item);
			if (num2 > num)
			{
				num = num2;
				proxyObject = component2;
				hurtCollider = item;
			}
		}
		if ((UnityEngine.Object)(object)proxyObject != (UnityEngine.Object)null)
		{
			status = string.Format("已选择网络伤害代理 `{0}`。", ((UnityEngine.Object)proxyObject).name);
			return true;
		}
		EnemyRigidbody fieldValue = ReflectionUtils.GetFieldValue<EnemyRigidbody>(enemy, "Rigidbody");
		if ((UnityEngine.Object)(object)fieldValue != (UnityEngine.Object)null)
		{
			PhysGrabObject fieldValue2 = ReflectionUtils.GetFieldValue<PhysGrabObject>(fieldValue, "physGrabObject");
			PhotonView component4 = ((UnityEngine.Object)(object)fieldValue2 != (UnityEngine.Object)null) ? ((Component)fieldValue2).GetComponent<PhotonView>() : null;
			if ((UnityEngine.Object)(object)fieldValue2 != (UnityEngine.Object)null && (UnityEngine.Object)(object)component4 != (UnityEngine.Object)null && component4.ViewID != 0)
			{
				proxyObject = fieldValue2;
				fallbackToEnemyBody = true;
				status = "未找到外部伤害代理，已回退为敌人刚体位置同步。";
				return true;
			}
		}
		return false;
	}

	private static float ScoreNetworkKillProxy(PhysGrabObject proxyObject, HurtCollider hurtCollider)
	{
		float num = 0f;
		if (hurtCollider.enemyKill)
		{
			num += 500f;
		}
		num += Mathf.Clamp(hurtCollider.enemyDamage, 0, 200);
		if (hurtCollider.deathPit)
		{
			num += 180f;
		}
		if (((Component)hurtCollider).gameObject.activeInHierarchy)
		{
			num += 40f;
		}
		if (proxyObject.grabbedLocal || proxyObject.playerGrabbing.Count > 0)
		{
			num += 220f;
		}
		if ((UnityEngine.Object)(object)((Component)proxyObject).GetComponent<ItemMelee>() != (UnityEngine.Object)null)
		{
			num += 120f;
		}
		return num;
	}

	public void TeleportToEnemy(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || (UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
		{
			_lastActionStatus = "目标怪物或本地玩家不可用，无法执行传送。";
			return;
		}

		Transform anchor = ((UnityEngine.Object)(object)enemy.CenterTransform != (UnityEngine.Object)null) ? enemy.CenterTransform : ((Component)enemy).transform;
		Vector3 basePosition = anchor.position;
		Vector3 lookDirection = Vector3.ProjectOnPlane(basePosition - ((Component)PlayerController.instance).transform.position, Vector3.up).normalized;
		if (lookDirection.sqrMagnitude <= 0.001f)
		{
			lookDirection = -Vector3.ProjectOnPlane(anchor.forward, Vector3.up).normalized;
		}
		if (lookDirection.sqrMagnitude <= 0.001f)
		{
			lookDirection = Vector3.forward;
		}

		Vector3 destination = ResolvePlayerTeleportDestination(anchor, lookDirection);
		Vector3 forward = Vector3.ProjectOnPlane(basePosition - destination, Vector3.up).normalized;
		if (forward.sqrMagnitude <= 0.001f)
		{
			forward = lookDirection;
		}
		if (forward.sqrMagnitude <= 0.001f)
		{
			forward = Vector3.forward;
		}

		Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
		if (!LocalPlayerManager.TeleportLocalPlayer(destination, rotation, string.Format("已传送到 `{0}` 身边。", GetEnemyName(enemy))))
		{
			_lastActionStatus = LocalPlayerManager.LastActionStatus;
			return;
		}

		if ((UnityEngine.Object)(object)PlayerController.instance.rb != (UnityEngine.Object)null)
		{
			PlayerController.instance.rb.velocity = Vector3.zero;
			PlayerController.instance.rb.angularVelocity = Vector3.zero;
		}

		_lastActionStatus = LocalPlayerManager.LastActionStatus;
	}

	private static Vector3 ResolvePlayerTeleportDestination(Transform anchor, Vector3 preferredDirection)
	{
		Vector3[] directions = new Vector3[4]
		{
			preferredDirection,
			-preferredDirection,
			Vector3.ProjectOnPlane(anchor.right, Vector3.up).normalized,
			-Vector3.ProjectOnPlane(anchor.right, Vector3.up).normalized
		};
		for (int i = 0; i < directions.Length; i++)
		{
			Vector3 direction = directions[i];
			if (direction.sqrMagnitude <= 0.001f)
			{
				continue;
			}

			Vector3 candidate = anchor.position - direction * 1.35f;
			Vector3 grounded = LocalPlayerManager.ResolveGroundedPointNearHeight(candidate, 0.12f, 0.6f, 10f);
			if (NavMesh.SamplePosition(grounded, out NavMeshHit hit, 1.8f, NavMesh.AllAreas))
			{
				Vector3 vector = hit.position + Vector3.up * 0.12f;
				if (!Physics.CheckCapsule(vector + Vector3.up * 0.2f, vector + Vector3.up * 1.35f, 0.25f, -1, QueryTriggerInteraction.Ignore))
				{
					return vector;
				}
			}

			if (!Physics.CheckCapsule(grounded + Vector3.up * 0.2f, grounded + Vector3.up * 1.35f, 0.25f, -1, QueryTriggerInteraction.Ignore))
			{
				return grounded;
			}
		}

		return LocalPlayerManager.ResolveGroundedPointNearHeight(anchor.position - preferredDirection * 1.2f, 0.12f, 0.6f, 10f);
	}

	public void TeleportEnemyToPlayer(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || (UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
		{
			_lastActionStatus = "目标怪物或本地玩家不可用，无法执行传送。";
			return;
		}

		if (!LocalPlayerManager.TeleportEnemyToLocalPlayer(enemy, GetEnemyName(enemy)))
		{
			_lastActionStatus = LocalPlayerManager.LastActionStatus;
			return;
		}

		_lastActionStatus = LocalPlayerManager.LastActionStatus;
	}

	private void SetupPreviewRendering()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		_previewTexture = new RenderTexture(256, 256, 16, (RenderTextureFormat)0);
		_previewTexture.Create();
		GameObject val = new GameObject("MonsterPreviewCamera");
		_previewCamera = val.AddComponent<Camera>();
		_previewCamera.targetTexture = _previewTexture;
		_previewCamera.clearFlags = (CameraClearFlags)2;
		_previewCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0f);
		_previewCamera.cullingMask = LayerMask.GetMask(new string[1] { "Default" });
		_previewCamera.fieldOfView = 60f;
		val.transform.position = new Vector3(0f, -1000f, 0f);
		UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)val);
		UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)((Component)this).gameObject);
	}

	private List<GameObject> GetSpawnObjects(EnemySetup enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return null;
		}
		try
		{
			FieldInfo field = typeof(EnemySetup).GetField("spawnObjects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.GetValue(enemy) is List<GameObject> result)
			{
				return result;
			}
		}
		catch
		{
		}
		try
		{
			List<GameObject> fieldValueByType = ReflectionUtils.GetFieldValueByType<List<GameObject>>(enemy);
			if (fieldValueByType != null)
			{
				return fieldValueByType;
			}
		}
		catch
		{
		}
		return null;
	}

	private void UpdatePreviewModel(EnemySetup enemy)
	{
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		if (((UnityEngine.Object)(object)_currentPreviewEnemy == (UnityEngine.Object)(object)enemy && (UnityEngine.Object)(object)_previewInstance != (UnityEngine.Object)null) || (UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return;
		}
		_currentPreviewEnemy = enemy;
		if ((UnityEngine.Object)(object)_previewInstance != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_previewInstance);
		}
		List<GameObject> spawnObjects = GetSpawnObjects(enemy);
		if (spawnObjects == null || spawnObjects.Count == 0)
		{
			return;
		}
		GameObject val = spawnObjects[0];
		_previewInstance = UnityEngine.Object.Instantiate<GameObject>(val);
		MonoBehaviour[] componentsInChildren = _previewInstance.GetComponentsInChildren<MonoBehaviour>();
		MonoBehaviour[] array = componentsInChildren;
		foreach (MonoBehaviour val2 in array)
		{
			if (((object)val2).GetType() != typeof(Transform) && ((object)val2).GetType() != typeof(MeshFilter) && ((object)val2).GetType() != typeof(MeshRenderer) && ((object)val2).GetType() != typeof(SkinnedMeshRenderer))
			{
				((Behaviour)val2).enabled = false;
			}
		}
		Rigidbody[] componentsInChildren2 = _previewInstance.GetComponentsInChildren<Rigidbody>();
		Rigidbody[] array2 = componentsInChildren2;
		foreach (Rigidbody val3 in array2)
		{
			val3.isKinematic = true;
		}
		Collider[] componentsInChildren3 = _previewInstance.GetComponentsInChildren<Collider>();
		Collider[] array3 = componentsInChildren3;
		foreach (Collider val4 in array3)
		{
			val4.enabled = false;
		}
		_previewInstance.transform.position = ((Component)_previewCamera).transform.position + Vector3.forward * 5f;
		_previewInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
		SetLayerRecursively(_previewInstance, 0);
	}

	private void SetLayerRecursively(GameObject obj, int layer)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		obj.layer = layer;
		foreach (Transform item in obj.transform)
		{
			Transform val = item;
			SetLayerRecursively(((Component)val).gameObject, layer);
		}
	}
}
