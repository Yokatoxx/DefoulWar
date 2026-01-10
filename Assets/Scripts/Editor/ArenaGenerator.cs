using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Outil Editor pour générer des arènes procédurales aléatoires mais logiques.
/// Accessible via Tools > Arena Generator
/// </summary>
public class ArenaGenerator : EditorWindow
{
    // === Paramètres de l'arène ===
    [Header("Dimensions")]
    private float arenaWidth = 50f;
    private float arenaLength = 50f;
    private float wallHeight = 8f;
    private float wallThickness = 1f;
    
    [Header("Obstacles")]
    private int obstacleCount = 8;
    private float minObstacleHeight = 2f;
    private float maxObstacleHeight = 4f;
    private float obstacleMinSize = 2f;
    private float obstacleMaxSize = 5f;
    private float minDistanceBetweenObstacles = 5f;
    private float centerClearRadius = 8f;
    
    [Header("Plateformes & Étages")]
    private int platformCount = 3;
    private float platformHeight = 3f;
    private float platformMinSize = 6f;
    private float platformMaxSize = 12f;
    private bool addStairs = true;
    private bool addRamps = true;
    private float stairWidth = 2f;
    
    [Header("Points de Spawn")]
    private int spawnPointCount = 4;
    private float spawnPointMargin = 3f;
    
    [Header("Options")]
    private bool addArenaSetter = true;
    private bool addDoors = false;
    private string arenaName = "GeneratedArena";
    private int randomSeed = 0;
    private bool useRandomSeed = true;
    
    // Materials (optionnel)
    private Material floorMaterial;
    private Material wallMaterial;
    private Material obstacleMaterial;
    private Material platformMaterial;
    
    // Prévisualisation
    private bool showPreview = false;
    private List<Vector3> previewObstacles = new List<Vector3>();
    private List<Vector3> previewSizes = new List<Vector3>();
    private List<PlatformData> previewPlatforms = new List<PlatformData>();
    
    // Structure pour les plateformes
    private struct PlatformData
    {
        public Vector3 position;
        public Vector3 size;
        public bool hasStairs;
        public bool hasRamp;
        public int stairDirection; // 0=Nord, 1=Est, 2=Sud, 3=Ouest
    }
    
    // Scroll
    private Vector2 scrollPosition;
    
    // État du random
    private System.Random rng;
    
    [MenuItem("Tools/Arena Generator")]
    public static void ShowWindow()
    {
        ArenaGenerator window = GetWindow<ArenaGenerator>("Arena Generator");
        window.minSize = new Vector2(380, 650);
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Titre
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        EditorGUILayout.LabelField("🏟️ Générateur d'Arène", titleStyle);
        EditorGUILayout.Space(10);
        
        // === Section Dimensions ===
        EditorGUILayout.LabelField("📐 Dimensions", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        arenaWidth = EditorGUILayout.Slider("Largeur", arenaWidth, 20f, 200f);
        arenaLength = EditorGUILayout.Slider("Longueur", arenaLength, 20f, 200f);
        wallHeight = EditorGUILayout.Slider("Hauteur des murs", wallHeight, 2f, 20f);
        wallThickness = EditorGUILayout.Slider("Épaisseur des murs", wallThickness, 0.5f, 3f);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        
        // === Section Obstacles ===
        EditorGUILayout.LabelField("🪨 Obstacles (au sol)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        obstacleCount = EditorGUILayout.IntSlider("Nombre d'obstacles", obstacleCount, 0, 30);
        minObstacleHeight = EditorGUILayout.Slider("Hauteur min", minObstacleHeight, 1f, 10f);
        maxObstacleHeight = EditorGUILayout.Slider("Hauteur max", maxObstacleHeight, minObstacleHeight, 15f);
        obstacleMinSize = EditorGUILayout.Slider("Taille min", obstacleMinSize, 1f, 10f);
        obstacleMaxSize = EditorGUILayout.Slider("Taille max", obstacleMaxSize, obstacleMinSize, 15f);
        minDistanceBetweenObstacles = EditorGUILayout.Slider("Distance minimum", minDistanceBetweenObstacles, 2f, 15f);
        centerClearRadius = EditorGUILayout.Slider("Zone libre au centre", centerClearRadius, 0f, 30f);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        
        // === Section Plateformes ===
        EditorGUILayout.LabelField("🏗️ Plateformes & Étages", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        platformCount = EditorGUILayout.IntSlider("Nombre de plateformes", platformCount, 0, 8);
        platformHeight = EditorGUILayout.Slider("Hauteur plateforme", platformHeight, 2f, 10f);
        platformMinSize = EditorGUILayout.Slider("Taille min", platformMinSize, 4f, 15f);
        platformMaxSize = EditorGUILayout.Slider("Taille max", platformMaxSize, platformMinSize, 25f);
        addStairs = EditorGUILayout.Toggle("Ajouter escaliers", addStairs);
        addRamps = EditorGUILayout.Toggle("Ajouter rampes", addRamps);
        if (addStairs || addRamps)
        {
            stairWidth = EditorGUILayout.Slider("Largeur escaliers/rampes", stairWidth, 1.5f, 5f);
        }
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        
        // === Section Spawn ===
        EditorGUILayout.LabelField("📍 Points de Spawn", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        spawnPointCount = EditorGUILayout.IntSlider("Nombre de points", spawnPointCount, 2, 12);
        spawnPointMargin = EditorGUILayout.Slider("Marge depuis les murs", spawnPointMargin, 1f, 10f);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        
        // === Section Options ===
        EditorGUILayout.LabelField("⚙️ Options", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        arenaName = EditorGUILayout.TextField("Nom de l'arène", arenaName);
        addArenaSetter = EditorGUILayout.Toggle("Ajouter ArenaSetter", addArenaSetter);
        addDoors = EditorGUILayout.Toggle("Ajouter Portes", addDoors);
        
        EditorGUILayout.Space(5);
        useRandomSeed = EditorGUILayout.Toggle("Seed aléatoire", useRandomSeed);
        if (!useRandomSeed)
        {
            randomSeed = EditorGUILayout.IntField("Seed personnalisé", randomSeed);
        }
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        
        // === Section Materials (optionnel) ===
        EditorGUILayout.LabelField("🎨 Materials (optionnel)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        floorMaterial = (Material)EditorGUILayout.ObjectField("Sol", floorMaterial, typeof(Material), false);
        wallMaterial = (Material)EditorGUILayout.ObjectField("Murs", wallMaterial, typeof(Material), false);
        obstacleMaterial = (Material)EditorGUILayout.ObjectField("Obstacles", obstacleMaterial, typeof(Material), false);
        platformMaterial = (Material)EditorGUILayout.ObjectField("Plateformes", platformMaterial, typeof(Material), false);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(20);
        
        // === Boutons ===
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🔄 Prévisualiser", GUILayout.Height(35)))
        {
            InitializeRandom();
            GeneratePreviewPositions();
            showPreview = true;
            SceneView.RepaintAll();
        }
        
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("✅ Générer l'Arène", GUILayout.Height(35)))
        {
            InitializeRandom();
            GeneratePreviewPositions();
            GenerateArena();
            showPreview = false;
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("🎲 Nouveau Seed Aléatoire", GUILayout.Height(25)))
        {
            useRandomSeed = true;
            InitializeRandom();
            GeneratePreviewPositions();
            showPreview = true;
            SceneView.RepaintAll();
        }
        
        if (showPreview)
        {
            EditorGUILayout.HelpBox($"Prévisualisation: {previewObstacles.Count} obstacles, {previewPlatforms.Count} plateformes", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// Initialise le générateur aléatoire avec un nouveau seed
    /// </summary>
    private void InitializeRandom()
    {
        if (useRandomSeed)
        {
            randomSeed = System.Environment.TickCount;
        }
        rng = new System.Random(randomSeed);
        Random.InitState(randomSeed);
        Debug.Log($"🎲 Seed utilisé: {randomSeed}");
    }
    
    /// <summary>
    /// Retourne un float aléatoire dans une plage
    /// </summary>
    private float RandomRange(float min, float max)
    {
        return (float)(rng.NextDouble() * (max - min) + min);
    }
    
    /// <summary>
    /// Retourne un int aléatoire dans une plage
    /// </summary>
    private int RandomRangeInt(int min, int max)
    {
        return rng.Next(min, max);
    }
    
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    /// <summary>
    /// Dessine la prévisualisation dans la Scene View
    /// </summary>
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPreview) return;
        
        Handles.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        
        // Sol
        Vector3[] floorVerts = new Vector3[]
        {
            new Vector3(-arenaWidth / 2, 0, -arenaLength / 2),
            new Vector3(arenaWidth / 2, 0, -arenaLength / 2),
            new Vector3(arenaWidth / 2, 0, arenaLength / 2),
            new Vector3(-arenaWidth / 2, 0, arenaLength / 2)
        };
        Handles.DrawSolidRectangleWithOutline(floorVerts, new Color(0.2f, 0.6f, 0.2f, 0.3f), Color.green);
        
        // Murs (wireframe)
        Handles.color = Color.cyan;
        float halfW = arenaWidth / 2;
        float halfL = arenaLength / 2;
        
        // Contour bas et haut
        DrawWireBox(Vector3.zero, new Vector3(arenaWidth, wallHeight, arenaLength));
        
        // Zone centrale libre
        Handles.color = new Color(1f, 1f, 0.3f, 0.2f);
        Handles.DrawSolidDisc(Vector3.zero, Vector3.up, centerClearRadius);
        
        // Obstacles prévisualisés
        Handles.color = new Color(0.8f, 0.4f, 0.1f, 0.6f);
        for (int i = 0; i < previewObstacles.Count; i++)
        {
            Vector3 pos = previewObstacles[i];
            Vector3 size = previewSizes[i];
            Handles.DrawWireCube(pos + Vector3.up * size.y / 2, size);
        }
        
        // Plateformes prévisualisées
        Handles.color = new Color(0.2f, 0.6f, 1f, 0.6f);
        foreach (var platform in previewPlatforms)
        {
            // Plateforme principale
            Handles.DrawWireCube(platform.position + Vector3.up * platform.size.y / 2, platform.size);
            
            // Escaliers/Rampes
            if (platform.hasStairs || platform.hasRamp)
            {
                Vector3 stairPos = GetStairPosition(platform);
                Vector3 stairSize = GetStairSize(platform);
                Handles.color = platform.hasRamp ? Color.yellow : Color.magenta;
                Handles.DrawWireCube(stairPos, stairSize);
                Handles.color = new Color(0.2f, 0.6f, 1f, 0.6f);
            }
        }
        
        // Points de spawn
        Handles.color = Color.magenta;
        List<Vector3> spawnPositions = CalculateSpawnPositions();
        foreach (Vector3 sp in spawnPositions)
        {
            Handles.SphereHandleCap(0, sp + Vector3.up * 0.5f, Quaternion.identity, 1f, EventType.Repaint);
        }
        
        sceneView.Repaint();
    }
    
    private void DrawWireBox(Vector3 center, Vector3 size)
    {
        float halfX = size.x / 2;
        float halfZ = size.z / 2;
        float h = size.y;
        
        // Bas
        Handles.DrawLine(center + new Vector3(-halfX, 0, -halfZ), center + new Vector3(halfX, 0, -halfZ));
        Handles.DrawLine(center + new Vector3(halfX, 0, -halfZ), center + new Vector3(halfX, 0, halfZ));
        Handles.DrawLine(center + new Vector3(halfX, 0, halfZ), center + new Vector3(-halfX, 0, halfZ));
        Handles.DrawLine(center + new Vector3(-halfX, 0, halfZ), center + new Vector3(-halfX, 0, -halfZ));
        
        // Haut
        Handles.DrawLine(center + new Vector3(-halfX, h, -halfZ), center + new Vector3(halfX, h, -halfZ));
        Handles.DrawLine(center + new Vector3(halfX, h, -halfZ), center + new Vector3(halfX, h, halfZ));
        Handles.DrawLine(center + new Vector3(halfX, h, halfZ), center + new Vector3(-halfX, h, halfZ));
        Handles.DrawLine(center + new Vector3(-halfX, h, halfZ), center + new Vector3(-halfX, h, -halfZ));
        
        // Verticaux
        Handles.DrawLine(center + new Vector3(-halfX, 0, -halfZ), center + new Vector3(-halfX, h, -halfZ));
        Handles.DrawLine(center + new Vector3(halfX, 0, -halfZ), center + new Vector3(halfX, h, -halfZ));
        Handles.DrawLine(center + new Vector3(halfX, 0, halfZ), center + new Vector3(halfX, h, halfZ));
        Handles.DrawLine(center + new Vector3(-halfX, 0, halfZ), center + new Vector3(-halfX, h, halfZ));
    }
    
    /// <summary>
    /// Génère les positions de prévisualisation
    /// </summary>
    private void GeneratePreviewPositions()
    {
        previewObstacles.Clear();
        previewSizes.Clear();
        previewPlatforms.Clear();
        
        float halfW = arenaWidth / 2 - obstacleMaxSize - 2;
        float halfL = arenaLength / 2 - obstacleMaxSize - 2;
        
        // D'abord générer les plateformes
        GeneratePlatformPositions(halfW, halfL);
        
        // Puis les obstacles en évitant les plateformes
        int maxAttempts = obstacleCount * 30;
        int attempts = 0;
        
        while (previewObstacles.Count < obstacleCount && attempts < maxAttempts)
        {
            attempts++;
            
            float x = RandomRange(-halfW, halfW);
            float z = RandomRange(-halfL, halfL);
            Vector3 pos = new Vector3(x, 0, z);
            
            // Vérifier zone centrale
            if (pos.magnitude < centerClearRadius) continue;
            
            // Vérifier collision avec plateformes
            bool hitsPlatform = false;
            foreach (var platform in previewPlatforms)
            {
                if (Mathf.Abs(pos.x - platform.position.x) < (platform.size.x / 2 + obstacleMaxSize) &&
                    Mathf.Abs(pos.z - platform.position.z) < (platform.size.z / 2 + obstacleMaxSize))
                {
                    hitsPlatform = true;
                    break;
                }
            }
            if (hitsPlatform) continue;
            
            // Vérifier distance avec autres obstacles
            bool tooClose = false;
            foreach (Vector3 existing in previewObstacles)
            {
                if (Vector3.Distance(pos, existing) < minDistanceBetweenObstacles)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;
            
            previewObstacles.Add(pos);
            
            float sizeX = RandomRange(obstacleMinSize, obstacleMaxSize);
            float sizeZ = RandomRange(obstacleMinSize, obstacleMaxSize);
            float height = RandomRange(minObstacleHeight, maxObstacleHeight);
            previewSizes.Add(new Vector3(sizeX, height, sizeZ));
        }
    }
    
    /// <summary>
    /// Génère les positions des plateformes
    /// </summary>
    private void GeneratePlatformPositions(float halfW, float halfL)
    {
        float minPlatformDist = platformMaxSize * 1.5f;
        int maxAttempts = platformCount * 30;
        int attempts = 0;
        
        while (previewPlatforms.Count < platformCount && attempts < maxAttempts)
        {
            attempts++;
            
            float x = RandomRange(-halfW + platformMaxSize/2, halfW - platformMaxSize/2);
            float z = RandomRange(-halfL + platformMaxSize/2, halfL - platformMaxSize/2);
            Vector3 pos = new Vector3(x, 0, z);
            
            // Éviter le centre
            if (pos.magnitude < centerClearRadius + platformMaxSize/2) continue;
            
            // Éviter les autres plateformes
            bool tooClose = false;
            foreach (var existing in previewPlatforms)
            {
                if (Vector3.Distance(pos, existing.position) < minPlatformDist)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;
            
            float sizeX = RandomRange(platformMinSize, platformMaxSize);
            float sizeZ = RandomRange(platformMinSize, platformMaxSize);
            
            // Varier légèrement la hauteur
            float height = platformHeight + RandomRange(-0.5f, 1f);
            
            // Décider aléatoirement escalier ou rampe
            bool hasStairs = addStairs && rng.NextDouble() > 0.3;
            bool hasRamp = addRamps && !hasStairs && rng.NextDouble() > 0.4;
            
            // Si ni l'un ni l'autre n'est possible, forcer un des deux
            if (!hasStairs && !hasRamp)
            {
                if (addStairs) hasStairs = true;
                else if (addRamps) hasRamp = true;
            }
            
            int stairDir = RandomRangeInt(0, 4);
            
            previewPlatforms.Add(new PlatformData
            {
                position = pos,
                size = new Vector3(sizeX, height, sizeZ),
                hasStairs = hasStairs,
                hasRamp = hasRamp,
                stairDirection = stairDir
            });
        }
    }
    
    private Vector3 GetStairPosition(PlatformData platform)
    {
        float stairLength = platform.size.y * 1.5f; // Longueur de la rampe/escalier
        Vector3 offset = Vector3.zero;
        
        switch (platform.stairDirection)
        {
            case 0: // Nord
                offset = new Vector3(0, platform.size.y / 2, platform.size.z / 2 + stairLength / 2);
                break;
            case 1: // Est
                offset = new Vector3(platform.size.x / 2 + stairLength / 2, platform.size.y / 2, 0);
                break;
            case 2: // Sud
                offset = new Vector3(0, platform.size.y / 2, -platform.size.z / 2 - stairLength / 2);
                break;
            case 3: // Ouest
                offset = new Vector3(-platform.size.x / 2 - stairLength / 2, platform.size.y / 2, 0);
                break;
        }
        
        return platform.position + offset;
    }
    
    private Vector3 GetStairSize(PlatformData platform)
    {
        float stairLength = platform.size.y * 1.5f;
        
        if (platform.stairDirection == 0 || platform.stairDirection == 2)
        {
            return new Vector3(stairWidth, platform.size.y, stairLength);
        }
        else
        {
            return new Vector3(stairLength, platform.size.y, stairWidth);
        }
    }
    
    /// <summary>
    /// Calcule les positions des points de spawn
    /// </summary>
    private List<Vector3> CalculateSpawnPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        float halfW = arenaWidth / 2 - spawnPointMargin;
        float halfL = arenaLength / 2 - spawnPointMargin;
        
        // Ajouter un offset aléatoire pour varier les positions
        float angleOffset = RandomRange(0, Mathf.PI * 2);
        
        for (int i = 0; i < spawnPointCount; i++)
        {
            float angle = angleOffset + (i / (float)spawnPointCount) * Mathf.PI * 2;
            float radiusFactor = 0.6f + RandomRange(0, 0.2f);
            float x = Mathf.Sin(angle) * halfW * radiusFactor;
            float z = Mathf.Cos(angle) * halfL * radiusFactor;
            positions.Add(new Vector3(x, 0, z));
        }
        
        return positions;
    }
    
    /// <summary>
    /// Génère l'arène complète
    /// </summary>
    private void GenerateArena()
    {
        // Créer le parent
        GameObject arenaRoot = new GameObject(arenaName);
        Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Arena");
        
        // Créer la structure
        CreateFloor(arenaRoot.transform);
        CreateWalls(arenaRoot.transform);
        CreateObstacles(arenaRoot.transform);
        CreatePlatforms(arenaRoot.transform);
        GameObject spawns = CreateSpawnPoints(arenaRoot.transform);
        
        // Ajouter composants Arena
        if (addArenaSetter)
        {
            ArenaSetter setter = arenaRoot.AddComponent<ArenaSetter>();
            setter.totalWaves = 1;
            setter.waves = new List<WaveSetter>();
        }
        
        // Ajouter trigger
        BoxCollider trigger = arenaRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(arenaWidth * 0.8f, wallHeight, arenaLength * 0.8f);
        trigger.center = new Vector3(0, wallHeight / 2, 0);
        arenaRoot.AddComponent<ArenaTriggerBox>();
        
        // Ajouter portes si demandé
        if (addDoors)
        {
            CreateDoors(arenaRoot.transform);
        }
        
        // Sélectionner l'arène générée
        Selection.activeGameObject = arenaRoot;
        SceneView.lastActiveSceneView?.FrameSelected();
        
        Debug.Log($"✅ Arène '{arenaName}' générée! (Seed: {randomSeed}, {previewObstacles.Count} obstacles, {previewPlatforms.Count} plateformes, {spawnPointCount} spawns)");
    }
    
    private void CreateFloor(Transform parent)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.localPosition = new Vector3(0, -0.5f, 0);
        floor.transform.localScale = new Vector3(arenaWidth, 1, arenaLength);
        floor.isStatic = true;
        
        if (floorMaterial != null)
            floor.GetComponent<Renderer>().material = floorMaterial;
    }
    
    private void CreateWalls(Transform parent)
    {
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(parent);
        
        CreateWall(wallsParent.transform, "Wall_North", new Vector3(0, wallHeight/2, arenaLength/2), new Vector3(arenaWidth, wallHeight, wallThickness));
        CreateWall(wallsParent.transform, "Wall_South", new Vector3(0, wallHeight/2, -arenaLength/2), new Vector3(arenaWidth, wallHeight, wallThickness));
        CreateWall(wallsParent.transform, "Wall_East", new Vector3(arenaWidth/2, wallHeight/2, 0), new Vector3(wallThickness, wallHeight, arenaLength));
        CreateWall(wallsParent.transform, "Wall_West", new Vector3(-arenaWidth/2, wallHeight/2, 0), new Vector3(wallThickness, wallHeight, arenaLength));
    }
    
    private void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.isStatic = true;
        
        if (wallMaterial != null)
            wall.GetComponent<Renderer>().material = wallMaterial;
    }
    
    private void CreateObstacles(Transform parent)
    {
        GameObject obstaclesParent = new GameObject("Obstacles");
        obstaclesParent.transform.SetParent(parent);
        
        for (int i = 0; i < previewObstacles.Count; i++)
        {
            Vector3 pos = previewObstacles[i];
            Vector3 size = previewSizes[i];
            
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_{i+1}";
            obstacle.transform.SetParent(obstaclesParent.transform);
            obstacle.transform.localPosition = pos + Vector3.up * size.y / 2;
            obstacle.transform.localScale = size;
            obstacle.isStatic = true;
            
            if (obstacleMaterial != null)
                obstacle.GetComponent<Renderer>().material = obstacleMaterial;
        }
    }
    
    private void CreatePlatforms(Transform parent)
    {
        GameObject platformsParent = new GameObject("Platforms");
        platformsParent.transform.SetParent(parent);
        
        for (int i = 0; i < previewPlatforms.Count; i++)
        {
            var platform = previewPlatforms[i];
            
            GameObject platformObj = new GameObject($"Platform_{i+1}");
            platformObj.transform.SetParent(platformsParent.transform);
            platformObj.transform.localPosition = platform.position;
            
            // Surface de la plateforme
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "Surface";
            surface.transform.SetParent(platformObj.transform);
            surface.transform.localPosition = new Vector3(0, platform.size.y, 0);
            surface.transform.localScale = new Vector3(platform.size.x, 0.5f, platform.size.z);
            surface.isStatic = true;
            
            if (platformMaterial != null)
                surface.GetComponent<Renderer>().material = platformMaterial;
            
            // Piliers de support
            CreatePlatformSupports(platformObj.transform, platform);
            
            // Escalier ou rampe
            if (platform.hasStairs)
            {
                CreateStairs(platformObj.transform, platform);
            }
            else if (platform.hasRamp)
            {
                CreateRamp(platformObj.transform, platform);
            }
        }
    }
    
    private void CreatePlatformSupports(Transform parent, PlatformData platform)
    {
        float pillarSize = 0.8f;
        float offsetX = platform.size.x / 2 - pillarSize;
        float offsetZ = platform.size.z / 2 - pillarSize;
        
        Vector3[] pillarPositions = new Vector3[]
        {
            new Vector3(-offsetX, platform.size.y / 2, -offsetZ),
            new Vector3(offsetX, platform.size.y / 2, -offsetZ),
            new Vector3(-offsetX, platform.size.y / 2, offsetZ),
            new Vector3(offsetX, platform.size.y / 2, offsetZ)
        };
        
        for (int i = 0; i < pillarPositions.Length; i++)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = $"Support_{i+1}";
            pillar.transform.SetParent(parent);
            pillar.transform.localPosition = pillarPositions[i];
            pillar.transform.localScale = new Vector3(pillarSize, platform.size.y, pillarSize);
            pillar.isStatic = true;
            
            if (obstacleMaterial != null)
                pillar.GetComponent<Renderer>().material = obstacleMaterial;
        }
    }
    
    private void CreateStairs(Transform parent, PlatformData platform)
    {
        int stepCount = Mathf.CeilToInt(platform.size.y / 0.3f);
        float stepHeight = platform.size.y / stepCount;
        float stepDepth = 0.4f;
        float stairLength = stepDepth * stepCount;
        
        GameObject stairsParent = new GameObject("Stairs");
        stairsParent.transform.SetParent(parent);
        
        Vector3 stairDirection = GetStairDirectionVector(platform.stairDirection);
        Vector3 basePos = stairDirection * (GetPlatformEdgeDistance(platform) + stairLength / 2);
        stairsParent.transform.localPosition = basePos;
        
        // Rotation selon la direction
        float rotation = platform.stairDirection * 90f;
        stairsParent.transform.localRotation = Quaternion.Euler(0, rotation, 0);
        
        for (int i = 0; i < stepCount; i++)
        {
            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"Step_{i+1}";
            step.transform.SetParent(stairsParent.transform);
            
            float yPos = stepHeight * (i + 0.5f);
            float zPos = -stairLength / 2 + stepDepth * (i + 0.5f);
            
            step.transform.localPosition = new Vector3(0, yPos, zPos);
            step.transform.localScale = new Vector3(stairWidth, stepHeight, stepDepth);
            step.isStatic = true;
            
            if (platformMaterial != null)
                step.GetComponent<Renderer>().material = platformMaterial;
        }
    }
    
    private void CreateRamp(Transform parent, PlatformData platform)
    {
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp";
        ramp.transform.SetParent(parent);
        
        float rampLength = platform.size.y * 2f; // Rampe douce
        Vector3 stairDirection = GetStairDirectionVector(platform.stairDirection);
        float edgeDist = GetPlatformEdgeDistance(platform);
        
        ramp.transform.localPosition = stairDirection * (edgeDist + rampLength / 2) + Vector3.up * platform.size.y / 2;
        
        // Rotation selon la direction et inclinaison
        float yRotation = platform.stairDirection * 90f;
        float angle = Mathf.Atan2(platform.size.y, rampLength) * Mathf.Rad2Deg;
        ramp.transform.localRotation = Quaternion.Euler(angle, yRotation, 0);
        
        float rampHypotenuse = Mathf.Sqrt(rampLength * rampLength + platform.size.y * platform.size.y);
        ramp.transform.localScale = new Vector3(stairWidth, 0.3f, rampHypotenuse);
        ramp.isStatic = true;
        
        if (platformMaterial != null)
            ramp.GetComponent<Renderer>().material = platformMaterial;
    }
    
    private Vector3 GetStairDirectionVector(int direction)
    {
        switch (direction)
        {
            case 0: return Vector3.forward;
            case 1: return Vector3.right;
            case 2: return Vector3.back;
            case 3: return Vector3.left;
            default: return Vector3.forward;
        }
    }
    
    private float GetPlatformEdgeDistance(PlatformData platform)
    {
        if (platform.stairDirection == 0 || platform.stairDirection == 2)
            return platform.size.z / 2;
        else
            return platform.size.x / 2;
    }
    
    private GameObject CreateSpawnPoints(Transform parent)
    {
        GameObject spawnsParent = new GameObject("SpawnPoints");
        spawnsParent.transform.SetParent(parent);
        
        SpawnPointGroup spawnGroup = spawnsParent.AddComponent<SpawnPointGroup>();
        spawnGroup.groupId = arenaName + "_Spawns";
        spawnGroup.points = new List<Transform>();
        
        List<Vector3> positions = CalculateSpawnPositions();
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject sp = new GameObject($"SpawnPoint_{i+1}");
            sp.transform.SetParent(spawnsParent.transform);
            sp.transform.localPosition = positions[i];
            spawnGroup.points.Add(sp.transform);
        }
        
        return spawnsParent;
    }
    
    private void CreateDoors(Transform parent)
    {
        GameObject doorsParent = new GameObject("Doors");
        doorsParent.transform.SetParent(parent);
        
        GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObj.name = "Door_North";
        doorObj.transform.SetParent(doorsParent.transform);
        doorObj.transform.localPosition = new Vector3(0, wallHeight / 2, arenaLength / 2);
        doorObj.transform.localScale = new Vector3(4, wallHeight, wallThickness * 1.2f);
        
        GameObject doorContainer = new GameObject("DoorArena_North");
        doorContainer.transform.SetParent(doorsParent.transform);
        DoorArena doorArena = doorContainer.AddComponent<DoorArena>();
        doorArena.doorObject = doorObj;
        doorArena.doorSpeed = 2f;
    }
}
