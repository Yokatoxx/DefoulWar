# FAQ Soutenance - State Machines & Scriptable Objects

## Questions techniques attendues

---

### Q1: Pourquoi utiliser une interface (IEnemyBehavior) plutôt qu'uniquement une classe abstraite ?

**Réponse courte :**
La classe abstraite `BaseEnemyBehavior` fournit l'implémentation commune, l'interface `IEnemyBehavior` définit le contrat.

**Réponse détaillée :**
- **Interface** = Contrat que tous les comportements DOIVENT respecter
- **Classe abstraite** = Implémentation partagée (vision, trajectoires, state machine)

**Avantage :** Si demain on veut un comportement qui n'a PAS besoin de BaseEnemyBehavior (ex: ennemi scripté avec timeline), on peut implémenter juste l'interface sans hériter de la classe abstraite.

**En code :**
```csharp
// Comportement normal
public class ChaserBehavior : BaseEnemyBehavior { }

// Comportement scripté spécial (futur)
public class ScriptedBossBehavior : IEnemyBehavior {
    // Implémente l'interface sans BaseEnemyBehavior
}
```

---

### Q2: Les ScriptableObjects ne sont-ils pas plus lents que des variables normales ?

**Réponse courte :**
Non, au contraire, ils sont plus performants en mémoire.

**Réponse détaillée :**
**Avec variables normales :**
- 100 ennemis identiques = 100 copies des mêmes valeurs en mémoire
- Mémoire gaspillée : 100 × sizeof(EnemyStats)

**Avec ScriptableObject :**
- 100 ennemis = 100 références vers LE MÊME asset
- Mémoire utilisée : 1 × sizeof(EnemyStats) + 100 × sizeof(reference)
- Une référence = 8 bytes (64-bit)

**Calcul :**
Si un EnemyBehaviorSettings = ~200 bytes
- Sans SO : 100 × 200 = 20 000 bytes
- Avec SO : 200 + (100 × 8) = 1 000 bytes
- **Économie : 95% de mémoire**

**Accès :** Lecture d'une référence = aussi rapide qu'une variable normale (cache CPU).

---

### Q3: Pourquoi pas un Behavior Tree au lieu d'une State Machine ?

**Réponse courte :**
State Machine = simple et suffisant pour nos besoins. Behavior Tree = overkill.

**Réponse détaillée :**

**Behavior Tree :**
- ✅ Très flexible, réutilisabilité des nœuds
- ✅ Bon pour IA complexe (NPCs avec multiples objectifs)
- ❌ Plus complexe à implémenter (framework nécessaire)
- ❌ Overhead de traversée de l'arbre chaque frame
- ❌ Debugging plus difficile

**State Machine :**
- ✅ Simple à comprendre et implémenter
- ✅ Performance optimale (simple switch)
- ✅ États clairement définis (facile à debug)
- ✅ Suffisant pour comportements ennemis (4-5 états max)
- ❌ Moins flexible pour IA très complexe

**Notre choix :** Nos ennemis ont des comportements relativement simples (poursuite, patrouille). State Machine = rapport simplicité/performance optimal.

**Si besoin futur :** On peut hybrider (state machine pour états globaux + behavior tree pour comportements complexes dans un état).

---

### Q4: Comment évitez-vous les transitions d'états invalides ?

**Réponse courte :**
Switch exhaustif + méthodes privées pour chaque état.

**Réponse détaillée :**

**1. Enum pour les états :**
```csharp
public enum DetectionState { Idle, Chasing, Investigating, Lost }
```
Le compilateur garantit qu'on ne peut pas avoir d'état invalide.

**2. Switch exhaustif :**
```csharp
switch (detectionState) {
    case Idle: HandleIdleState(); break;
    case Chasing: HandleChasingState(); break;
    // ...
}
```
Si on ajoute un état, le compilateur avertit si on oublie un case.

**3. Transitions contrôlées :**
```csharp
// Dans HandleIdleState()
if (canSeePlayer) {
    detectionState = DetectionState.Chasing; // Transition explicite
}
```
Pas de transition aléatoire, chaque état contrôle ses propres sorties.

**4. Validation en Debug :**
On pourrait ajouter :
```csharp
#if UNITY_EDITOR
private void ValidateTransition(DetectionState from, DetectionState to) {
    // Vérifier que la transition est valide
}
#endif
```

---

### Q5: Comment testez-vous ce système sans Unity ?

**Réponse courte :**
Les Behaviors sont des classes C# pures, testables avec NUnit/XUnit.

**Réponse détaillée :**

**Ce qui est testable sans Unity :**
- ✅ Logique des behaviors (ChaserBehavior, DistanceBehavior)
- ✅ Transitions d'états
- ✅ Calculs (trajectoires, distances)

**Structure de test :**
```csharp
[Test]
public void ChaserBehavior_WhenPlayerInSight_ShouldChase()
{
    // Arrange
    var mockAgent = new Mock<NavMeshAgent>();
    var mockPlayer = new Mock<Transform>();
    var settings = CreateTestSettings();
    var behavior = new ChaserBehavior();
    
    behavior.Initialize(mockAgent.Object, mockPlayer.Object, settings, mockOwner);
    
    // Act
    behavior.Execute();
    
    // Assert
    Assert.AreEqual(DetectionState.Chasing, behavior.GetDetectionState());
}
```

**Mocking :**
On utilise des mocks pour NavMeshAgent et Transform (dépendances Unity).

**Ce qui nécessite Unity :**
- Tests d'intégration (comportement en jeu réel)
- Tests de collision/raycast

**Stratégie :** Unit tests (95% du code) + Play Mode tests Unity (5% - intégration).

---

### Q6: Que se passe-t-il si on modifie le ScriptableObject pendant le runtime ?

**Réponse courte :**
Les changements sont appliqués immédiatement à TOUS les ennemis utilisant ce SO.

**Réponse détaillée :**

**En Play Mode (éditeur) :**
- Modifier `chaseSpeed` dans le SO = tous les ennemis changent de vitesse instantanément
- ✅ Très pratique pour le game design (tweaking en temps réel)
- ⚠️ Changements perdus à l'arrêt du play mode

**En Build (jeu compilé) :**
- Les SO sont en lecture seule
- Impossible de modifier depuis l'Inspector
- ✅ Sécurisé, pas de modifications accidentelles

**Si on veut modifier en runtime (build) :**
```csharp
// MAUVAIS - modifie le SO pour tout le monde
settings.chaseSpeed = 10f;

// BON - créer une copie locale
var localSettings = Instantiate(settings);
localSettings.chaseSpeed = 10f;
SetSettings(localSettings);
```

**Use case :** Boss qui devient plus rapide à 50% HP → créer une copie du SO avec vitesse augmentée.

---

### Q7: Pourquoi switch/case et non pas un Dictionary de fonctions ?

**Réponse courte :**
Switch = plus performant et plus lisible pour un petit nombre d'états.

**Réponse détaillée :**

**Alternative Dictionary :**
```csharp
Dictionary<DetectionState, Action> stateActions = new() {
    { DetectionState.Idle, HandleIdleState },
    { DetectionState.Chasing, HandleChasingState }
};

stateActions[detectionState](); // Exécution
```

**Comparaison :**

| Critère | Switch | Dictionary |
|---------|--------|------------|
| Performance | ⚡ O(1) - jump table | 🐢 O(1) mais avec hash + lookup |
| Lisibilité | ✅ Clair, tout au même endroit | ❌ Séparé (déclaration + méthodes) |
| Allocation | ✅ Aucune | ❌ Dictionary en mémoire |
| Compilateur | ✅ Vérifie exhaustivité | ❌ Erreur runtime si état manquant |
| Dynamisme | ❌ Statique | ✅ Peut ajouter états runtime |

**Notre choix :** Switch car :
- Nombre d'états fixe et petit (4-5)
- Performance critique (appelé 60 fois/sec)
- Pas besoin d'ajouter des états dynamiquement

**Dictionary utile si :** États dynamiques (système de quêtes, dialogue) ou > 20 états.

---

### Q8: Comment gérez-vous plusieurs ennemis qui veulent tous poursuivre le même joueur ?

**Réponse courte :**
Chaque ennemi a sa propre instance de Behavior, donc pas de conflit.

**Réponse détaillée :**

**Architecture :**
```
Enemy1 GameObject
  └─ EnemyBehaviour (instance 1)
      └─ ChaserBehavior (instance 1)
          └─ detectionState (propre à cet ennemi)

Enemy2 GameObject
  └─ EnemyBehaviour (instance 2)
      └─ ChaserBehavior (instance 2)
          └─ detectionState (indépendant)
```

**Ce qui est partagé :**
- ✅ EnemyBehaviorSettings (ScriptableObject)
  - Lecture seule, pas de problème

**Ce qui est unique :**
- ✅ Instance de Behavior (new ChaserBehavior())
- ✅ detectionState, lastKnownPosition, timers
- ✅ NavMeshAgent

**Pas de race condition :**
- Chaque ennemi a ses propres variables d'état
- Unity est single-threaded (pas de concurrence)

**Système d'alerte :**
```csharp
EnemyAlertSystem.Instance.AlertEnemiesInRadius(position, playerPos, radius);
```
Système centralisé qui notifie les ennemis proches, mais chaque ennemi traite l'alerte indépendamment.

---

### Q9: Pourquoi pas un MonoBehaviour avec des UnityEvents pour les transitions ?

**Réponse courte :**
UnityEvents = lourd, allocations, difficile à tester. Pas adapté pour code appelé 60 fois/sec.

**Réponse détaillée :**

**UnityEvent :**
```csharp
public UnityEvent OnStateChanged;

void ChangeState(DetectionState newState) {
    detectionState = newState;
    OnStateChanged.Invoke(); // Allocation + marshaling
}
```

**Problèmes :**
- ❌ **Allocations :** Chaque Invoke() alloue de la mémoire (GC)
- ❌ **Performance :** Réflexion pour appeler les callbacks
- ❌ **Debugging :** Difficile de suivre qui écoute quoi
- ❌ **Testabilité :** Nécessite Unity runtime

**Notre approche (méthodes directes) :**
```csharp
switch (detectionState) {
    case Idle: HandleIdleState(); break;
}
```

**Avantages :**
- ✅ Zero allocation
- ✅ Appel direct (inlining possible)
- ✅ Traçable au debugger
- ✅ Testable hors Unity

**UnityEvent utile pour :** Événements rares (OnDeath, OnLevelComplete), pas pour logique frame-by-frame.

---

### Q10: Comment debuggez-vous l'état actuel des ennemis en jeu ?

**Réponse courte :**
Gizmos + propriétés publiques exposées + Unity Debug Inspector.

**Réponse détaillée :**

**1. Gizmos visuels :**
```csharp
public override void DrawGizmos()
{
    // Zone de détection (jaune)
    Gizmos.DrawWireSphere(position, detectionRange);
    
    // Dernière position connue (magenta)
    if (detectionState == Investigating) {
        Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
    }
}
```
Voir en temps réel les zones de détection et états.

**2. Propriétés publiques exposées :**
```csharp
public DetectionState GetDetectionState() => detectionState;
public bool IsChasing => currentBehavior?.IsChasing() ?? false;
```
Visibles dans l'Inspector en runtime.

**3. Debug logs conditionnels :**
```csharp
[SerializeField] private bool enableDebugLogs = false;

if (enableDebugLogs) {
    Debug.Log($"[{gameObject.name}] State: {detectionState}");
}
```
Activable par ennemi.

**4. Custom Inspector (optionnel) :**
```csharp
[CustomEditor(typeof(EnemyBehaviour))]
public class EnemyBehaviourEditor : Editor {
    public override void OnInspectorGUI() {
        var enemy = target as EnemyBehaviour;
        EditorGUILayout.LabelField("State", enemy.GetDetectionState().ToString());
        // ...
    }
}
```

**5. Scene View overlay :**
Afficher l'état au-dessus de chaque ennemi avec `Handles.Label()`.

---

### Q11: Quelle est la complexité en temps (Big O) de votre système ?

**Réponse courte :**
**O(1)** pour l'exécution d'un comportement par frame.

**Réponse détaillée :**

**Par ennemi, par frame :**
1. `Execute()` : O(1) - simple appel de méthode
2. `switch(detectionState)` : O(1) - jump table
3. `IsPlayerInFieldOfView()` : O(1) - calculs géométriques
4. `CheckLineOfSight()` : O(1) - un seul raycast
5. `NavMeshAgent.SetDestination()` : O(1) - API Unity

**Système d'alerte (occasionnel) :**
`AlertEnemiesInRadius()` : O(n) où n = nombre d'ennemis dans le rayon
- Physics.OverlapSphere() : O(n) avec spatial hashing
- Appelé rarement (seulement à la détection)

**Total par frame, pour N ennemis :**
- Sans alerte : **O(N)** - linéaire, chaque ennemi O(1)
- Avec alerte : **O(N + k)** où k = ennemis alertés

**Scalabilité :**
100 ennemis = 100 × O(1) = parfaitement scalable
Testé avec 50+ ennemis simultanés sans drop de performance.

**Optimisation possible (si > 200 ennemis) :**
- LOD comportemental (ennemis loin = update moins fréquent)
- Spatial partitioning pour vision

---

### Q12: Que se passe-t-il si on delete le ScriptableObject pendant que des ennemis l'utilisent ?

**Réponse courte :**
**Référence devient null**, l'ennemi crash ou se désactive.

**Réponse détaillée :**

**En éditeur :**
```csharp
if (settings == null) {
    Debug.LogWarning($"No settings on {gameObject.name}!");
    return; // Évite le crash
}
```
Notre code vérifie les null, donc pas de NullReferenceException.

**Protection dans le code :**
```csharp
private void InitializeBehavior()
{
    if (settings == null) {
        Debug.LogWarning("No settings assigned!");
        return;
    }
    // ...
}
```

**En jeu (build) :**
Impossible de delete un SO en runtime, ils sont compilés dans le jeu.

**Best practice :**
- Toujours assigner un SO par défaut dans le prefab
- Validation avec `[RequireField]` (custom attribute) ou odin inspector

**Si vraiment on veut changer dynamiquement :**
```csharp
public void SetSettings(EnemyBehaviorSettings newSettings) {
    if (newSettings == null) return;
    settings = newSettings;
    InitializeBehavior();
}
```

---

### Q13: Pourquoi ne pas utiliser le pattern State avec des classes pour chaque état ?

**Réponse courte :**
Overkill pour nos besoins. 4 états simples ≠ besoin de 4 classes.

**Réponse détaillée :**

**Pattern State classique (GoF) :**
```csharp
interface IState {
    void Enter();
    void Execute();
    void Exit();
}

class IdleState : IState { /* ... */ }
class ChasingState : IState { /* ... */ }
// etc.
```

**Notre approche (switch-based) :**
```csharp
switch(state) {
    case Idle: HandleIdleState(); break;
    // ...
}
```

**Comparaison :**

| Critère | Pattern State (classes) | Switch-based |
|---------|------------------------|--------------|
| Nombre de fichiers | 4+ (une classe/état) | 1 fichier |
| Complexité | Élevée | Faible |
| Enter/Exit hooks | ✅ Explicites | ⚠️ Manuel |
| Performance | 🐢 Polymorphisme | ⚡ Direct |
| Réutilisation états | ✅ Possible | ❌ Non |
| Overhead mémoire | 4+ instances | 1 enum |

**Quand utiliser Pattern State :**
- États complexes avec beaucoup de logique
- Besoin de réutiliser des états entre différents contextes
- > 10 états

**Notre cas :**
- 4 états simples
- Logique spécifique au comportement
- Switch = plus simple et performant

**Compromis :** On pourrait ajouter Enter/Exit si besoin :
```csharp
if (previousState != detectionState) {
    OnStateExit(previousState);
    OnStateEnter(detectionState);
}
```

---

## Conseils pour la soutenance

### Si vous ne savez pas répondre :
1. **Admettez-le honnêtement** : "Je n'ai pas exploré cette piste"
2. **Proposez une réflexion** : "Mais je pense qu'on pourrait..."
3. **Ramenez à vos forces** : "Dans notre cas, on a choisi X parce que..."

### Phrases à préparer :
- "On a privilégié la simplicité et la performance"
- "Le système est extensible via le pattern Strategy"
- "Les ScriptableObjects permettent le data-driven design"
- "L'architecture respecte les principes SOLID"

### Points forts à mettre en avant :
✅ Code testable  
✅ Performance (O(1) par ennemi)  
✅ Scalable (100+ ennemis)  
✅ Maintenable (séparation des responsabilités)  
✅ Designer-friendly (pas besoin de coder pour nouveaux ennemis)

### Démonstration possible :
1. Montrer un ennemi en jeu avec les Gizmos
2. Créer un nouvel ennemi sans code (dupliquer SO)
3. Tweaker les valeurs en play mode (temps réel)
4. Montrer l'état dans l'Inspector pendant le jeu
