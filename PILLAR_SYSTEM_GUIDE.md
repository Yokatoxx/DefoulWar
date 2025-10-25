# Système de Spawn de Piliers - Guide Rapide

## Vue d'ensemble

Le système de spawn de piliers fait automatiquement apparaître un pilier à la position d'un ennemi lorsqu'il meurt. Chaque pilier peut avoir une rotation aléatoire entre deux angles définis, ce qui permet de créer des environnements dynamiques et variés.

## Composants du Système

### 1. **PillarSpawner**
Le gestionnaire principal qui écoute les morts d'ennemis et fait apparaître les piliers.

**Paramètres configurables :**
- **Pillar Prefab** : Le prefab du pilier à instancier
- **Min Angle X** : Angle minimum de rotation (en degrés) sur l'axe X (inclinaison avant/arrière)
- **Max Angle X** : Angle maximum de rotation (en degrés) sur l'axe X (inclinaison avant/arrière)
- **Min Angle Y** : Angle minimum de rotation (en degrés) sur l'axe Y (rotation horizontale)
- **Max Angle Y** : Angle maximum de rotation (en degrés) sur l'axe Y (rotation horizontale)
- **Min Angle Z** : Angle minimum de rotation (en degrés) sur l'axe Z (inclinaison gauche/droite)
- **Max Angle Z** : Angle maximum de rotation (en degrés) sur l'axe Z (inclinaison gauche/droite)
- **Spawn Offset** : Décalage de position par rapport à l'ennemi mort
- **Pillars Container** : Parent optionnel pour organiser les piliers

### 2. **PillarController**
Contrôle le comportement individuel de chaque pilier.

**Fonctionnalités :**
- Durée de vie optionnelle (disparition automatique)
- Animation d'apparition progressive
- Effets visuels (particules)
- Effets sonores
- Contrôle programmé de la destruction

## Installation Rapide

### Méthode 1 : Via le Menu Unity (Recommandé)

1. **Créer un Pilier**
   - Menu : `GameObject → 3D Object → Pillar`
   - Personnalisez l'apparence du pilier
   - Ajustez les paramètres dans `PillarController`
   - Créez un prefab : glissez le pilier dans le dossier Prefabs

2. **Ajouter le Système de Spawn**
   - Menu : `GameObject → FPS System → Pillar Spawner`
   - Dans l'inspecteur, assignez votre prefab de pilier
   - Configurez les angles de rotation (ex: Min=0, Max=360)

3. **Configuration Automatique**
   - Menu : `Tools → FPS System → Setup Pillar System`
   - Crée automatiquement un PillarSpawner dans la scène

### Méthode 2 : Configuration Manuelle

1. Créer un GameObject vide nommé "Pillar Spawner"
2. Ajouter le composant `PillarSpawner`
3. Créer un prefab de pilier avec le composant `PillarController`
4. Assigner le prefab au PillarSpawner

## Exemples de Configuration

### Configuration Piliers Verticaux (Rotation horizontale uniquement)
- **Min Angle X** : 0°
- **Max Angle X** : 0°
- **Min Angle Y** : 0°
- **Max Angle Y** : 360°
- **Min Angle Z** : 0°
- **Max Angle Z** : 0°
- **Spawn Offset** : (0, 0, 0)

*Résultat : Piliers parfaitement verticaux avec rotation horizontale aléatoire*

### Configuration Piliers Penchés (Recommandé)
- **Min Angle X** : -15°
- **Max Angle X** : 15°
- **Min Angle Y** : 0°
- **Max Angle Y** : 360°
- **Min Angle Z** : -15°
- **Max Angle Z** : 15°
- **Spawn Offset** : (0, 0, 0)

*Résultat : Piliers légèrement penchés dans toutes les directions, aspect naturel et dynamique*

### Configuration Piliers Très Inclinés
- **Min Angle X** : -30°
- **Max Angle X** : 30°
- **Min Angle Y** : 0°
- **Max Angle Y** : 360°
- **Min Angle Z** : -30°
- **Max Angle Z** : 30°
- **Spawn Offset** : (0, 0.5, 0)

*Résultat : Piliers fortement inclinés, effet chaotique et artistique*

### Configuration Piliers Orientés (Une direction)
- **Min Angle X** : 10°
- **Max Angle X** : 20°
- **Min Angle Y** : 0°
- **Max Angle Y** : 360°
- **Min Angle Z** : 0°
- **Max Angle Z** : 0°
- **Spawn Offset** : (0, 0, 0)

*Résultat : Piliers penchés vers l'avant, effet directionnel*

### Configuration Forêt de Piliers Temporaires
- **Min Angle X** : -10°
- **Max Angle X** : 10°
- **Min Angle Y** : 0°
- **Max Angle Y** : 360°
- **Min Angle Z** : -10°
- **Max Angle Z** : 10°
- **Has Lifetime** : Oui (sur PillarController)
- **Lifetime** : 30 secondes

*Résultat : Piliers temporaires avec inclinaisons variées, disparaissent progressivement*

## Intégration avec le Système FPS

Le système s'intègre automatiquement avec :
- **EnemyHealth** : Écoute l'événement `OnDeath`
- **WaveManager** : Enregistre automatiquement les nouveaux ennemis spawnés

Aucune configuration supplémentaire n'est nécessaire si vous utilisez le WaveManager existant.

## Personnalisation du Pilier

### Apparence Visuelle
Le prefab de pilier peut contenir :
- Mesh personnalisé (modèle 3D)
- Matériaux et textures
- Colliders pour les interactions
- Lumières ou effets visuels

### Animation d'Apparition
Dans `PillarController` :
- **Animate Spawn** : Active l'animation de croissance
- **Spawn Duration** : Durée de l'animation (0.5s par défaut)
- **Initial Scale** : Hauteur initiale (0 = sort du sol)

### Effets Visuels et Sonores
- **Spawn VFX** : Prefab de particules (fumée, étincelles, etc.)
- **Spawn Sound** : Clip audio joué à l'apparition

## API Programmation

### Utilisation depuis un Script

```csharp
// Obtenir le spawner
PillarSpawner spawner = FindFirstObjectByType<PillarSpawner>();

// Spawn manuel d'un pilier
spawner.SpawnPillarManually(position);

// Nettoyer tous les piliers
spawner.ClearAllPillars();

// Enregistrer un ennemi manuellement
EnemyHealth enemy = GetComponent<EnemyHealth>();
spawner.RegisterEnemy(enemy);
```

### Contrôle d'un Pilier

```csharp
PillarController pillar = GetComponent<PillarController>();

// Définir une durée de vie
pillar.SetLifetime(10f);

// Détruire avec délai
pillar.DestroyPillar(2f);
```

## Optimisation

### Performance
- Utilisez object pooling pour les piliers fréquemment spawnés
- Limitez le nombre de piliers actifs avec une durée de vie
- Désactivez les colliders si les piliers sont purement décoratifs

### Organisation
- Les piliers sont automatiquement organisés dans un conteneur
- Le conteneur se trouve dans la hiérarchie : "Pillars Container"
- Facilite le nettoyage et la gestion de la scène

## Troubleshooting

### Les piliers n'apparaissent pas
- ✓ Vérifiez que le prefab est assigné dans PillarSpawner
- ✓ Vérifiez que les ennemis ont un composant EnemyHealth
- ✓ Vérifiez la console pour les erreurs

### Les piliers ont tous la même rotation
- ✓ Vérifiez que Min Angle ≠ Max Angle
- ✓ Vérifiez que les angles sont en degrés (0-360)

### Les piliers apparaissent au mauvais endroit
- ✓ Ajustez le paramètre "Spawn Offset"
- ✓ Vérifiez le pivot du prefab de pilier

### Trop de piliers ralentissent le jeu
- ✓ Activez "Has Lifetime" sur PillarController
- ✓ Réduisez la valeur de Lifetime
- ✓ Utilisez des modèles low-poly pour les piliers

## Exemples de Gameplay

### Obstacles Dynamiques
Créez des piliers avec colliders qui bloquent le mouvement du joueur, créant un terrain de jeu qui évolue au fil des combats.

### Couverture Tactique
Utilisez les piliers comme zones de couverture pour le joueur, avec une durée de vie pour éviter l'accumulation.

### Indicateurs Visuels
Piliers colorés indiquant où des ennemis sont morts, utile pour le scoring ou les statistiques.

### Effet de Forêt Pétrifiée
Piliers permanents qui transforment progressivement l'arène en labyrinthe.

## Support et Extension

Pour étendre le système :
1. Héritez de `PillarController` pour ajouter des comportements personnalisés
2. Utilisez les événements Unity pour synchroniser avec d'autres systèmes
3. Modifiez `PillarSpawner` pour supporter différents types de piliers selon le type d'ennemi

---

**Créé pour Proto3GD** | Système FPS | Version 1.0
