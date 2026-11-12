using System.Collections.Generic;
using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Generador procedural de tráfico, coches Car-Fu, aceite, zonas de peligro y energía.
    /// Regla de oro: nunca elige los 5 carriles a la vez; siempre deja un hueco libre
    /// y evita apilar obstáculos en el mismo carril cerca de la zona de aparición.
    /// </summary>
    [DisallowMultipleComponent]
    public class HazardAndEnemySpawner : MonoBehaviour
    {
        public static HazardAndEnemySpawner Instance { get; private set; }

        [Header("Prefabs opcionales (si están vacíos se generan por código)")]
        public GameObject trafficPrefab;
        public GameObject aggressivePrefab;
        public GameObject oilPrefab;
        public GameObject fuelPrefab;
        public GameObject explosivePrefab;
        public GameObject dangerZonePrefab;

        [Header("Ritmo de oleadas")]
        [Min(0f)] public float startDelay = 2.2f;
        [Min(0.1f)] public float waveIntervalMin = 0.55f;
        [Min(0.1f)] public float waveIntervalMax = 1.5f;

        [Header("Energía")]
        [Min(0.5f)] public float fuelIntervalMin = 4.5f;
        [Min(0.5f)] public float fuelIntervalMax = 7f;

        [Header("Pesos de dificultad")]
        [Min(0f)] public float aggressiveWeight = 18f;
        [Min(0f)] public float oilWeight = 12f;
        [Min(0f)] public float dangerWeight = 8f;
        [Min(0f)] public float oilUnlockTime = 5f;
        [Min(0f)] public float dangerUnlockTime = 12f;

        readonly List<TrackObject> active = new List<TrackObject>();
        readonly float[] laneReservedUntil = new float[RaceLayout.LaneCount];

        float waveTimer;
        float fuelTimer;
        bool running;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            if (Instance == null) Instance = this;
            waveTimer = startDelay;
            fuelTimer = 2.5f;
            running = true;
        }

        void Update()
        {
            if (!running) return;
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) return;

            PruneActive();

            float diff = TrackScroller.Instance != null ? TrackScroller.Instance.SpeedNormalized : 0f;
            float dt = Time.deltaTime;

            waveTimer -= dt;
            if (waveTimer <= 0f)
            {
                SpawnWave(diff);
                float interval = Mathf.Lerp(waveIntervalMax, waveIntervalMin, diff);
                waveTimer = interval * Random.Range(0.85f, 1.15f);
            }

            fuelTimer -= dt;
            if (fuelTimer <= 0f)
            {
                SpawnFuel(diff);
                float interval = Random.Range(fuelIntervalMin, fuelIntervalMax);
                fuelTimer = interval * Mathf.Lerp(1.1f, 0.8f, diff);
            }
        }

        void PruneActive()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i] == null || !active[i].gameObject.activeInHierarchy)
                    active.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------------ oleadas

        void SpawnWave(float diff)
        {
            float elapsed = Time.timeSinceLevelLoad;
            float wTraffic = Mathf.Max(10f, 80f - diff * 25f);
            float wAggressive = aggressiveWeight + diff * 25f;
            float wOil = elapsed >= oilUnlockTime ? oilWeight + diff * 8f : 0f;
            float wDanger = elapsed >= dangerUnlockTime ? dangerWeight + diff * 12f : 0f;

            float total = wTraffic + wAggressive + wOil + wDanger;
            float roll = Random.value * total;

            if (roll < wTraffic)
            {
                SpawnTrafficWave(diff);
                return;
            }
            roll -= wTraffic;

            if (roll < wAggressive)
            {
                SpawnAggressiveWave(diff);
                return;
            }
            roll -= wAggressive;

            if (roll < wOil)
            {
                SpawnOilWave();
                return;
            }

            SpawnDangerZone();
        }

        void SpawnTrafficWave(float diff)
        {
            int maxRequested = diff < 0.33f ? 2 : (diff < 0.66f ? 3 : 4);
            int requested = Random.Range(1, maxRequested + 1);
            List<int> lanes = PickFreeLanes(requested, true);
            if (lanes.Count == 0) return;

            float y = RaceLayout.SpawnY + Random.Range(-0.25f, 0.25f);
            for (int i = 0; i < lanes.Count; i++)
                Spawn<TrafficCar>(trafficPrefab, lanes[i], y);
        }

        void SpawnAggressiveWave(float diff)
        {
            int requested = diff > 0.65f ? Random.Range(1, 3) : 1;
            List<int> lanes = PickFreeLanes(requested, true);
            if (lanes.Count == 0) return;

            for (int i = 0; i < lanes.Count; i++)
                Spawn<AggressiveCarAI>(aggressivePrefab, lanes[i], RaceLayout.SpawnY + i * 0.6f);
        }

        void SpawnOilWave()
        {
            List<int> lanes = PickFreeLanes(Random.Range(1, 3), true);
            if (lanes.Count == 0) return;

            float y = RaceLayout.SpawnY - Random.Range(0f, 1.5f);
            for (int i = 0; i < lanes.Count; i++)
                Spawn<OilSlick>(oilPrefab, lanes[i], y);
        }

        void SpawnDangerZone()
        {
            List<int> lanes = PickFreeLanes(Random.Range(1, 3), true);
            if (lanes.Count == 0) return;

            // Reserva los carriles mientras dura el aviso y cae el explosivo.
            for (int i = 0; i < lanes.Count; i++)
                laneReservedUntil[lanes[i]] = Time.time + 2.4f;

            GameObject go;
            if (dangerZonePrefab != null)
            {
                go = Instantiate(dangerZonePrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                go = new GameObject("DangerZone");
                go.transform.position = Vector3.zero;
            }

            DangerZone zone = go.GetComponent<DangerZone>();
            if (zone == null) zone = go.AddComponent<DangerZone>();
            zone.Configure(lanes.ToArray(), this, 1.15f);
        }

        void SpawnFuel(float diff)
        {
            List<int> lanes = PickFreeLanes(RaceLayout.LaneCount, false);
            if (lanes.Count == 0) return;

            // Prefiere un carril sin otro bidón cerca de la parte superior.
            int chosen = lanes[Random.Range(0, lanes.Count)];
            Spawn<FuelPickup>(fuelPrefab, chosen, RaceLayout.SpawnY + 0.2f);
        }

        // ------------------------------------------------------------------ utilidades

        List<int> PickFreeLanes(int requested, bool leaveGap)
        {
            var candidates = new List<int>();
            for (int lane = 0; lane < RaceLayout.LaneCount; lane++)
            {
                if (!LaneBlocked(lane, 0.5f, RaceLayout.SpawnY + 2f))
                    candidates.Add(lane);
            }

            int maxCount = leaveGap ? Mathf.Max(0, candidates.Count - 1) : candidates.Count;
            int count = Mathf.Min(requested, maxCount);
            if (count <= 0) return candidates.GetRange(0, 0);

            for (int i = 0; i < candidates.Count; i++)
            {
                int j = Random.Range(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            return candidates.GetRange(0, count);
        }

        bool LaneBlocked(int lane, float yMin, float yMax)
        {
            if (Time.time < laneReservedUntil[lane]) return true;

            for (int i = 0; i < active.Count; i++)
            {
                TrackObject obj = active[i];
                if (obj == null || !obj.gameObject.activeInHierarchy || !obj.blocksLane) continue;
                if (obj.Lane != lane) continue;

                float y = obj.transform.position.y;
                if (y >= yMin && y <= yMax) return true;
            }

            return false;
        }

        T Spawn<T>(GameObject prefab, int lane, float y) where T : Component
        {
            Vector3 position = new Vector3(RaceLayout.LaneX(lane), y, 0f);
            GameObject go;

            if (prefab != null)
            {
                go = RacePool.Spawn(prefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject(typeof(T).Name);
                go.transform.position = position;
                go.AddComponent<T>();
            }

            T component = go.GetComponent<T>();
            if (component == null) component = go.AddComponent<T>();

            if (component is TrackObject track)
            {
                track.SetLane(lane);
                active.Add(track);
            }

            return component;
        }

        /// <summary>La llama DangerZone al terminar la advertencia.</summary>
        public void DropExplosive(int lane)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) return;
            Spawn<Explosive>(explosivePrefab, lane, RaceLayout.SpawnY + 0.4f);
        }
    }
}
