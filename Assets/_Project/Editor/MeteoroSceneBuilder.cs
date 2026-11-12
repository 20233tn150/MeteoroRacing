using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MeteoroRacing.EditorTools
{
    /// <summary>
    /// Genera con un clic la escena jugable completa (cámara, pista, Mach 5, spawner y HUD).
    /// Menú: Meteoro Racing > Construir escena jugable.
    /// </summary>
    public static class MeteoroSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/MeteoroRace.unity";
        const string ScenesFolder = "Assets/_Project/Scenes";
        const string PrefabsFolder = "Assets/_Project/Prefabs";

        [MenuItem("Meteoro Racing/Construir escena jugable")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Meteoro Racing",
                    "Sal del modo Play antes de construir la escena.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------------------------------------------------------- cámara
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraGO.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = RaceLayout.CameraHalfHeight;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.005f, 0.03f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraGO.AddComponent<AudioListener>();
            cameraGO.AddComponent<CameraShake>();

            // Luz global para sprites lit de URP 2D (inofensiva si son unlit).
            var lightGO = new GameObject("Global Light 2D");
            Light2D light = lightGO.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = Color.white;
            light.intensity = 1f;

            // ---------------------------------------------------------- sistemas
            new GameObject("GameManager").AddComponent<GameManager>();

            var trackGO = new GameObject("Track");
            trackGO.AddComponent<TrackScroller>();

            var playerGO = new GameObject("Mach5");
            playerGO.transform.position = new Vector3(RaceLayout.LaneX(2), RaceLayout.PlayerY, 0f);

            Rigidbody2D rb = playerGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D collider = playerGO.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.82f, 1.7f);
            collider.isTrigger = true;

            playerGO.AddComponent<Mach5Controller>();

            new GameObject("Spawner").AddComponent<HazardAndEnemySpawner>();
            new GameObject("HUD").AddComponent<HUDController>();

            // ---------------------------------------------------------- guardado
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.Refresh();

            Selection.activeGameObject = playerGO;

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Meteoro Racing",
                    "Escena creada en:\n" + ScenePath +
                    "\n\nPulsa Play.\n\nControles: A/D o flechas, ESPACIO (Auto-Jacks), ESC (pausa).",
                    "¡A correr!");
            }

            Debug.Log("[Meteoro Racing] Escena jugable generada: " + ScenePath);
        }

        [MenuItem("Meteoro Racing/Abrir escena jugable")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Meteoro Racing",
                    "Todavía no existe la escena. Usa 'Construir escena jugable'.", "OK");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>Punto de entrada para compilar/generar en batch mode.</summary>
        public static void BuildFromBatch()
        {
            BuildScene();
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(ScenesFolder);
            Directory.CreateDirectory(PrefabsFolder);
            AssetDatabase.Refresh();
        }

        static void AddToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
