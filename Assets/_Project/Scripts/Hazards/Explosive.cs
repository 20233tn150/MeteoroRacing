using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Explosivo que cae a gran velocidad en el carril telegrafiado por DangerZone.
    /// Se puede esquivar cambiando de carril o con el Auto-Jacks.
    /// </summary>
    [DisallowMultipleComponent]
    public class Explosive : TrackObject, IHazard
    {
        [Header("Explosivo")]
        [Min(0.2f)] public float fuse = 2.2f;
        [Min(0f)] public float blastShake = 0.7f;

        public bool IsGroundHazard => true;

        float age;
        SpriteRenderer glow;
        float baseGlowAlpha;

        protected override void Awake()
        {
            base.Awake();
            relativeSpeed = 2f;
            extraSpeed = 2f;

            if (GetComponentInChildren<SpriteRenderer>() == null)
                glow = NeonArt.BuildBomb(transform, 9);
            else
                glow = transform.Find("BombGlow")?.GetComponent<SpriteRenderer>();

            if (glow != null) baseGlowAlpha = glow.color.a;

            if (GetComponent<Collider2D>() == null)
            {
                CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = 0.48f;
                circle.isTrigger = true;
            }
        }

        void OnEnable()
        {
            age = 0f;
            if (glow != null)
            {
                Color c = glow.color;
                c.a = baseGlowAlpha;
                glow.color = c;
                glow.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            }
        }

        protected override void Update()
        {
            base.Update();

            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) return;

            age += Time.deltaTime;

            if (glow != null)
            {
                float pulse = 0.85f + Mathf.Sin(Time.time * 26f) * 0.15f;
                glow.transform.localScale = new Vector3(1.8f * pulse, 1.8f * pulse, 1f);
                Color c = glow.color;
                c.a = baseGlowAlpha * (0.75f + Mathf.PingPong(Time.time * 4f, 0.25f));
                glow.color = c;
            }

            if (age >= fuse) Explode();
        }

        public void HitPlayer(Mach5Controller player)
        {
            player.Crash("¡Impacto explosivo!");
            Explode();
        }

        void Explode()
        {
            if (!gameObject.activeInHierarchy) return;

            NeonFx.Burst(transform.position, new Color(1f, 0.25f, 0.05f), 22, 11f, 0.22f, 0.7f);
            NeonFx.Burst(transform.position, new Color(1f, 0.9f, 0.2f), 14, 7f, 0.16f, 0.5f);
            CameraShake.Instance?.Shake(blastShake, 0.35f);
            Despawn();
        }
    }
}
