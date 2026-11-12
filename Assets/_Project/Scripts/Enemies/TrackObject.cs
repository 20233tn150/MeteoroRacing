using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Base de todo lo que se mueve con la pista: coches, aceite, combustible y explosivos.
    /// La velocidad es relativa a TrackScroller, por lo que el tráfico "avanza" con el jugador.
    /// </summary>
    public abstract class TrackObject : MonoBehaviour
    {
        [Header("Movimiento relativo")]
        [Tooltip("Fracción de la velocidad del mundo a la que se acerca. 1 = clavado al asfalto.")]
        [Range(0f, 3f)] public float relativeSpeed = 0.55f;

        [Tooltip("Velocidad extra hacia abajo en unidades/segundo.")]
        public float extraSpeed;

        [Tooltip("Si ocupa carril, el spawner nunca bloqueará los 5 carriles a la vez.")]
        public bool blocksLane = true;

        public int Lane { get; protected set; }

        protected SpriteRenderer Body;

        protected virtual void Awake()
        {
            Body = GetComponentInChildren<SpriteRenderer>();
        }

        public virtual void SetLane(int lane)
        {
            Lane = Mathf.Clamp(lane, 0, RaceLayout.LaneCount - 1);
            Vector3 p = transform.position;
            p.x = RaceLayout.LaneX(Lane);
            transform.position = p;
        }

        protected virtual void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) return;

            float worldSpeed = TrackScroller.Instance != null ? TrackScroller.Instance.CurrentSpeed : 10f;
            float dy = (worldSpeed * relativeSpeed + extraSpeed) * Time.deltaTime;
            transform.position += Vector3.down * dy;

            if (transform.position.y < RaceLayout.DespawnY) Despawn();
        }

        public virtual void Despawn()
        {
            RacePool.Release(gameObject);
        }
    }
}
