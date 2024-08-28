using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Simulator))]
public class SimulatorEditor : Editor
{
    private bool showSimulationSettings = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Simulator simulator = (Simulator)target;

        EditorGUILayout.Space();

        showSimulationSettings = EditorGUILayout.Foldout(showSimulationSettings, "Simulation Settings", true);

        if (showSimulationSettings)
        {
            EditorGUI.indentLevel++;

            if (simulator.simulationSettings != null)
            {
                SerializedObject settingsObject = new SerializedObject(simulator.simulationSettings);
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