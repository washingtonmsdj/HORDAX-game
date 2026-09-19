#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HORDAX.Benchmarks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HORDAX.EditorTools
{
    public static class IslandReferenceBenchmark
    {
        private const string Root = "Assets/HORDAX/Generated/IslandReferenceBenchmark";
        private const string ScenePath = "Assets/HORDAX/Scenes/Benchmarks/IslandReferenceBenchmark.unity";
        private const string ArtifactDirectory = "Artifacts/Unity/IslandReferenceBenchmark";
        private const int PreviewWidth = 1024;
        private const int PreviewHeight = 1024;

        private static Material waterMat;
        private static Material shallowMat;
        private static Material sandMat;
        private static Material grassMat;
        private static Material trunkMat;
        private static Material foliageMat;
        private static Material rockMat;

        [Serializable]
        private sealed class BenchmarkReport
        {
            public string generatedAtUtc;
            public string scenePath;
            public string reference;
            public string sourceMode;
            public string renderPipeline;
            public int gameObjects;
            public int islands;
            public int trees;
            public int rocks;
            public int materials;
            public int cameras;
            public string[] features;
        }

        [MenuItem("HORDAX/Benchmarks/Generate Island Reference")]
        public static void Generate()
        {
            EnsureFolders();
            CreateMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "IslandReferenceBenchmark";

            GameObject root = new GameObject("Island Reference Benchmark");

            CreateWater(root.transform);
            CreateShallows(root.transform, -5.9f);
            CreateShallows(root.transform, 5.9f);

            int treeCount = 0;
            int rockCount = 0;
            CreateIsland(root.transform, -5.7f, -1f, 1837, ref treeCount, ref rockCount);
            CreateIsland(root.transform, 5.7f, 1f, 9721, ref treeCount, ref rockCount);

            CreateLighting(root.transform);
            Camera camera = CreateCamera(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RenderPreview(camera);
            WriteReport(scene, treeCount, rockCount);

            Selection.activeGameObject = camera.gameObject;
            Debug.Log($"OrdaX island benchmark generated: {ScenePath}");
        }

        private static void EnsureFolders()
        {
            EnsureAssetFolder("Assets/HORDAX/Generated");
            EnsureAssetFolder(Root);
            EnsureAssetFolder(Root + "/Materials");
            EnsureAssetFolder(Root + "/Meshes");
            EnsureAssetFolder("Assets/HORDAX/Scenes");
            EnsureAssetFolder("Assets/HORDAX/Scenes/Benchmarks");

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            Directory.CreateDirectory(Path.Combine(projectRoot, ArtifactDirectory));
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }

        private static Material GetMaterial(string name, Color color, float smoothness, bool transparent = false)
        {
            string path = $"{Root}/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                    throw new InvalidOperationException("Unity Standard shader is unavailable.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", 0.02f);

            if (transparent)
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            else
            {
                material.SetFloat("_Mode", 0f);
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateMaterials()
        {
            waterMat = GetMaterial("WaterDeep", new Color(0.015f, 0.38f, 0.49f, 1f), 0.9f);
            shallowMat = GetMaterial("WaterShallow", new Color(0.16f, 0.83f, 0.80f, 0.42f), 0.82f, true);
            sandMat = GetMaterial("SandIvory", new Color(0.93f, 0.86f, 0.67f, 1f), 0.22f);
            grassMat = GetMaterial("IslandGreen", new Color(0.18f, 0.38f, 0.12f, 1f), 0.18f);
            trunkMat = GetMaterial("TreeTrunk", new Color(0.19f, 0.10f, 0.045f, 1f), 0.18f);
            foliageMat = GetMaterial("TreeFoliage", new Color(0.10f, 0.31f, 0.08f, 1f), 0.12f);
            rockMat = GetMaterial("CoastalRock", new Color(0.25f, 0.28f, 0.24f, 1f), 0.32f);
        }

        private static void CreateWater(Transform parent)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Deep Turquoise Water";
            water.transform.SetParent(parent, false);
            water.transform.position = new Vector3(0f, -0.12f, 0f);
            water.transform.localScale = new Vector3(6.5f, 1f, 6.5f);
            water.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
            UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
        }

        private static void CreateShallows(Transform parent, float centerX)
        {
            GameObject shallowRoot = new GameObject(centerX < 0 ? "Left Shallows" : "Right Shallows");
            shallowRoot.transform.SetParent(parent, false);

            Vector3[] offsets =
            {
                new Vector3(0f, 0.018f, 0f),
                new Vector3(centerX < 0 ? 1.8f : -1.8f, 0.022f, 1.8f),
                new Vector3(centerX < 0 ? 1.4f : -1.4f, 0.024f, -2.2f),
            };

            Vector3[] scales =
            {
                new Vector3(8.2f, 0.025f, 10.2f),
                new Vector3(5.2f, 0.022f, 5.7f),
                new Vector3(4.5f, 0.020f, 4.8f),
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = $"Shallow Halo {i + 1}";
                disc.transform.SetParent(shallowRoot.transform, false);
                disc.transform.position = new Vector3(centerX + offsets[i].x, offsets[i].y - 0.08f, offsets[i].z);
                disc.transform.localScale = scales[i];
                disc.GetComponent<MeshRenderer>().sharedMaterial = shallowMat;
                UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
            }
        }

        private static float IslandField(float x, float z, float sideSign)
        {
            float u = x / 5.8f;
            float v = z / 8.1f;

            float ellipse = 1f - (u * u + v * v);
            float innerU = -0.68f * sideSign;
            float notch = Mathf.Exp(
                -Mathf.Pow((u - innerU) / 0.34f, 2f)
                -Mathf.Pow((v - 0.02f) / 0.68f, 2f));

            float upperLobe = 0.20f * Mathf.Exp(-Mathf.Pow((u + 0.10f * sideSign) / 0.45f, 2f)
                                                -Mathf.Pow((v - 0.60f) / 0.31f, 2f));
            float lowerLobe = 0.16f * Mathf.Exp(-Mathf.Pow((u - 0.08f * sideSign) / 0.50f, 2f)
                                                -Mathf.Pow((v + 0.62f) / 0.34f, 2f));

            float edgeNoise = 0.035f * Mathf.Sin((u * 8.7f + v * 5.1f) * Mathf.PI)
                              + 0.025f * Mathf.Sin((u * 13.1f - v * 7.3f) * Mathf.PI);
            return ellipse - notch * 0.88f + upperLobe + lowerLobe + edgeNoise;
        }

        private static float IslandHeight(float x, float z, float sideSign)
        {
            float field = IslandField(x, z, sideSign);
            if (field <= 0.015f)
                return -0.08f;

            float n = Mathf.Clamp01(field);
            float micro = (Mathf.Sin(x * 2.8f + z * 1.7f) + Mathf.Sin(x * 4.3f - z * 2.2f)) * 0.035f;
            return 0.055f + Mathf.Pow(n, 1.45f) * 1.45f + micro * n;
        }

        private static void CreateIsland(
            Transform parent,
            float centerX,
            float sideSign,
            int seed,
            ref int treeCount,
            ref int rockCount)
        {
            const int resolutionX = 70;
            const int resolutionZ = 86;
            const float width = 13.2f;
            const float length = 18.5f;

            var vertices = new List<Vector3>(resolutionX * resolutionZ);
            var uv = new List<Vector2>(resolutionX * resolutionZ);
            var sandTriangles = new List<int>();
            var grassTriangles = new List<int>();

            for (int z = 0; z < resolutionZ; z++)
            {
                float nz = z / (float)(resolutionZ - 1);
                float localZ = Mathf.Lerp(-length * 0.5f, length * 0.5f, nz);
                for (int x = 0; x < resolutionX; x++)
                {
                    float nx = x / (float)(resolutionX - 1);
                    float localX = Mathf.Lerp(-width * 0.5f, width * 0.5f, nx);
                    float height = IslandHeight(localX, localZ, sideSign);
                    vertices.Add(new Vector3(localX, height, localZ));
                    uv.Add(new Vector2(nx, nz));
                }
            }

            for (int z = 0; z < resolutionZ - 1; z++)
            {
                for (int x = 0; x < resolutionX - 1; x++)
                {
                    int a = z * resolutionX + x;
                    int b = a + 1;
                    int c = a + resolutionX + 1;
                    int d = a + resolutionX;

                    float avg = (vertices[a].y + vertices[b].y + vertices[c].y + vertices[d].y) * 0.25f;
                    if (avg <= 0.025f)
                        continue;

                    List<int> target = avg < 0.34f ? sandTriangles : grassTriangles;
                    target.Add(a); target.Add(d); target.Add(c);
                    target.Add(a); target.Add(c); target.Add(b);
                }
            }

            Mesh mesh = new Mesh { name = sideSign < 0 ? "LeftIslandMesh" : "RightIslandMesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(sandTriangles, 0);
            mesh.SetTriangles(grassTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            string meshPath = $"{Root}/Meshes/{mesh.name}.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);

            GameObject island = new GameObject(sideSign < 0 ? "Left Crescent Island" : "Right Crescent Island");
            island.transform.SetParent(parent, false);
            island.transform.position = new Vector3(centerX, 0f, 0f);

            MeshFilter filter = island.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = island.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { sandMat, grassMat };
            MeshCollider collider = island.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            ScatterTrees(island.transform, centerX, sideSign, seed, ref treeCount);
            ScatterRocks(island.transform, centerX, sideSign, seed + 77, ref rockCount);
        }

        private static void ScatterTrees(Transform islandRoot, float centerX, float sideSign, int seed, ref int total)
        {
            System.Random rng = new System.Random(seed);
            GameObject trees = new GameObject("Vegetation");
            trees.transform.SetParent(islandRoot, false);

            int created = 0;
            for (int attempt = 0; attempt < 900 && created < 42; attempt++)
            {
                float localX = Mathf.Lerp(-5.2f, 5.2f, (float)rng.NextDouble());
                float localZ = Mathf.Lerp(-7.2f, 7.2f, (float)rng.NextDouble());
                float y = IslandHeight(localX, localZ, sideSign);
                if (y < 0.38f)
                    continue;

                float outerBias = Mathf.InverseLerp(-0.45f, 0.75f, localX * sideSign);
                if ((float)rng.NextDouble() > 0.25f + outerBias * 0.75f)
                    continue;

                float scale = Mathf.Lerp(0.72f, 1.25f, (float)rng.NextDouble());
                CreateTree(trees.transform, new Vector3(localX, y, localZ), scale, (float)rng.NextDouble() * 360f);
                created++;
                total++;
            }
        }

        private static void CreateTree(Transform parent, Vector3 position, float scale, float yaw)
        {
            GameObject root = new GameObject("Tropical Tree");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            root.transform.localScale = Vector3.one * scale;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            trunk.transform.localScale = new Vector3(0.16f, 0.72f, 0.16f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMat;
            UnityEngine.Object.DestroyImmediate(trunk.GetComponent<Collider>());

            Vector3[] crownOffsets =
            {
                new Vector3(0f, 1.75f, 0f),
                new Vector3(0.42f, 1.62f, 0.18f),
                new Vector3(-0.36f, 1.60f, -0.16f),
            };
            Vector3[] crownScales =
            {
                new Vector3(0.72f, 0.58f, 0.72f),
                new Vector3(0.55f, 0.44f, 0.55f),
                new Vector3(0.52f, 0.46f, 0.52f),
            };
            for (int i = 0; i < crownOffsets.Length; i++)
            {
                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Foliage";
                crown.transform.SetParent(root.transform, false);
                crown.transform.localPosition = crownOffsets[i];
                crown.transform.localScale = crownScales[i];
                crown.GetComponent<MeshRenderer>().sharedMaterial = foliageMat;
                UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());
            }
        }

        private static void ScatterRocks(Transform islandRoot, float centerX, float sideSign, int seed, ref int total)
        {
            System.Random rng = new System.Random(seed);
            GameObject rocks = new GameObject("Coastal Rocks");
            rocks.transform.SetParent(islandRoot, false);

            int created = 0;
            for (int attempt = 0; attempt < 600 && created < 18; attempt++)
            {
                float localX = Mathf.Lerp(-5.8f, 5.8f, (float)rng.NextDouble());
                float localZ = Mathf.Lerp(-8.0f, 8.0f, (float)rng.NextDouble());
                float y = IslandHeight(localX, localZ, sideSign);
                if (y < 0.08f || y > 0.48f)
                    continue;

                float outerBias = localX * sideSign;
                if (outerBias < -0.2f)
                    continue;

                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Coastal Rock";
                rock.transform.SetParent(rocks.transform, false);
                rock.transform.localPosition = new Vector3(localX, y + 0.08f, localZ);
                rock.transform.localRotation = Quaternion.Euler(\n                    Mathf.Lerp(-22f, 22f, (float)rng.NextDouble()),\n                    Mathf.Lerp(0f, 360f, (float)rng.NextDouble()),\n                    Mathf.Lerp(-22f, 22f, (float)rng.NextDouble()));
                float s = Mathf.Lerp(0.18f, 0.52f, (float)rng.NextDouble());
                rock.transform.localScale = new Vector3(s * 1.5f, s * 0.75f, s);
                rock.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
                UnityEngine.Object.DestroyImmediate(rock.GetComponent<Collider>());
                created++;
                total++;
            }
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject sun = new GameObject("Warm Sun");
            sun.transform.SetParent(parent, false);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.82f);
            light.intensity = 1.18f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.60f, 0.62f);
            RenderSettings.fog = false;
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Reference Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(0f, 31f, -10.5f);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.5f, 0f) - cameraObject.transform.position, Vector3.up);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 150f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.38f, 0.49f);
            camera.allowHDR = true;

            cameraObject.AddComponent<BenchmarkFlyCamera>();
            return camera;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != scenePath))
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void RenderPreview(Camera camera)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, ArtifactDirectory, "island-reference.png");
            RenderTexture target = new RenderTexture(PreviewWidth, PreviewHeight, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture = new Texture2D(PreviewWidth, PreviewHeight, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, PreviewWidth, PreviewHeight), 0, 0);
                texture.Apply();
                File.WriteAllBytes(output, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void WriteReport(Scene scene, int treeCount, int rockCount)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, ArtifactDirectory, "island-reference.json");
            GameObject[] sceneObjects = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .Distinct()
                .ToArray();

            var pipeline = GraphicsSettings.currentRenderPipeline;
            BenchmarkReport report = new BenchmarkReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                scenePath = ScenePath,
                reference = "two mirrored tropical crescent islands in turquoise water",
                sourceMode = "procedural-no-external-asset-pack",
                renderPipeline = pipeline != null ? pipeline.GetType().Name : "Built-in",
                gameObjects = sceneObjects.Length,
                islands = 2,
                trees = treeCount,
                rocks = rockCount,
                materials = 7,
                cameras = sceneObjects.Count(go => go.GetComponent<Camera>() != null),
                features = new[]
                {
                    "two separated crescent islands",
                    "central turquoise channel",
                    "ivory sand shoreline",
                    "dense outer-island vegetation",
                    "coastal rocks",
                    "shallow-water halos",
                    "reference camera",
                    "WASD/QE explorable camera"
                }
            };
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
        }
    }
}
#endif
