using UnityEngine;
using FMODUnity;
using System.Collections.Generic;

namespace Flutu.Audio
{
    public class AudioListener : MonoBehaviour
    {
        [SerializeField] private float detectionRadius = 50f;
        [SerializeField] private bool drawDebugSphere = true;

        private List<StudioEventEmitter> emitters = new();
        private Collider[] overlapResults = new Collider[128];
        private AudioOcclusionCalculator occlusionCalculator;

        private void Awake()
        {
            occlusionCalculator = new AudioOcclusionCalculator();
        }

        private void FixedUpdate()
        {
            FindEmitters();
            occlusionCalculator.Calculate(emitters, transform);
        }

        private void FindEmitters()
        {
            emitters.Clear();

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectionRadius,
                overlapResults
            );

            for (int i = 0; i < hitCount; i++)
            {
                StudioEventEmitter emitter = overlapResults[i].GetComponentInParent<StudioEventEmitter>();

                if (emitter != null && !emitters.Contains(emitter))
                    emitters.Add(emitter);
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugSphere)
                return;

            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
