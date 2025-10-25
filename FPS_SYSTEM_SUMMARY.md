# Système FPS avec Vagues Adaptatives - Récapitulatif

## 📋 Scripts Créés

### Joueur
1. **FPSPlayerController.cs** - Contrôleur de mouvement et caméra FPS
2. **PlayerHealth.cs** - Gestion de la santé avec régénération
3. **WeaponController.cs** - Système d'arme avec raycast et munitions

### Ennemis
4. **EnemyController.cs** - IA NavMesh (poursuite et attaque)
5. **EnemyHealth.cs** - Santé avec tracking des zones touchées
6. **HitZone.cs** - Zones de dégâts avec système d'armure
7. **EnemyVisualFeedback.cs** - Effets visuels (hit feedback, armure)

### Système de Jeu
8. **WaveManager.cs** - Gestionnaire de vagues avec adaptation intelligente
9. **GameUI.cs** - Interface utilisateur complète
10. **GameManager.cs** - Gestion pause/game over
11. **FPSDebugDisplay.cs** - Affichage de debug (F3)

### Outils Editor
12. **FPSSetupEditor.cs** - Menu Unity pour configuration rapide

## 🎯 Fonctionnalités Principales

### ✅ Système d'Adaptation Intelligent
- **Enregistrement des tirs** : Chaque zone touchée est comptabilisée
- **Analyse entre vagues** : Les statistiques déterminent les zones à renforcer
- **Application d'armures** : Les ennemis reçoivent des protections aux endroits ciblés
- **Exemple** : Si vous faites beaucoup de headshots → ennemis avec casques à la vague suivante

### ✅ Contrôleur FPS Complet
- Mouvement WASD avec sprint
- Caméra FPS avec souris
- Saut et gravité
- CharacterController pour collisions réalistes

### ✅ Système d'Arme
- Tir au raycast précis
- Munitions et rechargement (30 balles, 2s reload)
- Recul et récupération
- Support pour effets visuels et audio

### ✅ IA Ennemie
- Navigation NavMesh intelligente
- Détection du joueur à distance
- Poursuite et attaque au corps-à-corps
- États : Idle → Chase → Attack

### ✅ Gestion des Vagues
- Difficulté progressive (plus d'ennemis chaque vague)
- Spawn par vagues avec délai
- Compteur d'ennemis restants
- Temps de pause entre vagues

### ✅ Interface Utilisateur
- Barre de vie avec pourcentage
- Compteur de munitions
- Numéro de vague actuel
- Ennemis restants
- Panneau "Vague Terminée"
- Crosshair centré

### ✅ Système de Santé
- Santé joueur avec régénération automatique
- Feedback visuel sur dégâts
- Game Over à la mort
- Système d'événements UnityEvent

### ✅ Outils de Debug
- Affichage FPS en temps réel
- Statistiques de vague en direct
- Compteur de hits par zone
- Toggle avec F3

## 🎮 Contrôles

| Action | Touche |
|--------|--------|
| Déplacement | WASD |
| Regarder | Souris |
| Sauter | Espace |
| Sprint | Shift Gauche |
| Tirer | Clic Gauche |
| Recharger | R |
| Pause | Échap |
| Debug Info | F3 |

## 📊 Architecture

```
Proto3GD/
└── Assets/
    └── Scripts/
        └── FPS/
            ├── FPSPlayerController.cs     # Contrôle joueur
            ├── PlayerHealth.cs            # Santé joueur
            ├── WeaponController.cs        # Arme
            ├── EnemyController.cs         # IA ennemi
            ├── EnemyHealth.cs             # Santé ennemi
            ├── HitZone.cs                 # Zones de dégâts
            ├── EnemyVisualFeedback.cs     # Effets visuels
            ├── WaveManager.cs             # Gestion vagues
            ├── GameUI.cs                  # Interface
            ├── GameManager.cs             # Gestion jeu
            ├── FPSDebugDisplay.cs         # Debug
            └── Editor/
                └── FPSSetupEditor.cs      # Outils Unity
```

## 🚀 Installation Rapide

### 1. Menu Unity (Recommandé)
```
GameObject > FPS System > Create Complete FPS Scene
```
Cela crée automatiquement :
- Joueur avec tous les composants
- Ennemi exemple
- Wave Manager avec spawn points
- Sol et éclairage

### 2. Configurer NavMesh
```
Window > AI > Navigation
- Sélectionner Floor
- Cocher "Navigation Static"
- Onglet Bake > Cliquer "Bake"
```

### 3. Créer le Prefab Ennemi
- Glisser l'ennemi de la scène vers Assets/Prefabs/
- Assigner au Wave Manager

### 4. Configuration Input System
Ajouter ces actions dans votre InputActions :
- Move (Vector2)
- Jump (Button)
- Sprint (Button)
- Fire (Button)
- Reload (Button)

### 5. Créer l'UI
Voir FPS_QUICKSTART.md pour les détails complets

## 💡 Exemple de Gameplay

### Vague 1
```
Spawn: 5 ennemis sans armure
Joueur tire: 30 headshots, 10 body shots
Résultat: Vague 1 terminée
```

### Vague 2
```
Système analyse: "Head" = 30 hits (> seuil de 10)
Spawn: 7 ennemis AVEC CASQUES
Effect: Les dégâts à la tête sont réduits de 60%
Joueur adapte: Cible le corps à la place
```

### Vague 3
```
Système analyse: "Body" = 50 hits, "Head" = 20 hits
Spawn: 9 ennemis avec CASQUES + GILETS PARE-BALLES
Difficulté: Le joueur doit viser les jambes ou compenser
```

## ⚙️ Paramètres Configurables

### WaveManager
- `Starting Enemies Per Wave` : 5
- `Enemies Increase Per Wave` : 2
- `Time Between Waves` : 5 secondes
- `Hit Threshold For Armor` : 10 hits minimum
- `Top Zones To Reinforce` : 2 zones max

### Joueur
- `Move Speed` : 5 m/s
- `Sprint Speed` : 8 m/s
- `Max Health` : 100
- `Regen Rate` : 5 HP/s après 3s

### Arme
- `Damage` : 25
- `Fire Rate` : 0.1s (10 coups/sec)
- `Max Ammo` : 30
- `Reload Time` : 2s

### Ennemis
- `Max Health` : 100
- `Chase Speed` : 3.5 m/s
- `Detection Range` : 15m
- `Attack Damage` : 10
- `Attack Cooldown` : 1.5s

## 🎨 Zones de Dégâts Personnalisables

```csharp
// Tête (critique)
Zone: "Head"
Multiplier: 2.0x
Armor Reduction: 60%

// Torse (normal)
Zone: "Body" / "Chest"
Multiplier: 1.0x
Armor Reduction: 60%

// Jambes (réduit)
Zone: "Legs"
Multiplier: 0.5x
Armor Reduction: 40%

// Bras (réduit)
Zone: "Arms"
Multiplier: 0.7x
Armor Reduction: 50%
```

## 🔧 Personnalisation

### Ajouter une nouvelle zone
1. Créer un GameObject enfant sur l'ennemi
2. Ajouter un Collider
3. Ajouter le composant `HitZone`
4. Configurer nom et multiplicateur

### Modifier la difficulté
**Plus facile** :
- Réduire `Attack Damage` des ennemis
- Augmenter `Regen Rate` du joueur
- Réduire `Enemies Increase Per Wave`

**Plus difficile** :
- Augmenter nombre d'ennemis
- Réduire `Hit Threshold For Armor` (armures plus fréquentes)
- Augmenter vitesse des ennemis

### Ajouter des effets
```csharp
// Dans WeaponController
[SerializeField] private ParticleSystem muzzleFlash;
[SerializeField] private GameObject impactEffect;

// Dans EnemyVisualFeedback
[SerializeField] private GameObject helmetPrefab;
[SerializeField] private GameObject vestPrefab;
```

## 📝 Logs de Debug

Le système affiche automatiquement :
```
Wave 1 started! Enemies: 5
Enemy killed! Remaining: 4
Wave 1 complete!
Hit statistics:
  Head: 30 hits
  Body: 10 hits
Next wave will have armor on: Head
Applied armor to zones: Head
```

## 🐛 Résolution de Problèmes

| Problème | Solution |
|----------|----------|
| Joueur ne bouge pas | Vérifier Input Actions assignées |
| Ennemis ne bougent pas | Bake le NavMesh |
| Arme ne tire pas | Vérifier caméra assignée |
| Pas d'armure | Dépasser le seuil de hits (10 par défaut) |
| UI invisible | Vérifier Canvas en Screen Space - Overlay |

## 📚 Documentation Complète

- **FPS_QUICKSTART.md** : Guide de démarrage détaillé (5 min)
- **Scripts/** : Commentaires XML dans chaque fichier
- **Menu Unity** : GameObject > FPS System > ...

## 🎓 Prochaines Étapes

1. ✅ **Fonctionnalités de base** (FAIT)
   - Mouvement FPS
   - Tir et dégâts
   - Vagues d'ennemis
   - Système d'adaptation

2. 🎨 **Améliorer les visuels**
   - Remplacer primitives par modèles 3D
   - Ajouter animations ennemis
   - Effets de particules

3. 🔊 **Ajouter l'audio**
   - Sons de tir
   - Sons d'impact
   - Musique de fond
   - Voix d'annonce de vague

4. 🎮 **Nouvelles mécaniques**
   - Power-ups (santé, munitions)
   - Armes multiples
   - Boss toutes les 5 vagues
   - Système de score

5. 🎯 **Polish**
   - Menu principal
   - Écran de game over
   - Sauvegarde du meilleur score
   - Achievements

## 💻 Technologies Utilisées

- **Unity 6** (6000.0.58f2+)
- **C# 9.0+**
- **NavMesh AI**
- **Character Controller**
- **Unity Events**
- **TextMeshPro**
- **Input System** (optionnel, compatible aussi avec l'ancien système)

## 📄 License

Ce code fait partie du projet Proto3GD et suit les mêmes conventions que le système Slime existant.

---

**Créé pour Proto3GD** | Système FPS Adaptatif v1.0

