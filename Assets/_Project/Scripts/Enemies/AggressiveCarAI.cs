using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Coche "Car-Fu": parpadea en rojo (telegrafía), se lanza lateralmente hacia el
    /// carril del jugador y, si ya está alineado, acelera para embestir por detrás.
    /// </summary>
    [DisallowMultipleComponent]
    public class AggressiveCarAI : TrackObject, IHazard
    {
        [Header("Telegrafía")]
        [Min(0.1f)] public float telegraphDuration = 0.8f;
        [Min(0.05f)] public float dashDuration = 0.22f;
        [Min(0.2f)] public float minAttackInterval = 1.5f;
        [Min(0.2f)] public float maxAttackInterval = 3.0f;
        public float recoverDuration = 0.7f;

        [Header("Embestida frontal")]
        [Min(0f)] public float lungeBoost = 8f;
        [Min(0.1f)] public float lungeDuration = 0.45f;

        enum Mode { Cruise, Telegraph, Dash, Lunge, Recover }

        Mode mode = Mode.Cruise;
        Mach5Controller player;
        Transform warningRoot;
        SpriteRenderer underglow;
        Color baseBodyColor;
        Color baseGlowColor;
        float attackTimer;
        float telegraphTimer;
        float dashTimer;
        float recoverTimer;
        float lungeTimer;
        float startX;
        float targetX;
        int targetLane;

        public bool IsGroundHazard => true;

        protected override void Awake()
        {
            base.Awake();
            relativeSpeed = 0.52f;

            if (GetComponentInChildren<SpriteRenderer>() == null)
            {
                baseBodyColor = new Color(0.09f, 0.02f, 0.05f);
                baseGlowColor = new Color(1f, 0.08f, 0.2f);
                CarVisual visual = NeonArt.BuildCar(transform, CarKind.Aggressive, baseBodyColor,
                    new Color(1f, 0.1f, 0.25f), baseGlowColor, 8);
                Body = visual.body;
                underglow = visual.underglow;
                warningRoot = NeonArt.BuildWarningIcon(transform, new Vector2(0f, 1.55f),
                    new Color(1f, 0.1f, 0.2f), 14);
                warningRoot.gameObject.SetActive(false);
            }

            if (GetComponent<Collider2D>() == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.95f, 1.85f);
                box.isTrigger = true;
            }

            if (Body != null && baseBodyColor.a <= 0f) baseBodyColor = Body.color;
        }

        void OnEnable()
        {
            mode = Mode.Cruise;
            attackTimer = Random.Range(minAttackInterval, maxAttackInterval);
            telegraphTimer = 0f;
            dashTimer = 0f;
            recoverTimer = 0f;
            lungeTimer = 0f;
            extraSpeed = 0f;
            if (warningRoot != null) warningRoot.gameObject.SetActive(false);
            if (Body != null && baseBodyColor.a > 0f) Body.color = baseBodyColor;
        }

        protected override void Update()
        {
            base.Update();

            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) return;

            if (player == null)
            {
                player = Object.FindAnyObjectByType<Mach5Controller>();
                if (player == null) return;
            }

            float dt = Time.deltaTime;

            switch (mode)
            {
                case Mode.Cruise:
                    UpdateCruise(dt);
                    break;
                case Mode.Telegraph:
                    UpdateTelegraph(dt);
                    break;
                case Mode.Dash:
                    UpdateDash(dt);
                    break;
                case Mode.Lunge:
                    UpdateLunge(dt);
                    break;
                case Mode.Recover:
                    UpdateRecover(dt);
                    break;
            }
        }

        void UpdateCruise(float dt)
        {
            attackTimer -= dt;
            if (attackTimer > 0f) return;
            if (player == null || player.IsDead) return;

            targetLane = player.TargetLane;
            StartTelegraph();
        }

        void StartTelegraph()
        {
            mode = Mode.Telegraph;
            telegraphTimer = telegraphDuration;

            if (warningRoot != null) warningRoot.gameObject.SetActive(true);
            if (Body != null) Body.color = Color.red;
            if (underglow != null) underglow.color = new Color(1f, 0f, 0.1f, 0.9f);
        }

        void UpdateTelegraph(float dt)
        {
            telegraphTimer -= dt;

            float blink = Mathf.PingPong(Time.time * 14f, 1f);
            if (Body != null)
                Body.color = Color.Lerp(baseBodyColor, new Color(1f, 0.05f, 0.15f), 0.35f + blink * 0.65f);

            if (warningRoot != null)
            {
                float s = 1f + Mathf.Sin(Time.time * 18f) * 0.12f;
                warningRoot.localScale = new Vector3(s, s, 1f);
            }

            if (telegraphTimer > 0f) return;

            if (warningRoot != null) warningRoot.gameObject.SetActive(false);
            if (Body != null) Body.color = baseBodyColor;
            if (underglow != null) underglow.color = new Color(baseGlowColor.r, baseGlowColor.g, baseGlowColor.b, 0.5f);

            if (targetLane == Lane)
            {
                mode = Mode.Lunge;
                lungeTimer = lungeDuration;
                extraSpeed = lungeBoost;
            }
            else
            {
                mode = Mode.Dash;
                dashTimer = 0f;
                startX = transform.position.x;
                targetX = RaceLayout.LaneX(targetLane);
            }
        }

        void UpdateDash(float dt)
        {
            dashTimer += dt;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dashTimer / dashDuration));
            Vector3 p = transform.position;
            p.x = Mathf.Lerp(startX, targetX, k);
            transform.position = p;

            if (dashTimer < dashDuration) return;

            Lane = targetLane;
            mode = Mode.Recover;
            recoverTimer = recoverDuration;
        }

        void UpdateLunge(float dt)
        {
            lungeTimer -= dt;
            if (lungeTimer > 0f) return;
            extraSpeed = 0f;
            mode = Mode.Recover;
            recoverTimer = recoverDuration;
        }

        void UpdateRecover(float dt)
        {
            recoverTimer -= dt;
            if (recoverTimer > 0f) return;
            mode = Mode.Cruise;
            attackTimer = Random.Range(minAttackInterval, maxAttackInterval);
        }

        public void HitPlayer(Mach5Controller mach5)
        {
            mach5.Crash("¡Embestida Car-Fu!");
        }
    }
}
