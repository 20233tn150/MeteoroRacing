# Meteoro Racing — Neon Speedway

Arcade 2D cenital inspirado en *Road Fighter* y en la estética neón de *Speed Racer (2008)*.
Proyecto Unity 6.6 (URP 2D) con arte 100 % procedural: no necesita sprites, fuentes ni
materiales externos para arrancar.

## Cómo jugar

Abre `Assets/_Project/Scenes/MeteoroRace.unity` y pulsa **Play**.

| Acción | Tecla |
|---|---|
| Cambiar de carril | `A` / `D` o `←` / `→` |
| Auto-Jacks (salto) | `Espacio` |
| Pausa | `Esc` o `P` |
| Reiniciar tras Game Over | `R`, `Enter` o `Espacio` |

**Objetivo:** sobrevivir el máximo tiempo posible. La energía baja sola y más rápido a
mayor velocidad. Recoge cápsulas cian para recargar. Si la energía llega a 0 o el Mach 5
cruza la barrera lateral durante un derrape, la carrera termina. Los choques contra
tráfico, explosivos y muros también la terminan.

**Auto-Jacks:** durante 0,8 s el sprite se estira y el coche queda en el aire. Mientras
está en el aire ignora aceite, coches y explosivos, pero puede seguir recogiendo energía.

**Aceite:** provoca un derrape de 0,6 s. Se cancela pulsando la dirección **contraria**
a la deriva.

**Zonas de peligro:** 1 o 2 carriles se iluminan en rojo durante ~1,15 s; después cae un
explosivo por cada carril marcado. Cambia de carril o salta.

## Estructura de carpetas

```
Assets/_Project/
├── Editor/
│   ├── MeteoroSceneBuilder.cs      Genera la escena jugable completa desde el menú.
│   └── MeteoroSmokeTest.cs         Test automático de Play Mode (CI).
├── Scenes/
│   └── MeteoroRace.unity           Escena principal (ya incluida en Build Settings).
├── Scripts/
│   ├── Art/
│   │   ├── NeonArt.Primitives.cs   Texturas/sprites procedurales.
│   │   ├── NeonArt.Visuals.cs      Coches, pista, aceite, bombas, haces de aviso…
│   │   ├── NeonFx.cs               Explosiones y partículas ligeras.
│   │   └── FxParticle.cs
│   ├── Core/
│   │   ├── RaceLayout.cs           Constantes de carriles/geometría (fuente única).
│   │   ├── GameManager.cs          Energía, distancia, puntos, pausa, Game Over.
│   │   ├── CameraShake.cs          Shake + rumble por velocidad.
│   │   ├── RacePool.cs             Pool de objetos.
│   │   └── PooledTag.cs
│   ├── Player/
│   │   └── Mach5Controller.cs      Carriles, Auto-Jacks, derrape y colisiones.
│   ├── World/
│   │   └── TrackScroller.cs        Scroll infinito por capas + aceleración.
│   ├── Spawning/
│   │   └── HazardAndEnemySpawner.cs Generación procedural y reglas de justicia.
│   ├── Enemies/
│   │   ├── TrackObject.cs          Base de todo lo que se desplaza con la pista.
│   │   ├── TrafficCar.cs           Tráfico normal.
│   │   ├── AggressiveCarAI.cs      Car-Fu telegrafiado.
│   │   └── IHazard.cs              Interfaz de peligros.
│   ├── Hazards/
│   │   ├── OilSlick.cs             Derrape.
│   │   ├── DangerZone.cs           Aviso rojo en 1-2 carriles.
│   │   └── Explosive.cs            Bomba que cae y explota.
│   ├── Pickups/
│   │   └── FuelPickup.cs           Recarga de energía.
│   └── UI/
│       └── HUDController.cs        HUD uGUI construido por código.
└── README.md
```

## Jerarquía de la escena

La escena guarda solo los sistemas; la pista, el coche y el HUD generan su arte en
`Awake`/`Start`. El resultado en Play es:

```
MeteoroRace
├── Main Camera            Camera (ortográfica, size 6), AudioListener, CameraShake
├── Global Light 2D        Light2D (global, para materiales 2D lit)
├── GameManager            GameManager (singleton, estado y puntuación)
├── Track                  TrackScroller
│   └── NeonScrollLayers   (generado)
│       ├── DeepSpace      Tile_0 / Tile_1
│       ├── NeonGrid       Tile_0 / Tile_1
│       ├── Road           Tile_0 / Tile_1
│       ├── LaneDashes     Tile_0 / Tile_1
│       ├── RailL          Tile_0 / Tile_1
│       ├── RailR          Tile_0 / Tile_1
│       └── SpeedLines     Tile_0 / Tile_1
├── Mach5                  Rigidbody2D kinematic, BoxCollider2D trigger,
│   │                      Mach5Controller
│   └── Visual (runtime)   SpriteRenderer + sombra + brillo de motor + efectos
├── Spawner                HazardAndEnemySpawner
└── HUD                    HUDController
    └── Canvas (runtime)   energía, distancia, velocidad, puntos, avisos y Game Over
```

Los peligros se crean bajo demanda (sin prefabs obligatorios) con `RacePool` y se
destruyen/reciclan al pasar por debajo de la cámara.

## Scripts principales

### `Mach5Controller.cs`
- Movimiento por carril con `SmoothDamp` (`laneChangeSmoothTime = 0.065`) y buffer de
  input de 0,18 s para que los toques rápidos no se pierdan.
- `TargetLane` 0…4; el coche se mantiene en `RaceLayout.PlayerY` (la pista se mueve).
- Auto-Jacks: 0,8 s, escala X 1,18 / Y 1,45, desplazamiento vertical y sombra que se
  desvanece. `IsAirborne` hace que `HandleContact` ignore peligros.
- Aceite: `EnterSkid(direction)` arrastra lateralmente 0,6 s; pulsar la dirección
  contraria llama a `CancelSkid()`. Si `|x| >= RaceLayout.SideCrashX` durante el
  derrape, llama a `Crash("Choque contra la barrera neón")`.
- Recogida de energía y colisiones centralizadas mediante `IHazard`.

### `TrackScroller.cs`
- Capas de scroll con `parallax` (0,12 fondo, 0,28 rejilla, 1 pista, 1,15 raíles,
  1,65 líneas de velocidad) y reciclado de dos tiles por capa.
- La velocidad sube de 9 a 29 u/s con aceleración progresiva (`accelerationRamp`).
- `SpeedNormalized` alimenta dificultad, HUD, rumble y longitud de las líneas de velocidad.

### `HazardAndEnemySpawner.cs`
- Oleadas con pesos: tráfico, Car-Fu, aceite y zonas de peligro; el aceite y las zonas
  se desbloquean a los 5 s y 12 s.
- `PickFreeLanes(requested, leaveGap:true)` descarta carriles ocupados y reserva los
  carriles de las zonas de peligro. **Nunca elige los 5 carriles**: con `leaveGap`
  siempre deja al menos uno libre.
- `LaneBlocked` comprueba `active`, `laneReservedUntil` y la posición Y para no apilar
  obstáculos en el mismo carril cerca de la zona de aparición.
- Combustible: elige un carril libre y aparece cada 4,5–7 s (más rápido a alta velocidad).
- Dificultad: a más velocidad, más tráfico simultáneo (1→4 coches), más Car-Fu y dos
  carriles de artillería con más probabilidad.

### `AggressiveCarAI.cs`
- Estados `Cruising → Telegraph → Lunge`.
- En `Telegraph` parpadea en rojo (`blinkInterval = 0,07 s`), se escala sutilmente y
  fija el carril objetivo (el que ocupa el jugador al empezar el aviso).
- En `Lunge` se desplaza lateralmente a 12 u/s con inclinación; después vuelve a crucero.
- Se puede esquivar cambiando de carril durante el aviso o saltando con Auto-Jacks.

### `GameManager.cs`
- Singleton con eventos `FuelChanged`, `DistanceChanged`, `ScoreChanged`, `StateChanged`
  y `GameOverTriggered`; el HUD se suscribe, sin acoplar la UI a la lógica.
- Energía: drena `4,2 u/s` con un extra del 45 % a velocidad máxima.
- Distancia en metros (`metersPerUnit = 2,5`), puntuación por metros y bonus por recogida.
- `TriggerGameOver` congela `Time.timeScale`, lanza shake y avisa al HUD.
- Pausa/reanudar con `Esc`/`P`; reinicio por recarga de escena.

### `HUDController.cs`
- Construye un Canvas uGUI completo por código (sin prefabs): barra de energía, textos de
  distancia/velocidad/puntos, estado del Auto-Jacks, aviso de energía baja, intro,
  pausa y panel de Game Over.
- Se actualiza por eventos de `GameManager` y usa `Time.unscaledTime` para los parpadeos.

## Regenerar la escena

Menú **Meteoro Racing → Construir escena jugable**.

También en batch:

```powershell
Unity.exe -batchmode -projectPath . `
  -executeMethod MeteoroRacing.EditorTools.MeteoroSceneBuilder.BuildFromBatch `
  -logFile Logs/build.log
```

## Smoke test

Menú **Meteoro Racing → Ejecutar smoke test (Play Mode)** o:

```powershell
Unity.exe -batchmode -projectPath . `
  -executeMethod MeteoroRacing.EditorTools.MeteoroSmokeTest.Run `
  -logFile Logs/smoke.log
```

Comprueba arranque de la escena, cámara, HUD, scroll, generación de peligros, recogida
de energía y colisión con tráfico. Resultado esperado: `[SmokeTest] PASS`.

## Ajustes rápidos

| Quiero… | Dónde |
|---|---|
| Más/menos carriles, ancho, altura del jugador | `Scripts/Core/RaceLayout.cs` |
| Curva de velocidad y parallax | `TrackScroller` en la escena |
| Salto y derrape | `Mach5Controller` en el inspector |
| Frecuencia y pesos de peligros | `HazardAndEnemySpawner` en la escena |
| Combustible y puntuación | `GameManager` en la escena |
| Colores neón y geometría del arte | `Scripts/Art/NeonArt.*.cs` |
| Dificultad del Car-Fu | `AggressiveCarAI` en el inspector |

## Sustituir el arte procedural

`NeonArt` crea sprites en runtime. Para usar arte propio, asigna tus `Sprite` a los
`SpriteRenderer` (o crea prefabs y asígnalos a los campos `trafficPrefab`,
`aggressivePrefab`, `oilPrefab`, `fuelPrefab`, `explosivePrefab` y `dangerZonePrefab`
del spawner). La lógica no depende de la textura generada: solo de colliders, capas y
los componentes de peligro.
