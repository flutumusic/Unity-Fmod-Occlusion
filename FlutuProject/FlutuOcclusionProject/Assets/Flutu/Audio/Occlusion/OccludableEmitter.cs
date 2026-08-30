using FMODUnity;
using UnityEngine;

namespace Flutu.Audio.Occlusion
{
    public class OccludableEmitter : MonoBehaviour
    {
        private StudioEventEmitter emitter;

        private void OnEnable()
        {
            emitter = GetComponent<StudioEventEmitter>();
            if (emitter != null)
                OccludableEmitterRegistry.Register(emitter);
        }

        private void OnDisable()
        {
            if (emitter != null)
                OccludableEmitterRegistry.Unregister(emitter);
        }
    }
}
