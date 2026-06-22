# Unity Scene Setup

## MainPrototype Scene

Sahnede asagidaki minimum nesneleri olustur:

1. `Main Camera`
2. `Directional Light`
3. `GameRoot`
4. `Snake`
5. `EnemyRoot`
6. `UIRoot`

## GameRoot Components

`GameRoot` uzerine ekle:

- `GameBootstrap`
- `PrototypeRunController`
- `PrototypePowerController`
- `PrototypeMetaProgressionController`

Atamalar:

- `GameBootstrap.runConfig` -> `RunConfig` asset
- `GameBootstrap.powerLibrary` -> `PowerLibrary` asset
- `GameBootstrap.waveCatalog` -> `WaveCatalog` asset
- `PrototypeRunController.gameBootstrap` -> `GameRoot`
- `PrototypeRunController.snakeController` -> `Snake`
- `PrototypeRunController.powerController` -> `GameRoot`
- `PrototypeRunController.metaProgressionController` -> `GameRoot`
- `PrototypeRunController.chaserEnemyPrefab` -> `ChaserEnemy` prefab
- `PrototypeRunController.dasherEnemyPrefab` -> `DasherEnemy` prefab
- `PrototypeRunController.tankEnemyPrefab` -> `TankEnemy` prefab
- `PrototypeRunController.bossEnemyPrefab` -> `PrototypeBossEnemy` prefab
- `PrototypeRunController.xpPickupPrefab` -> opsiyonel `PrototypeXpPickup` prefab
- `PrototypeRunController.enemyRoot` -> `EnemyRoot`
- `PrototypePowerController.gameBootstrap` -> `GameRoot`
- `PrototypePowerController.snakeController` -> `Snake`
- `PrototypePowerController.runController` -> `GameRoot`
- `PrototypeMetaProgressionController.gameBootstrap` -> `GameRoot`
- `PrototypeMetaProgressionController.prototypeRunController` -> `GameRoot`
- `PrototypeMetaProgressionController.powerLibrary` -> `PowerLibrary` asset
- `PrototypeMetaProgressionController.relicLibrary` -> `PrototypeRelicLibrary` asset

## Snake Object

`Snake` uzerine ekle:

- `SnakeController`

Notlar:

- `SnakeController.gameplayCamera` -> `Main Camera`
- `bodyRoot` bos kalabilir
- `bodySegmentPrefab` yoksa runtime fallback sphere uretilir

## UIRoot

`UIRoot` uzerine ekle:

- `PrototypeDraftOverlay`
- `PrototypeHudOverlay`
- `PrototypeBossOverlay`
- `PrototypeRunEndOverlay`
- `PrototypeFeedbackController`
- `PrototypeAudioController`
- `PrototypeTelemetryController`

Atamalar:

- `PrototypeDraftOverlay.prototypeRunController` -> `GameRoot`
- `PrototypeHudOverlay.prototypeRunController` -> `GameRoot`
- `PrototypeHudOverlay.snakeController` -> `Snake`
- `PrototypeHudOverlay.prototypeMetaProgressionController` -> `GameRoot`
- `PrototypeBossOverlay.prototypeRunController` -> `GameRoot`
- `PrototypeRunEndOverlay.prototypeRunController` -> `GameRoot`
- `PrototypeRunEndOverlay.prototypeTelemetryController` -> `UIRoot`
- `PrototypeRunEndOverlay.prototypeMetaProgressionController` -> `GameRoot`
- `PrototypeFeedbackController.prototypeRunController` -> `GameRoot`
- `PrototypeFeedbackController.snakeController` -> `Snake`
- `PrototypeFeedbackController.gameplayCamera` -> `Main Camera`
- `PrototypeAudioController.gameBootstrap` -> `GameRoot`
- `PrototypeAudioController.prototypeRunController` -> `GameRoot`
- `PrototypeAudioController.snakeController` -> `Snake`
- `PrototypeAudioController.sfxSource` -> opsiyonel bos `AudioSource`
- `PrototypeTelemetryController.gameBootstrap` -> `GameRoot`
- `PrototypeTelemetryController.prototypeRunController` -> `GameRoot`
- `PrototypeTelemetryController.snakeController` -> `Snake`
- `PrototypeTelemetryController.prototypeMetaProgressionController` -> `GameRoot`

Not:

- `PrototypeAudioController` klip atanmasa bile calisir; tum alanlar null-safe
- `PrototypeTelemetryController` run sonunda local JSON yazar
- Varsayilan path: `Application.persistentDataPath/prototype_run_telemetry.json`
- `PrototypeMetaProgressionController` kalici shard ve unlock state yazar
- Varsayilan path: `Application.persistentDataPath/prototype_meta_progression.json`
- Relic ayarlari icin [prototype_relic_values.md](/Users/aleynakilic/Documents/Playground/snake-roguelite/docs/prototype_relic_values.md:1) dosyasindaki baslangic degerlerini kullan
- Ilk test icin su basit clip seti yeterli:
  - `enemyHitClips` -> 2-3 kisa tick/click
  - `enemyKillClips` -> 2 kisa pop/crunch
  - `snakeHitClips` -> 2 yumusak hurt thud
  - `growthClips` -> 1 kisa pickup blip
  - `waveStartClip` -> hafif whoosh
  - `draftOpenClip` -> yumusak UI rise
  - `powerPickClip` -> select click
  - `levelUpClip` -> parlak ding
  - `bossStartClip` -> dusuk boom
  - `runClearClip` -> kisa success sting
  - `runFailClip` -> kisa fail sting

## ChaserEnemy Prefab

Basit bir capsule veya cube prefab olustur.

Uzerine ekle:

- `ChaserEnemy`

Collider zorunlu degil; temas hesaplari script tarafinda yapiliyor.

## DasherEnemy Prefab

Basit bir capsule veya cube prefab olustur.

Uzerine ekle:

- `DasherEnemy`

## TankEnemy Prefab

Basit bir capsule veya cube prefab olustur.

Uzerine ekle:

- `TankEnemy`

## PrototypeBossEnemy Prefab

Basit bir capsule, cube veya daha buyuk bir sphere prefab olustur.

Uzerine ekle:

- `PrototypeBossEnemy`

## Data Assets

Project penceresinde su asset'leri olustur:

1. `Create > Snake Roguelite > Run Config`
2. `Create > Snake Roguelite > Power Library`
3. `Create > Snake Roguelite > Prototype Relic Library`
4. `Create > Snake Roguelite > Wave Definition`
5. `Create > Snake Roguelite > Wave Catalog`

`RunConfig` icin ilk test degerleri:

- `MaxWaveCount = 3`
- `DraftChoiceCount = 3`
- `StartingHealth = 5`
- `StartingXpToLevel = 4`
- `XpToLevelGrowthPerLevel = 2`

Power asset'lerinde prototype icin su eslestirmeyi kullan:

- `Tail Spike` -> `EffectType = TailSpike`
- `Body Shield` -> `EffectType = BodyShield`
- `Frenzy Coil` -> `EffectType = FrenzyCoil`
- `Hunger Scale` -> `EffectType = HungerScale`
- `Spark Molt` -> `EffectType = SparkMolt`
- `Magnet Tail` -> `EffectType = MagnetTail`
- `Overload` -> `EffectType = Overload`

## Ilk Wave Ayari

Ilk `WaveDefinition` icin:

- `Index = 1`
- `DurationSeconds = 90`
- `ChaserCount = 6`
- `DasherCount = 1`
- `TankCount = 1`

Sonra bu `WaveDefinition` asset'ini `WaveCatalog.Waves` listesine ekle.

Ilk testte:

- `PrototypeRunController.autoAdvanceWaves = true`
- `PrototypeRunController.autoResolveDrafts = false`
- `PrototypeRunController.immediateSpawnCount = 2`

## Ilk Test

Testin amaci:

- Yilan rahat donuyor mu?
- Govdeye carpan dusman eriyor mu?
- Kafa ile carpma riskli ama guclu hissettiriyor mu?
- Kuyruk tehlike bolgesi okunuyor mu?
- Dalen dusman yilanin buyudugunu hissettiriyor mu?
- XP kill odulu pickup olarak daha tatminli hissettiriyor mu?
- Boss health bar karar okumayi kolaylastiriyor mu?
- Wave ilk anda bos hissettirmeden baslayip sonra ritmik sekilde doluyor mu?
- Draft secenekleri ayni build eksenine dogru hafif akiyor mu?
- Sesler run ritmini guclendiriyor mu, yoksa spam gibi mi geliyor?
