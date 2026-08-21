using FMODUnity;
using UnityEngine;

namespace Flutu.Audio
{
    public class Emitter : MonoBehaviour
    {
        private StudioEventEmitter emitter;

        private void OnEnable()
        {
            emitter = GetComponent<StudioEventEmitter>();
            if (emitter != null)
                EmitterRegistry.Register(emitter);
        }

        private void OnDisable()
        {
            if (emitter != null)
                EmitterRegistry.Unregister(emitter);
        }
    }
}
