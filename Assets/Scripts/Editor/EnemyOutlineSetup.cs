using UnityEngine;
using UnityEditor;
using FPS;
using EPOOutline;

namespace Utils.Editor
{
    /// <summary>
    /// Outil d'éditeur pour ajouter automatiquement les composants Outlinable et TargetOutline
    /// aux prefabs ennemis sélectionnés. Utilise Easy Performant Outline.
    /// </summary>
    public class EnemyOutlineSetup : EditorWindow
    {
        private Color outlineColor = Color.yellow;
        private float dilateShift = 1f;
        
        [MenuItem("Tools/Enemy Outline Setup")]
        public static void ShowWindow()
        {
            GetWindow<EnemyOutlineSetup>("Enemy Outline Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Configuration de l'Outline pour les ennemis", EditorStyles.boldLabel);
            GUILayout.Label("Utilise Easy Performant Outline", EditorStyles.miniLabel);
            
            EditorGUILayout.Space();
            
            outlineColor = EditorGUILayout.ColorField("Couleur de l'Outline", outlineColor);
            dilateShift = EditorGUILayout.Slider("Dilate Shift", dilateShift, 0f, 1f);
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Sélectionnez des prefabs ennemis dans le Project, puis cliquez sur le bouton ci-dessous.", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Ajouter Outlinable + TargetOutline aux prefabs sélectionnés"))
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
            
            string prefabAssetPath = prefabPath;
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabAssetPath);
            
            bool modified = false;
            
            // Ajouter Outlinable (Easy Performant Outline) s'il n'existe pas
            Outlinable outlinable = prefabInstance.GetComponent<Outlinable>();
            if (outlinable == null)
            {
                outlinable = prefabInstance.AddComponent<Outlinable>();
                outlinable.OutlineParameters.Color = outlineColor;
                outlinable.OutlineParameters.DilateShift = dilateShift;
                outlinable.OutlineParameters.Enabled = false;
                
                // Ajouter tous les renderers enfants
                outlinable.AddAllChildRenderersToRenderingList(RenderersAddingMode.MeshRenderer | RenderersAddingMode.SkinnedMeshRenderer);
                
                modified = true;
                Debug.Log($"[EnemyOutlineSetup] Outlinable (EPO) ajouté à {prefab.name}");
            }
            else
            {
                Debug.Log($"[EnemyOutlineSetup] {prefab.name} a déjà un Outlinable.");
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
            
            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabAssetPath);
            }
            
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            return modified;
        }
    }
}
