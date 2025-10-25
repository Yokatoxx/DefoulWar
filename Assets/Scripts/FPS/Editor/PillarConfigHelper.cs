using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

namespace Proto3GD.FPS.Editor
{
    /// <summary>
    /// Outil pour configurer automatiquement les piliers avec tous les composants nécessaires
    /// </summary>
    public static class PillarConfigHelper
    {
        [MenuItem("Tools/FPS System/Configure Pillar for Highlight", false, 103)]
        private static void ConfigurePillarForHighlight()
        {
            GameObject selectedObject = Selection.activeGameObject;
            
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Aucun objet sélectionné",
                    "Veuillez sélectionner un pilier dans la hiérarchie ou la scène.",
                    "OK"
                );
                return;
            }
            
            // Vérifier/ajouter PillarController
            PillarController controller = selectedObject.GetComponent<PillarController>();
            if (controller == null)
            {
                controller = selectedObject.AddComponent<PillarController>();
                Debug.Log($"PillarController ajouté à {selectedObject.name}");
            }
            
            // Vérifier qu'il y a un Renderer
            Renderer renderer = selectedObject.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog(
                    "Pas de Renderer trouvé",
                    "Le pilier doit avoir un MeshRenderer ou un composant de rendu pour le highlight.\n\nAjoutez un mesh 3D (cube, cylindre, etc.) en enfant de cet objet.",
                    "OK"
                );
                return;
            }
            
            // Vérifier qu'il y a un Collider
            Collider collider = selectedObject.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                // Ajouter un collider automatiquement
                GameObject visualChild = renderer.gameObject;
                
                MeshFilter meshFilter = visualChild.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    MeshCollider meshCollider = visualChild.AddComponent<MeshCollider>();
                    meshCollider.convex = true;
                    Debug.Log($"MeshCollider ajouté à {visualChild.name}");
                }
                else
                {
                    BoxCollider boxCollider = visualChild.AddComponent<BoxCollider>();
                    Debug.Log($"BoxCollider ajouté à {visualChild.name}");
                }
            }
            
            // Configurer le tag et layer si nécessaire
            if (selectedObject.tag == "Untagged")
            {
                selectedObject.tag = "Untagged"; // Garder le tag par défaut ou créer "Pillar"
            }
            
            // Marquer comme modifié
            EditorUtility.SetDirty(selectedObject);
            
            EditorUtility.DisplayDialog(
                "Configuration réussie !",
                $"Le pilier '{selectedObject.name}' est maintenant configuré pour :\n\n" +
                $"✓ PillarController\n" +
                $"✓ Renderer (pour le highlight)\n" +
                $"✓ Collider (pour la détection)\n\n" +
                $"Le highlight devrait maintenant fonctionner !",
                "Super !"
            );
            
            Debug.Log($"✓ Pilier '{selectedObject.name}' configuré avec succès pour le système de highlight");
        }
        
        [MenuItem("Tools/FPS System/Configure All Pillars in Scene", false, 104)]
        private static void ConfigureAllPillarsInScene()
        {
            PillarController[] pillars = Object.FindObjectsByType<PillarController>(FindObjectsSortMode.None);
            
            if (pillars.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Aucun pilier trouvé",
                    "Aucun objet avec PillarController n'a été trouvé dans la scène.",
                    "OK"
                );
                return;
            }
            
            int configured = 0;
            
            foreach (PillarController pillar in pillars)
            {
                GameObject obj = pillar.gameObject;
                
                // Vérifier le Renderer
                Renderer renderer = obj.GetComponentInChildren<Renderer>();
                
                // Vérifier le Collider
                Collider collider = obj.GetComponentInChildren<Collider>();
                if (collider == null && renderer != null)
                {
                    GameObject visualChild = renderer.gameObject;
                    BoxCollider boxCollider = visualChild.AddComponent<BoxCollider>();
                    configured++;
                    Debug.Log($"Collider ajouté à {obj.name}");
                }
                
                EditorUtility.SetDirty(obj);
            }
            
            EditorUtility.DisplayDialog(
                "Configuration terminée",
                $"{pillars.Length} pilier(s) trouvé(s)\n" +
                $"{configured} pilier(s) configuré(s)\n\n" +
                $"Tous les piliers sont prêts pour le système de highlight !",
                "OK"
            );
            
            Debug.Log($"✓ {pillars.Length} piliers configurés dans la scène");
        }
        
        [MenuItem("Tools/FPS System/Add Outline to All Pillars", false, 106)]
        private static void AddOutlineToAllPillars()
        {
            PillarController[] pillars = Object.FindObjectsByType<PillarController>(FindObjectsSortMode.None);
            
            if (pillars.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Aucun pilier trouvé",
                    "Aucun objet avec PillarController n'a été trouvé dans la scène.",
                    "OK"
                );
                return;
            }
            
            int configured = 0;
            
            foreach (PillarController pillar in pillars)
            {
                GameObject obj = pillar.gameObject;
                
                // Vérifier si Outline existe déjà
                Outline outline = obj.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = obj.AddComponent<Outline>();
                    outline.OutlineMode = Outline.Mode.OutlineAll;
                    outline.OutlineColor = new Color(1f, 0.5f, 0f, 1f); // Orange par défaut
                    outline.OutlineWidth = 3f;
                    outline.enabled = false; // Désactivé par défaut
                    
                    configured++;
                    Debug.Log($"Composant Outline ajouté à {obj.name}");
                }
                
                EditorUtility.SetDirty(obj);
            }
            
            EditorUtility.DisplayDialog(
                "Configuration terminée",
                $"{pillars.Length} pilier(s) trouvé(s)\n" +
                $"{configured} pilier(s) configuré(s) avec Outline\n\n" +
                $"Le système de highlight fonctionne maintenant !",
                "OK"
            );
            
            Debug.Log($"✓ Composant Outline ajouté à {configured} piliers");
        }
        
        [MenuItem("Tools/FPS System/Configure NavMesh for All Pillars", false, 107)]
        private static void ConfigureNavMeshForAllPillars()
        {
            PillarController[] pillars = Object.FindObjectsByType<PillarController>(FindObjectsSortMode.None);
            
            if (pillars.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Aucun pilier trouvé",
                    "Aucun objet avec PillarController n'a été trouvé dans la scène.",
                    "OK"
                );
                return;
            }
            
            int configured = 0;
            
            foreach (PillarController pillar in pillars)
            {
                GameObject obj = pillar.gameObject;
                
                // Vérifier si NavMeshObstacle existe déjà
                NavMeshObstacle obstacle = obj.GetComponent<NavMeshObstacle>();
                if (obstacle == null)
                {
                    obstacle = obj.AddComponent<NavMeshObstacle>();
                    obstacle.carving = true;
                    obstacle.shape = NavMeshObstacleShape.Box;
                    
                    // Calculer la taille du collider
                    Collider col = obj.GetComponent<Collider>();
                    if (col != null)
                    {
                        if (col is BoxCollider boxCol)
                        {
                            obstacle.center = boxCol.center;
                            obstacle.size = boxCol.size;
                        }
                        else if (col is CapsuleCollider capsuleCol)
                        {
                            obstacle.center = capsuleCol.center;
                            obstacle.size = new Vector3(capsuleCol.radius * 2f, capsuleCol.height, capsuleCol.radius * 2f);
                            obstacle.shape = NavMeshObstacleShape.Capsule;
                        }
                        else
                        {
                            Bounds bounds = col.bounds;
                            obstacle.center = obj.transform.InverseTransformPoint(bounds.center);
                            obstacle.size = bounds.size;
                        }
                    }
                    else
                    {
                        obstacle.size = Vector3.one;
                        obstacle.center = Vector3.zero;
                    }
                    
                    configured++;
                    Debug.Log($"NavMeshObstacle ajouté à {obj.name}");
                }
                else
                {
                    // Mettre à jour un obstacle existant
                    obstacle.carving = true;
                    Debug.Log($"NavMeshObstacle mis à jour sur {obj.name}");
                }
                
                EditorUtility.SetDirty(obj);
            }
            
            EditorUtility.DisplayDialog(
                "Configuration NavMesh terminée",
                $"{pillars.Length} pilier(s) trouvé(s)\n" +
                $"{configured} pilier(s) configuré(s) avec NavMeshObstacle\n\n" +
                $"Les piliers seront maintenant des obstacles pour le NavMesh !\n\n" +
                $"Note : Si vous utilisez un NavMesh statique (baked), vous devrez le rebaker.",
                "OK"
            );
            
            Debug.Log($"✓ NavMeshObstacle configuré sur {configured} piliers");
        }
        
        [MenuItem("Tools/FPS System/Test Pillar Detection", false, 105)]
        private static void TestPillarDetection()
        {
            TestPillarDetectionFromScript();
        }
        
        // Méthodes publiques pour être appelées depuis d'autres scripts Editor
        public static void TestPillarDetectionFromScript()
        {
            PillarDashSystem dashSystem = Object.FindFirstObjectByType<PillarDashSystem>();
            
            if (dashSystem == null)
            {
                EditorUtility.DisplayDialog(
                    "PillarDashSystem introuvable",
                    "Aucun PillarDashSystem trouvé dans la scène.\n\n" +
                    "Utilisez 'Tools → FPS System → Setup Pillar Dash System' pour en créer un.",
                    "OK"
                );
                return;
            }
            
            PillarController[] pillars = Object.FindObjectsByType<PillarController>(FindObjectsSortMode.None);
            
            string report = $"=== RAPPORT DE DÉTECTION DES PILIERS ===\n\n";
            report += $"PillarDashSystem : ✓ Trouvé\n";
            report += $"Piliers dans la scène : {pillars.Length}\n\n";
            
            int validPillars = 0;
            
            foreach (PillarController pillar in pillars)
            {
                GameObject obj = pillar.gameObject;
                Renderer renderer = obj.GetComponentInChildren<Renderer>();
                Collider collider = obj.GetComponentInChildren<Collider>();
                
                bool isValid = renderer != null && collider != null;
                
                report += $"• {obj.name}\n";
                report += $"  Renderer: {(renderer != null ? "✓" : "✗ MANQUANT")}\n";
                report += $"  Collider: {(collider != null ? "✓" : "✗ MANQUANT")}\n";
                report += $"  État: {(isValid ? "✓ VALIDE" : "✗ INVALIDE")}\n\n";
                
                if (isValid) validPillars++;
            }
            
            report += $"\n=== RÉSUMÉ ===\n";
            report += $"Piliers valides : {validPillars}/{pillars.Length}\n";
            
            if (validPillars < pillars.Length)
            {
                report += $"\n⚠️ Utilisez 'Configure All Pillars in Scene' pour corriger les problèmes.";
            }
            
            Debug.Log(report);
            
            EditorUtility.DisplayDialog(
                "Test de détection",
                $"Piliers valides : {validPillars}/{pillars.Length}\n\n" +
                $"Consultez la Console pour le rapport complet.",
                "OK"
            );
        }
        
        public static void ConfigureAllPillarsFromScript()
        {
            PillarController[] pillars = Object.FindObjectsByType<PillarController>(FindObjectsSortMode.None);
            
            if (pillars.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Aucun pilier trouvé",
                    "Aucun objet avec PillarController n'a été trouvé dans la scène.",
                    "OK"
                );
                return;
            }
            
            int configured = 0;
            
            foreach (PillarController pillar in pillars)
            {
                GameObject obj = pillar.gameObject;
                
                // Vérifier le Renderer
                Renderer renderer = obj.GetComponentInChildren<Renderer>();
                
                // Vérifier le Collider
                Collider collider = obj.GetComponentInChildren<Collider>();
                if (collider == null && renderer != null)
                {
                    GameObject visualChild = renderer.gameObject;
                    BoxCollider boxCollider = visualChild.AddComponent<BoxCollider>();
                    configured++;
                    Debug.Log($"Collider ajouté à {obj.name}");
                }
                
                EditorUtility.SetDirty(obj);
            }
            
            EditorUtility.DisplayDialog(
                "Configuration terminée",
                $"{pillars.Length} pilier(s) trouvé(s)\n" +
                $"{configured} pilier(s) configuré(s)\n\n" +
                $"Tous les piliers sont prêts pour le système de highlight !",
                "OK"
            );
            
            Debug.Log($"✓ {pillars.Length} piliers configurés dans la scène");
        }
    }
}