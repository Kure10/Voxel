# Voxel World

Malý sandbox ve stylu Minecraftu postavený v Unity 6 (URP), zaměřený spíš na čistou architekturu než na hotovou hru.

## Technická poznámka

Kdybych měl víc času, zvolil bych DOTS (ECS + Burst) — pro voxelový svět je to dlouhodobě výkonnější řešení. Vzhledem k týdennímu časovému limitu jsem ale zvolil klasický přístup s `MonoBehaviour` a ručně generovanými meshemi (`MeshData`, `Chunk`, `ChunkRenderer`), doplněný o multithreadové generování chunků (UniTask) a object pooling, abych alespoň částečně pokryl výkonnostní nároky bez plného přechodu na DOTS. DOTS verzi plánuji jako samostatný navazující projekt.

## Tech stack
- Unity 6 / URP, nový Input System
- Vlastní DI framework (`Injector`, `Controller`, `IService`, `IManager`) + vlastní event systém (`MyEventManager`)
- [UniTask](https://github.com/Cysharp/UniTask) pro async/multithreadové generování chunků

## Funkce
- Procedurální terén pomocí octave Perlin noise (`TerrainGenerator` + `NoiseSettingsSO`)
- Výškové pásma bloků (Gray → Green → White → Ice), nastavitelné přes `WorldRules`
- Column chunk svět, streamovaný podle vzdálenosti od hráče (view distance), s object poolingem a generováním/meshováním na pozadí
- Těžba (poškození po zásazích, počet zásahů podle typu bloku) a stavění, s limitem hloubky/výšky
- Binární save/load (jeden slot), zachovává hráčovy úpravy terénu i seed
- Základní UI: loading/save obrazovka, hotbar pro výběr bloku

## Architektura
- `WorldRules` (ScriptableObject) — jediný zdroj konfigurace: velikost/výška chunku, terén, vodní/výšková pásma, limity těžby/stavění
- `WorldService` — vlastní veškerá data chunků a dotazy na svět; jediná věc, na které závisí ostatní systémy
- `World` — orchestruje generování (seed, spouští načítání chunků)
- Namespace `Character` — ovládání hráče, akce (těžba/stavění), vše postavené na `Controller` a událostech
