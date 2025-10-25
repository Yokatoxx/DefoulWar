# Ragdoll Slime System - Quick Start Guide

## What is the Ragdoll System?

The Ragdoll Slime System is a joint-based physics implementation that creates a slime made of multiple interconnected spheres, similar to an active ragdoll character. Unlike the default single-rigidbody approach, each sphere is an independent rigidbody connected to others with physics joints (SpringJoint or ConfigurableJoint).

### When to Use It

✅ **Use Ragdoll Mode when:**
- You want more realistic soft-body physics behavior
- Your slime needs to squeeze through narrow gaps
- Physical deformation from forces is important
- You prioritize realism over performance
- Working on hero characters or focal gameplay elements

❌ **Don't use Ragdoll Mode when:**
- You need maximum performance (use ShapeSolver instead)
- You have many slime instances (10+)
- Simple collision detection is sufficient
- Running on lower-end hardware

## Quick Setup (2 Minutes)

### Method 1: Using Unity Menu (Easiest)

1. In Unity Editor, go to **GameObject → 3D Object → Slime Character (Ragdoll)**
2. A fully configured ragdoll slime will be created
3. Assign Input Actions:
   - Select the slime GameObject
   - Find `SlimeMovementController` component
   - Assign `Move Action` to your move input
   - Assign `Jump Action` to your jump input
4. Press Play and test!

### Method 2: Converting Existing Slime

If you already have a slime character:

1. Select your slime GameObject
2. Find the `SlimeController` component
3. Change **Physics Mode** from `ShapeSolver` to `Ragdoll`
4. The system will automatically:
   - Add `SlimeRagdollManager` component
   - Disable `SlimeShapeSolver` and `SlimeColliderManager`
   - Configure joints and satellite spheres
5. Press Play to see the ragdoll in action

### Method 3: Manual Setup

For complete control:

1. Create a GameObject
2. Add `Rigidbody` component (mass: 10)
3. Add `SlimeCoreState` component
4. Add `SlimeRagdollManager` component
5. Add `SlimeMovementController` component
6. Configure as needed

## Configuration

### Basic Parameters (SlimeRagdollManager)

**Sphere Configuration:**
- `Sphere Count` (default: 8): Number of satellite spheres
  - More spheres = smoother deformation but higher cost
  - Recommended: 6-12 spheres
- `Sphere Radius` (default: 0.3): Size of each satellite sphere
- `Sphere Distance` (default: 0.5): Distance from core sphere
- `Sphere Mass` (default: 1): Mass per satellite

**Joint Configuration:**
- `Use Spring Joints` (default: true): Use SpringJoint vs ConfigurableJoint
  - SpringJoint: Simpler, better performance
  - ConfigurableJoint: More control, more expensive
- `Joint Spring` (default: 50): Stiffness of connections
  - Higher = stiffer (holds shape better)
  - Lower = softer (more fluid-like)
- `Joint Damper` (default: 5): Damping of oscillations
  - Higher = less bouncy
  - Lower = more springy
- `Joint Max Distance` (default: 0.8): Maximum stretch allowed
  - Prevents spheres from separating too far

### Tuning Tips

**For a Stiff Slime (gelatin-like):**
```
Joint Spring: 100-200
Joint Damper: 10-20
```

**For a Soft Slime (fluid-like):**
```
Joint Spring: 20-50
Joint Damper: 2-5
```

**For a Bouncy Slime:**
```
Joint Spring: 50
Joint Damper: 2
Increase SlimeCoreState → Hydration
```

## Common Issues

### Problem: Slime explodes on start
**Solution:** Reduce `Joint Spring` or increase `Joint Damper`

### Problem: Slime is too rigid
**Solution:** 
- Decrease `Joint Spring` (try 30-40)
- Increase `Sphere Count` for more flexibility
- Set `Use Spring Joints` to true (if not already)

### Problem: Slime falls through floor
**Solution:**
- Increase sphere collider radii
- Check collision detection is set to "Continuous"
- Verify ground has a collider

### Problem: Poor performance
**Solution:**
- Reduce `Sphere Count` (try 6 instead of 8)
- Switch to `ShapeSolver` mode for better performance
- Use LOD system (SlimeLODController)

### Problem: Movement feels unresponsive
**Solution:**
- Increase mass on satellite spheres
- Adjust `Joint Spring` (try 70-100)
- Check that SlimeMovementController is properly configured

## Controls

Same as standard slime:
- **WASD / Arrow Keys**: Move
- **Space**: Jump (hold still to charge)
- **X**: Split (if enabled)
- **C**: Merge (if enabled)

## Performance

**Typical Cost per Frame:**
- ShapeSolver Mode: ~0.1-0.2ms
- Ragdoll Mode: ~0.5-1.5ms (5-10x higher)

**Recommendation:**
- Use ragdoll for 1-3 main characters
- Use ShapeSolver for background/enemy slimes
- Consider LOD switching based on distance

## Comparison with Other Modes

| Feature | ShapeSolver | Ragdoll | Particle System |
|---------|------------|---------|-----------------|
| Performance | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| Realism | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Stability | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| Setup | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| Deformation | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |

## Next Steps

- Read [SLIME_SYSTEM_SUMMARY.md](SLIME_SYSTEM_SUMMARY.md) for complete system overview
- Check [Assets/Scripts/Slime/README.md](Assets/Scripts/Slime/README.md) for detailed component reference
- Review [Assets/Scripts/Slime/ARCHITECTURE.md](Assets/Scripts/Slime/ARCHITECTURE.md) for technical details
- Experiment with different joint configurations for your game's needs

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review the main documentation files
3. Inspect the SlimeRagdollManager component in the Inspector during Play Mode
4. Use Gizmos in Scene view to visualize joint connections

---

**Quick Tip:** Press play and select your slime in the Scene view to see the Gizmos showing all joints and spheres. The red sphere is the center of mass!
