# Tableau SAF - DefoulWar

> **Date:** 18 Janvier 2026

---

## 🎯 Movement

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Punch (1/3) | Activation | Le joueur débute le dash, il n'est pas encore tout à fait en mouvement | Flou radial en mode "Zoom", les côtés très flou et le milieu très net. Permet de donner cette impression de vitesse. | 🟠 Haute | Movement |
| Punch (2/3) | Dash en cours | Le joueur est dans la boucle du dash, il bouge actuellement | Le flou radial est toujours présent, il commence à s'atténuer. Lorsqu'il est en mouvement on a des particules de vents qui arrivent face à la caméra. | 🟠 Haute | Movement |
| Punch (3/3) | Sortie du dash | Le joueur entame la fin du dash | Le flou radial s'atténue énormément, jusqu'à disparaître lorsque le joueur est à l'arrêt. Des particules de vents sont encore présentes, mais moins nombreuses et moins rapides vu que le dash s'arrête. | 🟠 Haute | Movement |
| Mouvement | Se déplacer | Headbobing | Léger mouvement de haut en bas lors du déplacement | ✅ Déjà Présent | Movement |
| Mouvement | Déplacement latéral | Lean | Effet penché gauche/droite | ✅ Déjà Présent | Movement |
| Caméra | Rien | FOV | FOV entre 90 & 120 (standard de fast fps) | 🟠 Haute | Player |
| Air Control | Certaine vélocity + dans les airs | Air control | Effet de vent comme dash, p'tit effet de flou radial pour la sensation de vitesse | 🟠 Haute | Movement |
| Dash Bounce | Collision avec ennemi | Rebond du joueur | Le joueur est repoussé dans la direction opposée au dash après impact | ✅ Déjà Présent | Movement |
| Dash Chain | Combo de dashes | Slow-motion | Time.timeScale réduit progressivement, ramp-in/out fluide | ✅ Déjà Présent | Movement |
| Dash Impact | Touche un ennemi | Hit Stop | Freeze frame momentané à l'impact pour accentuer la puissance | ✅ Déjà Présent | Movement |
| Dash Cooldown | Charges de dash utilisées | UI icônes grisées | Icônes de dash grisées pendant le cooldown, pop cascade au rechargement | ✅ Déjà Présent | UI |

---

## 🔫 Weapon

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Rechargement (Gun) | Rechargement | Je recharge | Retrait du chargeur, "raquer/Tirer le levier d'armement" | 🟢 Basse | Weapon |
| Tir | Clic gauche | Munitions > 0 | Recul de l'arme, shake caméra, traînée de balle visible (LineRenderer), son de tir | ✅ Déjà Présent | Weapon |
| Impact balle | Touche un ennemi | Ennemi dans le viseur | Particules d'impact, barre de vie diminue, flash sur l'ennemi | ✅ Déjà Présent | Weapon |
| Headshot | Tir sur la tête | Zone "Head" touchée | Multiplicateur de dégâts x2, feedback visuel différent | ✅ Déjà Présent | Weapon |
| Blood Bullet | Plus de munitions | Réserve vide, vie > 0 | Visuel rouge/sang sur l'arme, chaque tir consomme de la vie du joueur | ✅ Déjà Présent | Weapon |
| Spread | Tir continu | Plusieurs tirs rapides | Dispersion des balles augmente, croix de visée s'élargit | 🟠 Haute | Weapon |

---

## ❤️ Player Health

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Dégâts reçus | Perte de vie | Attaque ennemie | Vignette rouge à l'écran, son "Ouch", UI vie diminue | ✅ Déjà Présent | Player |
| Vie basse | Danger | Vie < 30% | Intensité de la vignette rouge augmente proportionnellement | ✅ Déjà Présent | Player |
| Régénération | Soin passif | Pas de dégâts depuis 3s | Vie remonte progressivement (5/s) | ✅ Déjà Présent | Player |
| Mort | Game Over | Vie = 0 | Écran de mort, event OnDeath déclenché | ✅ Déjà Présent | Player |
| Invulnérabilité | Dash actif | Pendant le dash | Aucun dégât pris pendant le mouvement de dash | ✅ Déjà Présent | Player |

---

## 👹 Ennemis - Standard (Chaser)

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Détection | Poursuite du joueur | Joueur dans le FOV | L'ennemi court vers le joueur | ✅ Déjà Présent | Enemy |
| Attaque | Coup de mêlée | À portée du joueur | Animation d'attaque, dégâts infligés | ✅ Déjà Présent | Enemy |
| Dash reçu | Mort instantanée | Touché par dash | Ragdoll, son de mort | ✅ Déjà Présent | Enemy |
| Investigation | Recherche | Joueur perdu de vue | L'ennemi va à la dernière position connue | ✅ Déjà Présent | Enemy |
| Coordination | Alerte partagée | Allié détecte joueur | Tous les ennemis alertés convergent | ✅ Déjà Présent | Enemy |

---

## ⚡ Ennemi Électrique

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Dash reçu | Stun du joueur | Touché par dash | Joueur paralysé pendant 2.5s, auto-fire forcé | ✅ Déjà Présent | Enemy |
| Auto-fire | Tir incontrôlé | Pendant le stun | L'arme tire automatiquement sans input joueur | ✅ Déjà Présent | Enemy |
| Mort | Décharge électrique | Tué par balle | Arc électrique AOE qui touche les ennemis proches (5m radius, 15 dégâts) | ✅ Déjà Présent | Enemy |
| Résistance | Survie au dash | Aura électrique visible | Cet ennemi ne meurt PAS au dash contrairement aux autres | ✅ Déjà Présent | Enemy |

---

## 🛡️ Ennemi Bouclier (Shield)

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Tir frontal | Blocage | Balles sur le bouclier | Flash du bouclier, pas de dégâts | ✅ Déjà Présent | Enemy |
| Dash frontal | Rebond sans dégâts | Dash dans le cône 90° | Le joueur rebondit, l'ennemi survit | ✅ Déjà Présent | Enemy |
| Dash dos/côté | Mort | Dash hors cône frontal | L'ennemi meurt normalement | ✅ Déjà Présent | Enemy |
| Ground Slam | Attaque AOE | Animation bouclier | VFX au sol, zone de dégâts | ✅ Déjà Présent | Enemy |

---

## 💚 Ennemi Healer

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Actif | Soin des alliés | Zone de heal visible | Les ennemis proches récupèrent de la vie | ✅ Déjà Présent | Enemy |
| Dash reçu | Mort + Soin joueur | Tué par dash | Le joueur récupère 30 PV, VFX vert de soin | ✅ Déjà Présent | Enemy |

---

## 🔮 Ennemi Magique

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Tir reçu | Réflexion | Aura magique visible | Les balles sont renvoyées vers le joueur (cooldown 0.15s) | ✅ Déjà Présent | Enemy |
| Réflexion | Dégâts au joueur | Projectile violet | 15 dégâts renvoyés, laser/projectile visible | ✅ Déjà Présent | Enemy |
| Dash reçu | Mort + Munitions | Tué par dash | Le joueur reçoit 10 munitions | ✅ Déjà Présent | Enemy |
| Mode Sniper | Tir chargé | En hauteur, à distance | Laser vert → rouge pendant charge, puis tir | ✅ Déjà Présent | Enemy |
| Fuite | Cherche hauteur | Joueur trop proche | L'ennemi monte sur un point haut | ✅ Déjà Présent | Enemy |

---

## 🏟️ Environnement / Arène

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Trigger traversé | Fermeture portes | Entrée dans l'arène | Animation fermeture, barrière active | ✅ Déjà Présent | Arena |
| Vague terminée | Spawn prochaine | Tous ennemis morts | Nouvelle vague d'ennemis apparaît | ✅ Déjà Présent | Arena |
| Arène terminée | Ouverture portes | Toutes vagues finies | Portes s'ouvrent, progression possible | ✅ Déjà Présent | Arena |
| Chargeur Electric | Porte s'ouvre | Ennemi Electric près du chargeur | Le chargeur s'illumine, porte déverrouillée | ✅ Déjà Présent | Arena |

---

## 🎨 UI / HUD

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Santé change | Mise à jour | Dégâts/Soin | Barre de vie animée | ✅ Déjà Présent | UI |
| Dash rechargé | Pop icône | Charge récupérée | Animation cascade des icônes de dash | ✅ Déjà Présent | UI |
| Blood Bullet actif | Indicateur | Plus de munitions | Effets rouges sur l'UI de l'arme | ✅ Déjà Présent | UI |
| Vignette santé | Intensité variable | Vie basse | Rouge proportionnel aux dégâts | ✅ Déjà Présent | UI |

---

## 🎵 Audio

| Déclencheur | Action | Signe | Feedback | Priorité | Catégorie |
|-------------|--------|-------|----------|----------|-----------|
| Tir | Son de tir | Clic gauche + munitions | Bruit arme + écho | ✅ Déjà Présent | Audio |
| Dash | Whoosh | Activation dash | Son de déplacement rapide | 🟠 Haute | Audio |
| Dégâts joueur | Son douleur | Vie diminue | "OuchRoblox" (pitch random 0.9-1.1) | ✅ Déjà Présent | Audio |
| Mort ennemi | Son mort | Ennemi tué | Son de destruction | 🟠 Haute | Audio |
| Décharge Electric | Arc électrique | Ennemi Electric meurt | Son grésillant | 🟠 Haute | Audio |
| Reload | Son mécanique | Touche R | Bruits de rechargement | ✅ Déjà Présent | Audio |

---

## 📊 Légende Priorités

| Tag | Signification |
|-----|---------------|
| ✅ Déjà Présent | Fonctionnalité implémentée dans le code |
| 🟠 Haute | À implémenter en priorité |
| 🟢 Basse | Nice to have, peut attendre |

---

*Document généré à partir de l'analyse du code source DefoulWar - 18/01/2026*
