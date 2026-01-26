# DefoulWar - Plan d'Action (Retour Professeurs)

> **Date:** 13 Janvier 2026  
> **Contexte:** Intégration des retours des professeurs pour le rendu final

---

## 🎯 Résumé des Retours

Les professeurs demandent trois axes majeurs :
1. **Level Design (Encounter Design)** - Arènes structurées avec compositions d'ennemis
2. **Combat System** - Système de combo punch avec timing
3. **Interactions Environnementales** - Éléments interactifs pour complexifier les arènes

---

## 📋 Priorité 1 : Level Design (Arènes)

### Mini LD de Démonstration
Structure cible : **Rue → Intérieur → Toit** (finir sur belle vista)

| Zone | Description | Verticalité |
|------|-------------|-------------|
| **Rue** | Intro, premier combat | Aucune |
| **Intérieur** | Combat confiné (metrics agrandis pour 3C) | Escaliers, échelles |
| **Toit** | Arène finale + Vista | Plateformes, sauts |

### 4-5 Arènes avec Compositions Ennemis

| Arène | Vagues | Synergie |
|-------|--------|----------|
| **Arène 1** | V1: 6x Standard / V2: 4x Standard + 2x Shield | Bases + contournement |
| **Arène 2** | V1: 4x Standard / V2: 3x Magique + 3x Standard / V3: 5x Magique + 2x Standard | Apprendre Magique |
| **Arène 3** | V1: 5x Standard / V2: 2x Electric + 4x Standard / V3: 3x Electric + 3x Standard / V4: 2x Electric + 2x Shield + 3x Standard | Punition dash |
| **Arène 4** | V1: 3x Standard + 2x Shield + 1x Magique / V2: 1x Healer + 4x Standard / V3: 3x Magique + 3x Standard / V4: 1x Healer + 2x Shield + 3x Standard / V5: 2x Healer + 2x Electric + 4x Standard | Priorité Healer |
| **Arène Finale** | V1: 4x Standard + 2x Magique / V2: 4x Magique + 3x Standard / V3: 3x Electric + 2x Shield + 3x Standard / V4: 1x Healer + 3x Electric + 4x Standard / V5: 2x Healer + 4x Magique + 3x Standard / V6: 1x Healer + 3x Magique + 2x Electric + 2x Shield + 4x Standard | Tous les 5 types |

> **Progression :** 2 → 3 → 4 → 5 → 6 vagues

> [!IMPORTANT]  
> Style **Devil May Cry / Bayonetta** : Fermeture virtuelle (barrières visuelles, pas de murs solides partout)

### Verticalité
Moyens de monter **autres que les escaliers** :
- Ascenseurs/plateformes
- Sauts sur caisses empilées
- Dash vers ennemis en hauteur
- Rampes/échelles

---

## 📋 Priorité 2 : Système de Combo Punch

> "Une sorte de timing pour le combo de coups de poing"

### Option A : Timing Window
```
Coup 1 → [0.3s-0.8s] → Coup 2 → [0.3s-0.8s] → Coup 3
         ↑ Fenêtre de combo
```
- Si input trop tôt ou trop tard → reset combo
- Chaque coup enchaîné = bonus dégâts

### Option B : Réduire le nombre, augmenter les dégâts
- Moins de coups dans le combo (3 au lieu de 5)
- Chaque coup fait plus de dégâts
- Plus de poids/impact par frappe

### Fichiers à modifier
- Création d'un nouveau script `MeleeComboSystem.cs`
- Intégration avec le système de dash existant

---

## 📋 Priorité 3 : Interactions Environnementales

### Éléments à implémenter

| Élément | Effet | Script existant? |
|---------|-------|------------------|
| **Flaque d'eau + Electric** | Électrocute en zone | Nouveau |
| **Objet explosif** | Dégâts zone + knockback | Nouveau |
| **Alarme** | Alerte ennemis / spawn renforts | Nouveau |
| **Porte électrique** | Ouverte par ennemi Electric | ✅ `ElectricDoor.cs` |

### Synergie avec ennemis
- L'ennemi **Electric** dans une flaque d'eau = zone de danger
- L'ennemi **Magique** près de barils = opportunité explosion
- L'ennemi **Shield** protège un **Healer** = urgence de contournement

---

## 📋 Priorité 4 : Récompenser le Joueur

### Options suggérées
- ✅ Munitions récupérées après dash (déjà prévu dans les specs)
- 🆕 Score multiplicateur selon combo
- 🆕 Évaluation de style (DMC-like : D, C, B, A, S, SS, SSS)
- 🆕 Drops de vie sur ennemis

---

## 📋 Notes Additionnelles

### Metrics Intérieurs
> "Pensez aux 3C (Caméra, Contrôles, Personnage) plutôt que la taille humaine"

- Agrandir les couloirs/pièces pour le mouvement FPS
- Dash doit être utilisable à l'intérieur
- Éviter les coins trop serrés

### LD Semi-Ouvert
- Plusieurs chemins possibles
- Pas de couloirs linéaires stricts
- Liberté d'approche tactique

---

## ✅ Checklist Globale

### Level Design
- [ ] Créer structure Rue → Intérieur → Toit
- [ ] Arène 1 : Standard + Shield (2 vagues)
- [ ] Arène 2 : Magique + Standard (3 vagues)
- [ ] Arène 3 : Electric + Shield + Standard (4 vagues)
- [ ] Arène 4 : Healer + tous types (5 vagues)
- [ ] Arène Finale : Tous les 5 types (6 vagues)
- [ ] Ajouter éléments de verticalité (non-escaliers)
- [ ] Finir sur une belle vista

### Combat
- [ ] Implémenter système de timing combo punch
- [ ] Tester les deux options (timing vs moins de coups)

### Environnement
- [ ] Script `WaterPuddle.cs` (électrifiable)
- [ ] Script `ExplosiveBarrel.cs`
- [ ] Script `AlarmTrigger.cs`

### Récompenses
- [ ] Définir système de récompense joueur
- [ ] Feedback visuel/audio de combo

---

*Document créé à partir des retours professeurs du 13/01/2026*
