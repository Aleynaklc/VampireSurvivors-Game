# Prototype Power Values

Bu degerler ilk playable prototype icin onerilen baslangic noktalaridir.

## Tail Spike

- `Rarity = Rare`
- `PrimaryTag = BodyTech`
- `IsRunDefining = true`
- `IsUnlockedByDefault = true`
- `UnlockCost = 0`
- `EffectType = TailSpike`
- `PrimaryValue = 8`
- `SecondaryValue = 5`
- `TertiaryValue = 0`

Anlam:

- Kuyruga temas eden dusman ek `8` burst damage yer
- `5` gucluk knockback alir

## Body Shield

- `Rarity = Common`
- `PrimaryTag = BodyTech`
- `IsRunDefining = false`
- `IsUnlockedByDefault = true`
- `UnlockCost = 0`
- `EffectType = BodyShield`
- `PrimaryValue = 0.06`
- `SecondaryValue = 3`
- `TertiaryValue = 0.30`

Anlam:

- Her `3` segmentte bir `%6` hasar azaltma
- Maksimum `%30` azaltma

## Frenzy Coil

- `Rarity = Rare`
- `PrimaryTag = Frenzy`
- `IsRunDefining = true`
- `IsUnlockedByDefault = false`
- `UnlockCost = 18`
- `EffectType = FrenzyCoil`
- `PrimaryValue = 0.3`
- `SecondaryValue = 1.45`
- `TertiaryValue = 1.8`

Anlam:

- Can `%30` altina dustugunde
- Hareket hizi `1.45x`
- Cikis hasari `1.8x`

## Hunger Scale

- `Rarity = Common`
- `PrimaryTag = BodyTech`
- `IsRunDefining = false`
- `IsUnlockedByDefault = true`
- `UnlockCost = 0`
- `EffectType = HungerScale`
- `PrimaryValue = 1`
- `SecondaryValue = 0`
- `TertiaryValue = 0`

Anlam:

- Her olen dusmanda ekstra `+1` govde segmenti

## Spark Molt

- `Rarity = Rare`
- `PrimaryTag = Electric`
- `IsRunDefining = true`
- `IsUnlockedByDefault = true`
- `UnlockCost = 0`
- `EffectType = SparkMolt`
- `PrimaryValue = 3.5`
- `SecondaryValue = 10`
- `TertiaryValue = 0`

Anlam:

- Secildiginde ve her level up'ta
- Yilan kafasi etrafinda `3.5` yaricapli alan vurur
- Alandaki dusmanlar `10` hasar alir

## Magnet Tail

- `Rarity = Common`
- `PrimaryTag = Utility`
- `IsRunDefining = false`
- `IsUnlockedByDefault = false`
- `UnlockCost = 12`
- `EffectType = MagnetTail`
- `PrimaryValue = 3.2`
- `SecondaryValue = 0`
- `TertiaryValue = 0`

Anlam:

- XP pickup'lari `3.2` birim yaricaptan kafaya cekmeye baslar
- Toplama akisi daha duzgun olur

## Overload

- `Rarity = Rare`
- `PrimaryTag = Electric`
- `IsRunDefining = true`
- `IsUnlockedByDefault = false`
- `UnlockCost = 24`
- `EffectType = Overload`
- `PrimaryValue = 4`
- `SecondaryValue = 3.4`
- `TertiaryValue = 14`

Anlam:

- Her `4` dusman oldurmede bir
- Yilan kafasi etrafinda `3.4` yaricapli alan patlamasi olur
- Alandaki dusmanlar `14` hasar alir

## Tuning Notlari

- Draft roller artik `Rarity`, `PrimaryTag` ve `IsRunDefining` alanlarini aktif kullanir
- Draft havuzu artik sadece unlock edilmis power'lari kullanir
- Ilk ekonomik hedef: oyuncu `1-2` basarisiz run sonra ilk unlock'a yakin hissetsin
- Ilk 5-6 power asset'inde en az `2` farkli tag ve en az `2` run-defining power bulunsun
- Ilk pick havuzunda tek bir build diline hafif egilim iyidir; tamamen ayni kartlar kotudur
- `Magnet Tail` cok guclu gelirse once yari capi kis
- `Overload` az hissediliyorsa hasari degil `PrimaryValue` yani kill esigini dusur
- Ilk testte `Tail Spike` cok guclu gelirse once `SecondaryValue` dusur
- `Body Shield` oyunu fazla guvenli yaparsa `TertiaryValue` degerini asagi cek
- `Frenzy Coil` hissedilmiyorsa once `TertiaryValue` artir, sonra `SecondaryValue`
- `Spark Molt` ekrani cok bosaltiyorsa yari capi degil hasari dusur
