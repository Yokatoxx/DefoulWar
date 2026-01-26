using UnityEngine;
using UnityEditor;

namespace FPS
{
    [CustomEditor(typeof(WeaponRenderSetup))]
    public class WeaponRenderSetupEditor : Editor
    {
        private const string DEFAULT_LAYER_NAME = "Weapon";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            var setup = (WeaponRenderSetup)target;
            
            // Vérifier si le layer existe
            int layer = LayerMask.NameToLayer(DEFAULT_LAYER_NAME);
            
            if (layer == -1)
            {
                EditorGUILayout.HelpBox(
                    $"Le layer '{DEFAULT_LAYER_NAME}' n'existe pas encore.\n" +
                    "Cliquez sur le bouton ci-dessous pour le créer automatiquement.",
                    MessageType.Warning);

                if (GUILayout.Button("Créer le Layer 'Weapon'", GUILayout.Height(30)))
                {
                    CreateWeaponLayer();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Layer '{DEFAULT_LAYER_NAME}' trouvé (index: {layer}).\n" +
                    "Le setup est prêt à être utilisé.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Forcer le Setup", GUILayout.Height(25)))
            {
                setup.Setup();
                EditorUtility.SetDirty(setup);
            }
        }

        private void CreateWeaponLayer()
        {
            // Trouver un slot de layer vide (layers 8-31 sont utilisables)
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            
            SerializedProperty layers = tagManager.FindProperty("layers");

            // Chercher un slot vide entre 8 et 31
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty layerSlot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSlot.stringValue))
                {
                    layerSlot.stringValue = DEFAULT_LAYER_NAME;
                    tagManager.ApplyModifiedProperties();
                    
                    Debug.Log($"[WeaponRenderSetupEditor] Layer '{DEFAULT_LAYER_NAME}' créé à l'index {i}");
                    
                    // Forcer le refresh
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // Relancer le setup
                    var setup = (WeaponRenderSetup)target;
                    EditorApplication.delayCall += () => setup.Setup();
                    
                    return;
                }
            }

            Debug.LogError("[WeaponRenderSetupEditor] Impossible de créer le layer : tous les slots sont utilisés !");
        }
    }
}
