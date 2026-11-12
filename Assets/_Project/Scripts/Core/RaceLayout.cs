using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Geometría compartida de la pista. Todo el juego asume 5 carriles verticales.
    /// Si cambias LaneCount aquí, el jugador, el spawner y el fondo se adaptan.
    /// </summary>
    public static class RaceLayout
    {
        public const int LaneCount = 5;
        public const float LaneWidth = 2.25f;
        public const float RoadHalfWidth = LaneCount * LaneWidth * 0.5f; // 5.625
        public const float PlayerY = -3.55f;
        public const float SpawnY = 8.75f;
        public const float DespawnY = -8.5f;
        public const float SideCrashX = RoadHalfWidth + 0.5f; // 6.125
        public const float CameraHalfHeight = 6f;
        public const float CameraHalfWidth = 10.6667f; // 16:9 con orthographicSize = 6

        /// <summary>Centro X del carril 0..4.</summary>
        public static float LaneX(int lane)
        {
            lane = Mathf.Clamp(lane, 0, LaneCount - 1);
            return (lane - (LaneCount - 1) * 0.5f) * LaneWidth;
        }

        /// <summary>Carril más cercano a una coordenada X (para terminar derrapes alineado).</summary>
        public static int NearestLane(float x)
        {
            int lane = Mathf.RoundToInt(x / LaneWidth + (LaneCount - 1) * 0.5f);
            return Mathf.Clamp(lane, 0, LaneCount - 1);
        }

        public static bool IsLaneValid(int lane) => lane >= 0 && lane < LaneCount;
    }
}
