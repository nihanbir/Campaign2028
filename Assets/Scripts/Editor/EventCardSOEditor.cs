using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventCardSO))]
public class EventCardSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw all default fields automatically
        DrawPropertiesExcluding(serializedObject, "m_Script", "requiredInstitution");

        // Conditional field for requiredInstitution
        SerializedProperty subTypeProp = serializedObject.FindProperty("subType");
        SerializedProperty requiredInstitutionProp = serializedObject.FindProperty("requiredInstitution");

        if (subTypeProp != null && requiredInstitutionProp != null)
        {
            EventSubType currentSubType = (EventSubType)subTypeProp.enumValueIndex;
            if (currentSubType == EventSubType.ExtraRoll_IfHasInstitution)
            {
                EditorGUILayout.PropertyField(requiredInstitutionProp, new GUIContent("Required Institution"));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}