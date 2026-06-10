using Photon.Pun;
using UnityEngine;

namespace Cheat.Features
{
    public class DecorationFixer : MonoBehaviour
    {
        [PunRPC]
        public void UpdateAllDecorationsCompressed(byte[] a, string[] b, string[] c, byte[] d)
        {
            // Dummy method to suppress RPC error from other mods
        }
    }
}