using UnityEngine;
using FMODUnity;
using System.Collections.Generic;

namespace Flutu.Audio.Occlusion
{
    public class OcclusionListener : MonoBehaviour
    {
        [SerializeField] private bool debugVisualize = false;
        [SerializeField] private LayerMask occlusionLayerMask = ~0;
        [Tooltip("Layers that contain Occluder colliders. Filters raycasts at the source.")]

        private List<StudioEventEmitter> emitters = new();
        private OcclusionCalculator occlusionCalculator;

        private void Awake()
        {
            occlusionCalculator = new OcclusionCalculator(occlusionLayerMask);
            occlusionCalculator.debugVisualsEnabled = debugVisualize;
        }

        private void FixedUpdate()
        {
            List<StudioEventEmitter> nearbyEmitters = new();
            foreach (var emitter in OccludableEmitterRegistry.GetEmitters())
            {
                float distance = Vector3.Distance(transform.position, emitter.transform.position);

                emitter.EventDescription.getMinMaxDistance(out _, out float maxDistance);
                if (distance <= maxDistance)
                    nearbyEmitters.Add(emitter);
            }

            occlusionCalculator.Calculate(nearbyEmitters, transform);
        }

        private void OnDrawGizmos()
        {
            if (!debugVisualize || occlusionCalculator == null)
                return;

            occlusionCalculator.debugVisualsEnabled = true;

            if (occlusionCalculator.debugRays.Count == 0)
            {
                Debug.LogWarning("OcclusionListener: No debug rays to draw. Check if OcclusionListener is in play mode and emitters are within FMOD max distance.");
                return;
            }

            foreach (var kvp in occlusionCalculator.debugRays)
            {
                List<DebugRay> rays = kvp.Value;
                foreach (DebugRay ray in rays)
                {
                    if (ray.hasHit)
                    {
                        Gizmos.color = new Color(1, 0, 0, 0.8f);
                    }
                    else
                    {
                        Gizmos.color = new Color(0, 1, 0, 0.6f);
                    }
                    Gizmos.DrawLine(ray.start, ray.end);
                }
            }
        }
    }
}
