# Prototype Enemy Values

Bu degerler ilk playable prototype icin referans baslangic noktalaridir.

## ChaserEnemy

- `Max Health = 12`
- `Contact Radius = 0.45`
- `Contact Damage = 1`
- `Growth Reward Segments = 1`
- `Experience Reward = 1`
- `Move Speed = 3.6`

Kullanim:

- Temel baski dusmani
- Sayica fazla gelir
- Oyuncuya govde eritme hissini ogretir

## DasherEnemy

- `Max Health = 9`
- `Contact Radius = 0.4`
- `Contact Damage = 1`
- `Growth Reward Segments = 1`
- `Experience Reward = 2`
- `Chase Speed = 4.2`
- `Dash Speed = 12`
- `Windup Seconds = 0.4`
- `Dash Duration Seconds = 0.35`
- `Recover Seconds = 0.45`
- `Dash Cooldown Seconds = 2.4`

Kullanim:

- Ani tehdit uretir
- Oyuncuyu kafa riski yerine pozisyon okumaya zorlar

## TankEnemy

- `Max Health = 28`
- `Contact Radius = 0.7`
- `Contact Damage = 1`
- `Growth Reward Segments = 2`
- `Experience Reward = 3`
- `Move Speed = 2`
- `Head Damage Taken Multiplier = 0.7`
- `Body Damage Taken Multiplier = 0.45`

Kullanim:

- Alan kaplar
- Kuyruk baskisi yaratir
- Guclu build farkini daha net hissettirir

## PrototypeBossEnemy

- `Max Health = 90`
- `Chase Speed = 2.8`
- `Charge Speed = 10`
- `Charge Duration Seconds = 0.9`
- `Recover Duration Seconds = 1.1`
- `Charge Cooldown Seconds = 2.5`
- `Contact Radius = 0.9`
- `Contact Damage = 1`
- `Body Damage Taken Multiplier = 0.55`
- `Head Damage Taken Multiplier = 0.8`
- `Pulse Radius = 3.5`
- `Pulse Cooldown Seconds = 4`
- `Pulse Damage = 1`

Kullanim:

- Kuyrugu kovalayip sonra kafaya charge atar
- Build’in hem DPS hem pozisyon kalitesini test eder

## Ilk Wave Dagilimi

### Wave 1

- `Chaser = 6`
- `Dasher = 1`
- `Tank = 1`

### Wave 2

- `Chaser = 8`
- `Dasher = 2`
- `Tank = 1`

### Wave 3

- `Chaser = 10`
- `Dasher = 2`
- `Tank = 2`

## Tuning Kurallari

- Oyuncu surekli arkadan yeniyorsa once `TankEnemy` sayisini dusur
- Oyun fazla kaotikse once `DasherEnemy` sayisini dusur, `ChaserEnemy`yi degil
- Boss cok yavas hissediliyorsa `chargeCooldown` degil `chaseSpeed` artir
- Body build cok zayif geliyorsa `TankEnemy.BodyDamageTakenMultiplier` biraz artir
