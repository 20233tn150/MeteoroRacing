using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MeteoroRacing
{
    /// <summary>
    /// HUD arcade construido con uGUI por código (sin assets externos):
    /// energía, distancia, velocidad, puntos, estado del Auto-Jacks y mensajes.
    /// </summary>
    [DisallowMultipleComponent]
    public class HUDController : MonoBehaviour
    {
        [Header("Referencias opcionales (se crean solas si están vacías)")]
        public Canvas canvas;
        public Text fuelLabel;
        public Text distanceText;
        public Text speedText;
        public Text scoreText;
        public Text centerText;
        public Text statusText;
        public Text jackText;
        public Text hintText;
        public Image fuelFill;
        public Image lowFuelOverlay;

        Font font;
        GameManager gm;
        Mach5Controller player;
        float introTimer;
        bool introVisible = true;

        void Start()
        {
            BuildIfNeeded();

            gm = GameManager.Instance;
            if (gm != null)
            {
                gm.FuelChanged += OnFuelChanged;
                gm.DistanceChanged += OnDistanceChanged;
                gm.ScoreChanged += OnScoreChanged;
                gm.StateChanged += OnStateChanged;
                gm.GameOverTriggered += OnGameOver;
            }

            player = Object.FindAnyObjectByType<Mach5Controller>();

            OnFuelChanged(gm != null ? gm.Fuel : 100f, gm != null ? gm.MaxFuel : 100f);
            OnDistanceChanged(gm != null ? gm.DistanceMeters : 0f);
            OnScoreChanged(gm != null ? gm.Score : 0);
            OnStateChanged(gm != null ? gm.State : GameManager.RaceState.Playing);

            if (centerText != null)
            {
                centerText.text = "¡METEORO RACING!";
                centerText.color = new Color(0.5f, 1f, 1f);
                centerText.gameObject.SetActive(true);
            }

            StartCoroutine(IntroRoutine());
        }

        void Update()
        {
            if (gm == null) gm = GameManager.Instance;

            if (gm != null && TrackScroller.Instance != null)
            {
                float factor = (gm != null ? gm.metersPerUnit : 2.5f) * 3.6f;
                int kmh = Mathf.RoundToInt(TrackScroller.Instance.CurrentSpeed * factor);
                if (speedText != null) speedText.text = $"VELOCIDAD  {kmh:000} km/h";
            }

            if (lowFuelOverlay != null)
            {
                float fuel = gm != null ? gm.FuelNormalized : 1f;
                float a = 0f;
                if (fuel < 0.3f && gm != null && gm.IsPlaying)
                    a = 0.06f + (Mathf.Sin(Time.unscaledTime * 7f) * 0.5f + 0.5f) * 0.09f;

                Color c = lowFuelOverlay.color;
                c.a = Mathf.Lerp(c.a, a, Time.unscaledDeltaTime * 8f);
                lowFuelOverlay.color = c;
            }

            if (jackText != null)
            {
                if (player == null) player = Object.FindAnyObjectByType<Mach5Controller>();

                if (player == null)
                {
                    jackText.text = "AUTO-JACKS: —";
                }
                else if (player.IsAirborne)
                {
                    jackText.text = "AUTO-JACKS: EN EL AIRE";
                    jackText.color = new Color(1f, 0.9f, 0.2f);
                }
                else
                {
                    jackText.text = "AUTO-JACKS: LISTO  [ESPACIO]";
                    jackText.color = new Color(0.4f, 1f, 1f);
                }
            }

            if (statusText != null && player != null)
            {
                bool skidding = player.IsSkidding && (gm == null || gm.IsPlaying);
                statusText.gameObject.SetActive(skidding);
                if (skidding)
                {
                    statusText.text = "¡DERRAPE!  PULSA LA DIRECCIÓN CONTRARIA";
                    statusText.color = new Color(1f, 0.25f, 0.85f);
                }
            }

            if (introVisible)
            {
                introTimer += Time.unscaledDeltaTime;
                if (introTimer > 1.3f)
                {
                    introVisible = false;
                    if (gm != null && gm.IsPlaying && centerText != null) centerText.gameObject.SetActive(false);
                }
            }
        }

        IEnumerator IntroRoutine()
        {
            yield return new WaitForSecondsRealtime(1.3f);
            if (gm != null && gm.IsPlaying && centerText != null)
                centerText.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ eventos

        void OnFuelChanged(float current, float max)
        {
            if (fuelFill == null) return;

            float t = max <= 0f ? 0f : Mathf.Clamp01(current / max);
            fuelFill.fillAmount = t;
            fuelFill.color = t < 0.3f
                ? Color.Lerp(new Color(1f, 0.1f, 0.2f), new Color(1f, 0.75f, 0.1f), t / 0.3f)
                : Color.Lerp(new Color(1f, 0.75f, 0.1f), new Color(0.1f, 1f, 0.75f), (t - 0.3f) / 0.7f);
        }

        void OnDistanceChanged(float meters)
        {
            if (distanceText != null) distanceText.text = $"{meters / 1000f:0.00} km";
        }

        void OnScoreChanged(int score)
        {
            if (scoreText != null) scoreText.text = $"PUNTOS  {score:000000}";
        }

        void OnStateChanged(GameManager.RaceState state)
        {
            if (centerText == null) return;

            switch (state)
            {
                case GameManager.RaceState.Playing:
                    if (!introVisible) centerText.gameObject.SetActive(false);
                    break;

                case GameManager.RaceState.Paused:
                    centerText.text = "PAUSA\n<size=28>ESC / P PARA CONTINUAR</size>";
                    centerText.color = new Color(0.5f, 1f, 1f);
                    centerText.gameObject.SetActive(true);
                    break;

                case GameManager.RaceState.GameOver:
                    centerText.gameObject.SetActive(true);
                    break;
            }
        }

        void OnGameOver(string reason)
        {
            if (centerText == null) return;

            int score = gm != null ? gm.Score : 0;
            float meters = gm != null ? gm.DistanceMeters : 0f;

            centerText.text =
                $"<size=64>FIN DE LA CARRERA</size>\n\n" +
                $"<size=32>{reason}</size>\n\n" +
                $"<size=30>DISTANCIA {meters / 1000f:0.00} km   ·   PUNTOS {score:000000}</size>\n\n" +
                "<size=26>PULSA  R  PARA REINICIAR</size>";

            centerText.color = new Color(1f, 0.25f, 0.35f);
            centerText.gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.FuelChanged -= OnFuelChanged;
            gm.DistanceChanged -= OnDistanceChanged;
            gm.ScoreChanged -= OnScoreChanged;
            gm.StateChanged -= OnStateChanged;
            gm.GameOverTriggered -= OnGameOver;
        }

        // ------------------------------------------------------------------ construcción de UI

        void BuildIfNeeded()
        {
            if (canvas != null) return;

            font = LoadFont();

            var canvasGO = new GameObject("HUD Canvas", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            Transform root = canvasGO.transform;

            // Bandas oscuras para legibilidad.
            CreateImage(root, "TopBand", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 120f), new Color(0.015f, 0.01f, 0.05f, 0.72f));
            CreateImage(root, "BottomBand", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 74f), new Color(0.015f, 0.01f, 0.05f, 0.7f));

            fuelLabel = CreateText(root, "FuelLabel", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(38f, -22f), new Vector2(340f, 30f), 22,
                TextAnchor.MiddleLeft, new Color(0.4f, 1f, 1f));
            fuelLabel.text = "ENERGÍA";

            Image fuelBg = CreateImage(root, "FuelBarBg", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(38f, -52f), new Vector2(340f, 28f),
                new Color(0.02f, 0.02f, 0.07f, 0.9f));

            fuelFill = CreateImage(fuelBg.transform, "FuelBarFill", Vector2.zero, Vector2.one,
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.1f, 1f, 0.75f));
            RectTransform fillRT = fuelFill.rectTransform;
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(3f, 3f);
            fillRT.offsetMax = new Vector2(-3f, -3f);
            fuelFill.type = Image.Type.Filled;
            fuelFill.fillMethod = Image.FillMethod.Horizontal;
            fuelFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fuelFill.fillAmount = 1f;

            distanceText = CreateText(root, "Distance", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(640f, 52f), 40,
                TextAnchor.MiddleCenter, Color.white);
            distanceText.text = "0.00 km";

            speedText = CreateText(root, "Speed", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(640f, 30f), 20,
                TextAnchor.MiddleCenter, new Color(0.5f, 1f, 1f));
            speedText.text = "VELOCIDAD  000 km/h";

            scoreText = CreateText(root, "Score", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-38f, -24f), new Vector2(460f, 44f), 30,
                TextAnchor.MiddleRight, new Color(1f, 0.35f, 0.85f));
            scoreText.text = "PUNTOS  000000";

            jackText = CreateText(root, "JackStatus", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(38f, 22f), new Vector2(560f, 30f), 20,
                TextAnchor.MiddleLeft, new Color(0.4f, 1f, 1f));
            jackText.text = "AUTO-JACKS: LISTO  [ESPACIO]";

            hintText = CreateText(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(1300f, 30f), 18,
                TextAnchor.MiddleCenter, new Color(0.7f, 0.85f, 1f, 0.9f));
            hintText.text = "A / D  o  ← / →  : cambiar de carril     ·     ESPACIO : Auto-Jacks     ·     ESC : pausa";

            statusText = CreateText(root, "Status", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(1100f, 44f), 26,
                TextAnchor.MiddleCenter, new Color(1f, 0.25f, 0.85f));
            statusText.gameObject.SetActive(false);

            centerText = CreateText(root, "CenterMessage", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1500f, 460f), 56,
                TextAnchor.MiddleCenter, Color.white);
            centerText.supportRichText = true;

            lowFuelOverlay = CreateImage(root, "LowFuelVignette", Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(1f, 0f, 0.1f, 0f));
            lowFuelOverlay.transform.SetSiblingIndex(2);
        }

        Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = NeonArt.White;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 pos, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.supportRichText = true;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(1f, 0.05f, 0.7f, 0.4f);
            shadow.effectDistance = new Vector2(3f, -3f);

            return txt;
        }

        static Font LoadFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { /* fallback abajo */ }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { /* sin fuente */ }
            }
            return f;
        }
    }
}
