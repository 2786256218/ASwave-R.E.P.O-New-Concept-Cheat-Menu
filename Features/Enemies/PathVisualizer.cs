using Cheat.Config;
using UnityEngine;
using UnityEngine.AI;

namespace Cheat.Features.Enemies;

public class PathVisualizer : MonoBehaviour
{
	private LineRenderer _lineRenderer;

	private NavMeshAgent _agent;

	public void Initialize(NavMeshAgent agent)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		_agent = agent;
		_lineRenderer = ((Component)this).gameObject.AddComponent<LineRenderer>();
		_lineRenderer.useWorldSpace = true;
		_lineRenderer.startWidth = 0.1f;
		_lineRenderer.endWidth = 0.1f;
		Material val = new Material(Shader.Find("Hidden/Internal-Colored"));
		val.SetInt("_ZTest", 8);
		val.SetInt("_ZWrite", 0);
		((Renderer)_lineRenderer).material = val;
		_lineRenderer.startColor = Color.yellow;
		_lineRenderer.endColor = Color.yellow;
		((Renderer)_lineRenderer).enabled = false;
	}

	private void Update()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Invalid comparison between Unknown and I4
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_agent == (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)this).gameObject);
		}
		else
		{
			if ((UnityEngine.Object)(object)_lineRenderer == (UnityEngine.Object)null)
			{
				return;
			}
			if (!ConfigManager.Config.Enemies.DrawPath && !ConfigManager.Config.Minimap.ShowPath)
			{
				((Renderer)_lineRenderer).enabled = false;
				return;
			}
			if (!_agent.hasPath || (int)_agent.pathStatus == 2)
			{
				((Renderer)_lineRenderer).enabled = false;
				return;
			}
			((Renderer)_lineRenderer).enabled = true;
			Vector3[] corners = _agent.path.corners;
			if (corners != null && corners.Length > 1)
			{
				_lineRenderer.positionCount = corners.Length;
				_lineRenderer.SetPositions(corners);
				_lineRenderer.startColor = Color.yellow;
				_lineRenderer.endColor = Color.yellow;
			}
			else
			{
				_lineRenderer.positionCount = 0;
			}
		}
	}

	private void OnDestroy()
	{
		if ((UnityEngine.Object)(object)_lineRenderer != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)_lineRenderer).gameObject);
		}
	}
}
