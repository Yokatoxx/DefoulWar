# Guide du Système de Knockback Ennemi

## Vue d'ensemble

Le système de knockback permet de repousser les ennemis lorsque le joueur leur dashe dessus. C'est un effet physique qui utilise un Rigidbody pour propulser l'ennemi dans la direction du dash.

## Fonctionnement

1. **Déclenchement** : Quand le joueur dash sur un ennemi (`DamageType.Dash`), le knockback est automatiquement appliqué
2. **Physique** : Le NavMeshAgent est temporairement désactivé, le Rigidbody passe en mode non-kinématique et une impulsion est appliquée
3. **Récupération** : Après la durée du knockback, l'ennemi est repositionné sur le NavMesh et reprend son comportement IA normal

## Configuration

### Paramètres globaux du dash (DashDefinition ScriptableObject)

Dans le fichier `Assets/SO/` ou là où se trouve votre DashDefinition :

| Paramètre | Description | Valeur par défaut |
|-----------|-------------|-------------------|
| `Knockback Force` | Force de repousse appliquée à l'ennemi | 15 |
| `Knockback Duration` | Durée avant que l'ennemi reprenne son IA (secondes) | 0.5 |
| `Knockback Affects Y Axis` | Si activé, l'ennemi est aussi propulsé verticalement | false |

### Paramètres par ennemi (EnemyKnockback component)

| Paramètre | Description |
|-----------|-------------|
| `Resist To Knockback` | Si coché, cet ennemi ne sera pas repoussé |
| `Impact Particle Prefab` | Particule à instancier lors de l'impact |
| `Particle Offset` | Décalage de position pour la particule |
| `Impact Sound` | Son joué lors de l'impact |
| `Impact Volume` | Volume du son (0-1) |

## Résistance au Knockback

Un ennemi peut résister au knockback de deux façons :

1. **Via EnemyKnockback** : Cocher `Resist To Knockback` dans le composant
2. **Via ElectricEnnemis** : Si l'ennemi a `ResistToDash = true`, il résiste aussi au knockback

## Auto-ajout des composants

Le système ajoute automatiquement les composants nécessaires (Rigidbody et EnemyKnockback) lors du premier dash sur un ennemi qui ne les possède pas. Cependant, pour personnaliser les effets (particules, sons), il est recommandé d'ajouter manuellement le composant `EnemyKnockback` aux prefabs d'ennemis.

## Setup manuel d'un prefab ennemi

1. Sélectionner le prefab ennemi
2. Ajouter un composant **Rigidbody** :
   - `Is Kinematic` = true
   - `Interpolation` = Interpolate
   - `Constraints` = Freeze Rotation (X, Y, Z)
3. Ajouter le composant **EnemyKnockback**
4. (Optionnel) Configurer les particules et sons d'impact

## Ajustements recommandés

### Force de knockback par type d'ennemi

- **Ennemis légers** : Force 15-20, Durée 0.5s
- **Ennemis moyens** : Force 10-15, Durée 0.4s  
- **Ennemis lourds** : Force 5-10, Durée 0.3s (ou résistance)
- **Boss** : `Resist To Knockback = true`

### Effets visuels suggérés

- Particule de poussière/impact
- Flash visuel (géré automatiquement via EnemyVisualFeedback)
- Son d'impact sourd

## Dépannage

### L'ennemi n'est pas repoussé
- Vérifier que `Resist To Knockback` n'est pas coché
- Vérifier que l'ennemi n'a pas `ElectricEnnemis.ResistToDash = true`
- S'assurer que le Rigidbody n'est pas "frozen" sur tous les axes

### L'ennemi se bloque après le knockback
- Vérifier que le NavMeshAgent est bien configuré
- S'assurer que la zone de destination est sur un NavMesh valide

### L'ennemi traverse les murs
- Réduire `Knockback Force`
- Activer `Collision Detection = Continuous Dynamic` sur le Rigidbody
- Ajouter des colliders appropriés aux murs

