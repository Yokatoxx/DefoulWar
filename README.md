# DefoulWar

> **FPS Arena Shooter** | Film Noir Surnaturel | Unity 6.0+

Un jeu d'action frénétique dans un Londres géorgien-victorien envahi par les démons. Maîtrisez l'art du **dash ciblé** et du **tir tactique** pour survivre aux arènes infernales.

---

## 🎮 Concept

DefoulWar repose sur une **dualité tir/dash** où chaque type d'ennemi réagit différemment à vos capacités. Tirez à distance ou dashiez au corps-à-corps — mais attention, certains ennemis punissent le mauvais choix !

### Direction Artistique
- **Style architectural :** Georgian Victorian London
- **Univers :** Film Noir, Surnaturel (Urban Fantasy)
- **Ton :** Sérieux
- **Ennemis :** Démons aux formes et couleurs distinctes

---

## ⚔️ Mécaniques Principales

### Système de Combat Dual

| Capacité | Description | Risque |
|----------|-------------|--------|
| **Tir** | Arme à distance avec munitions limitées | Reload = vulnérabilité |
| **Dash** | Mouvement ciblé vers un ennemi avec dégâts | Certains ennemis punissent le dash |

### Types d'Ennemis (6 variantes)

| Type | Comportement | Réaction au Dash |
|------|--------------|------------------|
| **Electric** ⚡ | Stun le joueur | **PUNITIF** - Stun + immunité |
| **Healer** 💚 | Zone de soin + invocateur | Normal |
| **Distance** 🎯 | Projectiles | Normal |
| **Magique** ✨ | Attaque hitscan | Normal |
| **Shield** 🛡️ | Bouclier frontal | Contournable par derrière |
| **Standard** 💀 | Chaser (poursuite) | Normal |

---

## 🕹️ Contrôles

| Action | Touche |
|--------|--------|
| Déplacement | ZQSD |
| Regarder | Souris |
| Sauter | Espace |
| Sprint | Shift Gauche |
| **Tirer** | **Clic Gauche** |
| Recharger | R |
| **Dash ciblé** | **E** |
| Pause | Échap |

---

## 🏛️ Arènes

- **Déclenchement :** Le joueur entre dans une zone → portes fermées → ennemis arrivent
- **Victoire :** Éliminer tous les ennemis
- **Progression :** Vagues d'ennemis de difficulté croissante
- **Environnement :** Lieux victoriens extérieurs (marchés, cimetières, quais, ruelles...)

---

## 📁 Structure du Projet

```
Assets/Scripts/
├── FPS/
│   ├── Player/      # DashCible, DashSlowMo, DashHighlight, FPSMovement
│   ├── Weapon/      # Armes et presets
│   └── Effect/      # Effets visuels (CameraShake, etc.)
├── Ennemies/
│   ├── Behaviors/   # IA (Chaser, Distance, Patrol, Companion)
│   ├── Effect/      # Effets spéciaux (Electric, Magic, Shield)
│   ├── Main/        # EnemyBehaviour, EnemyAttackHandler
│   └── Settings/    # ScriptableObjects de configuration
├── Manager/         # Game, UI managers
├── System/Arena/    # Système d'arènes et vagues
└── Utils/           # Utilitaires divers
```

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [SPECIFICATIONS.md](Docs/SPECIFICATIONS.md) | Spécifications complètes du jeu |
| [DEVELOPMENT_PLAN.md](Docs/DEVELOPMENT_PLAN.md) | Plan de développement par phases |

---

## ⚙️ Configuration Requise

- **Unity 6.0+**
- Input System package
- NavMesh pour l'IA ennemis

---

## 🚀 Installation

1. Ouvrir le projet dans **Unity 6.0+**
2. Configurer le NavMesh : `Window > AI > Navigation > Bake`
3. Vérifier les tags `Player` et `Enemy`
4. Lancer la scène principale

---

## 👥 Équipe

Projet développé par une équipe de **4 personnes**.

---

**DefoulWar** | FPS Arena Shooter | Film Noir Surnaturel
