using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>Explosiones y chispas neón sin dependencias externas.</summary>
    public static class NeonFx
    {
        public static void Burst(Vector3 position, Color color, int count = 12,
            float speed = 7f, float size = 0.15f, float life = 0.5f)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("NeonFx");
                go.transform.position = position;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = NeonArt.Circle;
                sr.sortingOrder = 30;

                var particle = go.AddComponent<FxParticle>();
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                particle.Init(dir * Random.Range(speed * 0.35f, speed), color,
                    Random.Range(life * 0.6f, life * 1.15f),
                    Random.Range(size * 0.6f, size * 1.4f));
            }
        }
    }
}
