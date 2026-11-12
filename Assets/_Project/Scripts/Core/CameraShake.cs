using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Sacudida de cámara por trauma + vibración continua según la velocidad.
    /// Usa tiempo no escalado para que el impacto se vea aunque el juego esté congelado.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraShake : MonoBehaviour
    {
        static CameraShake instance;

        public static CameraShake Instance
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<CameraShake>();
                return instance;
            }
            private set => instance = value;
        }

        [Min(0f)] public float maxOffset = 0.38f;
        [Min(0f)] public float rumbleOffset = 0.025f;
        [Min(0f)] public float decaySpeed = 2.4f;

        Vector3 baseLocalPosition;
        float trauma;
        float rumble;
        bool initialized;

        void Awake()
        {
            Initialize();
        }

        void OnEnable()
        {
            Initialize();
        }

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            instance = this;
            baseLocalPosition = transform.localPosition;
        }

        public void SetRumble(float normalized)
        {
            rumble = Mathf.Clamp01(normalized);
        }

        public void Shake(float amount, float duration)
        {
            trauma = Mathf.Max(trauma, Mathf.Clamp01(amount));
        }

        void LateUpdate()
        {
            Initialize();
            float dt = Time.unscaledDeltaTime;
            trauma = Mathf.MoveTowards(trauma, 0f, decaySpeed * dt);

            float amp = trauma * trauma * maxOffset;
            Vector2 shake = Random.insideUnitCircle * amp;
            Vector2 idle = Random.insideUnitCircle * (rumble * rumbleOffset);

            transform.localPosition = baseLocalPosition + new Vector3(shake.x + idle.x, shake.y + idle.y, 0f);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
