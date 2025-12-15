using UnityEngine;
using UnityEditor;
using FPS;

namespace Utils.Editor
{
    /// <summary>
    /// Outil d'éditeur pour ajouter automatiquement les composants Outline et TargetOutline
    /// aux prefabs ennemis sélectionnés.
    /// </summary>
    public class EnemyOutlineSetup : EditorWindow
    {
        private Color outlineColor = Color.yellow;
        private float outlineWidth = 2f;
        private Outline.Mode outlineMode = Outline.Mode.OutlineAll;
        
        [MenuItem("Tools/Enemy Outline Setup")]
        public static void ShowWindow()
        {
            GetWindow<EnemyOutlineSetup>("Enemy Outline Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Configuration de l'Outline pour les ennemis", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            outlineColor = EditorGUILayout.ColorField("Couleur de l'Outline", outlineColor);
            outlineWidth = EditorGUILayout.Slider("Épaisseur de l'Outline", outlineWidth, 0f, 10f);
            outlineMode = (Outline.Mode)EditorGUILayout.EnumPopup("Mode de l'Outline", outlineMode);
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Sélectionnez des prefabs ennemis dans le Project, puis cliquez sur le bouton ci-dessous.", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Ajouter Outline + TargetOutline aux prefabs sélectionnés"))
            {
                AddOutlineToSelectedPrefabs();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Ajouter aux prefabs dans Assets/Prefabs/EnemyPrefab/"))
            {
                AddOutlineToAllEnemyPrefabs();
            }
        }
        
        private void AddOutlineToSelectedPrefabs()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            
            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Aucune sélection", "Veuillez sélectionner des prefabs ennemis dans le Project.", "OK");
                return;
            }
            
            int count = 0;
            foreach (GameObject obj in selectedObjects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
                {
                    Debug.LogWarning($"[EnemyOutlineSetup] {obj.name} n'est pas un prefab, ignoré.");
                    continue;
                }
                
                if (AddOutlineToPrefab(path))
                    count++;
            }
            
            EditorUtility.DisplayDialog("Terminé", $"Outline ajouté à {count} prefab(s).", "OK");
        }
        
        private void AddOutlineToAllEnemyPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/EnemyPrefab" });
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Aucun prefab trouvé", "Aucun prefab trouvé dans Assets/Prefabs/EnemyPrefab/", "OK");
                return;
            }
            
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AddOutlineToPrefab(path))
                    count++;
            }
            
            EditorUtility.DisplayDialog("Terminé", $"Outline ajouté à {count} prefab(s).", "OK");
        }
        
        private bool AddOutlineToPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[EnemyOutlineSetup] Impossible de charger le prefab: {prefabPath}");
                return false;
            }
            
            // Vérifier si c'est un ennemi (a un EnemyHealth)
            EnemyHealth enemyHealth = prefab.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = prefab.GetComponentInChildren<EnemyHealth>();
            }
            
            if (enemyHealth == null)
            {
                Debug.Log($"[EnemyOutlineSetup] {prefab.name} n'a pas de EnemyHealth, ignoré.");
                return false;
            }
            
            // Ouvrir le prefab pour modification
            string prefabAssetPath = prefabPath;
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabAssetPath);
            
            bool modified = false;
            
            // Ajouter Outline s'il n'existe pas
            Outline outline = prefabInstance.GetComponent<Outline>();
            if (outline == null)
            {
                outline = prefabInstance.AddComponent<Outline>();
                outline.OutlineColor = outlineColor;
                outline.OutlineWidth = outlineWidth;
                outline.OutlineMode = outlineMode;
                outline.enabled = false; // Désactivé par défaut
                modified = true;
                Debug.Log($"[EnemyOutlineSetup] Outline ajouté à {prefab.name}");
            }
            else
            {
                Debug.Log($"[EnemyOutlineSetup] {prefab.name} a déjà un Outline.");
            }
            
            // Ajouter TargetOutline s'il n'existe pas
            TargetOutline targetOutline = prefabInstance.GetComponent<TargetOutline>();
            if (targetOutline == null)
            {
                targetOutline = prefabInstance.AddComponent<TargetOutline>();
                modified = true;
                Debug.Log($"[EnemyOutlineSetup] TargetOutline ajouté à {prefab.name}");
            }
            else
            {
                Debug.Log($"[EnemyOutlineSetup] {prefab.name} a déjà un TargetOutline.");
            }
            
            // Sauvegarder les modifications
            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabAssetPath);
            }
            
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            return modified;
        }
    }
}

