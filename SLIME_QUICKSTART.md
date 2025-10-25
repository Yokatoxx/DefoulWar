# Slime Controller Quick Start Guide

This guide will help you get started with the Slime Controller System in under 5 minutes.

## Prerequisites

- Unity 6 (version 6000.0.58f2 or later)
- Unity Input System package (included)
- Universal Render Pipeline (URP) - optional but recommended

## Quick Setup

### Step 1: Create a Slime Character

1. **Create a new GameObject**
   - In Unity Hierarchy, right-click → Create Empty
   - Name it "Slime"

2. **Add the SlimeController component**
   - Select the Slime GameObject
   - In Inspector, click "Add Component"
   - Search for "Slime Controller"
   - Add the component

3. **That's it!** The system will automatically add all required components when you enter Play mode.

### Step 2: Set Up Input

1. **Locate the SlimeMovementController component**
   - It will be automatically added to your Slime GameObject
   - Find it in the Inspector

2. **Assign Input Actions**
   - In SlimeMovementController:
     - **Move Action**: Select the dropdown → Player → Move
     - **Jump Action**: Select the dropdown → Player → Jump

### Step 3: Create a Test Environment

1. **Add a ground plane**
   - Right-click in Hierarchy → 3D Object → Plane
   - Position at (0, 0, 0)
   - Scale to (10, 1, 10) or larger

2. **Add a camera** (if not present)
   - GameObject → Camera
   - Position at (0, 5, -10)
   - Rotate to look at the slime (e.g., Rotation: 20, 0, 0)

3. **Position the slime**
   - Select your Slime GameObject
   - Set Position to (0, 1, 0)

### Step 4: Add Visual Representation (Optional but Recommended)

1. **Create a visual mesh**
   - Right-click the Slime GameObject → 3D Object → Sphere
   - Name it "Visual"
   - Set its Position to (0, 0, 0)
   - Scale to (1, 1, 1)

2. **Assign to Deformation FX**
   - Select the Slime GameObject
   - Find the SlimeDeformationFX component
   - Drag the "Visual" child into the "Visual Mesh" field

3. **Add a material**
   - Create a material (Right-click in Project → Create → Material)
   - Name it "SlimeMaterial"
   - Set color to green or your preferred slime color
   - Add some transparency (Alpha channel) for a gooey look
   - Assign to the Visual sphere

### Step 5: Test!

1. **Enter Play Mode** (press Play button or F5)
2. **Controls**:
   - **WASD**: Move the slime
   - **Space**: Jump
   - **Hold still + Space**: Charged jump (bounce higher)
   - **X**: Split the slime (requires enough mass)
   - **C**: Merge with nearby slimes

## Advanced Setup

### Creating Absorbable Objects

1. **Create small objects** (spheres, cubes, etc.)
2. **Add the AbsorbableItem component**
   ```
   Add Component → Absorbable Item
   ```
3. **Configure properties**:
   - Mass: How much mass it adds (e.g., 1-5)
   - Energy Value: Energy gained (e.g., 10)
   - Type: Generic, Water, Food, or Material

4. **Set the layer**:
   - Create a layer named "Absorbable"
   - Assign the object to this layer

5. **Configure Absorption System**:
   - On Slime → SlimeAbsorptionSystem
   - Set "Absorbable Mask" to include the "Absorbable" layer

### Enabling Split/Merge

For splitting to work:
1. Create a complete slime (follow all steps above)
2. Create a Prefab from it:
   - Drag the Slime GameObject to the Project window
3. On the Slime → SlimeSplitMergeSystem:
   - Assign the prefab to "Slime Prefab"

Now X will split the slime when it has enough mass!

### Adding Audio

1. **Find or create sound effects**:
   - Gloopy step sounds
   - Impact/splat sounds
   - Absorption sounds

2. **Assign to SlimeAudioFeedback**:
   - Select Slime GameObject
   - Find SlimeAudioFeedback component
   - Assign audio clips to the arrays

## Customization

### Adjusting Movement Feel

In **SlimeMovementController**:
- `Base Acceleration`: Higher = faster acceleration (default: 20)
- `Max Speed`: Maximum movement speed (default: 5)
- `Jump Force`: Jump height (default: 10)
- `Lateral Damping`: Higher = more controlled movement (default: 0.5)

### Adjusting Physics Properties

In **SlimeMaterialResponse**:
- `Base Viscosity`: 0 = runny, 1 = thick (default: 0.5)
- `Min/Max Restitution`: Bounce range (default: 0.2 - 0.8)
- `Base Friction`: Surface friction (default: 0.6)

### Adjusting Visual Deformation

In **SlimeDeformationFX**:
- `Max Stretch`: How much to stretch when moving fast (default: 0.5)
- `Squash Amount`: How much to squash when charging jump (default: 0.3)
- `Smooth Time`: Deformation smoothness (default: 0.1)

### Adjusting Mass/Volume

In **SlimeCoreState**:
- `Base Mass`: Starting mass (default: 10)
- `Max Mass`: Maximum capacity (default: 50)
- `Hydration`: Fluid level 0-1 (default: 1)

## Troubleshooting

### Slime doesn't move
- **Check**: Input actions are assigned in SlimeMovementController
- **Check**: Input System is enabled (Window → Analysis → Input Debugger)
- **Check**: Rigidbody is not kinematic

### Slime falls through floor
- **Check**: Ground has a collider (MeshCollider or BoxCollider)
- **Check**: Rigidbody collision detection is set to "Continuous"
- **Fix**: Increase ground collider thickness

### Controls don't work
- **Check**: Input actions are enabled
- **Fix**: Window → Analysis → Input Debugger → check if actions respond to keys
- **Check**: SlimeMovementController has the correct action references

### Slime looks weird/jittery
- **Adjust**: Increase damping in SlimeMovementController
- **Adjust**: Reduce maxSpeed
- **Check**: Fixed Timestep is 0.02 (Edit → Project Settings → Time)

### Absorption doesn't work
- **Check**: Objects are on correct layer
- **Check**: AbsorptionSystem's "Absorbable Mask" includes that layer
- **Check**: Objects are within absorption radius (see green wire sphere in Scene view)

## Performance Tips

1. **For multiple slimes**: Enable the LOD system to reduce detail at distance
2. **For mobile**: Reduce satellite collider count to 4-6
3. **For VR**: Increase update frequencies for smoother visuals
4. **For many objects**: Disable unused systems on distant slimes

## Next Steps

- Read the full [README.md](Assets/Scripts/Slime/README.md) for detailed documentation
- Experiment with different property values
- Create custom behaviors by extending the system
- Add particle effects for absorption/splitting
- Create custom shaders for advanced visual effects

## Controls Reference

### Default Keyboard Controls
- **W/A/S/D or Arrow Keys**: Move
- **Space**: Jump
- **Left Shift**: Sprint (if enabled)
- **X**: Split slime
- **C**: Merge with nearby slimes
- **E**: Interact (hold)
- **Left Ctrl**: Crouch

### Default Gamepad Controls
- **Left Stick**: Move
- **South Button (A/Cross)**: Jump
- **West Button (X/Square)**: Split
- **East Button (B/Circle)**: Merge

## Example Scene Setup

For a complete example:
1. Follow all steps in "Quick Setup"
2. Add multiple absorbable objects scattered around
3. Create ramps and platforms for testing jumps
4. Add obstacles to test collision response
5. Place multiple slimes to test merge mechanics

Happy sliming! 🟢
