using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Bidón de energía. Recarga combustible y suma puntos. No bloquea carril.
    /// </summary>
    [DisallowMultipleComponent]
    public class FuelPickup : TrackObject
    {
        [Header("Recompensa")]
        [Min(1f)] public float fuelAmount = 26f;
        public int scoreBonus = 150;

        bool collected;
        Transform ring;

        protected override void Awake()
        {
            base.Awake();
            relativeSpeed = 1f;
            blocksLane = false;

            if (GetComponentInChildren<SpriteRenderer>() == null)
                NeonArt.BuildFuelPickup(transform, 5);

            ring = transform.Find("Ring");

            if (GetComponent<Collider2D>() == null)
            {
                CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = 0.62f;
                circle.isTrigger = true;
            }
        }

        void OnEnable()
        {
            collected = false;
        }

        protected override void Update()
        {
            base.Update();
            if (ring != null) ring.Rotate(0f, 0f, 110f * Time.deltaTime);
        }

        public void Collect(Mach5Controller player)
        {
            if (collected) return;
            collected = true;

            player.CollectFuel(fuelAmount);
            GameManager.Instance?.AddScore(scoreBonus);
            NeonFx.Burst(transform.position, new Color(0.1f, 1f, 0.8f), 18, 8f, 0.16f, 0.55f);
            Despawn();
        }
    }
}
