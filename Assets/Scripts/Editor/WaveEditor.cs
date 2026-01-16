using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor pour Wave qui utilise IMGUI au lieu de UIToolkit
/// pour éviter les bugs UIElements de Unity 6
/// </summary>
[CustomEditor(typeof(Wave))]
public class WaveEditor : Editor
{
    SerializedProperty enemiesInWave;
    SerializedProperty timeBeforeThisWave;
    SerializedProperty numberToSpawn;

    void OnEnable()
    {
        enemiesInWave = serializedObject.FindProperty("EnemiesInWave");
        timeBeforeThisWave = serializedObject.FindProperty("TimeBeforeThisWave");
        numberToSpawn = serializedObject.FindProperty("NumberToSpawn");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Configuration de la Vague", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Timing
        EditorGUILayout.PropertyField(timeBeforeThisWave, new GUIContent("Délai avant spawn (s)"));
        
        // Nombre à spawn
        EditorGUILayout.PropertyField(numberToSpawn, new GUIContent("Nombre à spawner"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Ennemis dans cette vague", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Liste des ennemis avec affichage amélioré
        EditorGUILayout.PropertyField(enemiesInWave, new GUIContent("Prefabs Ennemis"), true);

        serializedObject.ApplyModifiedProperties();
    }
}
