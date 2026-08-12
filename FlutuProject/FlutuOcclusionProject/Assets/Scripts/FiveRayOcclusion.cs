using System;
using UnityEngine;
using FMODUnity;


[RequireComponent(typeof(StudioEventEmitter))]
public class FiveRayOcclusion : MonoBehaviour
{
    [Header("FMOD")]
    [Tooltip("Exact name of the parameter in the FMOD event.")]
    [SerializeField] private string occlusionParameterName = "Occlusion";


    [Header("References")]
    [Tooltip("Listener transform. Usually the player object containing the StudioListener component.")]
    [SerializeField] private Transform listener;


    [Header("Ray Setup")]
    [Tooltip("Horizontal distance between the center ray and the inner side rays.")]
    [SerializeField] private float lateralOffset = 0.5f;


    [Tooltip("Vertical offset applied to the emitter target point.")]
    [SerializeField] private float rayHeightOffset = 0.0f;


    [Tooltip("Smoothing speed applied to the FMOD occlusion parameter.")]
    [SerializeField] private float smoothingSpeed = 10.0f;


    [Tooltip("Ignore trigger colliders when checking for occluders.")]
    [SerializeField] private QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Ignore;


    [Header("Debug")]
    [Tooltip("Draw the rays in the Scene view.")]
    [SerializeField] private bool drawSceneGizmos = true;


    [Tooltip("Only draw gizmos while the game is playing.")]
    [SerializeField] private bool drawOnlyWhenPlaying = true;


    [Tooltip("Draw a sphere at the target point of each ray.")]
    [SerializeField] private bool drawTargetSpheres = true;


    [SerializeField] private float targetSphereRadius = 0.05f;


    private StudioEventEmitter emitter;


    private float currentOcclusion;


    private bool hitCenter;
    private bool hitLeft;
    private bool hitRight;
    private bool hitFarLeft;
    private bool hitFarRight;


    private Vector3 origin;
    private Vector3 centerPoint;
    private Vector3 leftPoint;
    private Vector3 rightPoint;
    private Vector3 farLeftPoint;
    private Vector3 farRightPoint;


    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
    }


    private void Start()
    {
        TryAssignListener();
    }


    private void Update()
    {
        if (listener == null)
        {
            TryAssignListener();

            if (listener == null)
                return;
        }


        if (emitter == null)
            return;


        UpdateRayPoints();


        float maximumMaterialOcclusion = 0.0f;


        hitCenter = EvaluateRay(
            origin,
            centerPoint,
            ref maximumMaterialOcclusion
        );


        hitLeft = EvaluateRay(
            origin,
            leftPoint,
            ref maximumMaterialOcclusion
        );


        hitRight = EvaluateRay(
            origin,
            rightPoint,
            ref maximumMaterialOcclusion
        );


        hitFarLeft = EvaluateRay(
            origin,
            farLeftPoint,
            ref maximumMaterialOcclusion
        );


        hitFarRight = EvaluateRay(
            origin,
            farRightPoint,
            ref maximumMaterialOcclusion
        );


        int hitCount = 0;


        if (hitCenter)
            hitCount++;


        if (hitLeft)
            hitCount++;


        if (hitRight)
            hitCount++;


        if (hitFarLeft)
            hitCount++;


        if (hitFarRight)
            hitCount++;


        // Five-ray coverage factor:
        // 0 hits = 0.0
        // 1 hit  = 0.2
        // 2 hits = 0.4
        // 3 hits = 0.6
        // 4 hits = 0.8
        // 5 hits = 1.0
        float rayCoverageFactor = hitCount / 5.0f;


        // The AudioOccluder value defines the maximum occlusion.
        float targetOcclusion =
            rayCoverageFactor * maximumMaterialOcclusion;


        currentOcclusion = Mathf.Lerp(
            currentOcclusion,
            targetOcclusion,
            smoothingSpeed * Time.deltaTime
        );


        currentOcclusion = Mathf.Clamp01(currentOcclusion);


        emitter.SetParameter(
            occlusionParameterName,
            currentOcclusion
        );
    }


    private void TryAssignListener()
    {
        if (listener != null)
            return;


        StudioListener studioListener =
            UnityEngine.Object.FindFirstObjectByType<StudioListener>();


        if (studioListener != null)
            listener = studioListener.transform;
    }


    private void UpdateRayPoints()
    {
        origin = listener.position;


        centerPoint =
            transform.position +
            Vector3.up * rayHeightOffset;


        Vector3 direction =
            centerPoint - origin;


        if (direction.sqrMagnitude <= 0.000001f)
        {
            leftPoint = centerPoint;
            rightPoint = centerPoint;
            farLeftPoint = centerPoint;
            farRightPoint = centerPoint;
            return;
        }


        Vector3 forward = direction.normalized;


        Vector3 lateral =
            Vector3.Cross(Vector3.up, forward).normalized;


        // Inner side rays.
        Vector3 innerLateral =
            lateral * lateralOffset;


        // Outer side rays.
        // Their distance from the center is twice the lateralOffset.
        Vector3 outerLateral =
            lateral * (lateralOffset * 2.0f);


        leftPoint = centerPoint - innerLateral;
        rightPoint = centerPoint + innerLateral;


        farLeftPoint = centerPoint - outerLateral;
        farRightPoint = centerPoint + outerLateral;
    }


    private bool EvaluateRay(
        Vector3 start,
        Vector3 target,
        ref float maximumMaterialOcclusion
    )
    {
        Vector3 direction = target - start;
        float distance = direction.magnitude;


        if (distance <= 0.001f)
            return false;


        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction.normalized,
            distance,
            ~0,
            triggerInteraction
        );


        bool foundOccluder = false;


        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];


            if (hit.collider == null)
                continue;


            if (IsPartOfThisEmitter(hit.collider.transform))
                continue;


            if (IsPartOfListener(hit.collider.transform))
                continue;


            AudioOccluder occluder =
                hit.collider.GetComponentInParent<AudioOccluder>();


            if (occluder == null)
                continue;


            foundOccluder = true;


            if (occluder.occlusionValue > maximumMaterialOcclusion)
            {
                maximumMaterialOcclusion =
                    occluder.occlusionValue;
            }
        }


        return foundOccluder;
    }


    private bool IsPartOfThisEmitter(Transform hitTransform)
    {
        return hitTransform == transform ||
               hitTransform.IsChildOf(transform);
    }


    private bool IsPartOfListener(Transform hitTransform)
    {
        if (listener == null)
            return false;


        return hitTransform == listener ||
               hitTransform.IsChildOf(listener);
    }


    private void OnDrawGizmos()
    {
        if (!drawSceneGizmos)
            return;


        if (drawOnlyWhenPlaying && !Application.isPlaying)
            return;


        if (listener == null)
            return;


        DrawRay(
            origin,
            centerPoint,
            hitCenter,
            Color.yellow
        );


        DrawRay(
            origin,
            leftPoint,
            hitLeft,
            Color.cyan
        );


        DrawRay(
            origin,
            rightPoint,
            hitRight,
            Color.magenta
        );


        DrawRay(
            origin,
            farLeftPoint,
            hitFarLeft,
            Color.blue
        );


        DrawRay(
            origin,
            farRightPoint,
            hitFarRight,
            Color.green
        );
    }


    private void DrawRay(
        Vector3 start,
        Vector3 end,
        bool hasOccluder,
        Color clearColor
    )
    {
        Gizmos.color = hasOccluder
            ? Color.red
            : clearColor;


        Gizmos.DrawLine(start, end);


        if (drawTargetSpheres)
            Gizmos.DrawSphere(end, targetSphereRadius);
    }
}