# Guide NavMesh pour les Piliers

Ce guide explique comment configurer les piliers créés dynamiquement par les ennemis pour qu'ils soient pris en compte par le système de navigation NavMesh.

---

## 🎯 Aperçu

Les piliers générés à la mort des ennemis peuvent maintenant servir d'**obstacles dynamiques** pour le NavMesh, ce qui permet aux ennemis de les contourner intelligemment lors de leurs déplacements.

---

## ⚙️ Configuration Automatique

### Pour les Nouveaux Piliers (Recommandé)

**Les piliers créés après cette mise à jour sont automatiquement configurés !**

Le composant `PillarController` ajoute maintenant automatiquement un `NavMeshObstacle` lors de l'apparition du pilier. Aucune action manuelle n'est requise.

#### Paramètres dans PillarController

Dans l'Inspector du prefab de votre pilier, vous trouverez une nouvelle section **"NavMesh Settings"** :

- **Is NavMesh Obstacle** : Active/désactive l'obstacle NavMesh (activé par défaut)
- **Carve NavMesh** : Permet au pilier de "creuser" un trou dans le NavMesh (recommandé)
- **NavMesh Activation Delay** : Délai avant activation (0.5s par défaut, utile pour l'animation de spawn)

### Pour les Piliers Existants

Si vous avez déjà des piliers dans votre scène ou dans vos prefabs, utilisez l'un des outils suivants :

#### Méthode 1 : Via le Menu Unity

1. Allez dans le menu **Tools → FPS System → Configure NavMesh for All Pillars**
2. Cliquez et confirmez
3. ✅ Tous les piliers de la scène seront configurés automatiquement !

#### Méthode 2 : Via l'Inspecteur du PillarDashSystem

1. Sélectionnez le GameObject avec le composant `PillarDashSystem`
2. Dans l'Inspector, cliquez sur le bouton **"🧭 Configurer NavMesh pour Piliers"**
3. ✅ Configuration automatique !

---

## 🔧 Configuration Manuelle (Optionnel)

Si vous préférez configurer un pilier manuellement :

1. Sélectionnez votre pilier dans la hiérarchie
2. Dans l'Inspector, cliquez sur **Add Component**
3. Ajoutez **Nav Mesh Obstacle**
4. Configurez les paramètres :
   - ✅ Activez **Carve**
   - Forme : **Box** (ou Capsule selon votre collider)
   - Ajustez **Size** et **Center** pour correspondre au collider

---

## 📋 Fonctionnement Technique

### NavMeshObstacle vs NavMesh Statique

Le système utilise **NavMeshObstacle** avec l'option **Carving** activée, ce qui permet :

- ✅ **Obstacles dynamiques** : Les piliers peuvent apparaître/disparaître pendant le jeu
- ✅ **Pas de rebake** : Le NavMesh n'a pas besoin d'être recalculé
- ✅ **Performance optimale** : Les ennemis recalculent leur chemin automatiquement
- ✅ **Compatible** : Fonctionne avec le NavMesh baked et les NavMeshSurface

### Processus d'Activation

Lors du spawn d'un pilier :

1. Le `PillarController` détecte le collider du pilier
2. Un `NavMeshObstacle` est créé avec les bonnes dimensions
3. L'obstacle est désactivé pendant l'animation de spawn (0.5s par défaut)
4. L'obstacle est activé automatiquement et "creuse" le NavMesh
5. Les ennemis contournent désormais le pilier !

---

## 🎮 Utilisation en Jeu

Une fois configurés, les piliers fonctionnent automatiquement :

1. **Un ennemi meurt** → Un pilier apparaît
2. **Le pilier s'anime** (montée progressive)
3. **NavMeshObstacle s'active** après 0.5 secondes
4. **Les autres ennemis contournent** le nouveau pilier automatiquement

---

## 🔍 Vérification

### Comment vérifier que ça fonctionne ?

1. **Mode Play** : Lancez le jeu
2. **Tuez un ennemi** : Un pilier apparaît
3. **Observez la Console** : Vous devriez voir "NavMeshObstacle activé pour [nom du pilier]"
4. **Observez les ennemis** : Ils devraient contourner le nouveau pilier

### Debug Visuel

Pour visualiser les obstacles NavMesh dans la scène :

1. Ouvrez **Window → AI → Navigation**
2. Dans l'onglet **Bake**, en bas, activez **Show NavMesh**
3. Les zones bleues = NavMesh navigable
4. Les zones creusées = Obstacles (vos piliers)

---

## ⚡ Performance

### Impact sur les Performances

- **Très faible** : NavMeshObstacle avec Carving est optimisé par Unity
- **Recommandé** : Jusqu'à 50-100 piliers simultanés sans problème
- **Si trop de piliers** : Utilisez la durée de vie (`hasLifetime = true`) pour les détruire après un certain temps

### Optimisation

Dans `PillarController`, vous pouvez :

```csharp
[Header("Lifetime Settings")]
[SerializeField] private bool hasLifetime = true;  // Activer
[SerializeField] private float lifetime = 30f;     // 30 secondes
```

Cela détruira automatiquement les piliers après 30 secondes, libérant les ressources.

---

## 🐛 Dépannage

### Les ennemis traversent encore les piliers

**Cause possible** : Le NavMesh n'est pas configuré pour les ennemis

**Solution** :
1. Vérifiez que vos ennemis ont un composant **NavMeshAgent**
2. Vérifiez que le NavMesh est baked (Window → AI → Navigation → Bake)
3. Vérifiez que "Carve" est activé sur le NavMeshObstacle du pilier

### Le pilier n'a pas de NavMeshObstacle

**Solution** :
1. Utilisez **Tools → FPS System → Configure NavMesh for All Pillars**
2. Ou vérifiez que `isNavMeshObstacle = true` dans le PillarController

### L'obstacle NavMesh a la mauvaise taille

**Solution** :
1. Le système détecte automatiquement la taille du Collider
2. Assurez-vous que votre pilier a un **Collider** (Box, Capsule, ou Mesh)
3. Si nécessaire, ajustez manuellement les paramètres dans l'Inspector

---

## 📝 Résumé Rapide

### Pour Commencer

1. ✅ Vos nouveaux piliers sont **déjà configurés** !
2. ✅ Pour les piliers existants : **Tools → FPS System → Configure NavMesh for All Pillars**
3. ✅ Testez en Play Mode : Les ennemis contournent les piliers

### Configuration Recommandée

```
PillarController (sur le prefab de pilier) :
├─ NavMesh Settings
│  ├─ Is NavMesh Obstacle : ✅ True
│  ├─ Carve NavMesh       : ✅ True
│  └─ Activation Delay    : 0.5s
└─ Lifetime Settings (optionnel)
   ├─ Has Lifetime        : ✅ True (pour optimisation)
   └─ Lifetime            : 30s (ajustez selon vos besoins)
```

---

## 🚀 Prochaines Étapes

- Testez avec plusieurs ennemis et piliers
- Ajustez les paramètres selon votre gameplay
- Utilisez la durée de vie si vous avez beaucoup de piliers
- Expérimentez avec les tailles d'obstacles pour différents types de piliers

---

**Besoin d'aide ?** Consultez la documentation Unity sur NavMeshObstacle : https://docs.unity3d.com/Manual/class-NavMeshObstacle.html

