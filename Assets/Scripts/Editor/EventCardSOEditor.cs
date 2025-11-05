using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventCardSO))]
public class EventCardSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything except the ones we control manually
        DrawPropertiesExcluding(serializedObject, "m_Script", "altState1", "altState2", "requiredInstitution", "eventConditions", "blueTeam", "redTeam", "benefitingTeam");

        // Grab relevant serialized properties
        SerializedProperty typeProp = serializedObject.FindProperty("eventType");
        
        SerializedProperty conditionProp = serializedObject.FindProperty("eventConditions");
        SerializedProperty requiredInstitutionProp = serializedObject.FindProperty("requiredInstitution");
        
        SerializedProperty blueTeamProp = serializedObject.FindProperty("blueTeam");
        SerializedProperty redTeamProp = serializedObject.FindProperty("redTeam");
        SerializedProperty benefitingTeamProp = serializedObject.FindProperty("benefitingTeam");
        
        SerializedProperty altState1Prop = serializedObject.FindProperty("altState1");
        SerializedProperty altState2Prop = serializedObject.FindProperty("altState2");
        
        
        if (typeProp != null)
        {
            EventType currentEventType = (EventType)typeProp.enumValueIndex;

            // TeamConditional events
            if (currentEventType == EventType.TeamBased)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Team Outcomes", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(blueTeamProp, new GUIContent("Blue Team Outcome"));
                EditorGUILayout.PropertyField(redTeamProp, new GUIContent("Red Team Outcome"));
                EditorGUILayout.PropertyField(benefitingTeamProp, new GUIContent("Beneficial to team"));
                
            }
            else if (currentEventType == EventType.AlternativeStates)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Alternative States", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(altState1Prop, new GUIContent("Alternative State 1"));
                EditorGUILayout.PropertyField(altState2Prop, new GUIContent("Alternative State 2"));
            }
                
            // Only show SubType for relevant event types
            if (IsConditioningRelevant(currentEventType))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Subtype Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(conditionProp, new GUIContent("Event Conditions"));

                if (conditionProp != null)
                {
                    EventConditions currentCondition = (EventConditions)conditionProp.enumValueIndex;

                    // Show institution condition if applicable
                    if (currentCondition == EventConditions.IfOwnsInstitution || 
                        currentCondition == EventConditions.IfInstitutionCaptured)
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
    private bool IsConditioningRelevant(EventType eventType)
    {
        switch (eventType)
        {
            case EventType.ExtraRoll:
            case EventType.Challenge:
                return true;
            // Add more cases as you add subtype support for other types
            default:
                return false;
        }
    }
}
