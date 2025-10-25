using UnityEngine;
using UnityEditor;

namespace Proto3GD.FPS.Editor
{
    /// <summary>
    /// Outils Unity Editor pour créer rapidement des piliers et configurer le système de spawn.
    /// </summary>
    public static class PillarSetupEditor
    {
        [MenuItem("GameObject/3D Object/Pillar", false, 10)]
        private static void CreatePillar(MenuCommand menuCommand)
        {
            // Créer un GameObject pour le pilier
            GameObject pillar = new GameObject("Pillar");
            
            // Créer la géométrie du pilier (un cylindre par défaut)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(pillar.transform);
            visual.transform.localPosition = new Vector3(0, 1, 0);
            visual.transform.localScale = new Vector3(1, 1, 1);
            
            // Ajouter le composant PillarController
            PillarController controller = pillar.AddComponent<PillarController>();
            
            // Configurer la position dans la hiérarchie
            GameObjectUtility.SetParentAndAlign(pillar, menuCommand.context as GameObject);
            
            // Enregistrer l'action pour Undo
            Undo.RegisterCreatedObjectUndo(pillar, "Create Pillar");
            
            // Sélectionner le nouvel objet
            Selection.activeObject = pillar;
            
            Debug.Log("Pilier créé ! Configurez les paramètres dans l'inspecteur et créez un prefab.");
        }
        
        [MenuItem("GameObject/FPS System/Pillar Spawner", false, 10)]
        private static void CreatePillarSpawner(MenuCommand menuCommand)
        {
            // Créer le GameObject pour le spawner
            GameObject spawnerObj = new GameObject("Pillar Spawner");
            
            // Ajouter le composant PillarSpawner
            PillarSpawner spawner = spawnerObj.AddComponent<PillarSpawner>();
            
            // Configurer la position dans la hiérarchie
            GameObjectUtility.SetParentAndAlign(spawnerObj, menuCommand.context as GameObject);
            
            // Enregistrer l'action pour Undo
            Undo.RegisterCreatedObjectUndo(spawnerObj, "Create Pillar Spawner");
            
            // Sélectionner le nouvel objet
            Selection.activeObject = spawnerObj;
            
            Debug.Log("PillarSpawner créé ! Assignez un prefab de pilier dans l'inspecteur et configurez les angles de rotation.");
        }
        
        [MenuItem("Tools/FPS System/Setup Pillar System", false, 100)]
        private static void SetupPillarSystem()
        {
            // Vérifier si un spawner existe déjà
            PillarSpawner existingSpawner = Object.FindFirstObjectByType<PillarSpawner>();
            if (existingSpawner != null)
            {
                Debug.LogWarning("Un PillarSpawner existe déjà dans la scène.");
                Selection.activeObject = existingSpawner.gameObject;
                return;
            }
            
            // Créer le spawner
            GameObject spawnerObj = new GameObject("Pillar Spawner");
            PillarSpawner spawner = spawnerObj.AddComponent<PillarSpawner>();
            
            // Enregistrer pour Undo
            Undo.RegisterCreatedObjectUndo(spawnerObj, "Setup Pillar System");
            
            Debug.Log("Système de piliers configuré ! N'oubliez pas d'assigner un prefab de pilier.");
            
            // Sélectionner le spawner
            Selection.activeObject = spawnerObj;
            EditorGUIUtility.PingObject(spawnerObj);
        }
        
        [MenuItem("Tools/FPS System/Setup Pillar Dash System", false, 101)]
        private static void SetupPillarDashSystem()
        {
            // Trouver le joueur
            FPSPlayerController player = Object.FindFirstObjectByType<FPSPlayerController>();
            if (player == null)
            {
                Debug.LogError("Aucun FPSPlayerController trouvé dans la scène ! Créez d'abord un joueur.");
                return;
            }
            
            // Vérifier si le système de dash existe déjà
            PillarDashSystem existingDash = player.GetComponent<PillarDashSystem>();
            if (existingDash != null)
            {
                Debug.LogWarning("Le PillarDashSystem existe déjà sur le joueur.");
                Selection.activeObject = player.gameObject;
                return;
            }
            
            // Ajouter le composant
            PillarDashSystem dashSystem = player.gameObject.AddComponent<PillarDashSystem>();
            
            // Enregistrer pour Undo
            Undo.RegisterCreatedObjectUndo(dashSystem, "Setup Pillar Dash System");
            
            Debug.Log("Système de dash sur piliers configuré ! Appuyez sur E pour dasher sur un pilier ciblé.");
            
            // Sélectionner le joueur
            Selection.activeObject = player.gameObject;
            EditorGUIUtility.PingObject(player.gameObject);
        }
        
        [MenuItem("Tools/FPS System/Create Pillar Layer", false, 102)]
        private static void CreatePillarLayer()
        {
            // Ouvrir les paramètres de tags et layers
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );
            
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            
            // Chercher un slot de layer libre
            bool layerAdded = false;
            for (int i = 8; i < 32; i++) // Les layers 0-7 sont réservés
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    layerProp.stringValue = "Pillar";
                    tagManager.ApplyModifiedProperties();
                    layerAdded = true;
                    Debug.Log($"Layer 'Pillar' créé sur le slot {i}.");
                    break;
                }
                else if (layerProp.stringValue == "Pillar")
                {
                    Debug.Log($"Layer 'Pillar' existe déjà sur le slot {i}.");
                    return;
                }
            }
            
            if (!layerAdded)
            {
                Debug.LogWarning("Tous les slots de layers sont utilisés. Créez manuellement un layer 'Pillar'.");
            }
        }
    }
}
