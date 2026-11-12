using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Scroll infinito vertical con capas de parallax y aceleración progresiva.
    /// Genera el fondo neón por código si no se le asignan capas en el inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class TrackScroller : MonoBehaviour
    {
        static TrackScroller instance;

        public static TrackScroller Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<TrackScroller>();
                    if (instance != null) instance.EnsureInitialized();
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Velocidad (unidades/segundo)")]
        [Min(0.1f)] public float baseSpeed = 9f;
        [Min(0.1f)] public float maxSpeed = 29f;
        [Min(0f)] public float acceleration = 0.18f;
        [Tooltip("Aumenta la aceleración con el tiempo para que la curva de dificultad sea más agresiva.")]
        [Min(0f)] public float accelerationRamp = 0.012f;

        [Header("Capas")]
        [SerializeField] List<ScrollLayer> layers = new List<ScrollLayer>();

        [Header("Auto-construcción")]
        public bool buildVisualsIfMissing = true;

        public float CurrentSpeed { get; private set; }
        public float SpeedNormalized => maxSpeed <= baseSpeed
            ? 0f
            : Mathf.InverseLerp(baseSpeed, maxSpeed, CurrentSpeed);

        [Serializable]
        public class ScrollLayer
        {
            public string name = "Layer";
            public Transform root;
            [Min(0f)] public float parallax = 1f;
            [Min(0.1f)] public float tileHeight = 14f;
            [Min(1)] public int tileCount = 2;
            [NonSerialized] public List<Transform> tiles = new List<Transform>();
        }

        float elapsed;
        bool initialized;
        readonly List<SpriteRenderer> speedLineRenderers = new List<SpriteRenderer>();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            EnsureInitialized();
        }

        void OnEnable()
        {
            if (instance == null) instance = this;
        }

        void Start()
        {
            EnsureInitialized();
        }

        void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            if (instance == null) instance = this;
            if (CurrentSpeed <= 0f) CurrentSpeed = baseSpeed;

            if (buildVisualsIfMissing && (layers == null || layers.Count == 0))
                BuildDefaultLayers();

            CacheTiles();
        }

        void Update()
        {
            EnsureInitialized();

            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying)
            {
                UpdateSpeedLines(0f);
                return;
            }

            float dt = Time.deltaTime;
            elapsed += dt;

            float accel = acceleration * (1f + elapsed * accelerationRamp);
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, maxSpeed, accel * dt);

            ScrollLayers(dt);
            UpdateSpeedLines(SpeedNormalized);
            CameraShake.Instance?.SetRumble(SpeedNormalized);
        }

        void ScrollLayers(float dt)
        {
            if (layers == null) return;

            for (int i = 0; i < layers.Count; i++)
            {
                ScrollLayer layer = layers[i];
                if (layer == null || layer.tileHeight <= 0.01f) continue;

                float dy = CurrentSpeed * layer.parallax * dt;
                if (dy <= 0f) continue;

                int count = Mathf.Max(1, layer.tileCount);
                float wrap = layer.tileHeight * count;

                for (int t = 0; t < layer.tiles.Count; t++)
                {
                    Transform tile = layer.tiles[t];
                    if (tile == null) continue;

                    Vector3 p = tile.localPosition;
                    p.y -= dy;
                    if (p.y <= -layer.tileHeight) p.y += wrap;
                    tile.localPosition = p;
                }
            }
        }

        void UpdateSpeedLines(float normalized)
        {
            if (speedLineRenderers.Count == 0) return;
            float a = Mathf.Lerp(0.08f, 0.7f, normalized);
            for (int i = 0; i < speedLineRenderers.Count; i++)
            {
                SpriteRenderer sr = speedLineRenderers[i];
                if (sr == null) continue;
                Color c = sr.color;
                c.a = a;
                sr.color = c;
            }
        }

        void CacheTiles()
        {
            if (layers == null) return;

            for (int i = 0; i < layers.Count; i++)
            {
                ScrollLayer layer = layers[i];
                if (layer == null || layer.root == null) continue;

                if (layer.tiles == null) layer.tiles = new List<Transform>();
                if (layer.tiles.Count > 0) continue;

                for (int c = 0; c < layer.root.childCount; c++)
                    layer.tiles.Add(layer.root.GetChild(c));

                layer.tileCount = Mathf.Max(1, layer.tiles.Count);
            }
        }

        // ------------------------------------------------------------------ construcción

        void BuildDefaultLayers()
        {
            layers = new List<ScrollLayer>();
            var container = new GameObject("NeonScrollLayers").transform;
            container.SetParent(transform, false);

            CreateLayer(container, "DeepSpace", NeonArt.SpaceTile(), 24f, 14f, 0.12f, -100, 0f);
            CreateLayer(container, "NeonGrid", NeonArt.GridTile(), 24f, 14f, 0.28f, -90, 0f);
            CreateLayer(container, "Road", NeonArt.RoadTile(), RaceLayout.RoadHalfWidth * 2f, 14f, 1f, -80, 0f);
            CreateLayer(container, "LaneDashes", NeonArt.LaneDashTile(), RaceLayout.RoadHalfWidth * 2f, 14f, 1f, -70, 0f);

            CreateLayer(container, "RailL", NeonArt.RailTile(), 0.42f, 14f, 1.15f, -60,
                -RaceLayout.RoadHalfWidth - 0.3f);
            CreateLayer(container, "RailR", NeonArt.RailTile(), 0.42f, 14f, 1.15f, -60,
                RaceLayout.RoadHalfWidth + 0.3f);

            ScrollLayer speed = CreateLayer(container, "SpeedLines", NeonArt.SpeedLinesTile(), 24f, 14f, 1.65f, -50, 0f);
            speedLineRenderers.Clear();
            foreach (Transform tile in speed.tiles)
            {
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null) speedLineRenderers.Add(sr);
            }
        }

        ScrollLayer CreateLayer(Transform container, string name, Sprite sprite, float width, float height,
            float parallax, int sortingOrder, float xOffset, int tileCount = 2)
        {
            var root = new GameObject(name).transform;
            root.SetParent(container, false);
            root.localPosition = new Vector3(xOffset, 0f, 0f);

            var layer = new ScrollLayer
            {
                name = name,
                root = root,
                parallax = parallax,
                tileHeight = height,
                tileCount = tileCount,
                tiles = new List<Transform>()
            };

            for (int i = 0; i < tileCount; i++)
            {
                var tile = new GameObject($"Tile_{i}").transform;
                tile.SetParent(root, false);
                tile.localPosition = new Vector3(0f, i * height, 0f);
                tile.localScale = new Vector3(width, height, 1f);

                var sr = tile.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = sortingOrder;
                layer.tiles.Add(tile);
            }

            layers.Add(layer);
            return layer;
        }
    }
}
