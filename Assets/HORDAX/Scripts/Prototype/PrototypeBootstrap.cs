using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;
using HORDAX.Core;
using HORDAX.Enemies;
using HORDAX.Player;
using HORDAX.UI;
using HORDAX.World;

namespace HORDAX.Prototype
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float RoadHalfWidth = 6f;
        private const float FinishZ = 185f;
        private RunnerController player;

        private void Awake()
        {
            if (FindObjectOfType<RunnerController>() != null) return;

            Application.targetFrameRate = 60;
            BuildGameManager();
            BuildLighting();
            BuildRoad();
            BuildPlayer();
            BuildEnemyPool();
            BuildCamera();
            BuildHud();
            BuildLevel();

            GameManager.Instance.FinishZ = FinishZ;
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

            for (int i = 0; i < 10; i++)
            {
                float centerZ = i * 20f + 10f;
                CreateBlock("Road", new Vector3(0f, -0.3f, centerZ), new Vector3(12f, 0.6f, 20.2f), PrototypeMaterials.Road, world.transform, true);
                CreateBlock("Left Rail", new Vector3(-6.25f, 0.25f, centerZ), new Vector3(0.35f, 1.1f, 20.2f), PrototypeMaterials.Rail, world.transform, false);
                CreateBlock("Right Rail", new Vector3(6.25f, 0.25f, centerZ), new Vector3(0.35f, 1.1f, 20.2f), PrototypeMaterials.Rail, world.transform, false);
            }

            for (float z = 4f; z < FinishZ; z += 8f)
                CreateBlock("Lane Mark", new Vector3(0f, 0.02f, z), new Vector3(0.12f, 0.03f, 2.5f), PrototypeMaterials.Bullet, world.transform, false);

            for (float z = 18f; z < FinishZ; z += 24f)
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
            WeaponController weapon = root.AddComponent<WeaponController>();

            GameObject visual = CreatePrimitiveWithoutCollider(PrimitiveType.Capsule, "Player Visual", root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
            visual.GetComponent<Renderer>().sharedMaterial = PrototypeMaterials.Player;

            GameObject weaponVisual = CreateBlock("Weapon", root.transform.position + new Vector3(0.45f, 0.35f, 0.55f), new Vector3(0.18f, 0.18f, 1.1f), PrototypeMaterials.Rail, root.transform, false);
            weaponVisual.transform.localPosition = new Vector3(0.45f, 0.35f, 0.55f);

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0.45f, 0.35f, 1.18f);
            weapon.SetMuzzle(muzzle.transform);
        }

        private void BuildEnemyPool()
        {
            GameObject poolObject = new GameObject("ENEMY POOL - Replace enemy prefab later");
            poolObject.AddComponent<EnemyPool>();
        }

        private void BuildCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 350f;
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
            CreateHorde("Wave 01", 30f, 30, 6, 5f, 3.1f, 7f);
            CreateGate("Gate 50", 54f, 50f);
            CreateUpgrade("Upgrade 01", 60f, 3f, 1.12f, "+DMG");

            CreateHorde("Wave 02", 82f, 48, 8, 8f, 3.45f, 8f);
            CreateGate("Gate 120", 108f, 120f);
            CreateUpgrade("Upgrade 02", 114f, 4f, 1.15f, "+FIRE");

            CreateHorde("Wave 03", 136f, 72, 9, 11f, 3.8f, 9f);
            CreateGate("Gate 230", 165f, 230f);
            CreateUpgrade("Upgrade 03", 171f, 5f, 1.15f, "POWER");

            CreateFinish(FinishZ);
        }

        private void CreateHorde(string label, float z, int count, int columns, float health, float speed, float damage)
        {
            GameObject spawner = new GameObject(label);
            spawner.transform.position = new Vector3(0f, 0f, z);
            HordeSpawner horde = spawner.AddComponent<HordeSpawner>();
            horde.Configure(player, count, columns, health, speed, damage);
        }

        private void CreateGate(string label, float z, float hitPoints)
        {
            GameObject root = new GameObject(label);
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
            GameObject root = new GameObject(label);
            root.transform.position = new Vector3(0f, 0.9f, z);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.2f, 2.2f, 2.2f);

            GameObject visual = CreatePrimitiveWithoutCollider(PrimitiveType.Cylinder, "Upgrade Visual", root.transform);
            visual.transform.localScale = new Vector3(0.75f, 0.18f, 0.75f);
            visual.GetComponent<Renderer>().sharedMaterial = PrototypeMaterials.Pickup;

            GameObject top = CreateBlock("Upgrade Icon", Vector3.zero, new Vector3(0.24f, 0.9f, 0.24f), PrototypeMaterials.Bullet, root.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            GameObject textObject = new GameObject("Upgrade Label");
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = 0.055f;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.text = displayText;

            UpgradePickup pickup = root.AddComponent<UpgradePickup>();
            pickup.Configure(damage, cadence);
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
