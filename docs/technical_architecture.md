# Technical Architecture

## Teknoloji Karari

- Engine: `Unity LTS`
- Dil: `C#`
- Render: `URP`
- Mimari: `Hybrid MVC + lightweight gameplay services`

MVP asamasinda DOTS zorunlu degil. Once hissi dogrulamak, sonra performans darbogazlarini ayiklamak daha dogru.

## Katmanlar

### Domain

Pure C# veri ve kurallar:

- `RunState`
- `SnakeState`
- `EnemyState`
- `WaveState`
- `HealthModel`
- `PowerDefinition`
- `DraftRoller`
- `RunRewardModel`

### Application

Oyun akisini koordine eden servisler:

- `RunFlowService`
- `SnakeCombatService`
- `GrowthService`
- `WaveDirector`
- `PowerDraftService`
- `RunResultService`

### Presentation

Unity `MonoBehaviour` tabakasi:

- `SnakeView`
- `EnemyView`
- `PickupView`
- `HUDView`
- `DraftPanelView`
- `BossView`
- `ScreenFxController`

## Ilk Unity Klasor Yapisi

```text
Assets/_Game/
  Core/
  Gameplay/
    Snake/
    Combat/
    Enemies/
    Waves/
    Powers/
  UI/
  Audio/
  VFX/
  Data/
  Prefabs/
  Scenes/
  Tests/
```

## Sistem Notlari

### Snake Movement

- Tek parmak drag input
- Snake head hedef noktaya yumusak donus yapar
- Govde segmentleri fizik yerine takip verisi ile hareket eder
- Segment gorunumu ile hasar hesaplama segmentleri ayrilabilir

### Collision Strategy

- Tam fizik zinciri yerine orneklenmis segment noktalarindan kontrol
- Kafa icin ayrik hitbox
- Kuyruk tehlike alani icin ayri durum etiketi

### Enemy Strategy

- Ilk MVP icin 3 archetype yeterli:
  - `Chaser`
  - `Dasher`
  - `Tank`

### Power System

- `ScriptableObject` tabanli data
- Runtime etkiler `modifier + event hook` olarak uygulanir
- Gucler tag ve nadirlik bilgisi tasir

### Boss

- Tek patternli ama pozisyon test eden yapi
- Buyuk alan tarama + tail punishment mantigi

## Performans Oncelikleri

1. Govde guncelleme maliyeti
2. Dusman sayisi arttiginda temasin maliyeti
3. VFX spam anlarinda kare hizi
4. Garbage allocation sifira yakin olmali

## Teknik Spike Sirasi

1. Snake movement feel
2. Sampled body collision
3. 30+ dusman ile performans
4. Draft secip etkisini aninda gormek
5. Boss arena testi

## Kod Ilkeleri

- Domain kurallari `MonoBehaviour` icine gomulmez
- Power etkileri hardcode zinciri olarak yazilmaz
- Data-driven tanim tercih edilir
- Ilk gunlerden itibaren basit telemetry hook'lari eklenir
