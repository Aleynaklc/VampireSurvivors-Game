# Prototype Relic Values

Bu relic'ler ilk pre-run loadout denemesi icin baslangic degerleridir.

## Coiled Start

- `IsUnlockedByDefault = true`
- `UnlockCost = 0`
- `EffectType = ExtraStartingSegments`
- `PrimaryValue = 3`
- `SecondaryValue = 0`
- `TertiaryValue = 0`

Anlam:

- Her run'a ekstra `+3` govde segmentiyle baslar
- Ilk run'da guclenme hissini hemen verir

## Shard Charm

- `IsUnlockedByDefault = false`
- `UnlockCost = 10`
- `EffectType = MetaShardBonus`
- `PrimaryValue = 4`
- `SecondaryValue = 0`
- `TertiaryValue = 0`

Anlam:

- Her run sonunda ekstra `+4` Meta Shard kazandirir
- Erken meta economy hizini test etmek icin kullanilir

## Pull Scale

- `IsUnlockedByDefault = false`
- `UnlockCost = 14`
- `EffectType = StartingMagnet`
- `PrimaryValue = 3.2`
- `SecondaryValue = 0`
- `TertiaryValue = 0`

Anlam:

- XP pickup'lari baslangictan itibaren daha uzaktan ceker
- Pickup toplama yorgunlugunu azaltir

## Iron Egg

- `IsUnlockedByDefault = false`
- `UnlockCost = 20`
- `EffectType = OpeningBodyShield`
- `PrimaryValue = 0.05`
- `SecondaryValue = 3`
- `TertiaryValue = 0.25`

Anlam:

- Her `3` segmentte bir `%5` hasar azaltma verir
- Maksimum `%25` hasar azaltmaya kadar cikar

## Tuning Notlari

- Ilk hedef: oyuncu ilk 1-2 run sonra `Shard Charm` veya `Pull Scale` acmaya yaklasmali
- `Coiled Start` default relic oldugu icin snake hissini ilk saniyede guclendirir
- `Shard Charm` meta hizini artirdigi icin ileride rewarded ad/booster ekonomisiyle cakisabilir
- `Iron Egg` cok guvenli hissettirirse once `TertiaryValue`, sonra `PrimaryValue` dusur
