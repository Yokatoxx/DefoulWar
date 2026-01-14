# DefoulWar - Plan de Développement

> **Équipe:** 4 personnes  
> **Objectif:** Prototype "Toy" (mécaniques et métriques)  
> **Référence:** [SPECIFICATIONS.md](./SPECIFICATIONS.md)

---

## Phase 1 : Fondations du Prototype (Priorité Haute)

### 1.1 Système Joueur
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Finaliser métrics du dash (dégâts, cooldown, distance) | ⬜ | |
| Implémenter limite de chaîne de dash | ⬜ | |
| Ajouter feedback visuel/audio dash réussi | ⬜ | |
| Définir et implémenter système de vie joueur | ⬜ | |
| Implémenter récupération de munitions (dash = recharge?) | ⬜ | |

### 1.2 Système Ennemis
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Valider comportement Electric (stun + immunité dash) | ✅ | |
| Implémenter système de sacrifice du Healer (meurt pour invoquer des ennemis) | ⬜ | |
| Ajouter indicateur visuel état Healer (aura) | ⬜ | |
| Valider contournement Shield par dash | ✅ | |

### 1.3 Arènes
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Système de déclenchement arène (zone + portes) | ✅ | |
| Système de vagues fonctionnel | ✅ | |
| Condition de victoire (tous ennemis morts) | ⬜ | |

---

## Phase 2 : Équilibrage et Métriques

### 2.1 Équilibrage Combat
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Forcer alternance tir/dash (ex: dash recharge munitions, ennemis résistants au tir punitifs au dash) | ⬜ | |
| Définir dégâts dash selon contexte (base, chaîne, type ennemi) | ⬜ | |
| Équilibrer cooldown dash vs cadence de tir | ⬜ | |
| Tester limite de chaîne optimale | ⬜ | |

### 2.2 Équilibrage Ennemis
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| HP par type d'ennemi | ✅ | |
| Dégâts par type d'ennemi | ✅ | |
| Vitesse de déplacement | ⬜ | |
| Portée d'attaque (Distance, Magique) | ⬜ | |
| Durée du stun Electric | ✅ | |
| Définir seuil d'ennemis restants pour déclencher sacrifice Healer | ⬜ | |

### 2.3 Équilibrage Arènes
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Nombre d'ennemis par vague | ⬜ | |
| Composition des vagues | ⬜ | |
| Durée cible par arène (~3 min début) | ⬜ | |

---

## Phase 3 : Level Design et Contenu

### 3.1 Tutoriel
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Niveau tutoriel (tir + dash) | ⬜ | |
| Introduction progressive des ennemis (Tease/Learn/Practice/Master) | ⬜ | |

### 3.2 Arène Prototype
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Créer 1 arène complète de test | ⬜ | |
| Placement covers et obstacles | ⬜ | |
| Points de spawn ennemis | ⬜ | |
| Murs invisibles | ⬜ | |

### 3.3 Éléments Interactifs (Optionnel)
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Barils explosifs | ⬜ | |
| Pièges environnementaux | ⬜ | |
| Éléments destructibles | ⬜ | |

---

## Phase 4 : Feedback et Polish

### 4.1 Audio
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Sons de dash (réussi vs punitif) | ⬜ | |
| Sons distinctifs par type d'ennemi | ⬜ | |
| Musique dynamique (exploration/combat) | ⬜ | |

### 4.2 Visuel
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Particules dash réussi | ⬜ | |
| Feedback stun joueur | ✅ | |
| Indicateur état Healer | ⬜ | |
| Couleurs distinctes par ennemi | ✅ | |

### 4.3 UI
| Tâche | Statut | Responsable |
|-------|--------|-------------|
| HUD vie joueur | ✅ | |
| Indicateur ennemis restants | ⬜ | |
| Jauges cooldown dash | ✅ | |
| Menu pause | ⬜ | |

---

## Phase 5 : Mini-Boss (Si temps disponible)

| Tâche | Statut | Responsable |
|-------|--------|-------------|
| Conception mécanique mini-boss | ⬜ | |
| Implémentation comportement | ⬜ | |
| Intégration dans arène | ⬜ | |

---

## Légende
- ⬜ À faire
- 🔄 En cours
- ✅ Terminé
- ❌ Bloqué

---

## Décisions à Prendre (Bloquantes)

> [!WARNING]
> Ces décisions doivent être prises avant de pouvoir avancer :

1. **Système de vie joueur** : Régénération lente ou "Dernier souffle" ?
2. **Récupération munitions** : Dash recharge 30% ou drops ennemis ?
3. **Limite chaîne dash** : Nombre max de chaînages ?

---

*Plan basé sur les spécifications du 11/01/2026*
