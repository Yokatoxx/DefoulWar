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
    private float wallHeight = 5f;
    private float wallThickness = 1f;
    
    [Header("Obstacles")]
    private int obstacleCount = 8;
    private float minObstacleHeight = 2f;
    private float maxObstacleHeight = 4f;
    private float obstacleMinSize = 2f;
    private float obstacleMaxSize = 5f;
    private float minDistanceBetweenObstacles = 5f;
    private float centerClearRadius = 8f; // Zone libre au centre
    
    [Header("Points de Spawn")]
    private int spawnPointCount = 4;
    private float spawnPointMargin = 3f;
    
    [Header("Options")]
    private bool addArenaSetter = true;
    private bool addDoors = false;
    private bool createAsChild = true;
    private string arenaName = "GeneratedArena";
    
    // Materials (optionnel)
    private Material floorMaterial;
    private Material wallMaterial;
    private Material obstacleMaterial;
    
    // Prévisualisation
    private bool showPreview = false;
    private List<Vector3> previewObstacles = new List<Vector3>();
    private List<Vector3> previewSizes = new List<Vector3>();
    
    // Scroll
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Arena Generator")]
    public static void ShowWindow()
    {
        ArenaGenerator window = GetWindow<ArenaGenerator>("Arena Generator");
        window.minSize = new Vector2(350, 500);
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Titre
        EditorGUILayout.LabelField("🏟️ Générateur d'Arène", EditorStyles.boldLabel);
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
        EditorGUILayout.LabelField("🪨 Obstacles", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        obstacleCount = EditorGUILayout.IntSlider("Nombre d'obstacles", obstacleCount, 0, 30);
        minObstacleHeight = EditorGUILayout.Slider("Hauteur min", minObstacleHeight, 1f, 10f);
        maxObstacleHeight = EditorGUILayout.Slider("Hauteur max", maxObstacleHeight, minObstacleHeight, 15f);
        obstacleMinSize = EditorGUILayout.Slider("Taille min", obstacleMinSize, 1f, 10f);
        obstacleMaxSize = EditorGUILayout.Slider("Taille max", obstacleMaxSize, obstacleMinSize, 15f);
        minDistanceBetweenObstacles = EditorGUILayout.Slider("Distance min entre obstacles", minDistanceBetweenObstacles, 2f, 15f);
        centerClearRadius = EditorGUILayout.Slider("Zone libre au centre", centerClearRadius, 0f, 30f);
        
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
        createAsChild = EditorGUILayout.Toggle("Créer comme enfant unique", createAsChild);
        addArenaSetter = EditorGUILayout.Toggle("Ajouter ArenaSetter", addArenaSetter);
        addDoors = EditorGUILayout.Toggle("Ajouter Portes", addDoors);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        
        // === Section Materials (optionnel) ===
        EditorGUILayout.LabelField("🎨 Materials (optionnel)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        floorMaterial = (Material)EditorGUILayout.ObjectField("Sol", floorMaterial, typeof(Material), false);
        wallMaterial = (Material)EditorGUILayout.ObjectField("Murs", wallMaterial, typeof(Material), false);
        obstacleMaterial = (Material)EditorGUILayout.ObjectField("Obstacles", obstacleMaterial, typeof(Material), false);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(20);
        
        // === Boutons ===
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🔄 Prévisualiser", GUILayout.Height(30)))
        {
            GeneratePreviewPositions();
            showPreview = true;
            SceneView.RepaintAll();
        }
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("✅ Générer l'Arène", GUILayout.Height(30)))
        {
            GenerateArena();
            showPreview = false;
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        if (showPreview)
        {
            EditorGUILayout.HelpBox("Prévisualisation active. Regardez la fenêtre Scene.", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
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
        
        // Contour bas
        Handles.DrawLine(new Vector3(-halfW, 0, -halfL), new Vector3(halfW, 0, -halfL));
        Handles.DrawLine(new Vector3(halfW, 0, -halfL), new Vector3(halfW, 0, halfL));
        Handles.DrawLine(new Vector3(halfW, 0, halfL), new Vector3(-halfW, 0, halfL));
        Handles.DrawLine(new Vector3(-halfW, 0, halfL), new Vector3(-halfW, 0, -halfL));
        
        // Contour haut
        Handles.DrawLine(new Vector3(-halfW, wallHeight, -halfL), new Vector3(halfW, wallHeight, -halfL));
        Handles.DrawLine(new Vector3(halfW, wallHeight, -halfL), new Vector3(halfW, wallHeight, halfL));
        Handles.DrawLine(new Vector3(halfW, wallHeight, halfL), new Vector3(-halfW, wallHeight, halfL));
        Handles.DrawLine(new Vector3(-halfW, wallHeight, halfL), new Vector3(-halfW, wallHeight, -halfL));
        
        // Piliers verticaux
        Handles.DrawLine(new Vector3(-halfW, 0, -halfL), new Vector3(-halfW, wallHeight, -halfL));
        Handles.DrawLine(new Vector3(halfW, 0, -halfL), new Vector3(halfW, wallHeight, -halfL));
        Handles.DrawLine(new Vector3(halfW, 0, halfL), new Vector3(halfW, wallHeight, halfL));
        Handles.DrawLine(new Vector3(-halfW, 0, halfL), new Vector3(-halfW, wallHeight, halfL));
        
        // Zone centrale libre
        Handles.color = new Color(1f, 1f, 0.3f, 0.2f);
        Handles.DrawSolidDisc(Vector3.zero, Vector3.up, centerClearRadius);
        
        // Obstacles prévisualisés
        Handles.color = new Color(0.8f, 0.4f, 0.1f, 0.5f);
        for (int i = 0; i < previewObstacles.Count; i++)
        {
            Vector3 pos = previewObstacles[i];
            Vector3 size = previewSizes[i];
            Handles.DrawWireCube(pos + Vector3.up * size.y / 2, size);
        }
        
        // Points de spawn
        Handles.color = Color.magenta;
        List<Vector3> spawnPositions = CalculateSpawnPositions();
        foreach (Vector3 sp in spawnPositions)
        {
            Handles.SphereHandleCap(0, sp + Vector3.up * 0.5f, Quaternion.identity, 1f, EventType.Repaint);
            Handles.Label(sp + Vector3.up * 1.5f, "Spawn");
        }
        
        sceneView.Repaint();
    }
    
    /// <summary>
    /// Génère les positions de prévisualisation des obstacles
    /// </summary>
    private void GeneratePreviewPositions()
    {
        previewObstacles.Clear();
        previewSizes.Clear();
        
        float halfW = arenaWidth / 2 - obstacleMaxSize;
        float halfL = arenaLength / 2 - obstacleMaxSize;
        
        int maxAttempts = obstacleCount * 20;
        int attempts = 0;
        
        while (previewObstacles.Count < obstacleCount && attempts < maxAttempts)
        {
            attempts++;
            
            float x = Random.Range(-halfW, halfW);
            float z = Random.Range(-halfL, halfL);
            Vector3 pos = new Vector3(x, 0, z);
            
            // Vérifier zone centrale
            if (pos.magnitude < centerClearRadius) continue;
            
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
            
            float sizeX = Random.Range(obstacleMinSize, obstacleMaxSize);
            float sizeZ = Random.Range(obstacleMinSize, obstacleMaxSize);
            float height = Random.Range(minObstacleHeight, maxObstacleHeight);
            previewSizes.Add(new Vector3(sizeX, height, sizeZ));
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
        
        // Distribution autour du périmètre
        for (int i = 0; i < spawnPointCount; i++)
        {
            float angle = (i / (float)spawnPointCount) * Mathf.PI * 2;
            float x = Mathf.Sin(angle) * halfW * 0.7f;
            float z = Mathf.Cos(angle) * halfL * 0.7f;
            positions.Add(new Vector3(x, 0, z));
        }
        
        return positions;
    }
    
    /// <summary>
    /// Génère l'arène complète
    /// </summary>
    private void GenerateArena()
    {
        // Générer les positions si pas déjà fait
        if (previewObstacles.Count == 0)
        {
            GeneratePreviewPositions();
        }
        
        // Créer le parent
        GameObject arenaRoot = new GameObject(arenaName);
        Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Arena");
        
        // Créer la structure
        GameObject floor = CreateFloor(arenaRoot.transform);
        GameObject walls = CreateWalls(arenaRoot.transform);
        GameObject obstacles = CreateObstacles(arenaRoot.transform);
        GameObject spawns = CreateSpawnPoints(arenaRoot.transform);
        
        // Ajouter composants Arena
        if (addArenaSetter)
        {
            ArenaSetter setter = arenaRoot.AddComponent<ArenaSetter>();
            setter.totalWaves = 1;
            
            // Lier le SpawnPointGroup
            SpawnPointGroup spGroup = spawns.GetComponent<SpawnPointGroup>();
            if (spGroup != null)
            {
                setter.waves = new List<WaveSetter>();
            }
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
        
        // Focus sur l'arène
        SceneView.lastActiveSceneView?.FrameSelected();
        
        Debug.Log($"✅ Arène '{arenaName}' générée avec succès! ({arenaWidth}x{arenaLength}, {obstacleCount} obstacles, {spawnPointCount} spawns)");
    }
    
    private GameObject CreateFloor(Transform parent)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.localPosition = new Vector3(0, -0.5f, 0);
        floor.transform.localScale = new Vector3(arenaWidth, 1, arenaLength);
        floor.isStatic = true;
        
        if (floorMaterial != null)
        {
            floor.GetComponent<Renderer>().material = floorMaterial;
        }
        
        return floor;
    }
    
    private GameObject CreateWalls(Transform parent)
    {
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(parent);
        
        // 4 murs
        CreateWall(wallsParent.transform, "Wall_North", new Vector3(0, wallHeight/2, arenaLength/2), new Vector3(arenaWidth, wallHeight, wallThickness));
        CreateWall(wallsParent.transform, "Wall_South", new Vector3(0, wallHeight/2, -arenaLength/2), new Vector3(arenaWidth, wallHeight, wallThickness));
        CreateWall(wallsParent.transform, "Wall_East", new Vector3(arenaWidth/2, wallHeight/2, 0), new Vector3(wallThickness, wallHeight, arenaLength));
        CreateWall(wallsParent.transform, "Wall_West", new Vector3(-arenaWidth/2, wallHeight/2, 0), new Vector3(wallThickness, wallHeight, arenaLength));
        
        return wallsParent;
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
        {
            wall.GetComponent<Renderer>().material = wallMaterial;
        }
    }
    
    private GameObject CreateObstacles(Transform parent)
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
            {
                obstacle.GetComponent<Renderer>().material = obstacleMaterial;
            }
        }
        
        return obstaclesParent;
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
        
        // Porte Nord
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
