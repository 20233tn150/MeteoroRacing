using System.Collections.Generic;
using UnityEngine;

namespace MeteoroRacing
{
    public enum CarKind { Mach5, Traffic, Aggressive }

    /// <summary>Referencias a las piezas generadas de un coche para animarlas.</summary>
    public sealed class CarVisual
    {
        public Transform root;
        public Transform shadow;
        public SpriteRenderer body;
        public SpriteRenderer underglow;
        public Color bodyColor;
        public Color accentColor;
        public readonly List<SpriteRenderer> neonParts = new List<SpriteRenderer>();
    }

    public static partial class NeonArt
    {
        // ------------------------------------------------------------------ coches

        public static CarVisual BuildCar(Transform parent, CarKind kind, Color bodyColor, Color accentColor,
            Color glowColor, int sortingOrder)
        {
            var visual = new CarVisual
            {
                bodyColor = bodyColor,
                accentColor = accentColor
            };

            var visualGO = new GameObject("Visual");
            visual.root = visualGO.transform;
            visual.root.SetParent(parent, false);

            // Sombra fuera del Visual para que no se escale en el salto.
            SpriteRenderer shadow = AddPart(parent, "Shadow", Circle, new Color(0f, 0f, 0f, 0.55f),
                new Vector2(1.2f, 2.0f), new Vector2(0f, -0.12f), sortingOrder - 3);
            visual.shadow = shadow.transform;

            visual.underglow = AddPart(visual.root, "Underglow", Glow, WithAlpha(glowColor, 0.5f),
                new Vector2(2.2f, 3.0f), new Vector2(0f, -0.05f), sortingOrder - 1);
            visual.neonParts.Add(visual.underglow);

            Color wheel = new Color(0.025f, 0.025f, 0.04f, 1f);
            AddPart(visual.root, "WheelFL", Rounded, wheel, new Vector2(0.2f, 0.46f), new Vector2(-0.51f, 0.55f), sortingOrder);
            AddPart(visual.root, "WheelFR", Rounded, wheel, new Vector2(0.2f, 0.46f), new Vector2(0.51f, 0.55f), sortingOrder);
            AddPart(visual.root, "WheelRL", Rounded, wheel, new Vector2(0.2f, 0.46f), new Vector2(-0.51f, -0.55f), sortingOrder);
            AddPart(visual.root, "WheelRR", Rounded, wheel, new Vector2(0.2f, 0.46f), new Vector2(0.51f, -0.55f), sortingOrder);

            visual.body = AddPart(visual.root, "Body", Rounded, bodyColor,
                new Vector2(0.98f, 1.9f), Vector2.zero, sortingOrder + 1);

            // Morro y alerón.
            AddPart(visual.root, "Nose", Rounded, accentColor, new Vector2(0.62f, 0.3f), new Vector2(0f, 0.76f), sortingOrder + 2);
            AddPart(visual.root, "Spoiler", Rounded, accentColor, new Vector2(0.94f, 0.17f), new Vector2(0f, -0.82f), sortingOrder + 2);

            // Cabina.
            Color cockpit = new Color(0.02f, 0.07f, 0.13f, 0.97f);
            AddPart(visual.root, "Cockpit", Rounded, cockpit, new Vector2(0.6f, 0.74f), new Vector2(0f, 0.02f), sortingOrder + 3);
            AddPart(visual.root, "CockpitGlow", Rounded, WithAlpha(glowColor, 0.45f),
                new Vector2(0.48f, 0.08f), new Vector2(0f, 0.34f), sortingOrder + 4);

            // Faros y pilotos.
            Color headlight = new Color(0.85f, 0.98f, 1f, 1f);
            Color taillight = new Color(1f, 0.08f, 0.25f, 1f);
            AddPart(visual.root, "HeadL", Rounded, headlight, new Vector2(0.2f, 0.12f), new Vector2(-0.29f, 0.88f), sortingOrder + 3);
            AddPart(visual.root, "HeadR", Rounded, headlight, new Vector2(0.2f, 0.12f), new Vector2(0.29f, 0.88f), sortingOrder + 3);
            AddPart(visual.root, "TailL", Rounded, taillight, new Vector2(0.22f, 0.1f), new Vector2(-0.29f, -0.9f), sortingOrder + 3);
            AddPart(visual.root, "TailR", Rounded, taillight, new Vector2(0.22f, 0.1f), new Vector2(0.29f, -0.9f), sortingOrder + 3);

            // Tiras laterales neón.
            visual.neonParts.Add(AddPart(visual.root, "NeonL", White, WithAlpha(accentColor, 0.9f),
                new Vector2(0.05f, 1.15f), new Vector2(-0.49f, 0f), sortingOrder + 2));
            visual.neonParts.Add(AddPart(visual.root, "NeonR", White, WithAlpha(accentColor, 0.9f),
                new Vector2(0.05f, 1.15f), new Vector2(0.49f, 0f), sortingOrder + 2));

            if (kind == CarKind.Mach5)
            {
                // Círculo con el "5" del Mach 5.
                AddPart(visual.root, "RaceRoundel", Circle, accentColor, new Vector2(0.46f, 0.46f),
                    new Vector2(0f, 0.06f), sortingOrder + 4);
                AddPart(visual.root, "RaceNumber", Digit5, Color.white, new Vector2(0.23f, 0.32f),
                    new Vector2(0f, 0.06f), sortingOrder + 5);
                AddPart(visual.root, "EngineGlow", Glow, WithAlpha(glowColor, 0.7f),
                    new Vector2(0.9f, 0.7f), new Vector2(0f, -1.0f), sortingOrder);
            }
            else if (kind == CarKind.Aggressive)
            {
                // Cuchillas laterales del coche Car-Fu.
                AddPart(visual.root, "BladeL", Rounded, accentColor, new Vector2(0.12f, 0.6f),
                    new Vector2(-0.56f, 0.42f), sortingOrder + 2, 22f);
                AddPart(visual.root, "BladeR", Rounded, accentColor, new Vector2(0.12f, 0.6f),
                    new Vector2(0.56f, 0.42f), sortingOrder + 2, -22f);
            }

            return visual;
        }

        // ------------------------------------------------------------------ props

        public static void BuildFuelPickup(Transform parent, int sortingOrder)
        {
            Color cyan = new Color(0.1f, 1f, 0.8f, 1f);
            Color yellow = new Color(1f, 0.95f, 0.25f, 1f);

            AddPart(parent, "Glow", Glow, WithAlpha(cyan, 0.55f), new Vector2(1.9f, 2.5f),
                Vector2.zero, sortingOrder - 1);
            AddPart(parent, "Ring", Ring, WithAlpha(cyan, 0.85f), new Vector2(1.05f, 1.05f),
                Vector2.zero, sortingOrder);
            AddPart(parent, "CanBody", Rounded, new Color(0.05f, 0.75f, 0.6f, 1f),
                new Vector2(0.58f, 0.82f), Vector2.zero, sortingOrder + 1);
            AddPart(parent, "CanInner", Rounded, new Color(0.02f, 0.18f, 0.14f, 1f),
                new Vector2(0.38f, 0.58f), Vector2.zero, sortingOrder + 2);
            AddPart(parent, "Cap", Rounded, cyan, new Vector2(0.28f, 0.12f), new Vector2(0f, 0.44f), sortingOrder + 2);
            AddPart(parent, "BoltA", Rounded, yellow, new Vector2(0.1f, 0.3f), new Vector2(0.02f, 0.08f), sortingOrder + 3, 22f);
            AddPart(parent, "BoltB", Rounded, yellow, new Vector2(0.1f, 0.3f), new Vector2(-0.05f, -0.1f), sortingOrder + 3, 22f);
        }

        public static void BuildOilSlick(Transform parent, int sortingOrder)
        {
            Color rim = new Color(1f, 0.18f, 0.9f, 0.6f);
            Color bubble = new Color(0.35f, 0.9f, 1f, 0.65f);

            AddPart(parent, "Puddle", Circle, new Color(0.015f, 0.008f, 0.03f, 0.94f),
                new Vector2(1.75f, 1.0f), Vector2.zero, sortingOrder);
            AddPart(parent, "OilGlow", Glow, new Color(0.45f, 0.1f, 0.75f, 0.4f),
                new Vector2(2.1f, 1.3f), Vector2.zero, sortingOrder - 1);
            AddPart(parent, "OilRim", Ring, rim, new Vector2(1.8f, 1.05f), Vector2.zero, sortingOrder + 1);
            AddPart(parent, "Bubble1", Circle, bubble, new Vector2(0.2f, 0.12f), new Vector2(-0.35f, 0.15f), sortingOrder + 2);
            AddPart(parent, "Bubble2", Circle, bubble, new Vector2(0.13f, 0.09f), new Vector2(0.22f, -0.18f), sortingOrder + 2);
            AddPart(parent, "Bubble3", Circle, WithAlpha(rim, 0.5f), new Vector2(0.16f, 0.1f), new Vector2(0.48f, 0.16f), sortingOrder + 2);
        }

        /// <summary>Devuelve el glow para poder pulsarlo desde Explosive.</summary>
        public static SpriteRenderer BuildBomb(Transform parent, int sortingOrder)
        {
            Color red = new Color(1f, 0.1f, 0.2f, 1f);
            SpriteRenderer glow = AddPart(parent, "BombGlow", Glow, WithAlpha(red, 0.65f),
                new Vector2(1.8f, 1.8f), Vector2.zero, sortingOrder);
            AddPart(parent, "BombBody", Circle, new Color(0.05f, 0.02f, 0.04f, 1f),
                new Vector2(0.72f, 0.72f), Vector2.zero, sortingOrder + 1);
            AddPart(parent, "BombRing", Ring, red, new Vector2(0.82f, 0.82f), Vector2.zero, sortingOrder + 2);
            AddPart(parent, "Fuse", Rounded, new Color(0.8f, 0.8f, 0.7f, 1f),
                new Vector2(0.09f, 0.28f), new Vector2(0.2f, 0.44f), sortingOrder + 2, -32f);
            AddPart(parent, "Spark", Glow, new Color(1f, 0.9f, 0.2f, 0.95f),
                new Vector2(0.4f, 0.4f), new Vector2(0.31f, 0.56f), sortingOrder + 3);
            return glow;
        }

        public static Transform BuildWarningIcon(Transform parent, Vector2 localPos, Color color, int sortingOrder)
        {
            var root = new GameObject("WarningIcon");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);

            AddPart(root.transform, "WarnGlow", Glow, WithAlpha(color, 0.45f),
                new Vector2(1.5f, 1.5f), Vector2.zero, sortingOrder - 1);
            AddPart(root.transform, "WarnRing", Ring, WithAlpha(color, 0.95f),
                new Vector2(0.9f, 0.9f), Vector2.zero, sortingOrder);
            AddPart(root.transform, "WarnBar", Rounded, color, new Vector2(0.13f, 0.42f),
                new Vector2(0f, 0.1f), sortingOrder + 1);
            AddPart(root.transform, "WarnDot", Circle, color, new Vector2(0.14f, 0.14f),
                new Vector2(0f, -0.28f), sortingOrder + 1);
            return root.transform;
        }

        /// <summary>Construye el aviso vertical de una zona de peligro y devuelve sus renderers.</summary>
        public static List<SpriteRenderer> BuildDangerBeam(Transform parent, int lane, float centerY, float height, int sortingOrder)
        {
            var renderers = new List<SpriteRenderer>();
            Color red = new Color(1f, 0.07f, 0.18f, 1f);
            float x = RaceLayout.LaneX(lane);
            float width = RaceLayout.LaneWidth * 0.82f;

            SpriteRenderer beam = AddPart(parent, $"Beam_{lane}", White, WithAlpha(red, 0.12f),
                new Vector2(width, height), new Vector2(x, centerY), sortingOrder);
            renderers.Add(beam);

            renderers.Add(AddPart(parent, $"BeamEdgeL_{lane}", White, WithAlpha(red, 0.6f),
                new Vector2(0.06f, height), new Vector2(x - width * 0.5f, centerY), sortingOrder + 1));
            renderers.Add(AddPart(parent, $"BeamEdgeR_{lane}", White, WithAlpha(red, 0.6f),
                new Vector2(0.06f, height), new Vector2(x + width * 0.5f, centerY), sortingOrder + 1));

            SpriteRenderer stripe = AddPart(parent, $"HazardStripe_{lane}", WarningTile(), Color.white,
                new Vector2(width, 0.42f), new Vector2(x, centerY - height * 0.5f + 0.5f), sortingOrder + 2);
            renderers.Add(stripe);

            Transform icon = BuildWarningIcon(parent, new Vector2(x, centerY - height * 0.5f + 1.4f), red, sortingOrder + 4);
            foreach (SpriteRenderer sr in icon.GetComponentsInChildren<SpriteRenderer>()) renderers.Add(sr);

            return renderers;
        }
    }
}
