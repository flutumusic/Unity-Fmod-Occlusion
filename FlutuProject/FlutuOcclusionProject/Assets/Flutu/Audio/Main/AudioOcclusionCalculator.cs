using UnityEngine;
using FMODUnity;
using System.Collections.Generic;
using FMOD.Studio;

namespace Flutu.Audio
{
    public class AudioOcclusionCalculator
    {
        private const int rayCount = 5;
        private const float coneAngle = 60f;
        private const float detectionRadius = 50f;
        private static readonly QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        private const string occlusionParameterName = "Occlusion";
        private const float smoothingSpeed = 10f;
        private const int rayHitBufferSize = 16;
        private const float parameterChangeThreshold = 0.005f;

        private readonly RaycastHit[] rayHitBuffer = new RaycastHit[rayHitBufferSize];
        private LayerMask occlusionLayerMask = ~0;

        private Dictionary<StudioEventEmitter, float> emitterOcclusion = new();
        private Dictionary<StudioEventEmitter, PARAMETER_ID> emitterParameterId = new();
        private Dictionary<StudioEventEmitter, float> emitterLastSentOcclusion = new();

        public void Calculate(List<StudioEventEmitter> emitters, Transform listenerTransform)
        {
            foreach (StudioEventEmitter emitter in emitters)
            {
                if (emitter == null)
                    continue;

                float targetOcclusion = CalculateOcclusion(emitter.transform, listenerTransform);

                float currentOcclusion = emitterOcclusion.TryGetValue(emitter, out var value) ? value : 0f;

                currentOcclusion = Mathf.Lerp(
                    currentOcclusion,
                    targetOcclusion,
                    smoothingSpeed * Time.fixedDeltaTime
                );

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
                return 0f;

            Vector3 forward = directionToListener.normalized;
            float maxOcclusion = 0f;
            int hitCount = 0;

            for (int i = 0; i < rayCount; i++)
            {
                Vector3 rayDirection = GetConeRayDirection(forward, i);
                bool hasHit = EvaluateRay(
                    emitterPos,
                    emitterPos + rayDirection * detectionRadius,
                    ref maxOcclusion,
                    emitterTransform,
                    listenerTransform
                );

                if (hasHit)
                    hitCount++;
            }

            float rayCoverageFactor = hitCount / (float)rayCount;
            return rayCoverageFactor * maxOcclusion;
        }

        private Vector3 GetConeRayDirection(Vector3 forward, int rayIndex)
        {
            if (rayCount == 1)
                return forward;

            float angle = (rayIndex / (float)(rayCount - 1)) * coneAngle - (coneAngle * 0.5f);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            return rotation * forward;
        }

        private bool EvaluateRay(
            Vector3 start,
            Vector3 target,
            ref float maxOcclusion,
            Transform emitterTransform,
            Transform listenerTransform
        )
        {
            Vector3 direction = target - start;
            float distance = direction.magnitude;

            if (distance < 0.001f)
                return false;

            int hitCount = Physics.RaycastNonAlloc(
                start,
                direction.normalized,
                rayHitBuffer,
                distance,
                occlusionLayerMask,
                triggerInteraction
            );

            bool foundOccluder = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = rayHitBuffer[i];

                if (hit.collider == null)
                    continue;

                if (IsPartOfTransform(hit.collider.transform, emitterTransform))
                    continue;

                if (IsPartOfTransform(hit.collider.transform, listenerTransform))
                    continue;

                if (!AudioObstacle.Registry.TryGetValue(hit.collider.GetInstanceID(), out var obstacle))
                    continue;

                foundOccluder = true;

                if (obstacle.occlusionValue > maxOcclusion)
                    maxOcclusion = obstacle.occlusionValue;
            }

            return foundOccluder;
        }

        private bool IsPartOfTransform(Transform hitTransform, Transform targetTransform)
        {
            if (targetTransform == null)
                return false;

            return hitTransform == targetTransform || hitTransform.IsChildOf(targetTransform);
        }
    }
}
