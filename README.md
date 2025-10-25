# Proto3GD - Unity Slime Character Controller

A comprehensive Unity project featuring an advanced slime character controller with **two physics modes**: traditional spring-based soft-body simulation and a new **particle-based fluid (PBF) system**.

## 🌟 New: Particle-Based Fluid System

The slime can now be simulated using a realistic particle-based fluid system with:
- **800-1500 individual particles** per slime
- **Position-Based Fluids (PBF)** algorithm
- **Natural cohesion** and surface tension
- **Pass through narrow gaps** realistically
- **Split and merge** naturally

### Quick Start (5 minutes)

**Get started immediately**: See [PARTICLE_QUICKSTART.md](PARTICLE_QUICKSTART.md)

1. Open project in Unity 6.0+
2. Create slime: `GameObject → 3D Object → Slime Character`
3. Enable particle system in `SlimeController` inspector
4. Press Play!

### Documentation

- **[PARTICLE_QUICKSTART.md](PARTICLE_QUICKSTART.md)** - Get running in 5 minutes
- **[Assets/Scripts/Slime/PARTICLE_SYSTEM_GUIDE.md](Assets/Scripts/Slime/PARTICLE_SYSTEM_GUIDE.md)** - Complete technical documentation
- **[Assets/Scripts/Slime/README.md](Assets/Scripts/Slime/README.md)** - Full system reference
- **[SLIME_SYSTEM_SUMMARY.md](SLIME_SYSTEM_SUMMARY.md)** - Implementation overview

## Features

### Two Physics Modes

**Shape Solver (Default)**:
- ✅ Fast (~0.5ms per frame)
- ✅ Spring-based soft-body
- ✅ Good for most use cases
- ✅ Easy to tune

**Particle System (NEW)**:
- ✅ Realistic fluid simulation
- ✅ 800-1500 particles
- ✅ Pass through gaps naturally
- ✅ Advanced cohesion and surface tension
- ⚠️ More demanding (~2-7ms per frame)

### Core Systems

- **Dynamic Mass System**: Mass affects all physics properties
- **Adaptive Colliders**: Multi-sphere system that scales
- **Hybrid Movement**: Input + physics + material response
- **Absorption**: Small objects can be absorbed
- **Split/Merge**: Divide and combine slimes
- **Visual Deformation**: Squash & stretch effects
- **LOD System**: Performance optimization
- **Audio Feedback**: Context-aware sounds

## Project Structure

```
Proto3GD/
├── Assets/
│   └── Scripts/
│       └── Slime/
│           ├── SlimeController.cs (Main orchestrator)
│           ├── Core/
│           │   └── SlimeCoreState.cs
│           ├── Physics/
│           │   ├── SlimeShapeSolver.cs (Spring-based)
│           │   ├── SlimeParticleManager.cs (PBF system - NEW)
│           │   ├── SlimeColliderManager.cs
│           │   ├── SlimeMovementController.cs
│           │   └── SlimeMaterialResponse.cs
│           ├── Effects/
│           │   ├── SlimeDeformationFX.cs
│           │   └── SlimeParticleRenderer.cs (NEW)
│           ├── Systems/
│           │   ├── SlimeAbsorptionSystem.cs
│           │   ├── SlimeSplitMergeSystem.cs
│           │   └── SlimeLODController.cs
│           ├── Examples/
│           │   └── ParticleSlimeExample.cs (NEW)
│           ├── README.md (Component reference)
│           ├── ARCHITECTURE.md (Technical details)
│           └── PARTICLE_SYSTEM_GUIDE.md (PBF documentation - NEW)
│
├── PARTICLE_QUICKSTART.md (Quick start guide - NEW)
├── SLIME_QUICKSTART.md (Original quick start)
└── SLIME_SYSTEM_SUMMARY.md (Implementation summary)
```

## Requirements

- Unity 6.0 or later
- Input System package
- Basic understanding of Unity physics

## Getting Started

### For Beginners

1. Read [SLIME_QUICKSTART.md](SLIME_QUICKSTART.md) for basic slime setup
2. Try [PARTICLE_QUICKSTART.md](PARTICLE_QUICKSTART.md) for particle system

### For Developers

1. Read [Assets/Scripts/Slime/README.md](Assets/Scripts/Slime/README.md) for API reference
2. Read [Assets/Scripts/Slime/ARCHITECTURE.md](Assets/Scripts/Slime/ARCHITECTURE.md) for technical details
3. Read [Assets/Scripts/Slime/PARTICLE_SYSTEM_GUIDE.md](Assets/Scripts/Slime/PARTICLE_SYSTEM_GUIDE.md) for PBF system

### Example Usage

#### Traditional Shape Solver
```csharp
GameObject slime = new GameObject("Slime");
slime.AddComponent<Rigidbody>();
slime.AddComponent<SlimeCoreState>();
slime.AddComponent<SlimeController>(); // Auto-adds components
// useParticleSystem = false by default
```

#### Particle-Based Fluid
```csharp
GameObject slime = new GameObject("ParticleSlime");
slime.AddComponent<Rigidbody>();
slime.AddComponent<SlimeCoreState>();
slime.AddComponent<SlimeParticleManager>();
slime.AddComponent<SlimeParticleRenderer>();
// Or: SlimeController with useParticleSystem = true
```

## Performance

| Mode | Particle/Point Count | Performance | Best For |
|------|---------------------|-------------|----------|
| Shape Solver | 8-12 control points | ~0.5ms | Fast prototypes, mobile |
| Particle System | 800 particles | ~2-3ms | Realistic fluid, PC/console |
| Particle System | 1500 particles | ~5-7ms | High-end only |

## Key Algorithms

### Position-Based Fluids (PBF)

The new particle system implements:
1. **Semi-implicit Euler integration**
2. **Spatial hash grid** for O(n) neighbor search
3. **Density constraint solving** with Lagrange multipliers
4. **Cohesion forces** for particle attraction
5. **Surface tension** for smooth surfaces
6. **Viscosity smoothing** for fluid behavior

See [PARTICLE_SYSTEM_GUIDE.md](Assets/Scripts/Slime/PARTICLE_SYSTEM_GUIDE.md) for complete algorithm details.

## Customization

All systems expose parameters in the Inspector:
- Particle radius, cohesion, surface tension
- Mass, viscosity, friction
- Visual colors and effects
- LOD distances
- Audio settings

## Examples

**ParticleSlimeExample.cs** provides:
- Runtime particle manipulation (P/O keys)
- Color cycling (C key)
- Metaball toggle (M key)
- Info logging (L key)
- On-screen debug UI

## Known Limitations

- Particle system requires Unity 6.0+ (uses new physics API)
- Not true soft-body (approximation with constraints)
- Tunneling possible at extreme velocities
- Performance scales with particle count

## Future Enhancements

- Compute shader acceleration for 10,000+ particles
- Advanced rendering (marching cubes, metaballs)
- Two-way coupling with rigidbodies
- Multi-phase fluid simulation
- Network synchronization

## Credits

System designed according to specifications for advanced soft-body slime character controller with modular architecture, physics-based interactions, and performance optimizations.

Particle system based on Position-Based Fluids research by Macklin & Müller (2013).

## License

This code is part of the Proto3GD Unity project.

---

**Status**: ✅ Complete and ready for use  
**Latest Addition**: Particle-Based Fluid system  
**Documentation**: 50KB+ comprehensive guides  
**Components**: 12 modular systems  
**Time to First Prototype**: < 5 minutes  
