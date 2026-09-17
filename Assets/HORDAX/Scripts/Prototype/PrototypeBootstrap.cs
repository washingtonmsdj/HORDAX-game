using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;
using HORDAX.Core;
using HORDAX.Data;
using HORDAX.Enemies;
using HORDAX.Player;
using HORDAX.UI;
using HORDAX.World;

namespace HORDAX.Prototype
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float DefaultFinishZ = 300f;
        [SerializeField] private LevelDefinition levelDefinition;

        private RunnerController player;
        private WeaponController playerWeapon;
        private float finishZ;

        private void Awake()
        {
            if (FindObjectOfType<RunnerController>() != null) return;

            if (GameSession.SelectedLevel != null)
                levelDefinition = GameSession.SelectedLevel;

            finishZ = levelDefinition != null ? Mathf.Max(20f, levelDefinition.Length) : DefaultFinishZ;
            Application.targetFrameRate = 60;

            BuildGameManager();
            BuildLighting();
            BuildRoad();
            BuildPlayer();
            ApplyPermanentProgression();
            BuildEnemyPool();
            BuildCombatFxPool();
            BuildCamera();
            BuildHud();
            BuildLevel();

            GameManager.Instance.FinishZ = finishZ;
            GameManager.Instance.ConfigureLevel(
                levelDefinition != null ? levelDefinition.LevelId : "prototype_level",
                levelDefinition != null ? levelDefinition.CompletionCoins : 100,
                levelDefinition != null ? levelDefinition.CompletionScore : 1000,
                CountRequiredBossKills());
            GameManager.Instance.Begin();
        }

        private void BuildGameManager()
        {
            if (GameManager.Instance != null) return;
            new GameObject("Game Manager").AddComponent<GameManager>();
        }

        private void BuildLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.55f, 0.76f, 0.90f);
            RenderSettings.fogDensity = 0.0085f;
            RenderSettings.ambientLight = new Color(0.62f, 0.67f, 0.72f);

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        private void BuildRoad()
        {
            GameObject world = new GameObject("WORLD - Replace visuals here");
            int roadSegments = Mathf.CeilToInt(finishZ / 20f) + 1;

            for (int i = 0; i < roadSegments; i++)
            {
                float centerZ = i * 20f + 10f;
                CreateBlock("Road", new Vector3(0f, -0.3f, centerZ), new Vector3(12f, 0.6f, 20.2f), PrototypeMaterials.Road, world.transform, true);
                CreateBlock("Left Rail", new Vector3(-6.25f, 0.25f, centerZ), new Vector3(0.35f, 1.1f, 20.2f), PrototypeMaterials.Rail, world.transform, false);
                CreateBlock("Right Rail", new Vector3(6.25f, 0.25f, centerZ), new Vector3(0.35f, 1.1f, 20.2f), PrototypeMaterials.Rail, world.transform, false);
            }

            for (float z = 4f; z < finishZ; z += 8f)
                CreateBlock("Lane Mark", new Vector3(0f, 0.02f, z), new Vector3(0.12f, 0.03f, 2.5f), PrototypeMaterials.Bullet, world.transform, false);

            for (float z = 18f; z < finishZ; z += 24f)
            {
                CreateBlock("Left Background Pillar", new Vector3(-14f, 4f, z), new Vector3(1.6f, 8f, 1.6f), PrototypeMaterials.Rail, world.transform, false);
                CreateBlock("Right Background Pillar", new Vector3(14f, 3f, z + 10f), new Vector3(1.4f, 6f, 1.4f), PrototypeMaterials.Rail, world.transform, false);
            }
        }

        private void BuildPlayer()
        {
            GameObject root = new GameObject("PLAYER");
            root.transform.position = new Vector3(0f, 0.8f, 0f);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.85f, 1.6f, 0.85f);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            player = root.AddComponent<RunnerController>();
            root.AddComponent<PlayerHealth>();
            playerWeapon = root.AddComponent<WeaponController>();

            GameObject visual = CreatePrimitiveWithoutCollider(PrimitiveType.Capsule, "Player Visual", root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
            visual.GetComponent<Renderer>().sharedMaterial = PrototypeMaterials.Player;

            GameObject weaponVisual = CreateBlock("Weapon", root.transform.position + new Vector3(0.45f, 0.35f, 0.55f), new Vector3(0.18f, 0.18f, 1.1f), PrototypeMaterials.Rail, root.transform, false);
            weaponVisual.transform.localPosition = new Vector3(0.45f, 0.35f, 0.55f);

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0.45f, 0.35f, 1.18f);
            playerWeapon.SetMuzzle(muzzle.transform);

            PrototypeWeaponView weaponView = root.AddComponent<PrototypeWeaponView>();
            weaponView.Initialize(playerWeapon, weaponVisual.transform);
        }

        private void ApplyPermanentProgression()
        {
            ProgressionService progression = ProgressionService.GetOrCreate();
            PermanentUpgradeDefinition healthUpgrade = PrototypeUpgradeCatalog.Get(PermanentUpgradeType.MaxHealth);
            PermanentUpgradeDefinition damageUpgrade = PrototypeUpgradeCatalog.Get(PermanentUpgradeType.WeaponDamage);
            PermanentUpgradeDefinition fireRateUpgrade = PrototypeUpgradeCatalog.Get(PermanentUpgradeType.FireRate);

            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (health != null)
                health.SetPermanentHealthMultiplier(progression.GetUpgradeMultiplier(healthUpgrade));

            if (playerWeapon != null)
            {
                playerWeapon.SetPermanentBonuses(
                    progression.GetUpgradeMultiplier(damageUpgrade),
                    progression.GetUpgradeMultiplier(fireRateUpgrade));
            }
        }

        private void BuildEnemyPool()
        {
            if (FindObjectOfType<EnemyPool>() != null) return;
            GameObject poolObject = new GameObject("ENEMY POOL - Replace enemy prefabs later");
            poolObject.AddComponent<EnemyPool>();
        }

        private void BuildCombatFxPool()
        {
            if (CombatFxPool.Instance != null) return;
            new GameObject("COMBAT FX POOL - Placeholder effects").AddComponent<CombatFxPool>();
        }

        private void BuildCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 420f;
            RunnerCamera follow = cameraObject.AddComponent<RunnerCamera>();
            follow.SetTarget(player.transform);
        }

        private void BuildHud()
        {
            GameObject hud = new GameObject("HUD");
            HudController controller = hud.AddComponent<HudController>();
            controller.Initialize(player);
        }

        private void BuildLevel()
        {
            if (levelDefinition != null && levelDefinition.Steps.Count > 0)
            {
                bool hasFinish = false;
                for (int i = 0; i < levelDefinition.Steps.Count; i++)
                {
                    LevelStep step = levelDefinition.Steps[i];
                    if (step == null) continue;

                    switch (step.type)
                    {
                        case LevelStepType.Horde:
                            CreateHorde(
                                step.label, step.z, step.enemyCount, step.columns,
                                step.enemyHealth, step.enemySpeed, step.enemyDamage,
                                step.enemyData, step.enemyRank, step.coinReward, step.scoreReward, step.scaleMultiplier);
                            break;

                        case LevelStepType.Elite:
                            CreateHorde(
                                step.label, step.z, Mathf.Clamp(step.enemyCount, 1, 8), Mathf.Clamp(step.columns, 1, 4),
                                step.enemyData != null ? step.enemyHealth : Mathf.Max(step.enemyHealth, 22f),
                                step.enemySpeed,
                                step.enemyData != null ? step.enemyDamage : Mathf.Max(step.enemyDamage, 12f),
                                step.enemyData, EnemyRank.Elite,
                                Mathf.Max(step.coinReward, 8),
                                Mathf.Max(step.scoreReward, 80),
                                Mathf.Max(step.scaleMultiplier, 1.35f));
                            break;

                        case LevelStepType.Boss:
                            CreateHorde(
                                step.label, step.z, 1, 1,
                                step.enemyData != null ? step.enemyHealth : Mathf.Max(step.enemyHealth, 350f),
                                step.enemyData != null ? step.enemySpeed : Mathf.Max(2.2f, step.enemySpeed),
                                step.enemyData != null ? step.enemyDamage : Mathf.Max(step.enemyDamage, 22f),
                                step.enemyData, EnemyRank.Boss,
                                Mathf.Max(step.coinReward, 100),
                                Mathf.Max(step.scoreReward, 1200),
                                Mathf.Max(step.scaleMultiplier, 2.25f));
                            break;

                        case LevelStepType.Gate:
                            CreateGate(step.label, step.z, step.gateHitPoints);
                            break;

                        case LevelStepType.Upgrade:
                            CreateUpgrade(step.label, step.z, step.damageAdd, step.fireRateMultiplier, step.upgradeLabel);
                            break;

                        case LevelStepType.Weapon:
                            CreateWeaponPickup(step.label, step.z, step.weaponData, step.prototypeWeapon, step.weaponLabel);
                            break;

                        case LevelStepType.Finish:
                            CreateFinish(step.z);
                            hasFinish = true;
                            break;
                    }
                }

                if (!hasFinish) CreateFinish(finishZ);
                return;
            }

            BuildDefaultLevel();
        }

        private int CountRequiredBossKills()
        {
            if (levelDefinition == null || levelDefinition.Steps.Count == 0)
                return 1;

            int required = 0;
            for (int i = 0; i < levelDefinition.Steps.Count; i++)
            {
                LevelStep step = levelDefinition.Steps[i];
                if (step == null) continue;

                if (step.type == LevelStepType.Boss)
                {
                    required++;
                    continue;
                }

                if (step.enemyData != null && step.enemyData.Rank == EnemyRank.Boss)
                    required += step.type == LevelStepType.Horde ? Mathf.Max(1, step.enemyCount) : 1;
            }

            return required;
        }

        private void BuildDefaultLevel()
        {
            CreateHorde("Wave 01", 30f, 30, 6, 5f, 3.1f, 7f);
            CreateGate("Gate 50", 54f, 50f);
            CreateWeaponPickup("SMG Pickup", 62f, null, WeaponArchetype.SMG, "SMG");

            CreateHorde("Wave 02", 88f, 48, 8, 8f, 3.45f, 8f);
            CreateHorde("Elite Squad", 116f, 4, 4, 26f, 3.8f, 12f, null, EnemyRank.Elite, 8, 90, 1.35f);
            CreateGate("Gate 120", 134f, 120f);
            CreateUpgrade("Upgrade 01", 141f, 3f, 1.12f, "+POWER");

            CreateHorde("Wave 03", 166f, 72, 9, 11f, 3.8f, 9f);
            CreateGate("Gate 230", 196f, 230f);
            CreateWeaponPickup("Shotgun Pickup", 203f, null, WeaponArchetype.Shotgun, "SHOTGUN");

            CreateHorde("Wave 04", 228f, 96, 10, 14f, 4.0f, 10f);
            CreateWeaponPickup("Minigun Pickup", 252f, null, WeaponArchetype.Minigun, "MINIGUN");
            CreateHorde("BLOCK BOSS", 276f, 1, 1, 450f, 2.6f, 25f, null, EnemyRank.Boss, 125, 1500, 2.5f);

            CreateFinish(finishZ);
        }

        private void CreateHorde(
            string label,
            float z,
            int count,
            int columns,
            float health,
            float speed,
            float damage,
            EnemyData data = null,
            EnemyRank rank = EnemyRank.Grunt,
            int coinReward = 1,
            int scoreReward = 10,
            float scaleMultiplier = 1f)
        {
            GameObject spawner = new GameObject(string.IsNullOrWhiteSpace(label) ? "Horde" : label);
            spawner.transform.position = new Vector3(0f, 0f, z);
            HordeSpawner horde = spawner.AddComponent<HordeSpawner>();
            horde.Configure(player, count, columns, health, speed, damage, data, rank, coinReward, scoreReward, scaleMultiplier);
        }

        private void CreateGate(string label, float z, float hitPoints)
        {
            GameObject root = new GameObject(string.IsNullOrWhiteSpace(label) ? "Gate" : label);
            root.transform.position = new Vector3(0f, 1.55f, z);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(10.5f, 3.1f, 1.2f);

            GameObject visual = CreateBlock("Gate Visual", root.transform.position, new Vector3(10.5f, 3.1f, 1.0f), PrototypeMaterials.Gate, root.transform, false);
            visual.transform.localPosition = Vector3.zero;

            GameObject textObject = new GameObject("HP Label");
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.56f);
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 90;
            text.characterSize = 0.08f;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;

            DamageGate gate = root.AddComponent<DamageGate>();
            gate.Initialize(hitPoints, text);
        }

        private void CreateUpgrade(string label, float z, float damage, float cadence, string displayText)
        {
            GameObject root = new GameObject(string.IsNullOrWhiteSpace(label) ? "Upgrade" : label);
            root.transform.position = new Vector3(0f, 0.9f, z);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.2f, 2.2f, 2.2f);

            GameObject visual = CreatePrimitiveWithoutCollider(PrimitiveType.Cylinder, "Upgrade Visual", root.transform);
            visual.transform.localScale = new Vector3(0.75f, 0.18f, 0.75f);
            visual.GetComponent<Renderer>().sharedMaterial = PrototypeMaterials.Pickup;

            GameObject top = CreateBlock("Upgrade Icon", root.transform.position, new Vector3(0.24f, 0.9f, 0.24f), PrototypeMaterials.Bullet, root.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            CreateWorldLabel(root.transform, string.IsNullOrWhiteSpace(displayText) ? "+POWER" : displayText, new Vector3(0f, 1.45f, 0f), 64, 0.055f);

            UpgradePickup pickup = root.AddComponent<UpgradePickup>();
            pickup.Configure(damage, cadence);
        }

        private void CreateWeaponPickup(string label, float z, WeaponData data, WeaponArchetype fallback, string displayText)
        {
            GameObject root = new GameObject(string.IsNullOrWhiteSpace(label) ? "Weapon Pickup" : label);
            root.transform.position = new Vector3(0f, 0.95f, z);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.6f, 2.5f, 2.6f);

            GameObject pedestal = CreatePrimitiveWithoutCollider(PrimitiveType.Cylinder, "Weapon Pedestal", root.transform);
            pedestal.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            pedestal.transform.localScale = new Vector3(0.95f, 0.12f, 0.95f);
            pedestal.GetComponent<Renderer>().sharedMaterial = PrototypeMaterials.Pickup;

            GameObject visualRoot = new GameObject("Weapon Visual");
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            WeaponArchetype type = data != null ? data.Archetype : fallback;
            Vector3 bodyScale = type == WeaponArchetype.Shotgun
                ? new Vector3(0.32f, 0.28f, 1.55f)
                : type == WeaponArchetype.Minigun
                    ? new Vector3(0.48f, 0.38f, 1.25f)
                    : type == WeaponArchetype.SMG
                        ? new Vector3(0.38f, 0.32f, 0.92f)
                        : new Vector3(0.30f, 0.28f, 1.25f);

            GameObject body = CreateBlock("Weapon Body", root.transform.position, bodyScale, PrototypeMaterials.Rail, visualRoot.transform, false);
            body.transform.localPosition = Vector3.zero;
            GameObject barrel = CreateBlock("Weapon Barrel", root.transform.position, new Vector3(0.13f, 0.13f, bodyScale.z * 0.85f), PrototypeMaterials.Bullet, visualRoot.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0f, bodyScale.z * 0.75f);

            string shownName = !string.IsNullOrWhiteSpace(displayText)
                ? displayText
                : data != null && !string.IsNullOrWhiteSpace(data.DisplayName) ? data.DisplayName : fallback.ToString();

            if (data != null && data.Rarity != WeaponRarity.Common)
                shownName = $"{shownName} [{data.Rarity}]";

            CreateWorldLabel(root.transform, shownName, new Vector3(0f, 1.55f, 0f), 60, 0.052f);

            WeaponPickup pickup = root.AddComponent<WeaponPickup>();
            pickup.Configure(data, fallback, visualRoot.transform);
        }

        private void CreateFinish(float z)
        {
            GameObject root = new GameObject("FINISH");
            root.transform.position = new Vector3(0f, 0f, z);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            trigger.size = new Vector3(11.5f, 3f, 1.2f);
            root.AddComponent<FinishZone>();

            CreateBlock("Finish Left", new Vector3(-5.2f, 1.8f, z), new Vector3(0.55f, 3.6f, 0.55f), PrototypeMaterials.Finish, root.transform, false).transform.localPosition = new Vector3(-5.2f, 1.8f, 0f);
            CreateBlock("Finish Right", new Vector3(5.2f, 1.8f, z), new Vector3(0.55f, 3.6f, 0.55f), PrototypeMaterials.Finish, root.transform, false).transform.localPosition = new Vector3(5.2f, 1.8f, 0f);
            CreateBlock("Finish Top", new Vector3(0f, 3.35f, z), new Vector3(10.9f, 0.55f, 0.55f), PrototypeMaterials.Finish, root.transform, false).transform.localPosition = new Vector3(0f, 3.35f, 0f);
        }

        private static TextMesh CreateWorldLabel(Transform parent, string value, Vector3 localPosition, int fontSize, float characterSize)
        {
            GameObject textObject = new GameObject("World Label");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = fontSize;
            text.characterSize = characterSize;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static GameObject CreateBlock(string label, Vector3 position, Vector3 scale, Material material, Transform parent, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = scale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            if (!keepCollider)
            {
                Collider collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }

            return go;
        }

        private static GameObject CreatePrimitiveWithoutCollider(PrimitiveType type, string label, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = label;
            go.transform.SetParent(parent, false);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return go;
        }
    }
}
