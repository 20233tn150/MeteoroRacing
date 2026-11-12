using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Zona de peligro telegrafiada: dibuja haces rojos intermitentes en 1-2 carriles
    /// y, al terminar la advertencia, el spawner deja caer un explosivo en cada uno.
    /// </summary>
    [DisallowMultipleComponent]
    public class DangerZone : MonoBehaviour
    {
        [Header("Aviso")]
        [Min(0.2f)] public float warningDuration = 1.15f;
        public float centerY = 3.4f;
        public float height = 11f;

        int[] lanes = new int[0];
        HazardAndEnemySpawner spawner;
        readonly List<SpriteRenderer> visuals = new List<SpriteRenderer>();
        readonly List<Color> baseColors = new List<Color>();
        bool configured;

        public void Configure(int[] targetLanes, HazardAndEnemySpawner owner, float duration)
        {
            lanes = targetLanes ?? new int[0];
            spawner = owner;
            warningDuration = Mathf.Max(0.2f, duration);
            configured = lanes.Length > 0;
        }

        void Start()
        {
            if (!configured)
            {
                Destroy(gameObject);
                return;
            }

            for (int i = 0; i < lanes.Length; i++)
            {
                List<SpriteRenderer> parts = NeonArt.BuildDangerBeam(transform, lanes[i], centerY, height, -35);
                for (int p = 0; p < parts.Count; p++)
                {
                    visuals.Add(parts[p]);
                    baseColors.Add(parts[p].color);
                }
            }

            CameraShake.Instance?.Shake(0.12f, 0.25f);
            StartCoroutine(WarningRoutine());
        }

        IEnumerator WarningRoutine()
        {
            float t = 0f;
            while (t < warningDuration)
            {
                GameManager gm = GameManager.Instance;
                if (gm != null && gm.IsGameOver) yield break;

                t += Time.deltaTime;
                float pulse = 0.55f + Mathf.PingPong(t * 4f, 0.45f);

                for (int i = 0; i < visuals.Count; i++)
                {
                    if (visuals[i] == null) continue;
                    Color c = baseColors[i];
                    c.a *= pulse;
                    visuals[i].color = c;
                }

                yield return null;
            }

            if (spawner != null)
            {
                for (int i = 0; i < lanes.Length; i++) spawner.DropExplosive(lanes[i]);
            }

            CameraShake.Instance?.Shake(0.3f, 0.25f);
            NeonFx.Burst(new Vector3(0f, centerY, 0f), new Color(1f, 0.1f, 0.2f), 12, 8f, 0.15f, 0.5f);
            Destroy(gameObject);
        }
    }
}
