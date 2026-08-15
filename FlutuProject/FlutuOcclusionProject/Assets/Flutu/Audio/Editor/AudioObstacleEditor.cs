#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Flutu.Audio.Editor
{
    [CustomEditor(typeof(AudioObstacle))]
    public class AudioObstacleEditor : UnityEditor.Editor
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
                float newValue = EditorGUILayout.Slider("AudioOcclusionLevels", _occlusionValueProp.floatValue, 0f, 1f);
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
            float closest = AudioOcclusionLevels.low;
            float closestDistance = Mathf.Abs(value - AudioOcclusionLevels.low);

            foreach (var level in AudioOcclusionLevels.values.Values)
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

            foreach (var entry in AudioOcclusionLevels.values)
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
