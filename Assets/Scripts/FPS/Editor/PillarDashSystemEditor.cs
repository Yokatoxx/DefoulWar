using UnityEngine;
using UnityEditor;

namespace Proto3GD.FPS.Editor
{
    /// <summary>
    /// Éditeur personnalisé pour PillarDashSystem avec prévisualisation de couleur
    /// </summary>
    [CustomEditor(typeof(PillarDashSystem))]
    public class PillarDashSystemEditor : UnityEditor.Editor
    {
        private bool showDetectionSettings = true;
        private bool showDashSettings = true;
        private bool showFOVSettings = true;
        private bool showVisualSettings = true;
        private bool showReferences = true;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Pillar Dash System", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Detection Settings
            showDetectionSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDetectionSettings, "Détection");
            if (showDetectionSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("detectionRange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("detectionRadius"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // Dash Settings
            showDashSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDashSettings, "Dash");
            if (showDashSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dashSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dashDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dashCooldown"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // FOV Settings
            showFOVSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showFOVSettings, "FOV");
            if (showFOVSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dashFOV"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fovTransitionSpeed"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // Visual Feedback simplifié
            showVisualSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showVisualSettings, "Outline du Highlight");
            if (showVisualSettings)
            {
                EditorGUI.indentLevel++;
                
                // Couleur simple
                EditorGUILayout.PropertyField(serializedObject.FindProperty("highlightColor"), new GUIContent("Couleur"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("outlineWidth"), new GUIContent("Épaisseur"));
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // References
            showReferences = EditorGUILayout.BeginFoldoutHeaderGroup(showReferences, "Références");
            if (showReferences)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playerController"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraTransform"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            EditorGUILayout.Space(10);
            
            // Boutons d'aide compacts
            if (GUILayout.Button("🔍 Tester Détection", GUILayout.Height(25)))
            {
                PillarConfigHelper.TestPillarDetectionFromScript();
            }
            
            if (GUILayout.Button("⚙️ Configurer Piliers", GUILayout.Height(25)))
            {
                PillarConfigHelper.ConfigureAllPillarsFromScript();
            }
            
            if (GUILayout.Button("✨ Ajouter Outline aux Piliers", GUILayout.Height(25)))
            {
                // Appeler directement la méthode du menu
                var method = typeof(PillarConfigHelper).GetMethod("AddOutlineToAllPillars", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                }
            }
            
            if (GUILayout.Button("🧭 Configurer NavMesh pour Piliers", GUILayout.Height(25)))
            {
                // Appeler directement la méthode du menu
                var method = typeof(PillarConfigHelper).GetMethod("ConfigureNavMeshForAllPillars", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
