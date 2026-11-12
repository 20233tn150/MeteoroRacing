using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MeteoroRacing
{
    /// <summary>
    /// Estado global de la carrera: energía, distancia, puntuación, pausa y fin de partida.
    /// El HUD se suscribe a sus eventos; el resto de sistemas consultan Instance.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        static GameManager instance;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameManager>();
                    if (instance != null) instance.Initialize();
                }
                return instance;
            }
            private set => instance = value;
        }

        public enum RaceState { Playing, Paused, GameOver }

        [Header("Energía / Combustible")]
        [Min(1f)] public float maxFuel = 100f;
        [Min(0f)] public float startingFuel = 100f;
        [Min(0f)] public float fuelDrainPerSecond = 4.2f;
        [Range(0f, 2f)] public float speedFuelFactor = 0.45f;

        [Header("Puntuación")]
        public float scorePerMeter = 1.1f;
        [Tooltip("Metros de pista por unidad de mundo. 2.5 hace que la velocidad mostrada (km/h) sea coherente con la distancia.")]
        [Min(0.01f)] public float metersPerUnit = 2.5f;

        public RaceState State { get; private set; } = RaceState.Playing;
        public float Fuel { get; private set; }
        public float MaxFuel => maxFuel;
        public float FuelNormalized => maxFuel <= 0f ? 0f : Mathf.Clamp01(Fuel / maxFuel);
        public float DistanceMeters { get; private set; }
        public int Score => Mathf.FloorToInt(scoreAccumulator);
        public string GameOverReason { get; private set; } = string.Empty;
        public bool IsPlaying => State == RaceState.Playing;
        public bool IsGameOver => State == RaceState.GameOver;

        public event Action<float, float> FuelChanged;   // actual, máximo
        public event Action<float> DistanceChanged;      // metros
        public event Action<int> ScoreChanged;           // puntos
        public event Action<RaceState> StateChanged;
        public event Action<string> GameOverTriggered;

        float scoreAccumulator;
        bool initialized;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Initialize();
        }

        void OnEnable()
        {
            if (instance == null) instance = this;
        }

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;

            if (instance == null) instance = this;
            Time.timeScale = 1f;
            Fuel = Mathf.Clamp(startingFuel, 0f, maxFuel);
        }

        void Update()
        {
            Initialize();
            HandleGlobalInput();
            if (State != RaceState.Playing) return;

            float speed = TrackScroller.Instance != null ? TrackScroller.Instance.CurrentSpeed : 10f;
            float speedNorm = TrackScroller.Instance != null ? TrackScroller.Instance.SpeedNormalized : 0f;
            float dt = Time.deltaTime;

            float meters = speed * dt * metersPerUnit;
            DistanceMeters += meters;
            scoreAccumulator += meters * scorePerMeter;

            float drain = fuelDrainPerSecond * (1f + speedNorm * speedFuelFactor);
            Fuel = Mathf.Max(0f, Fuel - drain * dt);

            FuelChanged?.Invoke(Fuel, maxFuel);
            DistanceChanged?.Invoke(DistanceMeters);
            ScoreChanged?.Invoke(Score);

            if (Fuel <= 0f) TriggerGameOver("¡Te has quedado sin energía!");
        }

        void HandleGlobalInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (State == RaceState.GameOver)
            {
                if (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame ||
                    kb.numpadEnterKey.wasPressedThisFrame)
                {
                    Restart();
                }
                return;
            }

            if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
            {
                if (State == RaceState.Playing) Pause();
                else if (State == RaceState.Paused) Resume();
            }
        }

        public void AddFuel(float amount)
        {
            if (amount <= 0f || IsGameOver) return;
            Fuel = Mathf.Clamp(Fuel + amount, 0f, maxFuel);
            FuelChanged?.Invoke(Fuel, maxFuel);
        }

        public void AddScore(int points)
        {
            if (points == 0 || IsGameOver) return;
            scoreAccumulator += points;
            ScoreChanged?.Invoke(Score);
        }

        public void TriggerGameOver(string reason)
        {
            if (State == RaceState.GameOver) return;

            State = RaceState.GameOver;
            GameOverReason = string.IsNullOrEmpty(reason) ? "Fin de la carrera" : reason;
            Time.timeScale = 0f;

            CameraShake.Instance?.Shake(0.9f, 0.6f);
            GameOverTriggered?.Invoke(GameOverReason);
            StateChanged?.Invoke(State);
        }

        public void Pause()
        {
            if (State != RaceState.Playing) return;
            State = RaceState.Paused;
            Time.timeScale = 0f;
            StateChanged?.Invoke(State);
        }

        public void Resume()
        {
            if (State != RaceState.Paused) return;
            State = RaceState.Playing;
            Time.timeScale = 1f;
            StateChanged?.Invoke(State);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            RacePool.Clear();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }
    }
}
