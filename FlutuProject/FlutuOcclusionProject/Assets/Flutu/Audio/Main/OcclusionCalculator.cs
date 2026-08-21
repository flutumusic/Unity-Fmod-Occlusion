using UnityEngine;
using FMODUnity;
using System.Collections.Generic;
using FMOD.Studio;

namespace Flutu.Audio
{
    public struct DebugRay
    {
        public Vector3 start;
        public Vector3 end;
        public bool hasHit;
        public float occlusionValue;
    }

    public class OcclusionCalculator
    {
        private const int rayCount = 5;
        private const float coneAngle = 60f;
        private static readonly QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        private const string occlusionParameterName = "Occlusion";
        private const float attackSpeed = 15f;
        private const float releaseSpeed = 4f;
        private const int rayHitBufferSize = 16;
        private const float parameterChangeThreshold = 0.005f;

        private readonly RaycastHit[] rayHitBuffer = new RaycastHit[rayHitBufferSize];
        private readonly LayerMask occlusionLayerMask;

        private Dictionary<StudioEventEmitter, float> emitterOcclusion = new();
        private Dictionary<StudioEventEmitter, PARAMETER_ID> emitterParameterId = new();
        private Dictionary<StudioEventEmitter, float> emitterLastSentOcclusion = new();

        private readonly List<StudioEventEmitter> deadEmitters = new();
        private float nextCleanupTime;
        private const float cleanupInterval = 5f;

        public bool debugVisualsEnabled = false;
        public Dictionary<Transform, List<DebugRay>> debugRays = new();

        public OcclusionCalculator(LayerMask occlusionLayerMask)
        {
            this.occlusionLayerMask = occlusionLayerMask;
        }

        public void Calculate(List<StudioEventEmitter> emitters, Transform listenerTransform)
        {
            if (listenerTransform == null)
            {
                return;
            }

            if (Time.time >= nextCleanupTime)
            {
                nextCleanupTime = Time.time + cleanupInterval;
                RemoveDeadEmitters();
            }

            if (debugVisualsEnabled)
                debugRays.Clear();

            foreach (StudioEventEmitter emitter in emitters)
            {
                if (emitter == null)
                    continue;

                float targetOcclusion = CalculateOcclusion(emitter.transform, listenerTransform);

                if (!emitterOcclusion.TryGetValue(emitter, out var currentOcclusion))
                {
                    currentOcclusion = targetOcclusion;
                }
                else
                {
                    float speed = targetOcclusion > currentOcclusion ? attackSpeed : releaseSpeed;
                    float smoothingFactor = 1f - Mathf.Exp(-speed * Time.fixedDeltaTime);
                    currentOcclusion = Mathf.Lerp(currentOcclusion, targetOcclusion, smoothingFactor);
                }

                currentOcclusion = Mathf.Clamp01(currentOcclusion);
                emitterOcclusion[emitter] = currentOcclusion;

                SetEmitterParameter(emitter, currentOcclusion);
            }
        }

        private void SetEmitterParameter(StudioEventEmitter emitter, float occlusionValue)
        {
            if (!emitterParameterId.TryGetValue(emitter, out var paramId))
            {
                if (emitter.EventDescription.getParameterDescriptionByName(occlusionParameterName, out var paramDesc) == FMOD.RESULT.OK)
                {
                    paramId = paramDesc.id;
                    emitterParameterId[emitter] = paramId;
                }
                else
                {
                    return;
                }
            }

            float lastSent = emitterLastSentOcclusion.TryGetValue(emitter, out var last) ? last : -1f;

            if (Mathf.Abs(occlusionValue - lastSent) > parameterChangeThreshold)
            {
                emitter.SetParameter(paramId, occlusionValue);
                emitterLastSentOcclusion[emitter] = occlusionValue;
            }
        }

        private float CalculateOcclusion(Transform emitterTransform, Transform listenerTransform)
        {
            Vector3 emitterPos = emitterTransform.position;
            Vector3 listenerPos = listenerTransform.position;
            Vector3 directionToListener = listenerPos - emitterPos;

            if (directionToListener.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            Vector3 forward = directionToListener.normalized;
            float rayLength = directionToListener.magnitude;

            if (debugVisualsEnabled && !debugRays.ContainsKey(emitterTransform))
                debugRays[emitterTransform] = new List<DebugRay>();

            // Check all rays and accumulate occlusion
            int centerRayIndex = rayCount / 2;
            float totalOcclusion = 0f;

            // Check direct ray first
            float[] directRayOcclusions = GetAllOcclusions(centerRayIndex, emitterPos, forward, rayLength, emitterTransform, listenerTransform);
            bool centerRayBlocked = directRayOcclusions.Length > 0;

            if (!centerRayBlocked)
                return 0f;

            totalOcclusion += GetTotalOcclusion(directRayOcclusions);

            // Check left side rays
            for (int i = 0; i < centerRayIndex; i++)
            {
                float[] sideRayOcclusions = GetAllOcclusions(i, emitterPos, forward, rayLength, emitterTransform, listenerTransform);
                totalOcclusion += GetTotalOcclusion(sideRayOcclusions);
            }

            // Check right side rays
            for (int i = centerRayIndex + 1; i < rayCount; i++)
            {
                float[] sideRayOcclusions = GetAllOcclusions(i, emitterPos, forward, rayLength, emitterTransform, listenerTransform);
                totalOcclusion += GetTotalOcclusion(sideRayOcclusions);
            }

            float occlusionValue = totalOcclusion / rayCount;
            return occlusionValue;
        }

        private float[] GetAllOcclusions(int rayIndex, Vector3 emitterPos, Vector3 forward, float rayLength, Transform emitterTransform, Transform listenerTransform)
        {
            Vector3 rayDirection = GetConeRayDirection(forward, rayIndex);
            Vector3 rayEnd = emitterPos + rayDirection * rayLength;

            float[] occlusionValues = EvaluateRay(emitterPos, rayEnd, emitterTransform, listenerTransform);

            if (debugVisualsEnabled)
            {
                bool hasHit = occlusionValues.Length > 0;
                float displayValue = hasHit ? GetTotalOcclusion(occlusionValues) : 0f;
                debugRays[emitterTransform].Add(new DebugRay
                {
                    start = emitterPos,
                    end = rayEnd,
                    hasHit = hasHit,
                    occlusionValue = displayValue
                });
            }

            return occlusionValues;
        }

        private float GetTotalOcclusion(float[] occlusionValues)
        {
            float sum = 0f;
            foreach (float value in occlusionValues)
                sum += value;
            return Mathf.Clamp01(sum);
        }

        private Vector3 GetConeRayDirection(Vector3 forward, int rayIndex)
        {
            if (rayCount == 1)
                return forward;

            float angle = (rayIndex / (float)(rayCount - 1)) * coneAngle - (coneAngle * 0.5f);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            return rotation * forward;
        }

        private float[] EvaluateRay(
            Vector3 start,
            Vector3 target,
            Transform emitterTransform,
            Transform listenerTransform
        )
        {
            Vector3 direction = target - start;
            float distance = direction.magnitude;

            if (distance < 0.001f)
                return new float[0];

            int hitCount = Physics.RaycastNonAlloc(
                start,
                direction.normalized,
                rayHitBuffer,
                distance,
                occlusionLayerMask,
                triggerInteraction
            );

            System.Array.Sort(rayHitBuffer, 0, hitCount, new RaycastHitComparer());

            List<float> occlusionValues = new();

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = rayHitBuffer[i];

                if (hit.collider == null)
                    continue;

                if (IsPartOfTransform(hit.collider.transform, emitterTransform))
                    continue;

                if (IsPartOfTransform(hit.collider.transform, listenerTransform))
                    return occlusionValues.ToArray();

                if (!Obstacle.Registry.TryGetValue(hit.collider.GetInstanceID(), out var obstacle))
                    continue;

                occlusionValues.Add(obstacle.occlusionValue);
            }

            return occlusionValues.ToArray();
        }

        private void RemoveDeadEmitters()
        {
            deadEmitters.Clear();

            foreach (var pair in emitterOcclusion)
            {
                if (pair.Key == null)
                    deadEmitters.Add(pair.Key);
            }

            for (int i = 0; i < deadEmitters.Count; i++)
            {
                emitterOcclusion.Remove(deadEmitters[i]);
                emitterParameterId.Remove(deadEmitters[i]);
                emitterLastSentOcclusion.Remove(deadEmitters[i]);
            }
        }

        private bool IsPartOfTransform(Transform hitTransform, Transform targetTransform)
        {
            if (targetTransform == null)
                return false;

            return hitTransform == targetTransform || hitTransform.IsChildOf(targetTransform);
        }

        private class RaycastHitComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public int Compare(RaycastHit a, RaycastHit b)
            {
                return a.distance.CompareTo(b.distance);
            }
        }
    }
}
