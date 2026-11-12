# Meteoro Racing — arcade top-down neón (Unity 6 / URP 2D)

Prototipo jugable inspirado en *Road Fighter* con estética *Speed Racer (2008)*.
Pista de 5 carriles, scroll infinito, energía, Auto-Jacks, tráfico, coches Car-Fu,
aceite y zonas de peligro telegrafiadas.

> Todo el arte se genera por código al arrancar. No necesitas sprites, fuentes ni
> prefabs para probar el juego. Después puedes sustituirlo por arte propio.

---

## 1. Puesta en marcha (2 minutos)

1. Abre el proyecto con **Unity 6000.6.0f1** (URP 2D).
2. Menú **Meteoro Racing → Construir escena jugable**.
   - Crea `Assets/_Project/Scenes/MeteoroRace.unity`.
   - La añade automáticamente a *Build Settings*.
3. Pulsa **Play**.

También puedes abrir la escena ya generada:

**Meteoro Racing → Abrir escena jugable**

### Controles

| Acción | Tecla |
|---|---|
| Cambiar de carril | `A` / `D` o `←` / `→` |
| Auto-Jacks (salto) | `Espacio` |
| Cancelar derrape | Dirección **contraria** a la deriva |
| Pausa | `Esc` o `P` |
| Reiniciar al perder | `R` o `Enter` |
| Gamepad | D-pad y botón Sur |

---

## 2. Estructura de carpetas

```text
Assets/
└── _Project/
    ├── Editor/
    │   ├── MeteoroSceneBuilder.cs      # Genera la escena completa (menú)
    │   └── MeteoroSmokeTest.cs         # Prueba automática de Play Mode (CI)
    ├── Prefabs/                        # (vacía) para prefabs de arte propio
    ├── Scenes/
    │   └── MeteoroRace.unity           # Escena generada
    └── Scripts/
        ├── Art/
        │   ├── NeonArt.Primitives.cs   # Texturas/sprites procedurales
        │   ├── NeonArt.Visuals.cs      # Coches, bidones, aceite, bombas, avisos
        │   ├── NeonFx.cs               # Explosiones de chispas
        │   └── FxParticle.cs           # Partícula individual
        ├── Core/
        │   ├── GameManager.cs          # Energía, distancia, puntos, estados
        │   ├── RaceLayout.cs           # Geometría de 5 carriles
        │   ├── RacePool.cs             # Pool por prefab
        │   ├── PooledTag.cs            # Marca de instancia pooled
        │   └── CameraShake.cs          # Trauma + vibración por velocidad
        ├── Enemies/
        │   ├── TrackObject.cs          # Base: scroll relativo y despawn
        │   ├── IHazard.cs              # Interfaz de peligro
        │   ├── TrafficCar.cs           # Tráfico normal
        │   └── AggressiveCarAI.cs      # Car-Fu telegrafiado
        ├── Hazards/
        │   ├── OilSlick.cs             # Derrape cancelable
        │   ├── Explosive.cs            # Bomba que cae
        │   └── DangerZone.cs           # Aviso vertical + caída de bombas
        ├── Pickups/
        │   └── FuelPickup.cs           # Bidón de energía
        ├── Player/
        │   └── Mach5Controller.cs      # Carriles, salto, derrape, colisiones
        ├── Spawning/
        │   └── HazardAndEnemySpawner.cs# Oleadas justas y procedurales
        ├── UI/
        │   └── HUDController.cs        # HUD uGUI creado por código
        └── World/
            └── TrackScroller.cs        # Scroll infinito + aceleración
```

---

## 3. Jerarquía de la escena

```text
MeteoroRace
├── Main Camera                  Camera (ortográfica, size 6), AudioListener, CameraShake
├── Global Light 2D              Light2D (Global) para URP 2D
├── GameManager                  GameManager
├── Track                        TrackScroller
│   └── NeonScrollLayers         (se crea en runtime)
│       ├── DeepSpace            parallax 0.12
│       ├── NeonGrid             parallax 0.28
│       ├── Road                 parallax 1.00
│       ├── LaneDashes           parallax 1.00
│       ├── RailL / RailR        parallax 1.15
│       └── SpeedLines           parallax 1.65
├── Mach5                        Rigidbody2D (Kinematic), BoxCollider2D (trigger), Mach5Controller
│   ├── Shadow                   (runtime)
│   └── Visual                   (runtime, se escala en el salto)
├── Spawner                      HazardAndEnemySpawner
└── HUD                          HUDController
    └── HUD Canvas               (runtime: uGUI)
```

### Jerarquía de los objetos generados

```text
TrafficCar / AggressiveCarAI / OilSlick / FuelPickup / Explosive
├── Visual
│   ├── Underglow
│   ├── WheelFL / WheelFR / WheelRL / WheelRR
│   ├── Body
│   ├── Cockpit, Nose, Spoiler, luces...
│   └── (Car-Fu) WarningIcon
└── Shadow
```

`DangerZone` no usa prefab: construye haces rojos, franjas de peligro e iconos de
advertencia en 1–2 carriles y, al terminar, llama a `DropExplosive` del spawner.

---

## 4. Scripts principales

### `Mach5Controller.cs`

- Movimiento entre 5 carriles con `SmoothDamp` (transición rápida) y buffer de input.
- Inclinación visual según velocidad lateral.
- **Auto-Jacks**: 0.8 s, escala el sprite (1.18 × 1.45), eleva el visual y agranda la
  sombra. Mientras está en el aire ignora tráfico, aceite y explosivos.
- **Derrape**: 0.6 s de deriva lateral; la dirección contraria lo cancela. Si la
  deriva alcanza la barrera (`RaceLayout.SideCrashX`) se pierde.
- Recogida de energía, choque con tráfico/Car-Fu/bombas y muerte con giro + FX.

### `TrackScroller.cs`

- `CurrentSpeed` sube de `baseSpeed` a `maxSpeed` con aceleración que crece con el tiempo.
- Capas de scroll recicladas verticalmente; si no hay capas, construye todo el fondo
  neón (espacio, rejilla, asfalto, líneas de carril, raíles y líneas de velocidad).
- Publica `SpeedNormalized` para spawner, HUD y vibración de cámara.

### `HazardAndEnemySpawner.cs`

- Oleadas con pesos que evolucionan con la dificultad.
- **Nunca bloquea los 5 carriles**: `PickFreeLanes(..., leaveGap: true)` reserva
  siempre al menos un carril libre y comprueba la ocupación en la banda de aparición.
- Aceite y zonas de peligro se desbloquean a los 5 s y 12 s.
- Los carriles avisados por `DangerZone` se reservan 2.4 s para no apilar peligros.
- Bidones de energía en carriles libres; no bloquean carril.
- Si los campos de prefab están vacíos, crea los objetos por código.
- Los prefabs asignados se reciclan con `RacePool`.

### `AggressiveCarAI.cs`

1. **Crucero** con temporizador aleatorio.
2. **Telegrafía** (0.8 s): cuerpo parpadea en rojo, glow e icono `!`.
3. **Dash** lateral con `SmoothStep` hacia el carril objetivo (fijado al empezar el aviso).
4. Si ya estaba en el carril del jugador, **embiste** acelerando hacia abajo.
5. **Recuperación** y nuevo intento.

### `GameManager.cs`

- Energía con drenaje constante + factor por velocidad.
- Distancia (`metersPerUnit = 2.5`, coherente con los km/h del HUD), puntuación,
  pausa, game over y reinicio de escena.
- Eventos: `FuelChanged`, `DistanceChanged`, `ScoreChanged`, `StateChanged`,
  `GameOverTriggered`.
- Al perder congela `Time.timeScale`; los FX y la cámara usan tiempo no escalado.

### `HUDController.cs`

- Construye Canvas, barra de energía, distancia, velocidad, puntos, estado del
  Auto-Jacks, aviso de derrape y mensajes de pausa/game over.
- Usa `UnityEngine.UI.Text` y la fuente integrada `LegacyRuntime.ttf`.
- La barra cambia de color (rojo → ámbar → cian) y hay viñeta roja con energía baja.

---

## 5. Parámetros de ajuste rápido

| Sistema | Campo | Valor por defecto |
|---|---|---|
| Energía | `GameManager.fuelDrainPerSecond` | 4.2 /s |
| Energía | `GameManager.speedFuelFactor` | 0.45 |
| Energía | `FuelPickup.fuelAmount` | 26 |
| Pista | `TrackScroller.baseSpeed` | 9 |
| Pista | `TrackScroller.maxSpeed` | 29 |
| Pista | `TrackScroller.acceleration` | 0.18 |
| Jugador | `Mach5Controller.laneChangeSmoothTime` | 0.065 s |
| Jugador | `Mach5Controller.jumpDuration` | 0.8 s |
| Jugador | `Mach5Controller.skidDuration` | 0.6 s |
| Jugador | `Mach5Controller.skidDriftSpeed` | 4.2 |
| Spawner | `waveIntervalMin / Max` | 0.55 / 1.5 s |
| Spawner | `fuelIntervalMin / Max` | 4.5 / 7 s |
| Car-Fu | `telegraphDuration` | 0.8 s |
| Car-Fu | `dashDuration` | 0.22 s |
| Peligro | `DangerZone.warningDuration` | 1.15 s |
| Bomba | `Explosive.fuse` | 2.2 s |

**Cambiar el número de carriles**: edita `RaceLayout.LaneCount` y `LaneWidth`.
El jugador, spawner, fondo y HUD se adaptan; revisa que el ancho de cámara siga
cubriendo la pista.

---

## 6. Sustituir el arte procedural

### Opción A — Mantener la jerarquía y cambiar sprites

1. Duplica un objeto generado en Play (por ejemplo, un `TrafficCar`).
2. Sustitúyelo por tus sprites dentro de `Visual` manteniendo el nombre `Visual`
   (el Mach 5 lo usa como raíz para el salto).
3. Crea un prefab en `Assets/_Project/Prefabs/` y asígnalo al campo correspondiente
   del `Spawner` (`trafficPrefab`, `aggressivePrefab`, `oilPrefab`, `fuelPrefab`,
   `explosivePrefab`, `dangerZonePrefab`).

### Opción B — Prefabs propios desde cero

Requisitos mínimos por prefab:

- **Coche**: componente `TrafficCar` o `AggressiveCarAI`, un `Collider2D` trigger,
  y opcionalmente un hijo `Visual`.
- **Aceite**: `OilSlick` + collider trigger.
- **Bidón**: `FuelPickup` + collider trigger.
- **Bomba**: `Explosive` + collider trigger.
- **Zona de peligro**: `DangerZone` (el spawner la configura; no necesita collider).

`RacePool` se encarga de instanciar/reciclar los prefabs asignados.

### Audio (puntos de enganche)

- Salto: `Mach5Controller.TryJump()`.
- Derrape: `Mach5Controller.EnterSkid()` / `CancelSkid()`.
- Recogida: `FuelPickup.Collect()`.
- Choque: `Mach5Controller.Crash()`.
- Bomba: `Explosive.Explode()`.
- Música/estado: eventos de `GameManager`.

---

## 7. Notas de diseño y rendimiento

- **Justicia**: las oleadas se eligen sobre carriles libres dentro de una banda
  vertical; si no hay al menos 2 carriles libres, la oleada se descarta. El aviso
  de `DangerZone` reserva sus carriles para que no aparezca tráfico encima.
- **Legibilidad**: los peligros usan orden de sorting negativo (fondo) y los coches
  positivo; los FX van en 30. La cámara sacude con trauma y vibra con la velocidad.
- **Pooling**: solo para prefabs asignados; los objetos procedurales se destruyen.
  El número de objetos vivos es bajo (decenas), apto para móvil.
- **Arte runtime**: las texturas se generan una vez y se cachean en `NeonArt`; no
  se recrean por objeto.
- **Input System**: el proyecto usa el paquete nuevo (`Keyboard.current` /
  `Gamepad.current`), sin depender de un asset de acciones.

---

## 8. Solución de problemas

| Síntoma | Causa / solución |
|---|---|
| La escena no está en Build Settings | Usa el menú de construcción; lo añade solo. |
| Todo se ve negro | Comprueba que el proyecto use el **URP 2D Renderer** y exista `Global Light 2D`. |
| No responde el teclado | *Project Settings → Player → Active Input Handling* debe incluir **Input System Package**. |
| El HUD no muestra texto | Unity 6 incluye `LegacyRuntime.ttf`; si tu versión no, cambia `HUDController.LoadFont()`. |
| Quiero más dificultad | Sube `maxSpeed`, baja `waveIntervalMin` o sube `aggressiveWeight`. |
| Quiero menos dificultad | Baja `fuelDrainPerSecond`, sube `fuelIntervalMin` o baja `skidDriftSpeed`. |

---

## 9. Generar la escena desde consola (CI)

```powershell
Unity.exe -batchmode -nographics -quit `
  -projectPath "C:\ruta\MeteoroRacing" `
  -executeMethod MeteoroRacing.EditorTools.MeteoroSceneBuilder.BuildFromBatch `
  -logFile build.log
```

La escena se guarda en `Assets/_Project/Scenes/MeteoroRace.unity`.

### Smoke test de Play Mode

Comprueba que la escena arranca, que el HUD y el scroll funcionan, que se recoge
un bidón de energía y que chocar con el tráfico termina la partida. Devuelve
código 0 si todo va bien y 1 si falla (no uses `-quit`, el test cierra Unity solo):

```powershell
Unity.exe -batchmode -nographics `
  -projectPath "C:\ruta\MeteoroRacing" `
  -executeMethod MeteoroRacing.EditorTools.MeteoroSmokeTest.Run `
  -logFile smoke.log
```
