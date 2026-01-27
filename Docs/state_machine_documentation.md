# State Machines + Scriptable Objects
## Architecture IA des Ennemis - DefoulWar

---

## 📋 SLIDE 1 : Vue d'ensemble

**En un paragraphe :** Notre système combine State Machines (machines à états) et Scriptable Objects pour créer des comportements d'ennemis modulaires : les State Machines gèrent la logique de décision (quand poursuivre, attaquer, chercher), tandis que les Scriptable Objects stockent les paramètres configurables (vitesses, portées), permettant ainsi de créer des dizaines de variants d'ennemis en dupliquant simplement un asset et en modifiant quelques valeurs, sans jamais toucher au code, tout en maintenant une architecture testable et performante.

### Concept central
**State Machines + Scriptable Objects** pour des comportements ennemis modulaires

### Objectifs
✓ Créer des variants d'ennemis **sans coder**  
✓ Modifier comportements **en temps réel**  
✓ Réutiliser configurations entre ennemis  
✓ Architecture **maintenable** et **extensible**

### Pourquoi ce choix ?
**Alternative 1 :** MonoBehaviour avec tous les comportements dans une classe
- ❌ Difficulté à maintenir (classe énorme)
- ❌ Impossible de réutiliser les configurations
- ❌ Modification = recompilation

**Alternative 2 :** Prefabs différents pour chaque ennemi
- ❌ Duplication de code
- ❌ Changement global difficile

**✅ Notre solution :** State Machines + ScriptableObjects
- Configuration séparée du code
- Variants par simple duplication d'assets
- Testabilité et modularité maximales

---

## 🏗️ SLIDE 2 : Architecture globale

**En un paragraphe :** L'architecture repose sur 3 couches séparées : le ScriptableObject stocke les données de configuration (vitesses, portées, type d'attaque) et est partagé entre tous les ennemis du même type pour économiser la mémoire ; le Behavior (une classe C# pure) contient la logique de décision avec une state machine à 4 états (Idle → Chasing → Investigating → Lost) qui détermine quand poursuivre, attaquer ou chercher le joueur ; et le MonoBehaviour fait le lien entre les deux en appelant Execute() 60 fois par seconde sur le Behavior, qui lit le ScriptableObject pour ses paramètres et retourne ses décisions, puis le MonoBehaviour exécute les actions concrètes (mouvement NavMesh, attaque).

```
┌─────────────────────────────────────────────────────────────┐
│                    ENEMY GAMEOBJECT                         │
│                                                             │
│  ┌───────────────────────────────────────────────────┐    │
│  │         EnemyBehaviour (MonoBehaviour)            │    │
│  │                                                   │    │
│  │  • Orchestre le comportement                     │    │
│  │  • Appelle Execute() chaque frame               │    │
│  │  • Gère les attaques                             │    │
│  └────────┬─────────────────────────┬────────────────┘    │
│           │                         │                      │
│           ▼                         ▼                      │
│  ┌────────────────┐       ┌─────────────────────┐        │
│  │ ScriptableObject│◄──────│  IEnemyBehavior     │        │
│  │   (Settings)    │       │   (Interface)       │        │
│  │                 │       │                     │        │
│  │ • Type          │       │ • Initialize()      │        │
│  │ • Vitesses      │       │ • Execute()         │        │
│  │ • Portées       │       │ • CanAttack()       │        │
│  │ • Trajectoires  │       │ • OnDamageTaken()   │        │
│  └────────────────┘       └──────────┬──────────┘        │
│                                       │                    │
│                                       ▼                    │
│                          ┌────────────────────────┐       │
│                          │ BaseEnemyBehavior      │       │
│                          │  (Classe abstraite)    │       │
│                          │                        │       │
│                          │ • State Machine        │       │
│                          │ • Vision FOV           │       │
│                          │ • Trajectoires         │       │
│                          └──────────┬─────────────┘       │
│                                     │                      │
│              ┌──────────────────────┼─────────────┐       │
│              ▼                      ▼             ▼       │
│       ┌─────────────┐      ┌──────────────┐  ┌────────┐ │
│       │ChaserBehavior│      │DistanceBehavior│  │  ...   │ │
│       └─────────────┘      └──────────────┘  └────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 SLIDE 3 : Scriptable Object (SO)

**En un paragraphe :** Le ScriptableObject EnemyBehaviorSettings est un asset de configuration réutilisable qui centralise tous les paramètres d'un type d'ennemi (type de comportement, vitesses de poursuite/patrouille, portées de détection/attaque, type de trajectoire, dégâts) et qui, contrairement à des variables publiques dans un MonoBehaviour, est partagé en mémoire entre toutes les instances d'ennemis utilisant le même asset (économie de 95% de mémoire), permet le tweaking en temps réel pendant le play mode, et rend les designers autonomes car créer un variant "Sniper Rapide" vs "Sniper Lent" = simplement dupliquer le ScriptableObject et changer la valeur chaseSpeed, sans programmer.

### EnemyBehaviorSettings.cs

**Rôle :** Asset de configuration réutilisable

**Paramètres configurables :**
```
┌────────────────────────────────────┐
│  BEHAVIOR TYPE                     │
│  • Chaser / Distance / Patrol      │
├────────────────────────────────────┤
│  MOVEMENT                          │
│  • Detection Range: 15m            │
│  • Chase Speed: 3.5 m/s            │
│  • Patrol Speed: 2.0 m/s           │
├────────────────────────────────────┤
│  ATTACK                            │
│  • Type: Melee / Ranged            │
│  • Damage: 10                      │
│  • Cooldown: 1.5s                  │
│  • Attack Range: 2m                │
├────────────────────────────────────┤
│  TRAJECTOIRE                       │
│  • Type: Zigzag / Spiral / ...     │
│  • Amplitude: 3m                   │
└────────────────────────────────────┘
```

**Avantage :** Créer "Sniper Rapide" et "Sniper Lent" = dupliquer SO + modifier vitesse !

### Pourquoi un Scriptable Object ?
**Alternative :** Variables publiques dans MonoBehaviour
- ❌ Valeurs perdues entre instances
- ❌ Pas de réutilisation entre ennemis
- ❌ Modification dans chaque prefab individuellement

**✅ Scriptable Object :**
- Asset partagé en mémoire (performance)
- Modification dans 1 fichier = tous les ennemis mis à jour
- Permet le tweaking en temps réel pendant le play mode
- Designers autonomes (pas besoin de programmer)

---

## 🔄 SLIDE 4 : State Machine de détection

**En un paragraphe :** La state machine de détection possède 4 états (Idle, Chasing, Investigating, Lost) au lieu de seulement 2 (Idle/Chasing) pour éviter un comportement robotique : quand l'ennemi perd le joueur de vue pendant une poursuite, il ne retourne pas instantanément au repos mais passe en état Investigating pour se rendre à la dernière position connue et chercher pendant 3 secondes, ce qui crée un comportement plus intelligent et réaliste ; les transitions entre états sont automatiques et basées sur la vision (angle FOV + raycast de ligne de vue), la distance au joueur, et des timers, permettant également la coordination avec le système d'alerte qui prévient les ennemis proches.

### 4 États principaux

```
                    ┌──────────────────┐
                    │   START / INIT   │
                    └────────┬─────────┘
                             │
                             ▼
                    ┌─────────────────┐
              ┌─────┤      IDLE       │
              │     │  (Au repos)     │
              │     └────────┬────────┘
              │              │
              │              │ Joueur détecté
              │              │ (Vision FOV)
              │              ▼
              │     ┌─────────────────┐
              │     │    CHASING      │◄────────────┐
              │     │  (Poursuite)    │             │
              │     └────────┬────────┘             │
              │              │                       │
              │              │ Perte de vue         │ Joueur
              │              │                       │ retrouvé
              │              ▼                       │
              │     ┌─────────────────┐             │
              │     │ INVESTIGATING   │─────────────┘
              │     │  (Va à dernière │
              │     │   position)     │
              │     └────────┬────────┘
              │              │
              │              │ Timer expiré
              │              │ (3 secondes)
              │              ▼
              │     ┌─────────────────┐
              └────►│      LOST       │
                    │  (Cible perdue) │
                    └─────────────────┘
```

**Transitions automatiques** basées sur :
- Vision (angle FOV + raycast)
- Distance
- Timers

### Pourquoi 4 états ?
**Alternative :** 2 états simples (Idle / Chasing)
- ❌ Comportement robotique (perd instantanément le joueur)
- ❌ Pas de mémoire spatiale
- ❌ Pas réaliste

**✅ Notre système à 4 états :**
- **Investigating** : L'ennemi cherche le joueur à sa dernière position
- **Lost** : Phase de transition avant retour au calme
- Comportement plus naturel et intelligent
- Permet la coordination avec système d'alerte

---

## 🧩 SLIDE 5 : BaseEnemyBehavior

**En un paragraphe :** BaseEnemyBehavior est la classe abstraite qui factorise toutes les fonctionnalités communes à tous les comportements ennemis : le système de vision avec angle de vue (FOV de 120°), distance de détection (15m), rayon d'écoute 360° (8m pour détecter le joueur très proche même dans le dos), et raycasts pour vérifier la ligne de vue ; la mémoire spatiale qui sauvegarde la dernière position connue du joueur avec timer d'investigation de 3 secondes ; le système d'alerte qui prévient les ennemis dans un rayon configurable ; les trajectoires avancées (zigzag, sinusoïdal, spirale, random) pour rendre les déplacements imprévisibles ; et le mode arène qui bypass la détection pour que les ennemis connaissent toujours la position du joueur dans les arènes fermées.

### Fonctionnalités communes

```
┌─────────────────────────────────────────────────────┐
│               BaseEnemyBehavior                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  SYSTÈME DE VISION                               │
│  - Angle de vue (FOV): 120°                       │
│  - Distance détection: 15m                        │
│  - Rayon d'écoute 360°: 8m                        │
│  - Raycast ligne de vue (obstacles)               │
│                                                     │
│  MÉMOIRE SPATIALE                                │
│  - Dernière position connue du joueur             │
│  - Timer d'investigation: 3s                      │
│  - Système d'alerte entre ennemis                 │
│                                                     │
│   TRAJECTOIRES PARTICULIÈRES                        │
│  - Zigzag (changements directionnels)             │
│  - Sinusoïdal (vague continue)                    │
│  - Spirale (convergence circulaire)               │
│  - Random (imprévisible)                          │
│                                                     │
│   MODE ARÈNE                                      │
│  - Bypass détection (ennemi sait toujours où      │
│    est le joueur dans les arènes fermées)         │
└─────────────────────────────────────────────────────┘
```

---

## 🎮 SLIDE 6 : Exemple - ChaserBehavior

**En un paragraphe :** ChaserBehavior illustre un comportement simple de poursuite directe où la logique est organisée par état via un switch sur l'enum detectionState : en Idle il attend et surveille, en Chasing il poursuit le joueur avec trajectoire configurable et attaque si distance < attackRange, en Investigating il va à la dernière position connue, et en Lost il retourne au repos ; cette approche avec une classe par comportement (pattern Strategy) plutôt qu'un gros script avec des if/else permet d'isoler chaque comportement, de respecter le principe Open/Closed (nouveau comportement = nouvelle classe sans modifier l'existant), et de tester chaque comportement indépendamment.

### Comportement simple : Poursuite

```
protected override void ExecuteBehavior()
{
    switch (detectionState)
    {
        case Idle:
            ├─ Attendre
            └─ Si joueur visible → CHASING
            
        case Chasing:
            ├─ Poursuivre le joueur
            ├─ Si distance < attackRange → Attaquer
            └─ Si perte de vue → INVESTIGATING
            
        case Investigating:
            ├─ Aller à dernière position connue
            ├─ Si joueur retrouvé → CHASING
            └─ Si timer expiré → LOST
            
        case Lost:
            └─ Retour à IDLE
    }
}
```

**Logique :** Switch sur enum → Code lisible et maintenable

### Pourquoi une classe par comportement ?
**Alternative :** Un seul script avec des if/else sur le type
- ❌ Code spaghetti difficile à lire
- ❌ Modification d'un comportement = risque de casser les autres
- ❌ Impossible de tester indépendamment

**✅ Pattern Strategy (Interface + classes) :**
- Chaque comportement est isolé
- Ajout d'un nouveau comportement = nouvelle classe (Open/Closed principle)
- Code organisé et testable
- Responsabilité unique par classe

---

## 🎯 SLIDE 7 : Exemple - DistanceBehavior (Sniper)

**En un paragraphe :** DistanceBehavior est un comportement complexe qui utilise 2 state machines imbriquées pour séparer les préoccupations : la state machine de détection standard (Idle/Chasing/Investigating/Lost) détermine "Suis-je en combat ?", tandis qu'une state machine de charge sniper (None → Charging → Locked) détermine "Comment attaquer ?" avec un laser rouge qui suit le joueur pendant 1 seconde (phase Charging où l'ennemi ralentit), puis un laser vert sur position verrouillée pendant 0,5 seconde (phase Locked avec prédiction de mouvement), puis tir ; cette séparation évite une explosion combinatoire d'états (ex: ChasingAndCharging, ChasingAndLocked, etc.) et permet à d'autres comportements d'avoir leur propre sous-state machine (ex: GroundSlamBehavior pourrait avoir Jump → Fall → Impact).

### Comportement complexe : 2 State Machines imbriquées

#### State Machine 1 : Détection
Standard (Idle → Chasing → Investigating → Lost)

#### State Machine 2 : Charge Sniper

```
     ┌─────────────┐
     │    NONE     │  (Pas de charge)
     └──────┬──────┘
            │ Joueur à portée
            ▼
     ┌─────────────┐
     │  CHARGING   │   Laser ROUGE suit le joueur
     │  (1.0s)     │     Ennemi ralentit
     └──────┬──────┘
            │ Timer écoulé
            ▼
     ┌─────────────┐
     │   LOCKED    │   Laser VERT position fixe
     │  (0.5s)     │     Position verrouillée
     └──────┬──────┘
            │ Timer écoulé
            ▼
         Tir
```

**Particularité :** Recherche points en hauteur (tag "HighGroundPoint")

### Pourquoi 2 state machines ?
**Alternative :** Une seule state machine avec états combinés
- Ex: "ChasingAndCharging", "ChasingAndLocked", etc.
- ❌ Explosion combinatoire des états
- ❌ Code dupliqué entre états similaires

**✅ State machines imbriquées :**
- **Détection** (Idle/Chasing/...) : "Suis-je en combat ?"
- **Charge** (None/Charging/Locked) : "Comment attaquer ?"
- Séparation des préoccupations
- Réutilisable : autre comportement peut avoir sa propre sous-state machine
- Plus facile à comprendre et maintenir

**Exemple :** GroundSlamBehavior pourrait avoir sa propre state machine de "Jump → Fall → Impact" pendant l'état Chasing

---

## 🔁 SLIDE 8 : Flux d'exécution (Runtime)

**En un paragraphe :** Chaque frame (60 fois par seconde), le MonoBehaviour EnemyBehaviour appelle Execute() sur le Behavior actuel, qui vérifie l'état de détection (voit-il le joueur ? quelle distance ?), lit les paramètres du ScriptableObject (detectionRange, chaseSpeed, attackRange), exécute la logique du state actuel via ExecuteBehavior() qui met à jour le NavMeshAgent (destination, vitesse) et la rotation vers la cible, puis retourne si l'attaque est possible via CanAttack(), et si oui le MonoBehaviour exécute attackHandler.TryAttack() ; ce flux en O(1) par ennemi permet de gérer 100+ ennemis simultanés sans problème de performance car chaque ennemi a sa propre instance de Behavior avec ses variables d'état indépendantes.

```
Unity Update Loop
       │
       ▼
┌──────────────────┐
│ EnemyBehaviour   │
│   Update()       │
└────────┬─────────┘
         │
         │ 1. Execute le comportement
         ▼
┌─────────────────────┐
│  currentBehavior    │
│    .Execute()       │
└────────┬────────────┘
         │
         │ 2. Vérifie état détection
         ▼
┌─────────────────────────────┐
│ Lire settings (SO)          │
│ ├─ detectionRange           │
│ ├─ chaseSpeed               │
│ └─ attackRange              │
└────────┬────────────────────┘
         │
         │ 3. Exécute logique du state
         ▼
┌─────────────────────────────┐
│ ExecuteBehavior()           │
│ ├─ switch(detectionState)   │
│ ├─ Mettre à jour NavMeshAgent│
│ └─ Rotation vers cible      │
└────────┬────────────────────┘
         │
         │ 4. Vérifier attaque
         ▼
┌─────────────────────────────┐
│ if (CanAttack())            │
│   attackHandler.TryAttack() │
└─────────────────────────────┘
```

**60 fois par seconde !**

---

## ✅ SLIDE 9 : Avantages architecture

**En un paragraphe :** Cette architecture applique les principes SOLID avec une séparation stricte des responsabilités : le ScriptableObject contient uniquement les données de configuration (Single Responsibility), les Behaviors contiennent uniquement la logique métier sans dépendance directe à Unity (testables avec NUnit/XUnit via mocking), et le MonoBehaviour orchestre uniquement l'interaction avec Unity ; le principe Open/Closed est respecté car ajouter un nouveau comportement = créer une nouvelle classe qui implémente IEnemyBehavior sans modifier le code existant ; le principe Dependency Inversion est appliqué car EnemyBehaviour dépend de l'interface IEnemyBehavior et non des implémentations concrètes, facilitant les tests avec des mocks ; résultat : code modulaire, testable, performant (SO partagés entre ennemis), et flexible (variants sans programmer).

### Séparation des responsabilités

```
┌───────────────────────────────────────┐
│  SCRIPTABLE OBJECTS                   │
│  → Configuration pure (données)       │
│  → Pas de logique                     │
└───────────────────────────────────────┘

┌───────────────────────────────────────┐
│  BEHAVIORS (IEnemyBehavior)           │
│  → Logique métier (algorithmes)       │
│  → Pas de dépendance Unity directe    │
└───────────────────────────────────────┘

┌───────────────────────────────────────┐
│  ENEMYBEHAVIOUR (MonoBehaviour)       │
│  → Orchestration (glue code)          │
│  → Interface avec Unity               │
└───────────────────────────────────────┘
```

### Bénéfices
✓ **Modularité** : Nouveau comportement = nouvelle classe  
✓ **Testabilité** : Behaviors testables sans Unity  
✓ **Performance** : SO partagés entre ennemis (mêmes données)  
✓ **Flexibilité** : Variants d'ennemis sans programmer  

### Pourquoi cette séparation ?
**Principe SOLID appliqué :**

**Single Responsibility :**
- ScriptableObject = données uniquement
- Behavior = logique uniquement
- MonoBehaviour = orchestration uniquement

**Open/Closed :**
- Ouvert à l'extension (nouveau behavior = nouvelle classe)
- Fermé à la modification (pas besoin de toucher le code existant)

**Dependency Inversion :**
- EnemyBehaviour dépend de l'interface IEnemyBehavior
- Pas de dépendance directe aux implémentations concrètes
- Facilite les tests avec des mocks

---

## 🛠️ SLIDE 10 : Créer un nouvel ennemi (SANS CODE)

**En un paragraphe :** Créer un nouvel ennemi prend 30 secondes sans écrire de code : clic droit dans Assets → Create → Enemies → Behavior Settings pour créer le ScriptableObject, le nommer (ex: "FastChaser"), configurer ses paramètres (Behavior Type: Chaser, Chase Speed: 6.0, Attack Cooldown: 0.8, Attack Range: 3.0), puis drag & drop ce ScriptableObject dans le champ settings du composant EnemyBehaviour sur le prefab ennemi ; cette approche data-driven rend les designers complètement autonomes car ils peuvent créer des dizaines de variants d'ennemis (Sniper Rapide, Sniper Lent, Tank Lourd, Rusher Agile, etc.) en dupliquant et modifiant des assets, la logique étant entièrement réutilisée.

### Étape 1 : Créer un ScriptableObject
```
Clic droit dans Assets
  → Create
    → Enemies
      → Behavior Settings
```

### Étape 2 : Configurer
```
Nom: "FastChaser"
┌────────────────────────┐
│ Behavior Type: Chaser  │
│ Chase Speed: 6.0       │
│ Attack Cooldown: 0.8   │
│ Attack Range: 3.0      │
└────────────────────────┘
```

### Étape 3 : Assigner au GameObject
Drag & Drop le SO "FastChaser" dans le champ `settings` du composant `EnemyBehaviour`

### ✨ Résultat
**Nouvel ennemi créé en 30 secondes !**

---

## 🚀 SLIDE 11 : Étendre le système (Nouveau comportement)

**En un paragraphe :** Étendre le système avec un nouveau comportement (ex: Teleporter) nécessite 3 étapes simples : ajouter l'enum (Teleporter) dans EnemyBehaviorType, créer la classe TeleporterBehavior qui hérite de BaseEnemyBehavior et implémente ExecuteBehavior() avec la logique de téléportation (tous les avantages de la classe de base comme vision, state machine, trajectoires sont automatiquement hérités), puis enregistrer ce nouveau type dans la factory CreateBehavior() avec un simple case statement ; le système gère automatiquement le reste (initialisation, exécution 60 fois/seconde, attaques) car le nouveau comportement respecte le contrat IEnemyBehavior, illustrant le principe Open/Closed où le système est ouvert à l'extension mais fermé à la modification.

### Ajouter un comportement "Teleporter"

**1. Ajouter l'enum**
```csharp
public enum EnemyBehaviorType
{
    Chaser,
    Distance,
    Teleporter  // ← NOUVEAU
}
```

**2. Créer la classe**
```csharp
public class TeleporterBehavior : BaseEnemyBehavior
{
    protected override void ExecuteBehavior()
    {
        // Logique de téléportation
        if (detectionState == Chasing)
        {
            TeleportBehindPlayer();
        }
    }
}
```

**3. Enregistrer dans la factory**
```csharp
case EnemyBehaviorType.Teleporter: 
    return new TeleporterBehavior();
```

**C'est tout !** Le système gère automatiquement le reste.

---

## 📊 SLIDE 12 : Résumé

**En un paragraphe :** L'architecture State Machines + Scriptable Objects offre une solution scalable pour gérer des dizaines de variants d'ennemis avec 5 types de comportements implémentés (Chaser pour poursuite directe, Distance pour sniper avec recherche de hauteur et charge laser, ZonePatrol pour patrouille dans une zone définie, FollowCompanion pour escorte d'alliés tankés, GroundSlam pour attaque en zone au sol) ; les avantages majeurs sont la création de contenu par designers autonomes sans code, la maintenance facilitée par la séparation des responsabilités, la performance optimale grâce aux ScriptableObjects partagés et state machines légères (O(1) par ennemi), l'extensibilité via nouveaux comportements par simple ajout de classes, et la testabilité des Behaviors isolés de Unity ; cette architecture respecte les principes SOLID et permet de supporter 100+ ennemis simultanés sans impact sur les performances.

### State Machines + Scriptable Objects

| Aspect | Avantage |
|--------|----------|
| **Création contenu** | Designers autonomes (pas de code) |
| **Maintenance** | Logique séparée et modulaire |
| **Performance** | SO partagés, state machines légères |
| **Extensibilité** | Nouveaux comportements faciles |
| **Testabilité** | Behaviors isolés de Unity |

### Types de comportements implémentés
- **Chaser** : Poursuite directe
- **Distance** : Sniper avec recherche de hauteur
- **ZonePatrol** : Patrouille dans une zone
- **FollowCompanion** : Escorte d'alliés
- **GroundSlam** : Attaque zone au sol

**Architecture scalable pour des dizaines de variants d'ennemis**
