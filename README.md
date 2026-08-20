# Voxel World

Malý sandbox ve stylu Minecraftu postavený v Unity 6 (URP), zaměřený spíš na čistou architekturu než na hotovou hru.

## Technická poznámka

v1 — výchozí rozhodnutí (do 7 dnů)

Kdybych měl víc času, zvolil bych DOTS (ECS + Burst) — pro voxelový svět je to dlouhodobě výkonnější řešení. Vzhledem k týdennímu časovému limitu jsem ale zvolil klasický přístup s MonoBehaviour a ručně generovanými meshemi (MeshData, Chunk, ChunkRenderer), doplněný o multithreadové generování chunků (UniTask) a object pooling, abych alespoň částečně pokryl výkonnostní nároky bez plného přechodu na DOTS. DOTS verzi plánuji jako samostatný navazující projekt.

v2 — cílené výkonnostní optimalizace (7 dnů ++)

Po prvním kole profilování jsem přidal Unity Job System + Burst Compiler tam, kde to šlo bez zásahu do zbytku architektury (tj. bez plného přechodu na DOTS/ECS): generování voxelů chunku (GenerateVoxelsJob, IJobParallelFor přes sloupce chunku, Unity.Mathematics.noise místo Mathf.PerlinNoise) teď běží multithreadově a Burst-kompilované místo jako obyčejný C# na jednom vlákně z UniTask thread poolu. Napojení na zbytek async pipeline řeší malá JobHandleExtensions.ToUniTask() extension metoda, díky které lze na JobHandle čekat přes await bez blokování hlavního vlákna.

Dál jsem přidal dvě streamovací optimalizace v WorldService/PlayerChunkStreamer:

Distance-based collider LOD — MeshCollider (nejdražší část renderu chunku, PhysX cook) se generuje jen chunkům blízko hráče (WorldRules.ColliderDistanceInChunks), vzdálenější chunky jsou jen vizuální (ChunkRenderer.HasCollider / ClearCollider(), WorldService.SetChunkColliderAsync).

Throttled streaming s nearest-first frontou — PlayerChunkStreamer už neodpaluje všechny chunky ve view distance najednou, ale řadí je do fronty podle vzdálenosti od hráče a zpracovává jen omezený počet souběžně (WorldRules.MaxConcurrentChunkLoads, PlayerChunkStreamer.ProcessLoadQueue()), aby se zátěž rozprostřela přes víc snímků místo jednoho velkého výpadku při spawnu/teleportu.

Meshing (Chunk.GetChunkMeshData) zatím zůstává na UniTask.RunOnThreadPool — na Job System/Burst ho zatím nepřevádět, protože čte data napříč hranicemi sousedních chunků, což by vyžadovalo přípravu tzv. padded bufferů. Je to naplánovaný další krok.

## Tech stack
- Unity 6 / URP, nový Input System
- Vlastní DI framework (Injector, Controller, IService, IManager) + vlastní event systém (MyEventManager)
- UniTask pro async/multithreadové generování chunků
- Unity Job System + Burst Compiler + Unity.Mathematics pro generování voxelů

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
