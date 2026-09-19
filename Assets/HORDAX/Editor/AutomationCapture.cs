#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HORDAX.Combat;
using HORDAX.Core;
using HORDAX.Data;
using HORDAX.Enemies;
using HORDAX.Player;
using HORDAX.World;

namespace HORDAX.EditorTools
{
    [InitializeOnLoad]
    public static class AutomationCapture
    {
        private const string ActiveKey = "HORDAX_AUTOMATION_CAPTURE_ACTIVE";
        private const string ExitPendingKey = "HORDAX_AUTOMATION_CAPTURE_EXIT_PENDING";
        private const string OutputKey = "HORDAX_AUTOMATION_CAPTURE_OUTPUT";
        private const string WidthKey = "HORDAX_AUTOMATION_CAPTURE_WIDTH";
        private const string HeightKey = "HORDAX_AUTOMATION_CAPTURE_HEIGHT";
        private const string WarmupKey = "HORDAX_AUTOMATION_CAPTURE_WARMUP";
        private const string FrameKey = "HORDAX_AUTOMATION_CAPTURE_FRAME";

        static AutomationCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void CapturePrototype()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Automation capture must start outside Play Mode.");

            string output = GetArgument("-hordaxCapturePath");
            if (string.IsNullOrWhiteSpace(output))
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
                output = Path.Combine(projectRoot, "Artifacts", "UnityCaptures", "prototype-latest.png");
            }

            output = Path.GetFullPath(output);
            int width = ParseIntArgument("-hordaxCaptureWidth", 1280, 320, 3840);
            int height = ParseIntArgument("-hordaxCaptureHeight", 720, 180, 2160);
            int warmup = ParseIntArgument("-hordaxCaptureWarmupFrames", 120, 1, 1200);

            string directory = Path.GetDirectoryName(output);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            SessionState.SetString(OutputKey, output);
            SessionState.SetInt(WidthKey, width);
            SessionState.SetInt(HeightKey, height);
            SessionState.SetInt(WarmupKey, warmup);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetBool(ExitPendingKey, false);
            SessionState.SetBool(ActiveKey, true);

            PrototypeSceneGenerator.OpenFromMenu();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log(
                $"[HORDAX Automation] Starting visual capture: {width}x{height}, " +
                $"{warmup} warmup frames -> {output}");

            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (SessionState.GetBool(ExitPendingKey, false))
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    SessionState.SetBool(ExitPendingKey, false);
                    Debug.Log("[HORDAX Automation] Visual capture complete.");
                    EditorApplication.Exit(0);
                }
                return;
            }

            if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying)
                return;

            int frame = SessionState.GetInt(FrameKey, 0) + 1;
            SessionState.SetInt(FrameKey, frame);

            int warmup = SessionState.GetInt(WarmupKey, 120);
            if (frame < warmup)
                return;

            try
            {
                CaptureCamera();
                SessionState.SetBool(ActiveKey, false);
                SessionState.SetBool(ExitPendingKey, true);
                EditorApplication.ExitPlaymode();
            }
            catch (Exception error)
            {
                SessionState.SetBool(ActiveKey, false);
                SessionState.SetBool(ExitPendingKey, false);
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureCamera()
        {
            string output = SessionState.GetString(OutputKey, string.Empty);
            int width = SessionState.GetInt(WidthKey, 1280);
            int height = SessionState.GetInt(HeightKey, 720);
            CaptureCameraTo(output, width, height);
        }

        internal static void CaptureCameraTo(string output, int width, int height)
        {
            Camera camera = Camera.main;
            if (camera == null)
                camera = UnityEngine.Object.FindAnyObjectByType<Camera>();

            if (camera == null)
                throw new InvalidOperationException("No camera was available for HORDAX automation capture.");

            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();

                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply(false, false);
                File.WriteAllBytes(output, texture.EncodeToPNG());
                WriteSnapshot(output, camera);

                Debug.Log($"[HORDAX Automation] Screenshot written: {output}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        internal static void WriteSnapshot(string screenshotPath, Camera camera)
        {
            RunnerController player = UnityEngine.Object.FindAnyObjectByType<RunnerController>();
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            WeaponController weapon = player != null ? player.GetComponent<WeaponController>() : null;
            EnemyPool enemyPool = UnityEngine.Object.FindAnyObjectByType<EnemyPool>();
            EnemyAgent boss = EnemyAgent.ActiveBoss;
            EnemyAgent[] enemies = UnityEngine.Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None);
            HordeSpawner[] spawners = UnityEngine.Object.FindObjectsByType<HordeSpawner>(FindObjectsSortMode.None);

            int elites = 0;
            int bosses = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null) continue;
                if (enemies[i].Rank == EnemyRank.Boss) bosses++;
                else if (enemies[i].Rank == EnemyRank.Elite) elites++;
            }

            int activatedSpawners = 0;
            int pendingSpawners = 0;
            int enemiesRemainingToSpawn = 0;
            float currentEncounterZ = float.PositiveInfinity;
            float currentEncounterDistance = float.PositiveInfinity;
            float nextPendingEncounterZ = float.PositiveInfinity;
            float playerZ = player != null ? player.transform.position.z : 0f;

            for (int i = 0; i < spawners.Length; i++)
            {
                HordeSpawner spawner = spawners[i];
                if (spawner == null) continue;

                if (spawner.Activated)
                {
                    activatedSpawners++;
                    enemiesRemainingToSpawn += spawner.RemainingCount;

                    float distance = Mathf.Abs(spawner.EncounterZ - playerZ);
                    if (distance < currentEncounterDistance)
                    {
                        currentEncounterDistance = distance;
                        currentEncounterZ = spawner.EncounterZ;
                    }
                }
                else
                {
                    pendingSpawners++;
                    if (spawner.EncounterZ >= playerZ &&
                        spawner.EncounterZ < nextPendingEncounterZ)
                    {
                        nextPendingEncounterZ = spawner.EncounterZ;
                    }
                }
            }

            bool hasCurrentEncounter = !float.IsPositiveInfinity(currentEncounterZ);
            bool hasNextPendingEncounter = !float.IsPositiveInfinity(nextPendingEncounterZ);
            CaptureSnapshot snapshot = new CaptureSnapshot
            {
                capturedAtUtc = DateTime.UtcNow.ToString("o"),
                revision = "TWO-LANE CORE / PASS 1",
                gameState = GameManager.Instance != null ? GameManager.Instance.State.ToString() : "unknown",
                playerPosition = player != null ? player.transform.position : Vector3.zero,
                cameraPosition = camera != null ? camera.transform.position : Vector3.zero,
                activeEnemies = enemies.Length,
                activeElites = elites,
                activeBosses = bosses,
                enemyBreaches = GameManager.Instance != null ? GameManager.Instance.EnemyBreaches : 0,
                enemyKills = GameManager.Instance != null ? GameManager.Instance.EnemyKills : 0,
                bossKills = GameManager.Instance != null ? GameManager.Instance.BossKills : 0,
                runCoins = GameManager.Instance != null ? GameManager.Instance.RunCoins : 0,
                score = GameManager.Instance != null ? GameManager.Instance.Score : 0,
                bossHealthNormalized = boss != null ? boss.HealthNormalized : 0f,
                weaponName = weapon != null ? weapon.DisplayName : string.Empty,
                weaponLevel = weapon != null ? weapon.UpgradeLevel : 0,
                weaponDamage = weapon != null ? weapon.Damage : 0f,
                weaponFireRate = weapon != null ? weapon.FireRate : 0f,
                weaponRange = weapon != null ? weapon.Range : 0f,
                weaponReservedTargets = weapon != null ? weapon.ReservedTargetCount : 0,
                weaponReservedDamage = weapon != null ? weapon.ReservedDamageTotal : 0f,
                playerHealthNormalized = health != null ? health.Normalized : 0f,
                playerHealthCurrent = health != null ? health.CurrentHealth : 0f,
                playerHealthMax = health != null ? health.MaxHealth : 0f,
                playerForwardSpeed = player != null ? player.ForwardSpeed : 0f,
                activatedSpawners = activatedSpawners,
                pendingSpawners = pendingSpawners,
                enemiesRemainingToSpawn = enemiesRemainingToSpawn,
                currentEncounterZ = hasCurrentEncounter ? currentEncounterZ : -1f,
                nextPendingEncounterZ = hasNextPendingEncounter ? nextPendingEncounterZ : -1f,
                distanceToNextPendingEncounter = hasNextPendingEncounter
                    ? Mathf.Max(0f, nextPendingEncounterZ - playerZ)
                    : -1f,
                enemyPoolCreated = enemyPool != null ? enemyPool.CreatedCount : 0,
                enemyPoolAvailable = enemyPool != null ? enemyPool.AvailableCount : 0,
                arsenalLaneCenterX = TrackLayout.ArsenalCenterX,
                hordeLaneCenterX = TrackLayout.HordeCenterX,
                laneHalfWidth = TrackLayout.LaneHalfWidth
            };

            string jsonPath = Path.ChangeExtension(screenshotPath, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(snapshot, true));
            Debug.Log($"[HORDAX Automation] Snapshot written: {jsonPath}");
        }

        [Serializable]
        private sealed class CaptureSnapshot
        {
            public string capturedAtUtc;
            public string revision;
            public string gameState;
            public Vector3 playerPosition;
            public Vector3 cameraPosition;
            public int activeEnemies;
            public int activeElites;
            public int activeBosses;
            public int enemyBreaches;
            public int enemyKills;
            public int bossKills;
            public int runCoins;
            public int score;
            public float bossHealthNormalized;
            public string weaponName;
            public int weaponLevel;
            public float weaponDamage;
            public float weaponFireRate;
            public float weaponRange;
            public int weaponReservedTargets;
            public float weaponReservedDamage;
            public float playerHealthNormalized;
            public float playerHealthCurrent;
            public float playerHealthMax;
            public float playerForwardSpeed;
            public int activatedSpawners;
            public int pendingSpawners;
            public int enemiesRemainingToSpawn;
            public float currentEncounterZ;
            public float nextPendingEncounterZ;
            public float distanceToNextPendingEncounter;
            public int enemyPoolCreated;
            public int enemyPoolAvailable;
            public float arsenalLaneCenterX;
            public float hordeLaneCenterX;
            public float laneHalfWidth;
        }

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }

        private static int ParseIntArgument(string name, int fallback, int min, int max)
        {
            string raw = GetArgument(name);
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return fallback;

            return Mathf.Clamp(value, min, max);
        }
    }
}
#endif
