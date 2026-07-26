# Voxel World

A small Minecraft-style voxel sandbox built in Unity 6 (URP), focused on clean architecture over polish.

## Tech
- Unity 6 / URP, new Input System
- Custom DI framework (`Injector`, `Controller`, `IService`, `IManager`) + custom event system (`MyEventManager`)
- [UniTask](https://github.com/Cysharp/UniTask) for async/multithreaded chunk generation

## Features
- Procedural terrain via octave Perlin noise (`TerrainGenerator` + `NoiseSettingsSO`)
- Height-based block bands (Gray → Green → White → Ice), tunable via `WorldRules`
- Column chunk world, streamed in/out around the player (view-distance based), with object pooling and background-thread generation/meshing
- Mining (hit-based damage, per-block-type hit counts) and building, with dig/build height limits
- Binary save/load (single slot) preserving player-modified terrain and seed
- Basic UI: loading/save screen, hotbar block selector

## Architecture
- `WorldRules` (ScriptableObject) — single source of config: chunk size/height, terrain, water/height bands, dig/build limits
- `WorldService` — owns all chunk data and world queries; the only thing other systems depend on
- `World` — orchestrates generation (seed, kicks off chunk loading)
- `Character` namespace — player controller, actions (mine/build), all `Controller`-based and event-driven
