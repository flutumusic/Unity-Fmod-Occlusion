#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Flutu.Audio.Occlusion.Editor
{
    [CustomEditor(typeof(Occluder))]
    public class OccluderEditor : UnityEditor.Editor
    {
        private SerializedProperty _hasOcclusionProp;
        private SerializedProperty _occlusionValueProp;

        private void OnEnable()
        {
            _hasOcclusionProp = serializedObject.FindProperty("_hasOcclusion");
            _occlusionValueProp = serializedObject.FindProperty("_occlusionValue");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_hasOcclusionProp, new GUIContent("Has Occlusion"));

            if (_hasOcclusionProp.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUILayout.Slider("Occlusion Level", _occlusionValueProp.floatValue, 0f, 1f);
                bool changed = EditorGUI.EndChangeCheck();

                if (changed)
                {
                    _occlusionValueProp.floatValue = SnapToNearestLevel(newValue);
                }

                string levelName = FindClosestLevelName(_occlusionValueProp.floatValue);
                EditorGUILayout.LabelField("Level", levelName);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private float SnapToNearestLevel(float value)
        {
            float closest = OcclusionLevels.low;
            float closestDistance = Mathf.Abs(value - OcclusionLevels.low);

            foreach (var level in OcclusionLevels.values.Values)
            {
                float distance = Mathf.Abs(value - level);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = level;
                }
            }

            return closest;
        }

        private string FindClosestLevelName(float value)
        {
            float closest = SnapToNearestLevel(value);

            foreach (var entry in OcclusionLevels.values)
            {
                if (Mathf.Approximately(entry.Value, closest))
                {
                    return entry.Key;
                }
            }

            return "Unknown";
        }
    }
}
#endif
