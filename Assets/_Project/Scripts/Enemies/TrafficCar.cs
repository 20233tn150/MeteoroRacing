using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Tráfico normal: ocupa un carril, avanza más lento que el jugador y bloquea el paso.
    /// </summary>
    [DisallowMultipleComponent]
    public class TrafficCar : TrackObject, IHazard
    {
        [Header("Tráfico")]
        public bool randomizeSpeed = true;
        public Vector2 speedRange = new Vector2(0.35f, 0.62f);

        static readonly Color[] Palette =
        {
            new Color(0.95f, 0.2f, 0.55f),
            new Color(1f, 0.55f, 0.1f),
            new Color(0.35f, 0.45f, 1f),
            new Color(0.6f, 0.2f, 1f),
            new Color(0.1f, 0.85f, 0.8f),
            new Color(0.9f, 0.85f, 0.2f)
        };

        public bool IsGroundHazard => true;

        protected override void Awake()
        {
            base.Awake();
            relativeSpeed = Random.Range(speedRange.x, speedRange.y);

            if (GetComponentInChildren<SpriteRenderer>() == null)
            {
                Color body = Palette[Random.Range(0, Palette.Length)];
                Color accent = Color.Lerp(body, Color.white, 0.55f);
                NeonArt.BuildCar(transform, CarKind.Traffic, body, accent,
                    new Color(1f, 0.25f, 0.75f), 6);
            }

            if (GetComponent<Collider2D>() == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.92f, 1.8f);
                box.isTrigger = true;
            }
        }

        void OnEnable()
        {
            if (randomizeSpeed) relativeSpeed = Random.Range(speedRange.x, speedRange.y);
        }

        public void HitPlayer(Mach5Controller player)
        {
            player.Crash("¡Has chocado con el tráfico!");
        }
    }
}
