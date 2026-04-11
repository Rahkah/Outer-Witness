# Project Structure

## Root Namespace
All gameplay code lives under the `OuterWitness` root namespace.

## Script Organization — `Assets/Scripts/`

| Folder | Namespace | Purpose |
|---|---|---|
| `Gravity/` | `OuterWitness.Gravity` | Planet gravity fields (`PlanetGravity`) — static registry pattern via `AllPlanets` list |
| `Orbit/` | `OuterWitness.Orbit` | Orbital mechanics — singleton `OrbitSystem` manages all `OrbitBody` components, sorted by dependency depth |
| `Player/` | `OuterWitness.Player` | Player systems: input (`PlayerController`), movement (`MovementController`), gravity alignment (`GravityController`), jetpack (`JetpackController`), fuel (`SpaceSuit`) |
| `PlayerTools/` | `OuterWitness.PlayerTools` | In-game tools/HUDs like `SpaceNavigator` (celestial body tracker) |
| `Light/` | `OuterWitness.Light` | Sun visuals (`SunController`), planet rotation (`PlanetRotation`), directional light sync (`SolarLightSynchronizer`) |
| `UI/` | `OuterWitness.UI` | HUD elements (`SpaceSuitStatus` fuel arc). Contains empty `Debug/` and `UiActionParts/` subfolders |
| `Editor/` | `OuterWitness.Editor` | Editor-only tools (`PlanetMeshFixer` — high-res sphere generation, normal fixing) |

## Architecture Patterns
- Component-based: each behavior is a separate MonoBehaviour, composed on GameObjects via `[RequireComponent]`
- Singleton: `OrbitSystem.Instance` for centralized orbit updates
- Static registry: `PlanetGravity.AllPlanets` — planets self-register in `OnEnable`/`OnDisable`
- Event-driven UI: `SpaceSuit.OnFuelChanged` event drives `SpaceSuitStatus` updates
- Physics in `FixedUpdate`, visuals/input in `Update`/`LateUpdate`

## Other Folders
- `Assets/Art/` — Materials (planet, cockpit, player, sun), UI sprites (SpaceNavigator, Status), skybox
- `Assets/Scenes/` — Single scene: `SampleScene.unity` with Global Volume Profile
- `Assets/TextMesh Pro/` — Bundled TMP resources (fonts, shaders, settings)

## Conventions
- One MonoBehaviour per file, file name matches class name
- `[Header]` and `[Tooltip]` attributes on all serialized fields (tooltips in Chinese)
- Private fields prefixed with underscore (`_rb`, `_suit`, `_gravity`)
- `[SerializeField] private` preferred over `public` for Inspector-exposed fields
- `.meta` files are committed — required by Unity for asset GUIDs
