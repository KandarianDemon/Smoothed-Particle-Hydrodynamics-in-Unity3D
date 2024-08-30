using UnityEngine;
using UnityEditor;
using Simulation;

[CustomEditor(typeof(SimulationObject))]
public class SimulationObjectEditor : Editor
{
    private bool showObjectSettings = true;
    

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SimulationObject obj = (SimulationObject)target;

        EditorGUILayout.Space();

        showObjectSettings = EditorGUILayout.Foldout(showObjectSettings, "Object Settings", true);

        if (showObjectSettings)
        {
            EditorGUI.indentLevel++;

            if (obj.settings != null)
            {
                SerializedObject settingsObject = new SerializedObject(obj.settings);
                SerializedProperty property = settingsObject.GetIterator();
                property.NextVisible(true);

                while (property.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(property, true);
                }

                settingsObject.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("No Simulation Settings assigned", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

     
    }
}
