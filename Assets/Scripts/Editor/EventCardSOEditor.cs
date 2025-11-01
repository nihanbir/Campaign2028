using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventCardSO))]
public class EventCardSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything except the ones we control manually
        DrawPropertiesExcluding(serializedObject, "m_Script", "requiredInstitution", "blueTeam", "redTeam", "subType", "benefitingTeam");

        // Grab relevant serialized properties
        SerializedProperty typeProp = serializedObject.FindProperty("eventType");
        SerializedProperty subTypeProp = serializedObject.FindProperty("subType");
        SerializedProperty requiredInstitutionProp = serializedObject.FindProperty("requiredInstitution");
        SerializedProperty blueTeamProp = serializedObject.FindProperty("blueTeam");
        SerializedProperty redTeamProp = serializedObject.FindProperty("redTeam");
        SerializedProperty benefitingTeamProp = serializedObject.FindProperty("benefitingTeam");
        

        if (typeProp != null)
        {
            EventType currentEventType = (EventType)typeProp.enumValueIndex;

            // TeamConditional events
            if (currentEventType == EventType.TeamConditional)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Team Conditionals", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(blueTeamProp, new GUIContent("Blue Team Outcome"));
                EditorGUILayout.PropertyField(redTeamProp, new GUIContent("Red Team Outcome"));
                EditorGUILayout.PropertyField(benefitingTeamProp, new GUIContent("Beneficial to team"));
                
            }
            // Only show SubType for relevant event types
            else if (IsSubtypeRelevant(currentEventType))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Subtype Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(subTypeProp, new GUIContent("Event Sub Type"));

                if (subTypeProp != null)
                {
                    EventSubType currentSubType = (EventSubType)subTypeProp.enumValueIndex;

                    // Show institution condition if applicable
                    if (currentSubType == EventSubType.ExtraRoll_IfHasInstitution)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Institution Condition", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(requiredInstitutionProp, new GUIContent("Required Institution"));
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Defines which event types support subtypes.
    /// </summary>
    private bool IsSubtypeRelevant(EventType eventType)
    {
        switch (eventType)
        {
            case EventType.ExtraRoll:
                return true;
            // Add more cases as you add subtype support for other types
            default:
                return false;
        }
    }
}
