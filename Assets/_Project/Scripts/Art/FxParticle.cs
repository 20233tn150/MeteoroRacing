using System.Collections;
using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>Partícula neón simple basada en SpriteRenderer (sin sistemas de partículas).</summary>
    public class FxParticle : MonoBehaviour
    {
        SpriteRenderer sr;
        Color color;
        Vector2 velocity;
        float life;
        float maxLife;
        float size;
        float spin;

        public void Init(Vector2 initialVelocity, Color tint, float lifetime, float startSize)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = NeonArt.Circle;
            sr.sortingOrder = 30;

            velocity = initialVelocity;
            color = tint;
            life = Mathf.Max(0.01f, lifetime);
            maxLife = life;
            size = startSize;
            spin = Random.Range(-360f, 360f);
            transform.localScale = Vector3.one * size;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            life -= dt;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(velocity * dt);
            velocity *= Mathf.Clamp01(1f - 2.2f * dt);
            transform.Rotate(0f, 0f, spin * dt);

            float k = life / maxLife;
            sr.color = new Color(color.r, color.g, color.b, color.a * k);
            transform.localScale = Vector3.one * (size * (0.4f + k * 0.6f));
        }
    }
}
