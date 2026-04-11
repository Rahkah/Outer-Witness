# Product Overview

Outer Witness is a 3D space exploration game built in Unity, inspired by Outer Wilds. The player explores a miniature solar system with multiple celestial bodies, each with its own gravity field.

## Core Gameplay
- First-person player movement on spherical planets with per-planet gravity alignment
- Jetpack-based traversal between planets and through open space
- Space suit with a fuel system (depletion on thrust, recharge on ground)
- Orbital mechanics simulation with elliptical orbits, dependency-sorted updates
- Space navigator HUD for tracking celestial bodies (distance, velocity, lock-on targeting)
- Dynamic solar lighting that follows the sun-to-player direction

## Key Mechanics
- Gravity is radial and per-planet; the player aligns to the nearest planet's surface normal
- Reference frame synchronization keeps the player stable on orbiting/rotating planets
- Jetpack roll (Q/E) is only available in zero-gravity (no planet in range)
- The sun is the visual and gravitational center of the system

## Language
- Code comments and Unity Inspector tooltips are written in Chinese (Simplified)
- Class/method/variable names and namespaces are in English
