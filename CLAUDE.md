# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an Outer Wilds-inspired space exploration game built with Unity. The core focus is physics-driven celestial movement, planetary gravity, and seamless space exploration.

**Unity Version**: 2022.3.62f3c1
**Render Pipeline**: Universal Render Pipeline (URP) 14.0.12
**Input System**: Unity Input System 1.14.2

## Architecture

### Orbit System (Core Celestial Mechanics)

The galaxy uses a centralized orbit system that manages all celestial bodies in a depth-sorted hierarchy to prevent coordinate jitter.

- **OrbitSystem** (`Assets/Scripts/Orbit/OrbitSystem.cs`): Singleton manager that updates all OrbitBody positions in `FixedUpdate`. Bodies are sorted by dependency depth (star → planet → moon) to ensure parent coordinates update before children.
- **OrbitBody** (`Assets/Scripts/Orbit/OrbitBody.cs`): Data-only component defining elliptical orbital parameters:
  - `semiMajorAxis` (a): Long semi-axis
  - `eccentricity` (e): Shape parameter (0 = circular, <1 = elliptical)
  - Semi-minor axis (b) is auto-calculated: `b = a * sqrt(1 - e²)`
  - `orbitNormal`: Orbital plane orientation
  - `angularSpeed`: Orbital velocity
  - `initialPhase`: Starting position on orbit

**Critical Rules**:
- Never manually set semi-minor axis; eccentricity is the single source of truth
- Never add position logic to OrbitBody; OrbitSystem handles all updates
- Orbits are calculated in local space relative to orbitCenter, then mapped to world space

### Gravity System

Planetary gravity uses custom physics (not Unity's built-in gravity) to support multi-body scenarios and varying gravitational fields.

- **PlanetGravity** (`Assets/Scripts/Gravity/PlanetGravity.cs`): Static registry of all gravity sources. Each defines `gravityStrength`, `gravityRange`, and optional distance falloff (inverse-square law).
- **GravityController** (`Assets/Scripts/Player/GravityController.cs`): Player component that:
  - Finds closest planet within range
  - Applies custom gravity force via `Rigidbody.AddForce()`
  - Aligns player's `transform.up` to gravity direction (planet surface normal)
  - Syncs player position to planet's reference frame (handles planet rotation/translation)
  - Disables Unity's built-in gravity and locks rigidbody rotation

### Player Controller

Multi-component system for player movement and abilities:

- **PlayerController** (`Assets/Scripts/Player/PlayerController.cs`): Input handling and camera rotation (yaw/pitch). Uses new Input System with `OnMove`, `OnLook`, `OnJump` callbacks.
- **MovementController** (`Assets/Scripts/Player/MovementController.cs`): Physics-based movement on planetary surfaces. Uses local-space movement (forward/right) that automatically aligns with gravity direction.
- **GravityController**: See above
- **SpaceSuit** (`Assets/Scripts/Player/SpaceSuit.cs`): Manages jetpack fuel system with depletion, recharging, and events for UI updates.
- **JetpackController** (`Assets/Scripts/Player/JetpackController.cs`): Handles vertical thrust (Shift/Ctrl) using SpaceSuit fuel.

### Lighting System

Dynamic solar lighting that tracks celestial positions:

- **SunController** (`Assets/Scripts/Light/SunController.cs`): Manages sun's emissive material using HDR colors.
- **SolarLightSynchronizer** (`Assets/Scripts/Light/SolarLightSynchronizer.cs`): Keeps Directional Light aligned from sun toward reference point (usually player). Uses `LateUpdate` to reduce shadow jitter.
- **PlanetRotation** (`Assets/Scripts/Light/PlanetRotation.cs`): Simple self-rotation for day/night cycles.

### HUD & UI System

Space navigation and suit status displays:

- **SpaceNavigator** (`Assets/Scripts/PlayerTools/SpaceNavigator.cs`): Space navigation HUD that tracks celestial bodies, displays distance/approach speed, and shows velocity arrows. Features:
  - Tab toggles HUD visibility
  - Left-click locks/unlocks target tracking
  - Automatic celestial body suggestion via raycast
  - Velocity arrows indicate relative movement direction
  - Distance display switches between m/km based on threshold
- **SpaceSuitStatus** (`Assets/Scripts/UI/SpaceSuitStatus.cs`): Displays jetpack fuel level via arc-shaped UI element. Subscribes to SpaceSuit fuel change events.

## Code Conventions

- **Naming**: PascalCase for public members, `_camelCase` for private fields
- **Namespaces**: All scripts use appropriate namespaces (`OuterWitness.Orbit`, `OuterWitness.Gravity`, etc.)
- **Component Design**: Follow single responsibility principle. Separate data (OrbitBody) from logic (OrbitSystem)
- **Performance**: Never use `GetComponent` or `GameObject.Find` in `Update`/`FixedUpdate`. Cache all references in `Awake`/`Start`
- **Physics**: All celestial motion and player movement use physics-based approaches, not coordinate manipulation

## Development Setup

1. **Scene Structure**: Place scenes in `Assets/Scenes` (organized by galaxy/system)
2. **Assets**: Models in `Assets/Models`, materials in `Assets/Materials`
3. **Physics Settings**:
   - Fixed Timestep: 0.01-0.02s (tuned for high-speed space travel)
   - Player rigidbody: `useGravity = false`, `interpolation = Interpolate`, `freezeRotation = true`

## Controls

- **WASD**: Movement on planetary surfaces
- **Mouse**: Camera rotation (yaw/pitch)
- **Space**: Jump
- **Shift**: Jetpack thrust upward
- **Ctrl**: Jetpack thrust downward
- **Q/E**: Space-only roll adjustment (when not in gravity field)
- **Tab**: Toggle space navigation HUD
- **Left-click**: Lock/unlock celestial body tracking in HUD

## Known Limitations & Future Work

- **Floating Origin**: Not yet implemented. Will be needed for large-scale space exploration to prevent floating-point precision issues
- **Kepler's Third Law**: Orbital speeds are manually set; could be auto-calculated based on orbital radius
- **Reference Frame Switching**: Gravity system works for single-planet scenarios; multi-planet transitions may need enhancement

## Testing

No test framework is currently configured. When adding tests:
1. Check for existing test commands in project
2. Likely candidates: NUnit (com.unity.ext.nunit@1.0.6 is installed)
3. Unity Test Runner: Window > General > Test Runner
