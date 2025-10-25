using FPS;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;

namespace Proto3GD.FPS.Editor
{
    /// <summary>
    /// Outils Unity Editor pour configurer rapidement le système FPS (proto sans VFX/SFX).
    /// </summary>
    public static class FPSSetupEditor
    {
        [MenuItem("GameObject/FPS System/Create Player", false, 0)]
        public static void CreatePlayer()
        {
            // Créer l'objet joueur
            GameObject player = new GameObject("FPS_Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");
            
            // Ajouter CharacterController
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1f, 0);
            
            // Ajouter les composants FPS
            FPSPlayerController playerController = player.AddComponent<FPSPlayerController>();
            player.AddComponent<PlayerHealth>();
            ProjectileWeaponController weaponController = player.AddComponent<ProjectileWeaponController>();
            
            // Créer le ground check
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform);
            groundCheck.transform.localPosition = new Vector3(0, 0.1f, 0);
            
            // Créer le modèle d'arme et assigner au contrôleur d'arme
            Transform cameraTransform = playerController.CameraTransform;
            GameObject weaponModel = null;
            Transform muzzlePoint = null;
            if (cameraTransform != null)
            {
                weaponModel = WeaponModelGenerator.CreateSimpleWeaponModel(cameraTransform);
                Transform mp = weaponModel.transform.Find("MuzzlePoint");
                muzzlePoint = mp != null ? mp : weaponModel.transform;
                weaponController.SetWeaponModel(weaponModel.transform);
                weaponController.SetFirePoint(muzzlePoint);
            }
            
            // Charger ou créer le prefab de balle et l'assigner à l'arme
            const string bulletPath = "Assets/Prefabs/Bullet.prefab";
            GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
            if (bulletPrefab == null)
            {
                GameObject tmp = Bullet.CreateBulletPrefab();
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                bulletPrefab = PrefabUtility.SaveAsPrefabAsset(tmp, bulletPath);
                Object.DestroyImmediate(tmp);
            }
            
            SerializedObject so = new SerializedObject(weaponController);
            so.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            
            // Positionner le joueur
            player.transform.position = new Vector3(0, 1f, 0);
            
            // Sélectionner l'objet créé
            Selection.activeGameObject = player;
            
            Debug.Log("FPS Player (proto) créé: déplacement WASD, saut Espace, tir Clic Gauche, recharge R");
        }
        
        [MenuItem("GameObject/FPS System/Create Enemy", false, 1)]
        public static void CreateEnemy()
        {
            // Créer l'objet ennemi
            GameObject enemy = new GameObject("Enemy");
            enemy.tag = "Enemy";
            
            // Créer le corps principal
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemy.transform);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            body.layer = LayerMask.NameToLayer("Default");
            
            // Ajouter HitZone au corps
            HitZone bodyZone = body.AddComponent<HitZone>();
            bodyZone.SetZoneName("Body");
            bodyZone.SetBaseDamageMultiplier(1f);
            
            // Créer la tête
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(enemy.transform);
            head.transform.localPosition = new Vector3(0, 1.8f, 0);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // Ajouter HitZone à la tête avec multiplicateur de dégâts
            HitZone headZone = head.AddComponent<HitZone>();
            headZone.SetZoneName("Head");
            headZone.SetBaseDamageMultiplier(2f);
            
            // Ajouter NavMeshAgent
            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.height = 2f;
            agent.radius = 0.5f;
            
            // Ajouter les composants ennemis
            enemy.AddComponent<EnemyHealth>();
            enemy.AddComponent<EnemyController>();
            
            // Ajouter Health Bar world-space
            GameObject hb = new GameObject("HealthBar");
            hb.transform.SetParent(enemy.transform);
            hb.transform.localPosition = Vector3.zero;
            hb.AddComponent<EnemyHealthBar>();
            
            // Positionner l'ennemi
            enemy.transform.position = new Vector3(5, 0, 5);
            
            // Sélectionner l'objet créé
            Selection.activeGameObject = enemy;
            
            Debug.Log("Ennemi créé avec succès! N'oubliez pas de bake le NavMesh.");
        }
        
        [MenuItem("GameObject/FPS System/Create Bullet Prefab", false, 2)]
        public static void CreateBulletPrefab()
        {
            GameObject bullet = Bullet.CreateBulletPrefab();
            
            // Créer le dossier Prefabs s'il n'existe pas
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            
            // Sauvegarder comme prefab
            string path = "Assets/Prefabs/Bullet.prefab";
            PrefabUtility.SaveAsPrefabAsset(bullet, path);
            
            // Sélectionner le prefab
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            // Détruire l'objet temporaire
            Object.DestroyImmediate(bullet);
            
            Debug.Log("Prefab de balle créé dans Assets/Prefabs/Bullet.prefab");
            Debug.Log("Assignez ce prefab au ProjectileWeaponController pour tirer des balles physiques!");
        }
        
        [MenuItem("GameObject/FPS System/Create Wave Manager", false, 3)]
        public static void CreateWaveManager()
        {
            // Créer le gestionnaire de vagues
            GameObject waveManager = new GameObject("WaveManager");
            waveManager.AddComponent<WaveManager>();
            
            // Créer des points de spawn par défaut
            GameObject spawnParent = new GameObject("SpawnPoints");
            spawnParent.transform.SetParent(waveManager.transform);
            
            Vector3[] spawnPositions = new Vector3[]
            {
                new Vector3(10, 0, 10),
                new Vector3(-10, 0, 10),
                new Vector3(10, 0, -10),
                new Vector3(-10, 0, -10)
            };
            
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject spawn = new GameObject($"SpawnPoint_{i + 1}");
                spawn.transform.SetParent(spawnParent.transform);
                spawn.transform.position = spawnPositions[i];
            }
            
            // Sélectionner l'objet créé
            Selection.activeGameObject = waveManager;
            
            Debug.Log("Wave Manager créé avec succès! Assignez le prefab ennemi dans l'inspecteur.");
        }
        
        [MenuItem("GameObject/FPS System/Create HUD", false, 4)]
        public static void CreateHUD()
        {
            // Canvas de base
            GameObject canvasGO = new GameObject("HUD_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Panel racine
            GameObject panel = new GameObject("HUD");
            panel.transform.SetParent(canvasGO.transform);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0);
            prt.anchorMax = new Vector2(1, 1);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            
            // Health bar (Slider + Text)
            GameObject healthSliderGO = new GameObject("HealthBar");
            healthSliderGO.transform.SetParent(panel.transform);
            Slider healthSlider = healthSliderGO.AddComponent<Slider>();
            RectTransform hrt = healthSlider.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.02f, 0.02f);
            hrt.anchorMax = new Vector2(0.3f, 0.07f);
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
            healthSlider.minValue = 0f; healthSlider.maxValue = 1f; healthSlider.value = 1f;
            
            GameObject healthTextGO = new GameObject("HealthText");
            healthTextGO.transform.SetParent(panel.transform);
            TextMeshProUGUI healthTMP = healthTextGO.AddComponent<TextMeshProUGUI>();
            RectTransform htxtRt = healthTMP.GetComponent<RectTransform>();
            htxtRt.anchorMin = new Vector2(0.31f, 0.02f);
            htxtRt.anchorMax = new Vector2(0.4f, 0.07f);
            htxtRt.offsetMin = Vector2.zero; htxtRt.offsetMax = Vector2.zero;
            healthTMP.fontSize = 24;
            healthTMP.alignment = TextAlignmentOptions.Left;
            healthTMP.text = "100/100";
            
            // Ammo text + reload
            GameObject ammoTextGO = new GameObject("AmmoText");
            ammoTextGO.transform.SetParent(panel.transform);
            TextMeshProUGUI ammoTMP = ammoTextGO.AddComponent<TextMeshProUGUI>();
            RectTransform ammoRt = ammoTMP.GetComponent<RectTransform>();
            ammoRt.anchorMin = new Vector2(0.8f, 0.02f);
            ammoRt.anchorMax = new Vector2(0.97f, 0.1f);
            ammoRt.offsetMin = Vector2.zero; ammoRt.offsetMax = Vector2.zero;
            ammoTMP.fontSize = 28;
            ammoTMP.alignment = TextAlignmentOptions.Right;
            ammoTMP.text = "30 / 30";
            
            GameObject reloadTextGO = new GameObject("ReloadText");
            reloadTextGO.transform.SetParent(panel.transform);
            TextMeshProUGUI reloadTMP = reloadTextGO.AddComponent<TextMeshProUGUI>();
            RectTransform rrt = reloadTMP.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.8f, 0.12f);
            rrt.anchorMax = new Vector2(0.97f, 0.17f);
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            reloadTMP.fontSize = 20;
            reloadTMP.alignment = TextAlignmentOptions.Right;
            reloadTMP.text = "Reloading...";
            reloadTMP.gameObject.SetActive(false);
            
            // Wave info
            GameObject waveTextGO = new GameObject("WaveText");
            waveTextGO.transform.SetParent(panel.transform);
            TextMeshProUGUI waveTMP = waveTextGO.AddComponent<TextMeshProUGUI>();
            RectTransform wrt = waveTMP.GetComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0.45f, 0.92f);
            wrt.anchorMax = new Vector2(0.55f, 0.98f);
            wrt.offsetMin = Vector2.zero; wrt.offsetMax = Vector2.zero;
            waveTMP.fontSize = 28;
            waveTMP.alignment = TextAlignmentOptions.Center;
            waveTMP.text = "Vague 1";
            
            GameObject enemiesTextGO = new GameObject("EnemiesRemainingText");
            enemiesTextGO.transform.SetParent(panel.transform);
            TextMeshProUGUI enemiesTMP = enemiesTextGO.AddComponent<TextMeshProUGUI>();
            RectTransform ert = enemiesTMP.GetComponent<RectTransform>();
            ert.anchorMin = new Vector2(0.02f, 0.92f);
            ert.anchorMax = new Vector2(0.25f, 0.98f);
            ert.offsetMin = Vector2.zero; ert.offsetMax = Vector2.zero;
            enemiesTMP.fontSize = 24;
            enemiesTMP.alignment = TextAlignmentOptions.Left;
            enemiesTMP.text = "Ennemis: 0";
            
            // Wave complete panel
            GameObject wavePanelGO = new GameObject("WaveCompletePanel");
            wavePanelGO.transform.SetParent(panel.transform);
            Image wavePanelImg = wavePanelGO.AddComponent<Image>();
            wavePanelImg.color = new Color(0, 0, 0, 0.5f);
            RectTransform wprt = wavePanelGO.GetComponent<RectTransform>();
            wprt.anchorMin = new Vector2(0.3f, 0.4f);
            wprt.anchorMax = new Vector2(0.7f, 0.6f);
            wprt.offsetMin = Vector2.zero; wprt.offsetMax = Vector2.zero;
            
            GameObject wavePanelTextGO = new GameObject("WaveCompleteText");
            wavePanelTextGO.transform.SetParent(wavePanelGO.transform);
            TextMeshProUGUI wavePanelTMP = wavePanelTextGO.AddComponent<TextMeshProUGUI>();
            RectTransform wtr = wavePanelTMP.GetComponent<RectTransform>();
            wtr.anchorMin = new Vector2(0.05f, 0.2f);
            wtr.anchorMax = new Vector2(0.95f, 0.8f);
            wtr.offsetMin = Vector2.zero; wtr.offsetMax = Vector2.zero;
            wavePanelTMP.fontSize = 28;
            wavePanelTMP.alignment = TextAlignmentOptions.Center;
            wavePanelTMP.text = "Vague Terminée";
            wavePanelGO.SetActive(false);
            
            // Crosshair simple
            GameObject crosshairGO = new GameObject("Crosshair");
            crosshairGO.transform.SetParent(panel.transform);
            Image crossImg = crosshairGO.AddComponent<Image>();
            RectTransform crt = crossImg.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(6, 6);
            crt.anchoredPosition = Vector2.zero;
            
            // Ajouter GameUI et assigner les références
            GameUI gameUI = canvasGO.AddComponent<GameUI>();
            SerializedObject so = new SerializedObject(gameUI);
            so.FindProperty("healthBar").objectReferenceValue = healthSlider;
            so.FindProperty("healthText").objectReferenceValue = healthTMP;
            so.FindProperty("ammoText").objectReferenceValue = ammoTMP;
            so.FindProperty("reloadText").objectReferenceValue = reloadTMP;
            so.FindProperty("waveNumberText").objectReferenceValue = waveTMP;
            so.FindProperty("enemiesRemainingText").objectReferenceValue = enemiesTMP;
            so.FindProperty("waveCompletePanel").objectReferenceValue = wavePanelGO;
            so.FindProperty("waveCompleteText").objectReferenceValue = wavePanelTMP;
            so.FindProperty("crosshair").objectReferenceValue = crossImg;
            so.ApplyModifiedPropertiesWithoutUndo();
            
            Selection.activeGameObject = canvasGO;
            Debug.Log("HUD Canvas créé et GameUI assigné.");
        }
        
        [MenuItem("GameObject/FPS System/Create Debug Tools", false, 5)]
        public static void CreateDebugTools()
        {
            GameObject obj = new GameObject("DebugTools");
            obj.AddComponent<FPSDebugTools>();
            Selection.activeGameObject = obj;
            Debug.Log("Debug Tools créé (F6 tuer tous les ennemis, F7 +armure, F8 reset armure).");
        }
        
        [MenuItem("GameObject/FPS System/Create Complete FPS Scene", false, 10)]
        public static void CreateCompleteFPSScene()
        {
            // Créer le sol
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(10, 1, 10);
            floor.isStatic = true;
            
            // Créer le joueur
            CreatePlayer();
            
            // Créer le prefab de balle (si pas déjà)
            CreateBulletPrefab();
            
            // Créer un ennemi exemple
            CreateEnemy();
            
            // Créer le wave manager
            CreateWaveManager();
            
            // Créer le HUD et les Debug Tools
            CreateHUD();
            CreateDebugTools();
            
            // Créer une lumière directionnelle si elle n'existe pas
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDirectionalLight = false;
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectionalLight = true;
                    break;
                }
            }
            
            if (!hasDirectionalLight)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
            
            Debug.Log("=== Scène FPS complète créée (proto) ===");
            Debug.Log("✅ Joueur avec arme projectile créé");
            Debug.Log("✅ Prefab de balle créé dans Assets/Prefabs/");
            Debug.Log("\nProchaines étapes:");
            Debug.Log("1. Bake le NavMesh (Window > AI > Navigation)");
            Debug.Log("2. Glissez l'ennemi dans Prefabs/ et assignez au Wave Manager");
            Debug.Log("3. Le joueur bouge avec WASD et tire avec Clic Gauche!");
        }
    }
}
