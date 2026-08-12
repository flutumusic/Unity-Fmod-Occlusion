using UnityEngine;


public class AudioOccluder : MonoBehaviour
{
    public enum OcclusionLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        OccludeAll = 3
    }


    [Header("Occlusion")]
    [Tooltip("Select the maximum occlusion level for this object.")]
    [SerializeField]
    private OcclusionLevel occlusionLevel =
        OcclusionLevel.Medium;


    /// <summary>
    /// Returns the occlusion value used by the raycast system.
    /// </summary>
    public float occlusionValue
    {
        get
        {
            switch (occlusionLevel)
            {
                case OcclusionLevel.Low:
                    return 0.40f;

                case OcclusionLevel.Medium:
                    return 0.60f;

                case OcclusionLevel.High:
                    return 0.80f;

                case OcclusionLevel.OccludeAll:
                    return 1.00f;

                default:
                    return 0.60f;
            }
        }
    }
}