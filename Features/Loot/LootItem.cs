using System.Collections.Generic;
using UnityEngine;

namespace Cheat.Features.Loot;

public class LootItem
{
	public PhysGrabObject PhysGrabObject;

	public ValuableObject ValuableObject;

	public Vector3 Position;

	public int Value;

	public string Name;

	public bool InCart;

	public List<Renderer> Renderers = new List<Renderer>();

	public List<Mesh> Meshes = new List<Mesh>();
}
