# DefoulWar - Document de Spécifications

> **Version:** 1.0  
> **Date:** 11 Janvier 2026  
> **Équipe:** 4 personnes  
> **Objectif:** Prototype "Toy" (mécaniques et métriques)

---

## 1. Vision du Projet

### 1.1 Concept
DefoulWar est un **FPS Arena Shooter** en campagne linéaire où le joueur affronte des hordes de démons dans un Londres victorien. La mécanique centrale repose sur une **dualité tir/dash** où chaque ennemi réagit différemment à ces deux capacités.

### 1.2 Direction Artistique
| Aspect | Description |
|--------|-------------|
| **Style architectural** | Georgian Victorian London |
| **Univers** | Film Noir, Surnaturel (Urban Fantasy) |
| **Ton** | Sérieux |
| **Ennemis** | Démons aux formes et couleurs distinctes |

### 1.3 Lieux Suggérés (Extérieurs)
- Place de marché avec stands
- Cimetière avec mausolées
- Jardins de manoir abandonné
- Quais de la Tamise avec grues
- Cour industrielle (usine)
- Pont victorien (style Tower Bridge)
- Parvis d'église gothique
- Ruelles avec lampadaires à gaz

---

## 2. Système de Combat

### 2.1 Philosophie
Le gameplay repose sur deux mécaniques principales équilibrées **50/50** :
- **Tir à distance** : Arme avec munitions limitées et rechargement
- **Dash ciblé** : Mouvement vers un ennemi avec dégâts fixes

Le joueur doit choisir tactiquement entre tir et dash selon le type d'ennemi.

### 2.2 Système de Dash

| Paramètre | Valeur/Description |
|-----------|-------------------|
| **Touche** | E |
| **Dégâts** | Fixes |
| **Cooldown** | Affiché via jauges multiples |
| **Limite de chaîne** | En cours d'équilibrage |
| **Modules** | SlowMo, Bounce, Highlight, HitStop |

#### Feedback Recommandé (Dash Réussi)
- Son d'impact satisfaisant (crunch + swoosh)
- Léger screen shake
- Flash de couleur sur l'ennemi
- Particules de sang/énergie démoniaque

### 2.3 Système de Tir

| Paramètre | Valeur/Description |
|-----------|-------------------|
| **Type** | Raycast |
| **Munitions** | Limitées avec chargeur |
| **Rechargement** | Touche R |
| **Effets** | Recul + camera shake |

#### Récupération de Munitions (Recommandation)
Le dash sur un ennemi recharge partiellement les munitions (~30% du chargeur) pour forcer l'alternance tir/dash.

---

## 3. Types d'Ennemis

### 3.1 Hiérarchie de Menace Recommandée

| Priorité | Type | Comportement | Réaction au Dash |
|----------|------|--------------|------------------|
| **1 (Critique)** | Electric | Stun le joueur | **PUNITIF** - Stun + feedback audio/visuel |
| **2 (Haute)** | Healer | Zone de soin + invocateur | Dégâts normaux |
| **3 (Haute)** | Distance | Tire des projectiles | Dégâts normaux |
| **4 (Haute)** | Magique | Attaque hitscan | Dégâts normaux |
| **5 (Moyenne)** | Shield | Bouclier frontal | **PARTIELLEMENT PUNITIF** - Repoussé si frontal, peut contourner |
| **6 (Base)** | Standard (Chaser) | Poursuite | Dégâts normaux |

### 3.2 Comportements Détaillés

#### Electric
- Émet des arcs électriques en continu (indicateur visuel)
- **Si dashé :** Stun le joueur, l'ennemi ne prend PAS de dégâts
- Apprentissage par Tease/Learn/Practice/Master

#### Healer (Companion Master)
- Crée une zone de soin pour les alliés
- **Invocation :** Fait spawner des ennemis quand X ennemis restants dans l'arène
- **Limite :** Une seule invocation par Healer
- **Sacrifice :** Peut mourir pour faire spawner d'autres ennemis

##### Indicateur d'État Recommandé
- Aura au sol **verte** = peut encore invoquer
- Aura **rouge/éteinte** = a déjà invoqué ou va se sacrifier

#### Shield
- Tient un bouclier devant lui
- Bloque toutes les attaques frontales (tir ET dash)
- **Contre-jeu :** Le joueur peut passer par-dessus via le dash et attaquer par derrière

#### Distance & Magique
- Distance : Tire des projectiles (esquivables)
- Magique : Attaque hitscan (instantanée)

### 3.3 Identification Visuelle
- **Formes distinctes** par type
- **Couleurs distinctes** par type (priorité sur la forme pour la lisibilité en combat)

---

## 4. Système d'Arènes

### 4.1 Déclenchement
1. Le joueur entre dans une zone
2. Les portes se ferment
3. Les ennemis arrivent par vagues

### 4.2 Condition de Victoire
- **Tous les ennemis morts** = victoire
- Les portes s'ouvrent

### 4.3 Système de Vagues
- Vagues distinctes (nombre à définir)
- Difficulté progressive : plus d'ennemis + nouveaux types simultanément

### 4.4 Durée Cible
| Position dans le jeu | Durée cible |
|---------------------|-------------|
| Début de partie | ~3 minutes |
| Milieu/Fin | Progressivement plus long |

### 4.5 Environnement
- **Murs invisibles** pour empêcher les chutes
- **Éléments interactifs** à définir (barils explosifs, pièges, destructibles)

---

## 5. Progression

### 5.1 Structure
- **Campagne linéaire** avec niveaux distincts
- **Pas de déverrouillage** : Le joueur a accès à tout dès le début
- **Progression par nouveaux ennemis** introduits progressivement

### 5.2 Narration
- **Narration environnementale** principalement
- Le joueur découvre l'histoire en explorant la ville

### 5.3 Mini-Boss
- Présence confirmée de mini-boss
- Différenciation mécanique à définir

---

## 6. Joueur

### 6.1 Contrôles

| Action | Touche |
|--------|--------|
| Déplacement | ZQSD |
| Regarder | Souris |
| Sauter | Espace |
| Sprint | Shift Gauche |
| Tirer | Clic Gauche |
| Recharger | R |
| **Dash ciblé** | **E** |
| Pause | Échap |

### 6.2 Système de Vie
À définir. Options recommandées :
1. **Barre de vie avec régénération lente** (reward l'évitement)
2. **"Dernier souffle"** : À 0 HP, 3 secondes pour tuer un ennemi et récupérer de la vie

### 6.3 Mouvement
- ZQSD avec sprint
- Saut avec conservation du momentum
- Dash ciblé avec FOV dynamique

---

## 7. Interface Utilisateur

### 7.1 HUD
| Élément | Affichage |
|---------|-----------|
| Vie joueur | À définir |
| Munitions | Affiché |
| Cooldown dash | Jauges multiples qui se remplissent |
| Ennemis restants | À définir (recommandé) |

### 7.2 Feedback Visuel

| Événement | Feedback |
|-----------|----------|
| Dash réussi | Screen shake léger, son satisfaisant, particules |
| Dash punitif (Electric) | Stun visuel/audio fort |
| Ennemi ciblable | Highlight (module DashHighlight) |

---

## 8. Audio

### 8.1 Musique
- **Dynamique** : Calme en exploration, intense en combat
- Changement selon le nombre d'ennemis

### 8.2 Sons Ennemis
- À implémenter : sons distinctifs par type d'ennemi
- Permettrait l'identification auditive en combat

---

## 9. Tutoriel et Apprentissage

### 9.1 Tutoriel Explicite
- Apprentissage des contrôles de base : tir + dash

### 9.2 Apprentissage Pratique
- Capacités des ennemis via **Tease/Learn/Practice/Master** en Level Design
- Chaque nouveau type d'ennemi est introduit dans un contexte contrôlé

---

## 10. Points à Définir

> [!IMPORTANT]
> Les éléments suivants nécessitent des décisions supplémentaires :

| Élément | Options/Notes |
|---------|---------------|
| Système de vie du joueur | Régénération vs Dernier souffle |
| Récupération de munitions | Dash recharge vs Drop vs Auto |
| Éléments interactifs d'arène | Barils, pièges, destructibles |
| Indicateur d'ennemis restants | Style et position |
| Mécaniques de mini-boss | À concevoir |
| Limite de chaîne de dash | Équilibrage en cours |

---

## 11. Architecture Technique Existante

### 11.1 Structure des Scripts
```
Assets/Scripts/
├── FPS/
│   ├── Player/       # DashCible, DashSlowMo, DashHighlight, etc.
│   ├── Weapon/       # Armes et presets
│   └── Effect/       # Effets visuels
├── Ennemies/
│   ├── Behaviors/    # IEnemyBehavior, ChaserBehavior, DistanceBehavior, etc.
│   ├── Effect/       # EnemyShield, effets spéciaux
│   └── Main/         # EnemyBehaviour, EnemyAttackHandler
├── Manager/          # Game, UI managers
└── System/Arena/     # Système d'arènes
```

### 11.2 Systèmes Clés
- **EnemyRegistry** : Gestion centralisée des ennemis
- **NavMesh** : IA de navigation
- **Input System** : Gestion des contrôles Unity

---

*Document généré automatiquement à partir de l'analyse du projet et des réponses du game designer.*
