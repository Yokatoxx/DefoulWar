# Système de Dash sur Piliers - Guide d'Utilisation

## Vue d'ensemble

Le système de dash permet au joueur de cibler et détruire les piliers en dashant dessus. Lorsqu'un pilier est à portée, il est automatiquement surligné (highlight), et le joueur peut appuyer sur **E** pour dasher vers lui avec un effet de FOV dynamique.

## Fonctionnalités

### 🎯 Ciblage et Highlight
- Détection automatique des piliers devant le joueur (portée configurable)
- Effet de highlight visuel orange émissif sur le pilier ciblé
- Feedback visuel clair pour savoir quel pilier sera détruit

### 🚀 Système de Dash
- Appuyez sur **E** pour dasher vers le pilier ciblé
- Vitesse de dash élevée (25 m/s par défaut)
- Durée configurable (0.4s par défaut)
- Cooldown entre chaque dash (1.5s par défaut)

### 📹 Changement de FOV
- Le FOV augmente pendant le dash (90° par défaut)
- Transition fluide et progressive
- Retour au FOV normal après le dash

### 💥 Destruction de Piliers
- Contact avec le pilier = destruction automatique
- Effets visuels de destruction (particules configurables)
- Compatible avec le système de spawn de piliers existant

## Correction du Problème de Saut

### Problème Résolu
**Avant** : Quand vous sprintiez et sauti ez, vous perdiez beaucoup de vitesse en l'air.

**Maintenant** : Le système conserve votre **momentum horizontal** au moment du saut !
- Si vous sprintez et sautez, vous gardez la vitesse du sprint en l'air
- Vous avez toujours un contrôle limité en l'air (40% par défaut)
- Le contrôle en l'air s'ajoute au momentum, vous donnant plus de mobilité

### Paramètre Important
- **Preserve Jump Momentum** : Activé par défaut dans l'inspecteur du `FPSPlayerController`
- Si désactivé, comportement classique (ralentissement en l'air)

## Installation Rapide

### Méthode Automatique (Recommandée)

1. **Créer le layer Pillar**
   - Menu : `Tools → FPS System → Create Pillar Layer`
   - Crée automatiquement un layer "Pillar" pour la détection

2. **Configurer le système de dash**
   - Menu : `Tools → FPS System → Setup Pillar Dash System`
   - Ajoute automatiquement le composant `PillarDashSystem` au joueur

3. **Assigner le layer aux piliers**
   - Sélectionnez votre prefab de pilier
   - Dans l'inspecteur, changez le Layer en "Pillar"
   - Sauvegardez le prefab

### Configuration Manuelle

1. Créer un layer "Pillar" dans les Project Settings
2. Ajouter le composant `PillarDashSystem` au GameObject du joueur
3. Configurer les paramètres dans l'inspecteur
4. Assigner tous les piliers au layer "Pillar"

## Paramètres Configurables

### Dans FPSPlayerController

**Advanced Movement**
- `Preserve Jump Momentum` : Conserver la vitesse lors du saut (✅ recommandé)
- `Air Control Factor` : Contrôle en l'air (0.4 = 40%)

### Dans PillarDashSystem

**Detection Settings**
- `Detection Range` : Distance max de détection (5m par défaut)
- `Detection Radius` : Rayon du raycast (0.5m par défaut)

**Dash Settings**
- `Dash Speed` : Vitesse du dash (25 m/s par défaut)
- `Dash Duration` : Durée du dash (0.4s par défaut)
- `Dash Cooldown` : Temps entre chaque dash (1.5s par défaut)

**FOV Settings**
- `Dash FOV` : FOV pendant le dash (90° par défaut)
- `FOV Transition Speed` : Vitesse de transition (15 par défaut)

**Visual Feedback**
- `Highlight Color` : Couleur du highlight (orange par défaut)
- `Highlight Emission Intensity` : Intensité de l'émission (2.0 par défaut)

## Utilisation en Jeu

### Contrôles
1. **Regarder un pilier** : Le pilier à portée s'illumine automatiquement en orange
2. **Appuyer sur E** : Lance le dash vers le pilier ciblé
3. **Contact** : Le pilier est détruit instantanément

### Astuces
- Utilisez le dash pour traverser rapidement le terrain de jeu
- Combinez sprint + saut + dash pour une mobilité maximale
- Le cooldown vous empêche de spammer le dash
- Vous pouvez dasher en l'air ou au sol

## Exemples de Configuration

### Configuration Agressive (Action rapide)
```
Dash Speed: 30 m/s
Dash Duration: 0.3s
Dash Cooldown: 1.0s
Detection Range: 7m
Dash FOV: 100°
```
*Résultat : Dash ultra-rapide avec large FOV, cooldown court*

### Configuration Équilibrée (Par défaut)
```
Dash Speed: 25 m/s
Dash Duration: 0.4s
Dash Cooldown: 1.5s
Detection Range: 5m
Dash FOV: 90°
```
*Résultat : Bon équilibre entre vitesse et contrôle*

### Configuration Tactique (Précision)
```
Dash Speed: 20 m/s
Dash Duration: 0.5s
Dash Cooldown: 2.0s
Detection Range: 4m
Dash FOV: 80°
```
*Résultat : Dash plus lent mais plus contrôlable, pour gameplay précis*

## Intégration avec le Système Existant

Le système s'intègre parfaitement avec :
- ✅ **PillarSpawner** : Les piliers spawnés sont automatiquement détectables
- ✅ **PillarController** : Destruction propre avec effets visuels
- ✅ **WaveManager** : Compatible avec le système de vagues d'ennemis
- ✅ **FPSPlayerController** : Utilise le CharacterController existant

## API Programmation

### Vérifier si un dash est possible

```csharp
PillarDashSystem dashSystem = GetComponent<PillarDashSystem>();

if (dashSystem.CanDash)
{
    Debug.Log("Le joueur peut dasher !");
}
```

### Récupérer le pilier ciblé

```csharp
PillarDashSystem dashSystem = GetComponent<PillarDashSystem>();
GameObject targetedPillar = dashSystem.CurrentTargetedPillar;

if (targetedPillar != null)
{
    Debug.Log($"Pilier ciblé : {targetedPillar.name}");
}
```

### Modifier les paramètres à runtime

```csharp
// Changer la vitesse de dash
PillarDashSystem dashSystem = GetComponent<PillarDashSystem>();
// Utiliser la réflexion ou créer des propriétés publiques

// Modifier le FOV du joueur
FPSPlayerController player = GetComponent<FPSPlayerController>();
Camera cam = player.CameraTransform.GetComponent<Camera>();
cam.fieldOfView = 80f;
```

## Troubleshooting

### Le highlight ne s'affiche pas
- ✅ Vérifiez que le pilier a bien un `Renderer` (MeshRenderer)
- ✅ Vérifiez que le pilier a bien un `Collider` (BoxCollider, MeshCollider, etc.)
- ✅ Utilisez le menu `Tools → FPS System → Configure Pillar for Highlight` sur votre pilier
- ✅ Utilisez `Tools → FPS System → Test Pillar Detection` pour diagnostiquer les problèmes
- ✅ Augmentez la `Detection Range` à 10m pour tester
- ✅ Regardez la Console Unity pour le message "Pilier ciblé : [nom]"
- ✅ Vérifiez qu'une ligne verte apparaît dans la Scene view quand vous visez le pilier
- ✅ Assurez-vous que le matériau du pilier n'est pas transparent

### Le dash ne fonctionne pas
- ✅ Vérifiez que vous avez un `PillarDashSystem` sur le joueur
- ✅ Vérifiez que le `FPSPlayerController` est bien assigné
- ✅ Vérifiez le cooldown (attendez 1.5s entre chaque dash)
- ✅ Assurez-vous qu'un pilier est ciblé (highlight visible)

### Le pilier ne se détruit pas
- ✅ Vérifiez que le pilier a un `PillarController`
- ✅ Réduisez la distance de collision (rapprochez-vous plus)
- ✅ Augmentez le `Dash Duration` pour avoir plus de temps

### Le joueur est toujours ralenti en l'air
- ✅ Activez `Preserve Jump Momentum` dans le `FPSPlayerController`
- ✅ Augmentez `Air Control Factor` (0.5-0.7 pour plus de contrôle)
- ✅ Vérifiez que vous sprintez AVANT de sauter

### Le FOV ne change pas pendant le dash
- ✅ Vérifiez que la `Camera` est bien assignée dans `PillarDashSystem`
- ✅ Augmentez `FOV Transition Speed` pour une transition plus rapide
- ✅ Vérifiez que `Dash FOV` est différent du FOV par défaut

## Effets Visuels Avancés

### Ajouter des Particules de Destruction

1. Créez un système de particules (fumée, débris, etc.)
2. Créez-en un prefab
3. Assignez-le au champ `Destroy VFX` dans le `PillarController`
4. Les particules apparaîtront automatiquement lors de la destruction

### Personnaliser le Highlight

Le système utilise un matériau émissif généré dynamiquement. Pour personnaliser :
- Changez `Highlight Color` pour une autre couleur
- Augmentez `Highlight Emission Intensity` pour un effet plus visible
- Le highlight s'adapte automatiquement à la géométrie du pilier

## Performance

### Optimisations Recommandées
- Limitez le nombre de piliers avec `Has Lifetime` activé
- Utilisez des modèles low-poly pour les piliers
- Le système de highlight crée des matériaux temporaires (nettoyés automatiquement)
- La détection utilise un SphereCast optimisé (1 par frame)

### Impact Performance
- Detection : ~0.01ms par frame
- Highlight : ~0.02ms lors du changement de cible
- Dash : Négligeable (mouvement standard)

## Combinaisons de Gameplay

### Mobilité Aérienne
Sprint → Saut → Dash sur pilier en l'air → Mouvement fluide et rapide

### Destruction de Zone
Sprintez entre plusieurs piliers en dashant sur chacun pour nettoyer une zone

### Échappement Tactique
Utilisez le dash pour fuir rapidement les ennemis en détruisant des piliers sur votre passage

### Course de Vitesse
Créez des parcours avec des piliers à détruire pour des défis de vitesse

---

**Version 1.0** | Créé pour Proto3GD | Système FPS
