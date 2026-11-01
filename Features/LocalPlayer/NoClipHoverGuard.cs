using UnityEngine;

namespace Cheat.Features.LocalPlayer;

/// <summary>Applies the noclip hover state after the game's player movement callbacks.</summary>
[DefaultExecutionOrder(32000)]
public sealed class NoClipHoverGuard : MonoBehaviour
{
	/// <summary>Reasserts the hover state after each game physics update.</summary>
	private void FixedUpdate()
	{
		LocalPlayerManager.EnforceNoClipHover();
	}

	/// <summary>Reasserts the hover state after the game's regular Update callbacks.</summary>
	private void LateUpdate()
	{
		LocalPlayerManager.EnforceNoClipHover();
	}
}
