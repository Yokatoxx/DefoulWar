using Proto3GD.FPS;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class AIWaveManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject basicEnemyPrefab;
    [SerializeField] private GameObject specialEnemy1Prefab;
    [SerializeField] private GameObject specialEnemy2Prefab;

    [Header("Spawn Zone")]
    [Tooltip("Points de spawn fixes (prioritaires si renseignés)")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Zone de spawn cuboïde (utilisée si aucun point n'est défini)")]
    [SerializeField] private BoxCollider spawnArea;
    [Tooltip("Évite de spawner trop près de la cible (ex: joueur)")]
    [SerializeField] private Transform avoidTarget;
    [SerializeField] private float minDistanceFromTarget = 10f;
    [SerializeField] private float fallbackRadius = 25f;

    [Header("Capacité et Pooling")]
    [SerializeField] private int prewarmBasic = 60;
    [SerializeField] private int prewarmSpecial1 = 12;
    [SerializeField] private int prewarmSpecial2 = 12;
    [SerializeField] private int maxActive = 1000;
    [SerializeField] private int maxSpawnsPerFrame = 10;

    [Header("Croissance du nombre d'ennemis")]
    [SerializeField] private float targetCountBase = 20f;
    [SerializeField] private float targetCountGrowthPerMinute = 80f;

    [Header("Débit de spawn")]
    [SerializeField] private float baseSpawnRate = 2f;
    [SerializeField] private float spawnRateGrowthPerMinute = 6f;

    [Header("Difficulté")]
    [Tooltip("Facteur de difficulté = 1 + growthPerMinute * minutes")]
    [SerializeField] private float difficultyGrowthPerMinute = 0.3f;

    [Header("Répartition des types (pondérations dynamiques)")]
    [SerializeField] private float special1BaseWeight = 0.05f;
    [SerializeField] private float special1WeightPerDifficulty = 0.6f;
    [SerializeField] private float special2BaseWeight = 0.03f;
    [SerializeField] private float special2WeightPerDifficulty = 0.5f;

    [Header("IA centralisée - budgets")]
    [Tooltip("Nombre max d'ennemis mis à jour par frame (round-robin)")]
    [SerializeField] private int aiUpdatesPerFrame = 200;
    [Tooltip("Distance au-delà de laquelle on réduit la fréquence de pathing")]
    [SerializeField] private float farDistance = 30f;
    [SerializeField] private float nearRepathInterval = 0.25f;
    [SerializeField] private float farRepathInterval = 1.0f;

    [Header("Configs par type")]
    [SerializeField] private EnemyConfig basicConfig = EnemyConfig.DefaultBasic();
    [SerializeField] private EnemyConfig special1Config = EnemyConfig.DefaultSpecial1();
    [SerializeField] private EnemyConfig special2Config = EnemyConfig.DefaultSpecial2();

    [Header("Scaling difficulté (appliqué au spawn)")]
    [SerializeField] private float moveSpeedScalePerDiff = 0.30f;
    [SerializeField] private float damageScalePerDiff = 0.50f;
    [SerializeField] private float detectionScalePerDiff = 0.20f;
    [SerializeField] private float healthScalePerDiff = 0.40f;

    private SimplePool _poolBasic;
    private SimplePool _poolS1;
    private SimplePool _poolS2;

    private float _elapsed;
    private float _spawnAccumulator;

    private static AIWaveManager _instance;
    public static AIWaveManager Instance => _instance;

    public enum EnemyKind { Basic, Special1, Special2 }

    private readonly List<ManagedEnemyHook> _enemies = new List<ManagedEnemyHook>(1024);
    private int _nextUpdateIndex;

    private Transform _player;
    private PlayerHealth _playerHealth;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;

        if (avoidTarget == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) avoidTarget = playerObj.transform;
        }
        _player = avoidTarget;
        if (_player != null) _playerHealth = _player.GetComponent<PlayerHealth>();

        _poolBasic = basicEnemyPrefab ? new SimplePool(basicEnemyPrefab, "[Pool] Basic", transform) : null;
        _poolS1 = specialEnemy1Prefab ? new SimplePool(specialEnemy1Prefab, "[Pool] Special1", transform) : null;
        _poolS2 = specialEnemy2Prefab ? new SimplePool(specialEnemy2Prefab, "[Pool] Special2", transform) : null;

        _poolBasic?.Prewarm(Mathf.Min(prewarmBasic, maxActive));
        _poolS1?.Prewarm(Mathf.Min(prewarmSpecial1, maxActive));
        _poolS2?.Prewarm(Mathf.Min(prewarmSpecial2, maxActive));
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float minutes = _elapsed / 60f;
        float difficulty = 1f + difficultyGrowthPerMinute * minutes;

        float targetCountF = Mathf.Min(maxActive, targetCountBase + targetCountGrowthPerMinute * minutes);
        int targetCount = Mathf.FloorToInt(targetCountF);

        int active = CurrentActiveCount;
        int deficit = Mathf.Max(0, targetCount - active);

        float spawnRate = baseSpawnRate + spawnRateGrowthPerMinute * minutes;
        _spawnAccumulator += spawnRate * Time.deltaTime;

        int toSpawn = Mathf.Min(deficit, Mathf.Min(maxSpawnsPerFrame, Mathf.FloorToInt(_spawnAccumulator)));
        if (toSpawn > 0) _spawnAccumulator -= toSpawn;

        for (int i = 0; i < toSpawn; i++)
            SpawnOne(difficulty);

        UpdateAIBatch();
    }

    private void UpdateAIBatch()
    {
        if (_player == null || _enemies.Count == 0) return;

        int count = Mathf.Min(aiUpdatesPerFrame, _enemies.Count);
        Vector3 playerPos = _player.position;
        float dt = Time.deltaTime;
        float farSqr = farDistance * farDistance;

        for (int i = 0; i < count; i++)
        {
            if (_nextUpdateIndex >= _enemies.Count) _nextUpdateIndex = 0;
            var e = _enemies[_nextUpdateIndex++];
            if (e == null || !e.gameObject.activeInHierarchy) continue;
            if (e.IsDead) continue;

            Vector3 pos = e.Tr.position;
            float sqrDist = (playerPos - pos).sqrMagnitude;
            float dist = Mathf.Sqrt(sqrDist);

            switch (e.State)
            {
                case ManagedEnemyHook.EnemyState.Idle:
                    if (dist <= e.Config.detectionRange)
                    {
                        e.State = ManagedEnemyHook.EnemyState.Chasing;
                        if (e.Agent != null) e.Agent.speed = e.Config.chaseSpeed;
                    }
                    break;

                case ManagedEnemyHook.EnemyState.Chasing:
                    if (e.Agent != null)
                    {
                        float repathInterval = sqrDist <= farSqr ? nearRepathInterval : farRepathInterval;
                        if (Time.time >= e.LastRepath + repathInterval)
                        {
                            e.Agent.isStopped = false;
                            e.Agent.SetDestination(playerPos);
                            e.LastRepath = Time.time;
                        }
                    }

                    if (dist <= e.Config.attackRange)
                    {
                        e.State = ManagedEnemyHook.EnemyState.Attacking;
                        if (e.Agent != null) e.Agent.isStopped = true;
                    }
                    else if (dist > e.Config.detectionRange * 1.5f)
                    {
                        e.State = ManagedEnemyHook.EnemyState.Idle;
                        if (e.Agent != null) e.Agent.speed = e.Config.patrolSpeed;
                    }
                    break;

                case ManagedEnemyHook.EnemyState.Attacking:
                    Vector3 dir = (playerPos - pos);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion look = Quaternion.LookRotation(dir.normalized);
                        e.Tr.rotation = Quaternion.Slerp(e.Tr.rotation, look, dt * e.Config.rotationSpeed);
                    }

                    if (Time.time >= e.LastAttack + e.Config.attackCooldown)
                    {
                        if (dist <= e.Config.attackRange && _playerHealth != null)
                        {
                            _playerHealth.TakeDamage(e.Config.attackDamage);
                        }
                        e.LastAttack = Time.time;
                    }

                    if (dist > e.Config.attackRange * 1.5f)
                    {
                        e.State = ManagedEnemyHook.EnemyState.Chasing;
                        if (e.Agent != null) e.Agent.isStopped = false;
                    }
                    break;
            }
        }
    }

    private void SpawnOne(float difficulty)
    {
        EnemyKind kind = ChooseKind(difficulty);

        SimplePool pool = null;
        switch (kind)
        {
            case EnemyKind.Basic: pool = _poolBasic; break;
            case EnemyKind.Special1: pool = _poolS1 ?? _poolBasic; break;
            case EnemyKind.Special2: pool = _poolS2 ?? _poolBasic; break;
        }
        if (pool == null) return;

        Vector3 pos = GetSpawnPosition();
        GameObject go = pool.Spawn(pos, Quaternion.identity);

        var hook = go.GetComponent<ManagedEnemyHook>();
        if (hook == null) hook = go.AddComponent<ManagedEnemyHook>();
        hook.Manager = this;
        hook.Kind = kind;

        var spawnable = go.GetComponent<ISpawnableEnemy>();
        if (spawnable != null)
        {
            var ctx = new SpawnContext { Difficulty = difficulty, ElapsedSeconds = _elapsed };
            spawnable.OnSpawn(ctx);
        }
    }

    private EnemyKind ChooseKind(float difficulty)
    {
        float d = Mathf.Max(0f, difficulty - 1f);
        float wBasic = 1f;
        float wS1 = special1BaseWeight + special1WeightPerDifficulty * d;
        float wS2 = special2BaseWeight + special2WeightPerDifficulty * d;

        float sum = wBasic + wS1 + wS2;
        float r = Random.value * sum;

        if (r < wS1) return EnemyKind.Special1;
        r -= wS1;
        if (r < wS2) return EnemyKind.Special2;
        return EnemyKind.Basic;
    }

    private Vector3 GetSpawnPosition()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 pos;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var t = spawnPoints[Random.Range(0, spawnPoints.Length)];
                pos = t ? t.position : transform.position;
            }
            else if (spawnArea != null)
            {
                var b = spawnArea.bounds;
                pos = new Vector3(
                    Random.Range(b.min.x, b.max.x),
                    Random.Range(b.min.y, b.max.y),
                    Random.Range(b.min.z, b.max.z)
                );
            }
            else
            {
                Vector2 ring = Random.insideUnitCircle * fallbackRadius;
                pos = transform.position + new Vector3(ring.x, 0f, ring.y);
            }

            if (avoidTarget != null && minDistanceFromTarget > 0f)
            {
                float sqrMin = minDistanceFromTarget * minDistanceFromTarget;
                if ((pos - avoidTarget.position).sqrMagnitude < sqrMin) continue;
            }
            return pos;
        }
        return transform.position + Vector3.forward * 10f;
    }

    private int GetPoolActiveCount(SimplePool p) => p != null ? p.ActiveCount : 0;

    public int CurrentActiveCount => GetPoolActiveCount(_poolBasic) + GetPoolActiveCount(_poolS1) + GetPoolActiveCount(_poolS2);
    public float CurrentDifficulty => 1f + difficultyGrowthPerMinute * (_elapsed / 60f);

    public EnemyConfig GetConfig(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Special1: return special1Config;
            case EnemyKind.Special2: return special2Config;
            default: return basicConfig;
        }
    }

    internal void RegisterEnemy(ManagedEnemyHook hook, float difficulty)
    {
        if (hook == null) return;

        if (hook.Tr == null) hook.Tr = hook.transform;
        if (hook.Agent == null) hook.Agent = hook.GetComponent<NavMeshAgent>();

        var cfg = GetConfig(hook.Kind);
        float d = Mathf.Max(0f, difficulty - 1f);
        float speedScale = 1f + moveSpeedScalePerDiff * d;
        float damageScale = 1f + damageScalePerDiff * d;
        float detectScale = 1f + detectionScalePerDiff * d;
        float healthScale = 1f + healthScalePerDiff * d;

        cfg.chaseSpeed *= speedScale;
        cfg.patrolSpeed *= speedScale;
        cfg.attackDamage *= damageScale;
        cfg.detectionRange *= detectScale;

        hook.Config = cfg;
        hook.State = ManagedEnemyHook.EnemyState.Idle;
        hook.LastAttack = 0f;
        hook.LastRepath = 0f;

        // Init santé
        hook.MaxHealth = Mathf.Max(1f, cfg.maxHealth * healthScale);
        hook.CurrentHealth = hook.MaxHealth;
        hook.IsDead = false;

        if (hook.Agent != null)
        {
            hook.Agent.isStopped = false;
            hook.Agent.speed = cfg.patrolSpeed;
        }

        _enemies.Add(hook);
    }

    internal void UnregisterEnemy(ManagedEnemyHook hook)
    {
        if (hook == null) return;
        int idx = _enemies.IndexOf(hook);
        if (idx >= 0)
        {
            if (idx <= _nextUpdateIndex && _nextUpdateIndex > 0) _nextUpdateIndex--;
            _enemies.RemoveAt(idx);
        }
    }

    public static void Despawn(GameObject go)
    {
        var po = go ? go.GetComponent<PooledObject>() : null;
        if (po != null) po.Despawn();
        else if (go != null) go.SetActive(false);
    }

    [System.Serializable]
    public struct EnemyConfig
    {
        public float maxHealth;
        public float chaseSpeed;
        public float patrolSpeed;
        public float rotationSpeed;
        public float detectionRange;
        public float attackRange;
        public float attackDamage;
        public float attackCooldown;

        public static EnemyConfig DefaultBasic()
        {
            return new EnemyConfig
            {
                maxHealth = 100f,
                chaseSpeed = 3.5f,
                patrolSpeed = 2f,
                rotationSpeed = 5f,
                detectionRange = 15f,
                attackRange = 2f,
                attackDamage = 10f,
                attackCooldown = 1.5f
            };
        }
        public static EnemyConfig DefaultSpecial1()
        {
            var c = DefaultBasic();
            c.maxHealth = 130f;
            c.chaseSpeed = 4.5f;
            c.attackDamage = 15f;
            c.attackCooldown = 1.25f;
            return c;
        }
        public static EnemyConfig DefaultSpecial2()
        {
            var c = DefaultBasic();
            c.maxHealth = 160f;
            c.detectionRange = 18f;
            c.attackRange = 2.5f;
            c.attackDamage = 20f;
            c.attackCooldown = 2.0f;
            return c;
        }
    }

    // Composant léger attaché à chaque ennemi pour lier pooling <-> manager et stocker l'état + santé
    public class ManagedEnemyHook : MonoBehaviour, ISpawnableEnemy
    {
        public enum EnemyState { Idle, Chasing, Attacking }

        [HideInInspector] public AIWaveManager Manager;
        [HideInInspector] public EnemyKind Kind;

        [HideInInspector] public Transform Tr;
        [HideInInspector] public NavMeshAgent Agent;

        [HideInInspector] public EnemyConfig Config;
        [HideInInspector] public EnemyState State = EnemyState.Idle;
        [HideInInspector] public float LastAttack;
        [HideInInspector] public float LastRepath;

        // Santé intégrée
        [HideInInspector] public float MaxHealth;
        [HideInInspector] public float CurrentHealth;
        [HideInInspector] public bool IsDead;

        // Événements optionnels (UI/FX)
        public UnityEvent OnDeath = new UnityEvent();
        public UnityEvent<float, string> OnDamageTaken = new UnityEvent<float, string>();

        public void OnSpawn(SpawnContext context)
        {
            Manager?.RegisterEnemy(this, context.Difficulty);
        }

        public void OnDespawn()
        {
            if (Agent != null)
            {
                Agent.isStopped = true;
                if (Agent.isOnNavMesh) Agent.ResetPath();
            }
            State = EnemyState.Idle;
            IsDead = false;
            CurrentHealth = MaxHealth;
            Manager?.UnregisterEnemy(this);
        }

        // API de dégâts pour les armes (ex-EnemyHealth.TakeDamage)
        public void TakeDamage(float damage, string zoneName = "Body")
        {
            if (IsDead) return;

            CurrentHealth -= Mathf.Max(0f, damage);
            OnDamageTaken?.Invoke(damage, zoneName);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;

            OnDeath?.Invoke();

            // Retour au pool
            AIWaveManager.Despawn(gameObject);
        }
    }
}