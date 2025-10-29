using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventCardSO))]
public class EventCardSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var eventName = serializedObject.FindProperty("eventName");
        var artwork = serializedObject.FindProperty("artwork");
        var backSide = serializedObject.FindProperty("backSide");
        var eventType = serializedObject.FindProperty("eventType");
        var subType = serializedObject.FindProperty("subType");
        var requiredInstitution = serializedObject.FindProperty("requiredInstitution");
        var mustPlayImmediately = serializedObject.FindProperty("mustPlayImmediately");
        var canSave = serializedObject.FindProperty("canSave");

        EditorGUILayout.PropertyField(eventName);
        EditorGUILayout.PropertyField(artwork);
        EditorGUILayout.PropertyField(backSide);
        EditorGUILayout.PropertyField(eventType);
        EditorGUILayout.PropertyField(subType);

        // 👇 Conditional display
        EventSubType currentSubType = (EventSubType)subType.enumValueIndex;
        if (currentSubType == EventSubType.ExtraRoll_IfHasInstitution)
        {
            EditorGUILayout.PropertyField(requiredInstitution, new GUIContent("Required Institution"));
        }

        EditorGUILayout.PropertyField(mustPlayImmediately);
        EditorGUILayout.PropertyField(canSave);

        serializedObject.ApplyModifiedProperties();
    }
}