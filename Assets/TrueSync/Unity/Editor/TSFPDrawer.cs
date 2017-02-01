using System;
using UnityEditor;
using UnityEngine;

namespace TrueSync {

    [CustomPropertyDrawer(typeof(FP))]
    public class TSFPDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            SerializedProperty serializedValueProperty = property.FindPropertyRelative("_serializedValue");
            string value = serializedValueProperty.stringValue;

            FP fpValue = FP.FromRaw(long.Parse(value));

            fpValue = EditorGUI.FloatField(position, label, (float)fpValue);

            if (EditorGUI.EndChangeCheck()) {
                serializedValueProperty.stringValue = fpValue.RawValue.ToString();
            }

            EditorGUI.EndProperty();
        }

    }

}