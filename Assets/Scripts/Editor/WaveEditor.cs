using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor pour Wave qui utilise IMGUI au lieu de UIToolkit
/// pour éviter les bugs UIElements de Unity 6
/// </summary>
[CustomEditor(typeof(Wave))]
public class WaveEditor : Editor
{
    SerializedProperty enemySpawnList;
    SerializedProperty timeBeforeThisWave;

    void OnEnable()
    {
        enemySpawnList = serializedObject.FindProperty("EnemySpawnList");
        timeBeforeThisWave = serializedObject.FindProperty("TimeBeforeThisWave");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Wave wave = (Wave)target;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Configuration de la Vague", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Timing
        EditorGUILayout.PropertyField(timeBeforeThisWave, new GUIContent("Délai avant spawn (s)"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Ennemis dans cette vague", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Liste des ennemis avec leur nombre
        EditorGUILayout.PropertyField(enemySpawnList, new GUIContent("Types d'ennemis"), true);

        EditorGUILayout.Space(10);
        
        // Affichage du total calculé
        EditorGUILayout.HelpBox($"Total d'ennemis: {wave.TotalEnemyCount}", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
