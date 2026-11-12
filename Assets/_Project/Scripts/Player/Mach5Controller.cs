using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MeteoroRacing
{
    /// <summary>
    /// Control del Mach 5: 5 carriles, Auto-Jacks (salto), derrape por aceite,
    /// recogida de energía y colisiones contra tráfico, trampas y bordes.
    /// </summary>
    [DisallowMultipleComponent]
    public class Mach5Controller : MonoBehaviour
    {
        [Header("Carriles")]
        [Min(0.01f)] public float laneChangeSmoothTime = 0.065f;
        [Min(0f)] public float laneChangeCooldown = 0.075f;
        [Min(0f)] public float maxTilt = 13f;
        public int startLane = 2;

        [Header("Auto-Jacks (salto)")]
        [Min(0.05f)] public float jumpDuration = 0.8f;
        [Min(1f)] public float jumpScaleX = 1.18f;
        [Min(1f)] public float jumpScaleY = 1.45f;
        [Min(0f)] public float jumpHeight = 0.55f;
        [Min(0f)] public float jumpCooldown = 0.2f;

        [Header("Derrape por aceite")]
        [Min(0.05f)] public float skidDuration = 0.6f;
        [Min(0f)] public float skidDriftSpeed = 4.2f;

        public int TargetLane { get; private set; }
        public bool IsAirborne { get; private set; }
        public bool IsSkidding => skidTimer > 0f;
        public bool IsDead => isDead;

        Rigidbody2D rb;
        Collider2D bodyCollider;
        Transform visualRoot;
        SpriteRenderer shadowRenderer;
        Transform engineGlow;

        float xVelocity;
        float jumpTimer;
        float jumpCooldownTimer;
        float jumpOffset;
        float skidTimer;
        int skidDirection;
        int bufferedDirection;
        float bufferedUntil;
        float lastLaneChangeTime = -10f;
        float nextHazardContactTime;
        bool isDead;
        float visualTilt;
        bool initialized;

        void Awake()
        {
            Initialize();
        }

        void OnEnable()
        {
            if (!initialized) Initialize();
        }

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;

            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            bodyCollider = GetComponent<Collider2D>();
            if (bodyCollider == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.82f, 1.7f);
                box.isTrigger = true;
                bodyCollider = box;
            }

            TargetLane = Mathf.Clamp(startLane, 0, RaceLayout.LaneCount - 1);
            transform.position = new Vector3(RaceLayout.LaneX(TargetLane), RaceLayout.PlayerY, 0f);

            BuildVisualIfNeeded();
        }

        void BuildVisualIfNeeded()
        {
            SpriteRenderer existing = GetComponentInChildren<SpriteRenderer>();
            if (existing != null)
            {
                // Arte propio: se espera un hijo llamado "Visual" para el salto.
                visualRoot = transform.Find("Visual");
                if (visualRoot == null) visualRoot = transform;
                return;
            }

            Color body = new Color(0.93f, 0.94f, 1f);
            Color accent = new Color(1f, 0.1f, 0.28f);
            Color glow = new Color(0.15f, 0.95f, 1f);

            CarVisual visual = NeonArt.BuildCar(transform, CarKind.Mach5, body, accent, glow, 10);
            visualRoot = visual.root;
            shadowRenderer = visual.shadow.GetComponent<SpriteRenderer>();
            engineGlow = visual.root.Find("EngineGlow");
        }

        void Update()
        {
            if (isDead) return;
            Initialize();
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) return;

            float dt = Time.deltaTime;
            ReadInput();
            UpdateJump(dt);
            UpdateSkid(dt);
            UpdateHorizontal(dt);
            UpdateVisuals(dt);
        }

        // ------------------------------------------------------------------ input

        void ReadInput()
        {
            Keyboard kb = Keyboard.current;
            Gamepad gp = Gamepad.current;

            bool left = (kb != null && (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame))
                        || (gp != null && gp.dpad.left.wasPressedThisFrame);
            bool right = (kb != null && (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame))
                         || (gp != null && gp.dpad.right.wasPressedThisFrame);
            bool jump = (kb != null && kb.spaceKey.wasPressedThisFrame)
                        || (gp != null && gp.buttonSouth.wasPressedThisFrame);

            int direction = left ? -1 : (right ? 1 : 0);

            if (direction != 0)
            {
                if (skidTimer > 0f)
                {
                    // Única forma de recuperar el control: dirección contraria al derrape.
                    if (direction == -skidDirection) CancelSkid();
                }
                else if (Time.time - lastLaneChangeTime >= laneChangeCooldown)
                {
                    ChangeLane(direction);
                }
                else
                {
                    bufferedDirection = direction;
                    bufferedUntil = Time.time + 0.18f;
                }
            }

            if (bufferedDirection != 0 && Time.time - lastLaneChangeTime >= laneChangeCooldown)
            {
                ChangeLane(bufferedDirection);
                bufferedDirection = 0;
            }
            if (Time.time > bufferedUntil) bufferedDirection = 0;

            if (jump && skidTimer <= 0f) TryJump();
        }

        void ChangeLane(int direction)
        {
            int next = Mathf.Clamp(TargetLane + direction, 0, RaceLayout.LaneCount - 1);
            if (next == TargetLane) return;
            TargetLane = next;
            lastLaneChangeTime = Time.time;
        }

        // ------------------------------------------------------------------ movimiento

        void UpdateHorizontal(float dt)
        {
            float x = transform.position.x;

            if (skidTimer > 0f)
            {
                x += skidDirection * skidDriftSpeed * dt;
                TargetLane = RaceLayout.NearestLane(x);

                if (Mathf.Abs(x) >= RaceLayout.SideCrashX)
                {
                    Crash("¡Choque contra la barrera neón!");
                    return;
                }
            }
            else
            {
                float targetX = RaceLayout.LaneX(TargetLane);
                x = Mathf.SmoothDamp(x, targetX, ref xVelocity, laneChangeSmoothTime);
            }

            transform.position = new Vector3(x, RaceLayout.PlayerY + jumpOffset, 0f);
        }

        void UpdateJump(float dt)
        {
            jumpCooldownTimer = Mathf.Max(0f, jumpCooldownTimer - dt);
            IsAirborne = jumpTimer > 0.001f;

            if (jumpTimer <= 0f)
            {
                if (jumpOffset > 0f)
                {
                    jumpOffset = 0f;
                    IsAirborne = false;
                    if (visualRoot != null) visualRoot.localScale = Vector3.one;
                    if (shadowRenderer != null)
                    {
                        shadowRenderer.color = new Color(0f, 0f, 0f, 0.55f);
                        shadowRenderer.transform.localScale = new Vector3(1.2f, 2f, 1f);
                    }
                }
                return;
            }

            jumpTimer = Mathf.Max(0f, jumpTimer - dt);
            float t = 1f - jumpTimer / jumpDuration;
            float h = Mathf.Sin(t * Mathf.PI);
            jumpOffset = jumpHeight * h;

            if (visualRoot != null)
            {
                float sx = Mathf.Lerp(1f, jumpScaleX, h);
                float sy = Mathf.Lerp(1f, jumpScaleY, h);
                visualRoot.localScale = new Vector3(sx, sy, 1f);
            }

            if (shadowRenderer != null)
            {
                Color c = shadowRenderer.color;
                c.a = Mathf.Lerp(0.55f, 0.16f, h);
                shadowRenderer.color = c;
                shadowRenderer.transform.localScale = new Vector3(1.2f + h * 0.5f, 2f + h * 0.7f, 1f);
            }

            IsAirborne = jumpTimer > 0.001f;
        }

        void TryJump()
        {
            if (jumpTimer > 0f || jumpCooldownTimer > 0f) return;

            jumpTimer = jumpDuration;
            jumpCooldownTimer = jumpDuration + jumpCooldown;

            NeonFx.Burst(transform.position + Vector3.down * 0.7f,
                new Color(0.15f, 1f, 0.9f), 12, 4.5f, 0.12f, 0.35f);
        }

        void UpdateVisuals(float dt)
        {
            if (visualRoot == null) return;

            float targetTilt;
            if (skidTimer > 0f)
            {
                targetTilt = Mathf.Sin(Time.time * 32f) * 9f - skidDirection * 8f;
            }
            else
            {
                targetTilt = Mathf.Clamp(-xVelocity * 1.8f, -maxTilt, maxTilt);
            }

            visualTilt = Mathf.Lerp(visualTilt, targetTilt, 1f - Mathf.Exp(-18f * dt));
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, visualTilt);

            if (engineGlow != null)
            {
                float speed = TrackScroller.Instance != null ? TrackScroller.Instance.SpeedNormalized : 0f;
                float pulse = 0.85f + Mathf.Sin(Time.time * 22f) * 0.15f;
                engineGlow.localScale = new Vector3(0.9f + speed * 0.5f, (0.7f + speed * 1.4f) * pulse, 1f);
            }
        }

        // ------------------------------------------------------------------ derrape

        public void EnterSkid(int direction)
        {
            if (isDead || IsAirborne || skidTimer > 0f) return;

            skidDirection = direction == 0 ? (Random.value < 0.5f ? -1 : 1) : (int)Mathf.Sign(direction);
            skidTimer = skidDuration;

            CameraShake.Instance?.Shake(0.25f, 0.2f);
            NeonFx.Burst(transform.position, new Color(0.9f, 0.2f, 1f), 10, 5f, 0.14f, 0.45f);
        }

        void CancelSkid()
        {
            if (skidTimer <= 0f) return;
            skidTimer = 0f;
            TargetLane = RaceLayout.NearestLane(transform.position.x);
            lastLaneChangeTime = Time.time;
            NeonFx.Burst(transform.position, new Color(0.4f, 1f, 0.9f), 6, 3f, 0.1f, 0.3f);
        }

        void UpdateSkid(float dt)
        {
            if (skidTimer <= 0f) return;
            skidTimer -= dt;
            if (skidTimer <= 0f)
            {
                TargetLane = RaceLayout.NearestLane(transform.position.x);
                lastLaneChangeTime = Time.time;
            }
        }

        // ------------------------------------------------------------------ colisiones

        void OnTriggerEnter2D(Collider2D other)
        {
            Initialize();
            HandleContact(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            Initialize();
            HandleContact(other);
        }

        void HandleContact(Collider2D other)
        {
            if (isDead || other == null) return;

            FuelPickup fuel = other.GetComponentInParent<FuelPickup>();
            if (fuel != null)
            {
                fuel.Collect(this);
                return;
            }

            if (IsAirborne) return;
            if (Time.time < nextHazardContactTime) return;

            IHazard hazard = other.GetComponentInParent<IHazard>();
            if (hazard == null) return;

            nextHazardContactTime = Time.time + 0.25f;
            hazard.HitPlayer(this);
        }

        public void CollectFuel(float amount)
        {
            if (isDead) return;
            GameManager.Instance?.AddFuel(amount);
            NeonFx.Burst(transform.position, new Color(0.1f, 1f, 0.8f), 16, 7f, 0.15f, 0.5f);
        }

        public void Crash(string reason)
        {
            if (isDead) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            isDead = true;
            if (bodyCollider != null) bodyCollider.enabled = false;

            NeonFx.Burst(transform.position, new Color(1f, 0.35f, 0.1f), 26, 10f, 0.2f, 0.7f);
            NeonFx.Burst(transform.position, new Color(1f, 0.9f, 0.3f), 16, 7f, 0.14f, 0.5f);
            CameraShake.Instance?.Shake(1f, 0.55f);
            GameManager.Instance?.TriggerGameOver(reason);

            StartCoroutine(DeathSpin());
        }

        IEnumerator DeathSpin()
        {
            float t = 0f;
            while (t < 1.2f)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                if (visualRoot != null) visualRoot.Rotate(0f, 0f, 420f * dt);
                yield return null;
            }
        }
    }
}
