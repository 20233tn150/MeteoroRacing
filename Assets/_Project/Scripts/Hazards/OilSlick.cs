using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Mancha de aceite: provoca un derrape de 0.6 s. El jugador puede cancelarlo
    /// pulsando la dirección contraria a la deriva.
    /// </summary>
    [DisallowMultipleComponent]
    public class OilSlick : TrackObject, IHazard
    {
        public bool IsGroundHazard => true;

        float rearmTime;

        protected override void Awake()
        {
            base.Awake();
            relativeSpeed = 1f;

            if (GetComponentInChildren<SpriteRenderer>() == null)
                NeonArt.BuildOilSlick(transform, 1);

            if (GetComponent<Collider2D>() == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1.55f, 0.85f);
                box.isTrigger = true;
            }
        }

        void OnEnable()
        {
            rearmTime = 0f;
        }

        public void HitPlayer(Mach5Controller player)
        {
            if (Time.time < rearmTime) return;
            rearmTime = Time.time + 1.2f;

            float delta = player.transform.position.x - transform.position.x;
            int direction = Mathf.Abs(delta) < 0.05f
                ? (Random.value < 0.5f ? -1 : 1)
                : (delta > 0f ? 1 : -1);

            player.EnterSkid(direction);
            NeonFx.Burst(transform.position, new Color(0.85f, 0.2f, 1f), 8, 4f, 0.12f, 0.4f);
        }
    }
}
