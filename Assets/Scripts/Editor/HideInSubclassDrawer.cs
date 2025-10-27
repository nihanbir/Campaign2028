using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HideInSubClassAttribute))]
public class HideInSubClassAttributeDrawer : PropertyDrawer
{
    private bool ShouldShow(SerializedProperty property)
    {
        Type type = property.serializedObject.targetObject.GetType();
        FieldInfo field = type.GetField(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (field == null)
        {
            Debug.LogWarning($"Field '{property.name}' not found in type '{type.Name}'.");
            return true; // Show the property if it can't be found
        }

        Type declaringType = field.DeclaringType;
        return type == declaringType;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return ShouldShow(property) ? base.GetPropertyHeight(property, label) : 0;
    }
}