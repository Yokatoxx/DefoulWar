using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui maintient une distance fixe avec le joueur.
    /// Cherche les hauteurs quand il détecte le joueur pour avoir un avantage tactique.
    /// Utilise un système de charge sniper avec visée laser.
    /// </summary>
    public class DistanceBehavior : BaseEnemyBehavior
    {
        private bool canAttack;
        
        // Recherche de hauteur
        private Vector3 currentHighGroundTarget;
        private bool hasHighGroundTarget;
        private bool isOnHighGround;
        private bool isNavigatingToHighGround;
        private const float MIN_HEIGHT_ADVANTAGE = 1.5f;
        private const float HIGH_GROUND_SEARCH_RADIUS = 20f;
        private const float HIGH_GROUND_ARRIVAL_THRESHOLD = 2f;
        private const float FLEE_TO_HIGH_GROUND_DISTANCE = 8f; // Si joueur plus proche que ça, fuir vers hauteur
        private const string HIGH_GROUND_TAG = "HighGroundPoint";

        // Cache des points de hauteur
        private static Transform[] cachedHighGroundPoints;
        private static bool pointsCached = false;

        // Système de charge sniper
        private enum ChargeState { None, Charging, Locked }
        private ChargeState chargeState = ChargeState.None;
        private float chargeTimer = 0f;
        private Vector3 lockedTargetPosition;
        private LineRenderer laserLine;
        private const float CHARGE_DURATION = 1.0f;  // Laser rouge, suit le joueur
        private const float LOCK_DURATION = 0.5f;    // Laser vert, position figée

        public override void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            base.Initialize(agent, player, settings, owner);
            detectionState = DetectionState.Idle;
            hasHighGroundTarget = false;
            isOnHighGround = false;
            isNavigatingToHighGround = false;
            chargeState = ChargeState.None;
            
            // Créer le LineRenderer pour le laser de visée
            CreateLaserLine();
            
            // Cache les points de hauteur une seule fois pour tous les ennemis
            if (!pointsCached)
            {
                CacheHighGroundPoints();
            }
        }

        private void CreateLaserLine()
        {
            GameObject laserObj = new GameObject("SniperLaser");
            laserObj.transform.SetParent(owner);
            laserLine = laserObj.AddComponent<LineRenderer>();
            laserLine.startWidth = 0.05f;
            laserLine.endWidth = 0.05f;
            laserLine.positionCount = 2;
            laserLine.material = new Material(Shader.Find("Sprites/Default"));
            laserLine.enabled = false;
        }

        private static void CacheHighGroundPoints()
        {
            GameObject[] points = GameObject.FindGameObjectsWithTag(HIGH_GROUND_TAG);
            cachedHighGroundPoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                cachedHighGroundPoints[i] = points[i].transform;
            }
            pointsCached = true;
        }

        protected override void ExecuteBehavior()
        {
            if (agent == null || player == null) return;

            float distanceToPlayer = Vector3.Distance(owner.position, player.position);
            bool canSeePlayer = IsPlayerInFieldOfView();

            // PRIORITÉ ABSOLUE: Si en hauteur, rester en position peu importe l'état
            if (isOnHighGround)
            {
                agent.isStopped = true;
                agent.ResetPath();
                RotateTowardsPlayerSmooth();
                
                // Gérer le système de charge sniper
                bool inAttackRange = distanceToPlayer <= settings.attackRange && canSeePlayer;
                
                if (inAttackRange)
                {
                    // Démarrer la charge si pas déjà en cours
                    if (chargeState == ChargeState.None)
                    {
                        StartCharging();
                    }
                    UpdateCharging(distanceToPlayer);
                }
                else
                {
                    // Joueur hors de portée ou hors de vue, annuler la charge
                    CancelCharging();
                    canAttack = false;
                }
                
                // Force retour en Chasing si on voit le joueur
                if (canSeePlayer)
                {
                    detectionState = DetectionState.Chasing;
                }
                
                // Quitter la hauteur si joueur hors de portée d'attaque
                if (distanceToPlayer > settings.attackRange)
                {
                    isOnHighGround = false;
                    hasHighGroundTarget = false;
                    isNavigatingToHighGround = false;
                    CancelCharging();
                }
                return;
            }

            switch (detectionState)
            {
                case DetectionState.Idle:
                    HandleIdleState(canSeePlayer);
                    break;

                case DetectionState.Chasing:
                    HandleChasingState(canSeePlayer, distanceToPlayer);
                    break;

                case DetectionState.Investigating:
                    HandleInvestigatingState(canSeePlayer);
                    break;

                case DetectionState.Lost:
                    HandleLostState(canSeePlayer);
                    break;
            }
        }

        private void HandleIdleState(bool canSeePlayer)
        {
            canAttack = false;
            hasHighGroundTarget = false;
            isOnHighGround = false;
            isNavigatingToHighGround = false;
            
            if (canSeePlayer)
            {
                if (TryStartChaseWithTurn())
                {
                    detectionState = DetectionState.Chasing;
                    return;
                }
                
                StartChasing();
            }
        }

        private void HandleChasingState(bool canSeePlayer, float distanceToPlayer)
        {
            // Si on navigue vers une hauteur, continuer même sans voir le joueur
            if (isNavigatingToHighGround && !isOnHighGround)
            {
                Debug.Log("[DistanceBehavior] Continue navigation vers hauteur (perte de vue ignorée)");
                agent.speed = settings.chaseSpeed;
                HandleHighGroundBehavior(distanceToPlayer);
                return;
            }

            if (canSeePlayer)
            {
                UpdateLastKnownPosition();
                AlertNearbyEnemies();
                
                RotateTowardsPlayerSmooth();

                // Priorité 1: Si déjà en position haute, rester et attaquer (géré dans ExecuteBehavior)
                if (isOnHighGround)
                {
                    // Géré au début de ExecuteBehavior
                    return;
                }

                // Priorité 2: Si joueur TROP PROCHE, fuir vers hauteur
                if (distanceToPlayer < FLEE_TO_HIGH_GROUND_DISTANCE)
                {
                    CancelCharging(); // Annuler toute charge en cours
                    
                    // Chercher un point en hauteur
                    if (!hasHighGroundTarget && !isNavigatingToHighGround)
                    {
                        SearchForHighGround();
                        if (hasHighGroundTarget)
                        {
                            isNavigatingToHighGround = true;
                        }
                    }
                    
                    // Fuir vers la hauteur si possible
                    if (hasHighGroundTarget)
                    {
                        agent.speed = settings.chaseSpeed * 1.2f; // Fuite rapide!
                        HandleHighGroundBehavior(distanceToPlayer);
                        return;
                    }
                    // Sinon, reculer (géré par HandleNormalDistanceBehavior)
                }

                // Priorité 3: Si à bonne distance d'attaque (entre FLEE et attackRange), TIRER
                if (distanceToPlayer >= FLEE_TO_HIGH_GROUND_DISTANCE && distanceToPlayer <= settings.attackRange)
                {
                    HandleNormalDistanceBehavior(distanceToPlayer);
                    return;
                }

                // Priorité 3: Si trop loin, soit aller en hauteur, soit poursuivre
                if (distanceToPlayer > settings.attackRange)
                {
                    // Chercher un point en hauteur seulement si on n'en a pas
                    if (!hasHighGroundTarget && !isNavigatingToHighGround)
                    {
                        SearchForHighGround();
                        if (hasHighGroundTarget)
                        {
                            isNavigatingToHighGround = true;
                        }
                    }

                    // Si on a une cible en hauteur, y aller
                    if (hasHighGroundTarget)
                    {
                        agent.speed = settings.chaseSpeed;
                        HandleHighGroundBehavior(distanceToPlayer);
                    }
                    else
                    {
                        // Pas de hauteur disponible, poursuivre le joueur
                        agent.speed = settings.chaseSpeed;
                        PursuePlayer(distanceToPlayer);
                    }
                }
            }
            else
            {
                Debug.Log("[DistanceBehavior] Perte de vue - investigation");
                detectionState = DetectionState.Investigating;
                investigationTimer = settings.investigationTime;
                agent.SetDestination(lastKnownPlayerPosition);
                agent.isStopped = false;
                canAttack = false;
                // NE PAS reset les flags de hauteur si on navigue vers la hauteur!
                if (!isNavigatingToHighGround)
                {
                    hasHighGroundTarget = false;
                    isOnHighGround = false;
                }
            }
        }

        // Cherche le point de hauteur le plus proche et suffisamment haut
        private void SearchForHighGround()
        {
            if (cachedHighGroundPoints == null || cachedHighGroundPoints.Length == 0) return;

            float bestDistance = float.MaxValue;
            Vector3 bestPosition = Vector3.zero;
            bool foundHighGround = false;

            foreach (var point in cachedHighGroundPoints)
            {
                if (point == null) continue;

                float distanceToPoint = Vector3.Distance(owner.position, point.position);
                
                // Le point doit être dans le rayon de recherche
                if (distanceToPoint > HIGH_GROUND_SEARCH_RADIUS) continue;

                // Le point doit être suffisamment plus haut que le joueur
                float heightDiff = point.position.y - player.position.y;
                if (heightDiff < MIN_HEIGHT_ADVANTAGE) continue;

                // Vérifie qu'on peut voir le joueur depuis ce point
                if (!CheckLineOfSightFromPosition(point.position)) continue;

                // Prendre le plus proche
                if (distanceToPoint < bestDistance)
                {
                    bestDistance = distanceToPoint;
                    bestPosition = point.position;
                    foundHighGround = true;
                }
            }

            if (foundHighGround)
            {
                currentHighGroundTarget = bestPosition;
                hasHighGroundTarget = true;
            }
        }

        // Vérifie la ligne de vue depuis une position donnée vers le joueur
        private bool CheckLineOfSightFromPosition(Vector3 fromPosition)
        {
            Vector3 eyePosition = fromPosition + Vector3.up * settings.eyeHeight;
            Vector3 targetPosition = player.position + Vector3.up * 1f;
            Vector3 direction = targetPosition - eyePosition;
            float distance = direction.magnitude;

            if (Physics.Raycast(eyePosition, direction.normalized, out RaycastHit hit, distance, settings.obstacleLayer))
            {
                return hit.transform == player || hit.transform.IsChildOf(player);
            }
            return true;
        }

        // Comportement quand on a une position en hauteur
        private void HandleHighGroundBehavior(float distanceToPlayer)
        {
            float distanceToHighGround = Vector3.Distance(owner.position, currentHighGroundTarget);
            Debug.Log($"[DistanceBehavior] HandleHighGround - distToTarget: {distanceToHighGround:F1}, threshold: {HIGH_GROUND_ARRIVAL_THRESHOLD}, isOn: {isOnHighGround}");
            
            if (distanceToHighGround > HIGH_GROUND_ARRIVAL_THRESHOLD && !isOnHighGround)
            {
                // Se déplacer vers la hauteur
                Debug.Log($"[DistanceBehavior] Navigation vers hauteur: {currentHighGroundTarget}");
                agent.SetDestination(currentHighGroundTarget);
                agent.isStopped = false;
            }
            else
            {
                // Arrivé en position, marquer comme en hauteur et rester
                Debug.Log("[DistanceBehavior] ARRIVÉ EN HAUTEUR - marquage isOnHighGround = true");
                isOnHighGround = true;
                agent.isStopped = true;
                agent.ResetPath();
            }
            
            canAttack = distanceToPlayer <= settings.attackRange;
        }

        // Poursuit le joueur (quand il est trop loin)
        private void PursuePlayer(float distanceToPlayer)
        {
            Vector3 directionToPlayer = (player.position - owner.position).normalized;
            Vector3 targetPosition = player.position - directionToPlayer * settings.keepDistance;
            
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, settings.keepDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            agent.isStopped = false;
            canAttack = distanceToPlayer <= settings.attackRange;
        }

        // Comportement normal de distance (fallback) avec système de charge sniper
        private void HandleNormalDistanceBehavior(float distanceToPlayer)
        {
            float minDistance = settings.keepDistance - settings.distanceTolerance;
            float maxDistance = settings.keepDistance + settings.distanceTolerance;

            bool inAttackRange = distanceToPlayer <= settings.attackRange;
            
            // Système de charge sniper au sol
            if (inAttackRange)
            {
                // Démarrer la charge si pas déjà en cours
                if (chargeState == ChargeState.None)
                {
                    StartCharging();
                }
                
                // Pendant la charge, ralentir et viser
                if (chargeState != ChargeState.None)
                {
                    agent.speed = settings.patrolSpeed * 0.3f; // Très ralenti pendant la visée
                    RotateTowardsPlayerSmooth();
                }
                
                UpdateCharging(distanceToPlayer);
            }
            else
            {
                // Hors de portée, annuler la charge
                CancelCharging();
                agent.speed = settings.chaseSpeed;
            }

            // Mouvement normal (affecté par le ralentissement ci-dessus)
            if (distanceToPlayer < minDistance)
            {
                // Trop proche, reculer
                Vector3 directionAway = (owner.position - player.position).normalized;
                Vector3 targetPosition = owner.position + directionAway * 2f;
                
                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                agent.isStopped = false;
            }
            else if (distanceToPlayer > maxDistance)
            {
                PursuePlayer(distanceToPlayer);
            }
            else
            {
                // Bonne distance, s'arrêter pour viser
                if (chargeState != ChargeState.None)
                {
                    agent.isStopped = true;
                }
            }
        }

        private void HandleInvestigatingState(bool canSeePlayer)
        {
            canAttack = false;

            if (canSeePlayer)
            {
                StartChasing();
                return;
            }

            if (!HasReachedLastKnownPosition())
            {
                agent.SetDestination(lastKnownPlayerPosition);
                agent.isStopped = false;
            }
            else
            {
                agent.isStopped = true;
                
                if (UpdateInvestigationTimer())
                {
                    detectionState = DetectionState.Lost;
                    ResetAlertState();
                }
            }
        }

        private void HandleLostState(bool canSeePlayer)
        {
            canAttack = false;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            if (canSeePlayer)
            {
                StartChasing();
            }
            else
            {
                detectionState = DetectionState.Idle;
            }
        }

        // Système de charge sniper - à appeler quand l'ennemi peut attaquer
        private void UpdateCharging(float distanceToPlayer)
        {
            Vector3 shootOrigin = owner.position + Vector3.up * settings.eyeHeight;
            
            switch (chargeState)
            {
                case ChargeState.None:
                    // Pas en charge, rien à faire
                    break;
                    
                case ChargeState.Charging:
                    chargeTimer -= Time.deltaTime;
                    
                    // Laser rouge suit le joueur
                    if (laserLine != null)
                    {
                        laserLine.enabled = true;
                        laserLine.startColor = Color.red;
                        laserLine.endColor = Color.red;
                        laserLine.SetPosition(0, shootOrigin);
                        laserLine.SetPosition(1, player.position + Vector3.up);
                    }
                    
                    if (chargeTimer <= 0f)
                    {
                        // Passer au verrouillage, prédire la position du joueur
                        chargeState = ChargeState.Locked;
                        chargeTimer = LOCK_DURATION;
                        
                        // Verrouiller la position (prédiction simple)
                        Rigidbody playerRb = player.GetComponent<Rigidbody>();
                        if (playerRb != null)
                        {
                            lockedTargetPosition = player.position + playerRb.linearVelocity * 0.3f + Vector3.up;
                        }
                        else
                        {
                            lockedTargetPosition = player.position + Vector3.up;
                        }
                    }
                    break;
                    
                case ChargeState.Locked:
                    chargeTimer -= Time.deltaTime;
                    
                    // Laser vert fixe sur la position verrouillée
                    if (laserLine != null)
                    {
                        laserLine.startColor = Color.green;
                        laserLine.endColor = Color.green;
                        laserLine.SetPosition(0, shootOrigin);
                        laserLine.SetPosition(1, lockedTargetPosition);
                    }
                    
                    if (chargeTimer <= 0f)
                    {
                        // Tir ! Permettre l'attaque
                        canAttack = true;
                        chargeState = ChargeState.None;
                        if (laserLine != null) laserLine.enabled = false;
                    }
                    break;
            }
        }

        // Démarre la charge sniper
        private void StartCharging()
        {
            if (chargeState == ChargeState.None)
            {
                chargeState = ChargeState.Charging;
                chargeTimer = CHARGE_DURATION;
                canAttack = false;
            }
        }

        // Annule la charge si le joueur sort de portée ou de vue
        private void CancelCharging()
        {
            chargeState = ChargeState.None;
            chargeTimer = 0f;
            if (laserLine != null) laserLine.enabled = false;
        }

        private void StartChasing()
        {
            detectionState = DetectionState.Chasing;
            agent.speed = settings.chaseSpeed;
            agent.isStopped = false;
            UpdateLastKnownPosition();
        }

        public override bool CanAttack() => canAttack;
        public override bool IsChasing() => detectionState == DetectionState.Chasing;
        public override bool IsPatrolling() => false;

        // Retourne la position verrouillée pour le tir sniper (null si pas de charge)
        public Vector3? GetSniperTargetPosition()
        {
            // Si on vient de terminer la charge, retourner la position verrouillée
            if (canAttack && lockedTargetPosition != Vector3.zero)
            {
                return lockedTargetPosition;
            }
            return null;
        }

        public override void OnDamageTaken()
        {
            if (player != null && detectionState != DetectionState.Chasing)
            {
                UpdateLastKnownPosition();
                StartChasing();
            }
        }

        public override void DrawGizmos()
        {
            if (owner == null || settings == null) return;

            // Zone de détection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange);

            // Distance à maintenir
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(owner.position, settings.keepDistance);

            // Portée d'attaque
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(owner.position, settings.attackRange);

            // Zone de tolérance
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(owner.position, settings.keepDistance - settings.distanceTolerance);
            Gizmos.DrawWireSphere(owner.position, settings.keepDistance + settings.distanceTolerance);

            // Cône de vision
            DrawFieldOfViewGizmo();

            // Dernière position connue
            if (detectionState == DetectionState.Investigating)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
                Gizmos.DrawLine(owner.position, lastKnownPlayerPosition);
            }
        }

        private void DrawFieldOfViewGizmo()
        {
            float halfAngle = settings.viewAngle / 2f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * owner.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * owner.forward;

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawRay(owner.position + Vector3.up * settings.eyeHeight, leftDir * settings.detectionRange);
            Gizmos.DrawRay(owner.position + Vector3.up * settings.eyeHeight, rightDir * settings.detectionRange);
        }
    }
}
