using UnityEngine;
using System.Collections.Generic;

namespace Flutu.Audio
{
    public class AudioObstacle : MonoBehaviour
    {
        public static readonly Dictionary<int, AudioObstacle> Registry = new();

        [SerializeField]
        private bool _hasOcclusion = true;
        public bool hasOcclusion => _hasOcclusion;

        [SerializeField]
        [Range(0f, 1f)]
        private float _occlusionValue = AudioOcclusionLevels.low;
        public float occlusionValue => _occlusionValue;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                Registry[col.GetInstanceID()] = this;
        }

        private void OnDestroy()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                Registry.Remove(col.GetInstanceID());
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
#endif
    }
}
