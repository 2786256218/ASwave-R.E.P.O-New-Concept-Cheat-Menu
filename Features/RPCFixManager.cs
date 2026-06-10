using System;
using Photon.Pun;
using UnityEngine;

namespace Cheat.Features
{
    public class RPCFixManager : MonoBehaviour
    {
        private float _timer = 0f;

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer > 3f)
            {
                _timer = 0f;
                try
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    PhotonView[] views = PhotonNetwork.PhotonViews;
#pragma warning restore CS0618 // Type or member is obsolete
                    if (views != null)
                    {
                        foreach (var view in views)
                        {
                            if (view != null && view.gameObject != null)
                            {
                                if (view.gameObject.GetComponent<DecorationFixer>() == null)
                                {
                                    view.gameObject.AddComponent<DecorationFixer>();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Cheat.Utils.Logger.Error("RPCFixManager error: " + ex.Message, "RPCFixManager");
                }
            }
        }
    }
}