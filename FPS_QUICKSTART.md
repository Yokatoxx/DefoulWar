# Guide de Démarrage Rapide - Système FPS avec Vagues Adaptatives

## Vue d'ensemble

Ce système FPS implémente un système de vagues d'ennemis qui s'adaptent à votre style de jeu. Les ennemis gagneront des armures sur les zones que vous ciblez le plus (tête, torse, etc.).

## Installation Rapide (5 minutes)

### Étape 1: Créer la scène

1. **Menu Unity**: `GameObject > FPS System > Create Complete FPS Scene`
   - Cela crée automatiquement: joueur, ennemi exemple, wave manager, sol

### Étape 2: Configurer le NavMesh

1. **Menu Unity**: `Window > AI > Navigation`
2. Sélectionnez le `Floor` dans la hiérarchie
3. Cochez `Navigation Static`
4. Onglet `Bake` > Cliquez sur `Bake`

### Étape 3: Créer le Prefab Ennemi

1. Sélectionnez l'ennemi dans la scène (créé à l'étape 1)
2. Glissez-le dans le dossier `Assets/Prefabs/` pour créer un prefab
3. Sélectionnez le `WaveManager`
4. Assignez le prefab ennemi dans le champ `Enemy Prefab`
5. Assignez les `Spawn Points` (enfants du WaveManager)

### Étape 4: Configurer les Input Actions

#### Option A: Utiliser l'Input System existant

Si vous avez déjà `InputSystem_Actions.inputactions`:

1. Ouvrez le fichier
2. Ajoutez ces actions au map "Player":
   - `Move` (Vector2) → WASD
   - `Jump` (Button) → Space
   - `Sprint` (Button) → Left Shift
   - `Fire` (Button) → Mouse Left
   - `Reload` (Button) → R

#### Option B: Créer un nouveau Input Actions

1. Clic droit dans Assets → `Create > Input Actions`
2. Nommez-le `FPS_InputActions`
3. Créez un Action Map "Player"
4. Ajoutez les actions ci-dessus

#### Assigner les Input Actions

1. Sélectionnez `FPS_Player` dans la hiérarchie
2. Dans `FPSPlayerController`:
   - Créez des Input Action References pour Move, Jump, Sprint
3. Dans `WeaponController`:
   - Créez des Input Action References pour Fire, Reload

### Étape 5: Créer l'Interface Utilisateur

1. **Créer un Canvas**:
   - `GameObject > UI > Canvas`
   - Canvas Scaler → UI Scale Mode: Scale With Screen Size

2. **Barre de vie**:
   ```
   Canvas
   └── HealthPanel
       ├── HealthBar (Slider)
       └── HealthText (TextMeshPro)
   ```

3. **Munitions**:
   ```
   Canvas
   └── WeaponPanel
       ├── AmmoText (TextMeshPro)
       └── ReloadText (TextMeshPro)
   ```

4. **Info de vague**:
   ```
   Canvas
   ├── WaveText (TextMeshPro)
   ├── EnemiesText (TextMeshPro)
   └── WaveCompletePanel
       └── CompleteText (TextMeshPro)
   ```

5. **Crosshair**:
   ```
   Canvas
   └── Crosshair (Image)
   ```

6. **Assigner au GameUI**:
   - Créez un GameObject vide nommé `GameUI`
   - Ajoutez le composant `GameUI`
   - Assignez tous les éléments UI

### Étape 6: Configuration des Tags et Layers

1. Assurez-vous que ces tags existent:
   - `Player`
   - `Enemy`

2. Le joueur doit avoir le tag `Player`
3. Les ennemis doivent avoir le tag `Enemy`

## Architecture du Système

### Composants Joueur

- **FPSPlayerController**: Mouvement et caméra FPS
- **PlayerHealth**: Gestion de la santé avec régénération
- **WeaponController**: Système d'arme avec raycast et munitions

### Composants Ennemis

- **EnemyController**: IA avec NavMesh (poursuite et attaque)
- **EnemyHealth**: Santé avec tracking des zones touchées
- **HitZone**: Zones de dégâts (tête, corps) avec système d'armure

### Système de Vagues

- **WaveManager**: Gère les vagues et l'adaptation
  - Enregistre quelle zone est touchée à chaque tir
  - Analyse les statistiques après chaque vague
  - Applique des armures aux zones les plus touchées

### Interface Utilisateur

- **GameUI**: Affiche santé, munitions, vague, ennemis restants

## Système d'Adaptation

### Comment ça fonctionne

1. **Pendant la vague**: Chaque fois que vous touchez un ennemi, la zone touchée est enregistrée
2. **Fin de vague**: Le système analyse quelles zones ont été le plus touchées
3. **Prochaine vague**: Les ennemis spawneront avec des armures sur ces zones

### Exemple

```
Vague 1: Vous faites 50 headshots, 20 body shots
→ Vague 2: Les ennemis auront des casques (réduit dégâts tête de 60%)

Vague 2: Vous compensez en tirant sur le corps (80 body shots)
→ Vague 3: Les ennemis auront casques + gilets pare-balles
```

### Configuration de l'Adaptation

Dans le `WaveManager`:
- `Hit Threshold For Armor`: Nombre de hits minimum pour ajouter une armure (défaut: 10)
- `Top Zones To Reinforce`: Combien de zones renforcer (défaut: 2)

## Configuration des Zones de l'Ennemi

### Créer des zones personnalisées

Sur votre prefab ennemi, ajoutez des colliders avec le composant `HitZone`:

```csharp
// Exemple: Tête avec x2 dégâts
Head GameObject:
- Sphere Collider
- HitZone:
  - Zone Name: "Head"
  - Damage Multiplier: 2.0

// Exemple: Corps avec dégâts normaux
Body GameObject:
- Capsule Collider
- HitZone:
  - Zone Name: "Body"
  - Damage Multiplier: 1.0

// Exemple: Jambes avec dégâts réduits
Legs GameObject:
- Capsule Collider
- HitZone:
  - Zone Name: "Legs"
  - Damage Multiplier: 0.5
```

## Paramètres Recommandés

### Joueur

**FPSPlayerController**:
- Move Speed: 5
- Sprint Speed: 8
- Jump Height: 1.5
- Mouse Sensitivity: 2

**PlayerHealth**:
- Max Health: 100
- Regen Delay: 3 secondes
- Regen Rate: 5 HP/sec

**WeaponController**:
- Damage: 25
- Fire Rate: 0.1 (10 coups/sec)
- Range: 100
- Max Ammo: 30
- Reload Time: 2 secondes

### Ennemis

**EnemyController**:
- Chase Speed: 3.5
- Detection Range: 15
- Attack Range: 2
- Attack Damage: 10
- Attack Cooldown: 1.5 secondes

**EnemyHealth**:
- Max Health: 100

### Vagues

**WaveManager**:
- Starting Enemies: 5
- Enemies Increase Per Wave: 2
- Time Between Waves: 5 secondes
- Spawn Delay: 0.5 secondes

## Contrôles par Défaut

- **WASD**: Déplacement
- **Souris**: Regarder autour
- **Espace**: Sauter
- **Shift Gauche**: Sprint
- **Clic Gauche**: Tirer
- **R**: Recharger
- **Échap**: Déverrouiller le curseur

## Dépannage

### Le joueur ne bouge pas
- Vérifiez que les Input Actions sont assignées
- Vérifiez que le `CharacterController` est présent
- Vérifiez que le script `FPSPlayerController` est activé

### Les ennemis ne bougent pas
- Assurez-vous que le NavMesh est baked
- Vérifiez que le tag `Player` est assigné au joueur
- Vérifiez que le `NavMeshAgent` est présent sur l'ennemi

### L'arme ne tire pas
- Vérifiez que la caméra est assignée dans `WeaponController`
- Vérifiez les Input Actions pour Fire
- Vérifiez que vous avez des munitions

### Les ennemis ne reçoivent pas d'armure
- Vérifiez que les `HitZone` sont présentes sur l'ennemi
- Augmentez le nombre de tirs pour dépasser le seuil (hitThresholdForArmor)
- Vérifiez les logs Unity pour voir les statistiques de vague

### L'UI ne s'affiche pas
- Vérifiez que le Canvas est en mode "Screen Space - Overlay"
- Vérifiez que tous les éléments UI sont assignés dans `GameUI`
- Vérifiez que TextMeshPro est installé

## Personnalisation

### Ajouter de nouvelles zones

1. Créez un nouveau GameObject sur votre ennemi (ex: "RightArm")
2. Ajoutez un collider
3. Ajoutez le composant `HitZone`
4. Configurez le nom et le multiplicateur de dégâts

### Modifier la difficulté

**Plus facile**:
- Réduire `Attack Damage` des ennemis
- Augmenter `Max Health` du joueur
- Réduire `Chase Speed` des ennemis

**Plus difficile**:
- Augmenter le nombre d'ennemis par vague
- Réduire le temps entre les vagues
- Augmenter les dégâts des ennemis
- Réduire `Hit Threshold For Armor` (armures plus fréquentes)

### Ajouter des effets visuels

Sur le `WeaponController`:
- `Muzzle Flash`: ParticleSystem pour l'effet de tir
- `Impact Effect`: Prefab pour les impacts de balles

## Prochaines Étapes

1. **Visuels**: Remplacez les primitives par des modèles 3D
2. **Audio**: Ajoutez des sons de tir, pas, impacts
3. **Animations**: Ajoutez des animations d'ennemis (marche, attaque, mort)
4. **Power-ups**: Créez des objets ramassables (santé, munitions)
5. **Boss**: Créez un ennemi boss qui apparaît toutes les 5 vagues
6. **Score**: Ajoutez un système de points
7. **Menu**: Créez un menu principal et game over

## Support

Pour plus d'informations sur l'architecture du système, consultez:
- `Assets/Scripts/FPS/` - Tous les scripts du système
- Commentaires XML dans chaque script

## Exemple de Code

### Créer un pickup de santé

```csharp
using UnityEngine;
using Proto3GD.FPS;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 25f;
    
    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
```

### Créer un nouvel ennemi avec comportement personnalisé

Héritez de `EnemyController` et override les méthodes nécessaires.

Bon jeu! 🎮

