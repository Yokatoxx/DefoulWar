# DefoulWar - FPS Arena Shooter

Un jeu FPS Arena développé sous **Unity 6.0+** avec un système de dash ciblé et des ennemis variés.

## 🎮 Fonctionnalités

### Système de Mouvement
- Mouvement WASD avec sprint
- Saut avec conservation du momentum
- **Dash ciblé** (touche E) vers les ennemis avec FOV dynamique
- Caméra FPS fluide

### Système d'Armes
- Tir au raycast
- Munitions et rechargement (R)
- Recul et effets de shake caméra

### Types d'Ennemis (7 variantes)
| Type | Comportement |
|------|--------------|
| Standard | Chaser (poursuite) |
| Healer | Zone de soin pour alliés |
| Distance | Tire des projectiles |
| Magique | Attaque hitscan |
| Électrique | Stun du joueur |
| Shield | Bouclier protecteur |
| Companion | Suit un autre ennemi |

### Arènes Dynamiques
- Portes et triggers
- Points de spawn groupés
- Système de progression

## 🕹️ Contrôles

| Action | Touche |
|--------|--------|
| Déplacement | WASD |
| Regarder | Souris |
| Sauter | Espace |
| Sprint | Shift Gauche |
| Tirer | Clic Gauche |
| Recharger | R |
| **Dash ciblé** | **E** |
| Pause | Échap |

## 📁 Structure

```
Assets/Scripts/
├── FPS/
│   ├── Player/      # Mouvement, dash, caméra
│   ├── Weapon/      # Armes et presets
│   └── Effect/      # Effets visuels
├── Ennemies/
│   ├── Behaviors/   # IA (Chaser, Distance, Patrol, Companion)
│   ├── Effect/      # Effets spéciaux (Electric, Magic, Shield)
│   └── Main/        # Scripts principaux
├── Manager/         # Game, UI managers
└── System/Arena/    # Système d'arènes
```

## ⚙️ Configuration Requise

- Unity 6.0+
- Input System package
- NavMesh pour l'IA ennemis

## 🚀 Installation

1. Ouvrir le projet dans Unity 6.0+
2. Configurer le NavMesh : `Window > AI > Navigation > Bake`
3. Vérifier les tags `Player` et `Enemy`
4. Lancer la scène principale

---

**DefoulWar** | FPS Arena Shooter
