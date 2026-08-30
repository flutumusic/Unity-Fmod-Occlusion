# Flutu Audio Occlusion System

FMOD Studio 2.03 integration for Unity 6+. Production-ready audio occlusion with dynamic filtering, smooth parameter transitions, and zero-overhead emitter registry.

## Features

- **Raycast-based occlusion** — 5-ray spread pattern (configurable) toward listener
- **Per-emitter max-distance filtering** — uses FMOD Studio event attenuation automatically
- **OccludableEmitterRegistry** — zero-overhead emitter tracking without colliders
- **OccludableEmitter component** — auto-register/unregister on enable/disable
- **Attack/Release smoothing** — faster close (15f), slower open (4f) prevents flutter on corner rays
- **Multi-material composition** — obstacles accumulate realistically, rays averaged
- **Dynamic layer filtering** — raycast only tests relevant collision layers
- **Memory cleanup** — automatic dead-emitter removal every 5 seconds
- **Debug visualization** — ray-by-ray hit inspection in Scene view
- **Zero external dependencies** — FMOD Studio only

## Components

### Components to Add to GameObjects

- **OcclusionListener** — attach to listener GameObject (script: `OcclusionListener.cs`)
  - Requires: GameObject with transform (usually Camera)
  - Detects nearby emitters, calls occlusion calculator
  
- **OccludableEmitter** — attach to each sound-emitting GameObject (script: `OccludableEmitter.cs`)
  - Requires: GameObject with StudioEventEmitter component
  - Auto-registers/unregisters with OccludableEmitterRegistry
  
- **Occluder** — attach to blocking geometry (script: `Occluder.cs`)
  - Requires: Collider component on the same GameObject
  - Defines occlusion strength (0–1 range in Inspector)

### Internal Components (Developer Reference)

- **OcclusionCalculator** — raycasting engine, parameter smoothing, registry cleanup
- **OccludableEmitterRegistry** — static registry with change notifications

### How It Works

1. **OccludableEmitterRegistry** tracks all active emitters (via `OccludableEmitter` component)
2. **OcclusionListener** subscribes to registry changes
3. For each nearby emitter, **OcclusionCalculator**:
   - Shoots 5 rays in a spread from emitter toward listener
   - Each ray tests for obstacles and accumulates their occlusion values
   - Center ray early-exits if clear (no occlusion = 0)
   - Averages occlusion across all rays
   - Applies attack/release smoothing
   - Sends parameter to FMOD via `SetParameter("Occlusion")`

## Setup

### 1. Add OcclusionListener Component

**Where:** OcclusionListener GameObject (usually main Camera)

Attach `OcclusionListener` component (script: `Assets/Flutu/Audio/OcclusionListener.cs`).

**Requirements:**
- GameObject must have a Transform component
- Must have an active Collider component (used for raycasts origin)

**Inspector fields:**
- **Occlusion Layer Mask** — which layers to raycast (filter out irrelevant colliders)
- **Debug Visualize** — shows ray hits in Scene view (red = blocked, green = clear)

### 2. Add OccludableEmitter Components

**Where:** Every sound-emitting GameObject

Attach `OccludableEmitter` component (script: `Assets/Flutu/Audio/OccludableEmitter.cs`) to each GameObject with `StudioEventEmitter`.

**Requirements:**
- Must have `StudioEventEmitter` component on the same GameObject
- No collider required

**Setup:**
- Just add the component — it auto-registers on enable, auto-unregisters on disable
- No Inspector configuration needed

### 3. Add Occluder Components

**Where:** Blocking geometry (walls, doors, etc.)

Attach `Occluder` component (script: `Assets/Flutu/Audio/Occluder.cs`) to each blocking GameObject.

**Requirements:**
- Must have a Collider component (Box, Sphere, Capsule, Mesh)
- Collider should be on the obstacle layer (matching Listener's Occlusion Layer Mask)

**Inspector fields:**
- **Has Occlusion** — enable to make obstacle block sound (0–1 slider)
- **Occlusion Value** — how much this obstacle blocks (0 = transparent, 1 = complete blockage)

### 4. Set Up FMOD Parameter

In FMOD Studio:
1. Create a global parameter `Occlusion` (range 0–1, default 0)
2. Modulate your busses or effects using this parameter
3. Example: Master bus compression increases with occlusion, high-pass filter opens as occlusion falls

### 5. Optional: Configure Smoothing

In `OcclusionCalculator`:

```csharp
private const float attackSpeed = 15f;    // closing speed (rising occlusion)
private const float releaseSpeed = 4f;    // opening speed (falling occlusion)
private const float cleanupInterval = 5f; // dead-emitter cleanup frequency
```

## Usage

### Automatic (Recommended)

Once `OcclusionListener` and `OccludableEmitter` components are in place, occlusion is calculated every FixedUpdate. No code needed.

### Manual Query

```csharp
// Get current occlusion value for an emitter
float occlusion = OcclusionCalculator.GetCurrentOcclusion(emitter);
```

### Debug Visualization

1. Select the GameObject with `OcclusionListener` component
2. Toggle **Debug Visualize** in Inspector
3. Rays appear in Scene view (update rate = FixedUpdate ~50fps)

Red = blocked, Green = clear

## Optimization

- **LayerMask filtering** — raycast only tests relevant obstacle layers
- **Direct ray early-exit** — returns 0 immediately if center ray is clear
- **Registry-based detection** — no Physics.OverlapSphere (way faster)
- **Dead-emitter cleanup** — prevents memory leak from streaming sub-scenes
- **Parameter threshold** — skips FMOD updates if change < 0.005

## Validation

- **Listener passing slowly behind corner** — no audible filter flutter
- **Emitter behind solid wall** — full occlusion immediately (no fade-in)
- **Multiple materials in ray path** — occlusion accumulates (0.3 + 0.3 ≈ 0.51, not 0.3)
- **Sub-scene streaming** — 20× load/unload cycles → no memory growth

## Troubleshooting

### No Occlusion Detected

- ✅ `OccludableEmitter` component added to emitter GameObject?
- ✅ Occluders have colliders?
- ✅ Occluders have `Occluder` component with hasOcclusion enabled?
- ✅ LayerMask includes obstacle layers?
- ✅ Debug Visualize shows rays? (Scene view, not Game view)

### Occlusion Too Strong / Too Weak

Adjust attack/release speeds in OcclusionCalculator:

```csharp
attackSpeed = 20f;   // faster blocking
releaseSpeed = 2f;   // slower opening
```

### Performance Issues

Profile in Unity Profiler (Window → Analysis → Profiler):
1. Check `OcclusionCalculator.Calculate` time
2. Reduce rayCount (5 → 3) if needed
3. Increase cleanupInterval if streaming heavy loads

## Support

hello@flutumusic.com  
https://flutumusic.com
