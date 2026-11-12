using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeteoroRacing.EditorTools
{
    /// <summary>
    /// Smoke test de Play Mode para CI: abre la escena, juega 6 segundos y comprueba
    /// que los sistemas principales existen y que no hay excepciones en consola.
    /// Menú: Meteoro Racing > Ejecutar smoke test.
    /// </summary>
    [InitializeOnLoad]
    public static class MeteoroSmokeTest
    {
        const string RunningKey = "MeteoroRacing.SmokeTest.Running";
        const string ScenePath = "Assets/_Project/Scenes/MeteoroRace.unity";

        static double playStartTime;
        static bool countingErrors;
        static int errorCount;
        static int phase;
        static double phaseStart;
        static float fuelBeforePickup;
        static int scoreBeforePickup;
        static FuelPickup testPickup;
        static bool baselineOk;
        static bool fuelPickupOk;

        static MeteoroSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                playStartTime = 0d;
                countingErrors = false;
                phase = 0;
                phaseStart = 0d;
                baselineOk = false;
                fuelPickupOk = false;
                testPickup = null;
                Application.logMessageReceived += OnLog;
                EditorApplication.update += Tick;
            }
        }

        [MenuItem("Meteoro Racing/Ejecutar smoke test (Play Mode)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[SmokeTest] Ya estás en Play Mode.");
                return;
            }

            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("[SmokeTest] No existe la escena. Ejecuta primero 'Construir escena jugable'.");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(RunningKey, true);
            playStartTime = 0d;
            countingErrors = false;
            errorCount = 0;
            phase = 0;
            phaseStart = 0d;
            fuelBeforePickup = 0f;
            baselineOk = false;
            fuelPickupOk = false;
            testPickup = null;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (!countingErrors) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errorCount++;
                Debug.LogError("[SmokeTest] Error en runtime: " + condition);
            }
        }

        static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!EditorApplication.isPlaying) return;

            if (playStartTime <= 0d)
            {
                playStartTime = EditorApplication.timeSinceStartup;
                countingErrors = true;
                return;
            }

            if (EditorApplication.timeSinceStartup - playStartTime < 3.5d) return;

            Mach5Controller player = Object.FindAnyObjectByType<Mach5Controller>();
            GameManager gm = GameManager.Instance;
            if (player == null || gm == null)
            {
                Debug.LogError("[SmokeTest] Faltan GameManager o Mach5Controller.");
                Finish(false);
                return;
            }

            if (phase == 0)
            {
                baselineOk = CheckBaseline(player, gm);
                fuelBeforePickup = gm.Fuel;
                scoreBeforePickup = gm.Score;

                var fuelGO = new GameObject("SmokeTest_Fuel");
                fuelGO.transform.position = player.transform.position;
                testPickup = fuelGO.AddComponent<FuelPickup>();
                testPickup.relativeSpeed = 0f;
                testPickup.extraSpeed = 0f;

                phase = 1;
                phaseStart = EditorApplication.timeSinceStartup;
                return;
            }

            if (phase == 1)
            {
                if (EditorApplication.timeSinceStartup - phaseStart < 0.4d) return;

                bool pickupGone = testPickup == null || !testPickup.gameObject.activeInHierarchy;
                bool scoreGained = gm.Score > scoreBeforePickup;
                fuelPickupOk = pickupGone && scoreGained;
                Debug.Log($"[SmokeTest] recogida energía: fuel={fuelBeforePickup:0.0}->{gm.Fuel:0.0} " +
                          $"score={scoreBeforePickup}->{gm.Score} pickupGone={pickupGone} (ok={fuelPickupOk})");

                var carGO = new GameObject("SmokeTest_Traffic");
                carGO.transform.position = player.transform.position;
                TrafficCar car = carGO.AddComponent<TrafficCar>();
                car.relativeSpeed = 0f;

                phase = 2;
                phaseStart = EditorApplication.timeSinceStartup;
                return;
            }

            if (phase == 2)
            {
                if (EditorApplication.timeSinceStartup - phaseStart < 0.4d) return;

                bool collisionOk = gm.IsGameOver && gm.GameOverReason.Contains("tráfico");
                Debug.Log($"[SmokeTest] colisión: state={gm.State} reason='{gm.GameOverReason}' (ok={collisionOk})");

                bool success = baselineOk && fuelPickupOk && errorCount == 0 && collisionOk && gm.Fuel > 50f;
                Debug.Log($"[SmokeTest] baseline={baselineOk} fuelOk={fuelPickupOk} errors={errorCount} fuel={gm.Fuel:0.0}");
                Finish(success);
            }
        }

        static bool CheckBaseline(Mach5Controller player, GameManager gm)
        {
            bool ok = true;
            TrackScroller trackComponent = Object.FindAnyObjectByType<TrackScroller>();
            HazardAndEnemySpawner spawner = Object.FindAnyObjectByType<HazardAndEnemySpawner>();

            ok &= gm != null && GameManager.Instance != null;
            ok &= trackComponent != null && TrackScroller.Instance != null;
            ok &= player != null && player.GetComponentInChildren<SpriteRenderer>() != null;
            ok &= spawner != null;
            ok &= trackComponent == null || trackComponent.transform.childCount > 0;

            Camera camera = Camera.main;
            ok &= camera != null && camera.transform.position.z < -1f;

            HUDController hud = Object.FindAnyObjectByType<HUDController>();
            ok &= hud != null && hud.canvas != null && hud.fuelFill != null && hud.fuelLabel != null;
            ok &= hud == null || (hud.fuelLabel != null && hud.fuelLabel.font != null);

            int hazards = Object.FindObjectsByType<TrackObject>().Length;
            int enemies = Object.FindObjectsByType<TrafficCar>().Length +
                          Object.FindObjectsByType<AggressiveCarAI>().Length;
            int pickups = Object.FindObjectsByType<FuelPickup>().Length;

            float speed = TrackScroller.Instance != null ? TrackScroller.Instance.CurrentSpeed : -1f;

            Debug.Log($"[SmokeTest] baseline ok={ok} state={gm.State} fuel={gm.Fuel:0.0} " +
                      $"speed={speed:0.00} hazards={hazards} enemies={enemies} pickups={pickups}");

            return ok && hazards > 0 && gm.State == GameManager.RaceState.Playing && gm.Fuel > 50f;
        }

        static void Finish(bool success)
        {
            Debug.Log(success ? "[SmokeTest] PASS" : "[SmokeTest] FAIL");

            SessionState.SetBool(RunningKey, false);
            countingErrors = false;
            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
