using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RippedMapBuilder
{
    private const string RootName = "RIPPED_Map";
    private const string OldCourseName = "MovementTestArea";
    private const string MaterialFolder = "Assets/Materials/RippedMap";
    private const int TargetRockCount = 18;

    private static readonly Vector3 SpawnPosition = new Vector3(0f, 1.02f, -4f);
    private static readonly Vector3 ShopPosition = new Vector3(-4f, 1f, 6f);
    private static readonly Vector3 ComputerPosition = new Vector3(5.5f, 1f, 6f);

    private static Transform root;
    private static int groundLayer;
    private static System.Random random;

    [MenuItem("RIPPED/6 - Build Map In Open Scene")]
    public static void BuildFromMenu()
    {
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void Build()
    {
        random = new System.Random(1337);

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            Debug.LogWarning("[RippedMap] Layer 'Ground' not found, using Default. Run RIPPED > 1 - Setup Layers And Settings first.");
            groundLayer = 0;
        }

        GameObject oldCourse = GameObject.Find(OldCourseName);
        if (oldCourse != null)
        {
            oldCourse.SetActive(false);
        }

        GameObject rootObject = new GameObject(RootName);
        root = rootObject.transform;

        Material sand = Mat("Sand", "#d9b77a", 0f);
        Material[] pads = { Mat("PadA", "#b98b52", 0f), Mat("PadB", "#9a7a8a", 0f), Mat("PadC", "#7a9a80", 0f) };
        Material cliffA = Mat("CliffA", "#8a5a44", 0f);
        Material cliffB = Mat("CliffB", "#6f4a3d", 0f);
        Material band = Mat("Band", "#d98c4a", 0f);
        Material plaza = Mat("Plaza", "#3f4a66", 0f);
        Material step = Mat("Step", "#566086", 0f);
        Material trim = Mat("Trim", "#ffb02e", 0.6f);
        Material post = Mat("Post", "#2b2f3a", 0f);
        Material lamp = Mat("Lamp", "#ffd37a", 2f);
        Material backdrop = Mat("Backdrop", "#2b3350", 0f);
        Material[] mesaMats = { Mat("MesaA", "#c86b4a", 0f), Mat("MesaB", "#a9565a", 0f), Mat("MesaC", "#d08a53", 0f) };
        Material boulder = Mat("Boulder", "#9b6b50", 0f);
        Material deskWood = Mat("DeskWood", "#7a4a2e", 0f);
        Material deskTop = Mat("DeskTop", "#e8d9b5", 0f);
        Material keyboard = Mat("Keyboard", "#2b2f3a", 0f);

        Box("Floor", root, new Vector3(0f, -0.5f, 0f), new Vector3(120f, 1f, 120f), 0f, sand, true);

        Vector3[] zoneCenters = new Vector3[3];
        for (int z = 0; z < 3; z++)
        {
            float angle = (30f + 120f * z) * Mathf.Deg2Rad;
            zoneCenters[z] = new Vector3(Mathf.Cos(angle) * 28f, 0f, Mathf.Sin(angle) * 28f);
            Box("ZonePad " + z, root, zoneCenters[z] + Vector3.up * 0.03f, new Vector3(20f, 0.06f, 20f), 20f * z, pads[z], false);
        }

        Box("Plaza", root, new Vector3(0f, 0.5f, 0f), new Vector3(20f, 1f, 20f), 0f, plaza, true);
        Box("Trim North", root, new Vector3(0f, 1.04f, 9.85f), new Vector3(20f, 0.08f, 0.3f), 0f, trim, false);
        Box("Trim South", root, new Vector3(0f, 1.04f, -9.85f), new Vector3(20f, 0.08f, 0.3f), 0f, trim, false);
        Box("Trim East", root, new Vector3(9.85f, 1.04f, 0f), new Vector3(0.3f, 0.08f, 20f), 0f, trim, false);
        Box("Trim West", root, new Vector3(-9.85f, 1.04f, 0f), new Vector3(0.3f, 0.08f, 20f), 0f, trim, false);

        for (int i = 0; i < 3; i++)
        {
            float height = (i + 1) * 0.33f;
            Box("Step " + i, root, new Vector3(0f, height / 2f, -10.5f - (2 - i)), new Vector3(8f, height, 1f), 0f, step, true);
        }

        float[] lampX = { -9f, 9f, -9f, 9f };
        float[] lampZ = { -9f, -9f, 9f, 9f };
        for (int i = 0; i < 4; i++)
        {
            Box("Lamp Post " + i, root, new Vector3(lampX[i], 2.5f, lampZ[i]), new Vector3(0.2f, 3f, 0.2f), 0f, post, true);
            Box("Lamp Head " + i, root, new Vector3(lampX[i], 4.15f, lampZ[i]), new Vector3(0.5f, 0.5f, 0.5f), 0f, lamp, false);
        }

        int segments = 36;
        float ringRadius = 46f;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 360f / segments;
            Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
            float height = Rand(14f, 24f);
            float yaw = -(angle + 90f);
            Vector3 position = direction * ringRadius;
            Box("Cliff " + i, root, position + Vector3.up * (height / 2f), new Vector3(9.2f, height, 6f), yaw, i % 2 == 0 ? cliffA : cliffB, true);
            Box("Cliff Band " + i, root, position - direction * 3.02f + Vector3.up * (height * 0.45f), new Vector3(9.3f, 0.7f, 0.12f), yaw, band, false);
        }

        float[,] mesas =
        {
            { 90f, 21f, 9f, 5.5f },
            { 210f, 21f, 8f, 4.5f },
            { 330f, 21f, 9f, 6f },
            { 90f, 33f, 7f, 3.2f },
            { 210f, 33f, 7f, 3.8f },
            { 330f, 33f, 8f, 3f }
        };
        Vector3[] mesaCenters = new Vector3[mesas.GetLength(0)];
        for (int i = 0; i < mesas.GetLength(0); i++)
        {
            float angle = mesas[i, 0] * Mathf.Deg2Rad;
            float radius = mesas[i, 1];
            float width = mesas[i, 2];
            float height = mesas[i, 3];
            mesaCenters[i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Box("Mesa " + i, root, mesaCenters[i] + Vector3.up * (height / 2f), new Vector3(width, height, width * 0.8f), Rand(0f, 90f), mesaMats[i % 3], true);
        }

        int placed = 0;
        int tries = 0;
        while (placed < 24 && tries < 500)
        {
            tries++;
            float angle = Rand(0f, 360f) * Mathf.Deg2Rad;
            float radius = Rand(13f, 40f);
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            if (NearAny(position, zoneCenters, 13f) || NearAny(position, mesaCenters, 9f))
            {
                continue;
            }

            float size = Rand(1f, 2.5f);
            Box("Boulder " + placed, root, position + Vector3.up * (size * 0.4f), new Vector3(size * 1.3f, size * 0.8f, size), Rand(0f, 360f), boulder, true);
            placed++;
        }

        GameObject ceiling = new GameObject("World Ceiling");
        ceiling.transform.SetParent(root, false);
        ceiling.transform.position = new Vector3(0f, 34f, 0f);
        ceiling.layer = groundLayer;
        ceiling.AddComponent<BoxCollider>().size = new Vector3(130f, 1f, 130f);

        float[] wallX = { 0f, 0f, 58f, -58f };
        float[] wallZ = { 58f, -58f, 0f, 0f };
        for (int i = 0; i < 4; i++)
        {
            GameObject wall = new GameObject("World Wall " + i);
            wall.transform.SetParent(root, false);
            wall.transform.position = new Vector3(wallX[i], 20f, wallZ[i]);
            wall.layer = groundLayer;
            wall.AddComponent<BoxCollider>().size = i < 2 ? new Vector3(120f, 40f, 1f) : new Vector3(1f, 40f, 120f);
        }

        Shop[] shops = Object.FindObjectsByType<Shop>(FindObjectsSortMode.None);
        for (int i = 0; i < shops.Length; i++)
        {
            shops[i].transform.SetPositionAndRotation(ShopPosition + Vector3.right * (i * 7f), Quaternion.identity);
        }

        Box("Shop Backdrop", root, ShopPosition + new Vector3(1.2f, 2.1f, 0.9f), new Vector3(6.4f, 4.2f, 0.3f), 0f, backdrop, true);
        Box("Shop Backdrop Trim", root, ShopPosition + new Vector3(1.2f, 4.25f, 0.9f), new Vector3(6.6f, 0.15f, 0.4f), 0f, trim, false);
        Box("Computer Backdrop", root, ComputerPosition + new Vector3(0f, 2.1f, 0.9f), new Vector3(3.4f, 4.2f, 0.3f), 0f, backdrop, true);
        Box("Computer Backdrop Trim", root, ComputerPosition + new Vector3(0f, 4.25f, 0.9f), new Vector3(3.6f, 0.15f, 0.4f), 0f, trim, false);

        PlaceRocks(zoneCenters);

        Monster[] monsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        for (int i = 0; i < monsters.Length; i++)
        {
            float angle = (200f + i * 50f) * Mathf.Deg2Rad;
            monsters[i].transform.position = new Vector3(Mathf.Cos(angle) * 34f, 0f, Mathf.Sin(angle) * 34f);
        }

        GameObject computer = new GameObject("Computer");
        computer.transform.SetParent(root, false);
        computer.transform.position = ComputerPosition;
        Box("Desk Top", computer.transform, new Vector3(0f, 0.85f, 0f), new Vector3(2f, 0.08f, 0.9f), 0f, deskTop, true);
        float[] legX = { -0.9f, 0.9f };
        float[] legZ = { -0.35f, 0.35f };
        for (int x = 0; x < legX.Length; x++)
        {
            for (int z = 0; z < legZ.Length; z++)
            {
                Box("Desk Leg", computer.transform, new Vector3(legX[x], 0.4f, legZ[z]), new Vector3(0.1f, 0.8f, 0.1f), 0f, deskWood, true);
            }
        }

        Box("Keyboard", computer.transform, new Vector3(0f, 0.9f, -0.15f), new Vector3(0.7f, 0.03f, 0.25f), 0f, keyboard, false);
        Box("Screen Stand", computer.transform, new Vector3(0f, 1f, 0.15f), new Vector3(0.12f, 0.3f, 0.08f), 0f, keyboard, false);
        computer.AddComponent<RippedComputer>();

        GameObject title = new GameObject("Title Sign");
        title.transform.SetParent(root, false);
        title.transform.position = new Vector3(0f, 12f, 40f);
        TextFactory.Create(title.transform, "Title Text", "RIPPED", Vector3.zero, 0.5f, new Color(1f, 0.69f, 0.18f), TextAnchor.MiddleCenter);

        ApplyEnvironment();
        PlacePlayer();

        AssetDatabase.SaveAssets();
        Debug.Log("[RippedMap] Map built.");
    }

    private static void PlaceRocks(Vector3[] zoneCenters)
    {
        GameObject miningArea = GameObject.Find("MiningArea");
        RockSpawnPoint[] spawns = Object.FindObjectsByType<RockSpawnPoint>(FindObjectsSortMode.None);
        System.Array.Sort(spawns, (a, b) => string.CompareOrdinal(a.name, b.name));

        GameObject rockPrefab = null;
        for (int i = 0; i < spawns.Length; i++)
        {
            SerializedObject serialized = new SerializedObject(spawns[i]);
            SerializedProperty prefabProperty = serialized.FindProperty("rockPrefab");
            SerializedProperty rockProperty = serialized.FindProperty("currentRock");
            if (rockPrefab == null && prefabProperty != null)
            {
                rockPrefab = prefabProperty.objectReferenceValue as GameObject;
            }

            Vector3 position = RockPosition(i, zoneCenters);
            Quaternion rotation = Quaternion.Euler(0f, 70f * i + 20f, 0f);
            spawns[i].transform.SetPositionAndRotation(position, rotation);

            MineableRock rock = rockProperty != null ? rockProperty.objectReferenceValue as MineableRock : null;
            if (rock != null)
            {
                rock.transform.SetPositionAndRotation(position, rotation);
            }
        }

        if (rockPrefab == null)
        {
            Debug.LogWarning("[RippedMap] No rock prefab found on a RockSpawnPoint, extra rock spots skipped.");
            return;
        }

        for (int i = spawns.Length; i < TargetRockCount; i++)
        {
            Vector3 position = RockPosition(i, zoneCenters);
            Quaternion rotation = Quaternion.Euler(0f, 70f * i + 20f, 0f);

            GameObject rockObject = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab);
            rockObject.name = "MiningRock " + (i + 1);
            rockObject.transform.SetPositionAndRotation(position, rotation);

            GameObject spawnObject = new GameObject("RockSpawn " + (i + 1));
            spawnObject.transform.SetPositionAndRotation(position, rotation);
            if (miningArea != null)
            {
                rockObject.transform.SetParent(miningArea.transform, true);
                spawnObject.transform.SetParent(miningArea.transform, true);
            }

            RockSpawnPoint spawn = spawnObject.AddComponent<RockSpawnPoint>();
            SerializedObject serialized = new SerializedObject(spawn);
            SerializedProperty prefabProperty = serialized.FindProperty("rockPrefab");
            SerializedProperty rockProperty = serialized.FindProperty("currentRock");
            if (prefabProperty != null)
            {
                prefabProperty.objectReferenceValue = rockPrefab;
            }

            if (rockProperty != null)
            {
                rockProperty.objectReferenceValue = rockObject.GetComponent<MineableRock>();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void PlacePlayer()
    {
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint != null)
        {
            spawnPoint.transform.SetPositionAndRotation(SpawnPosition, Quaternion.identity);
        }

        ArmLocomotion player = Object.FindFirstObjectByType<ArmLocomotion>();
        if (player != null)
        {
            player.transform.SetPositionAndRotation(SpawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[RippedMap] No ArmLocomotion in the scene, player not moved.");
        }
    }

    private static void ApplyEnvironment()
    {
        Color sky = new Color(0.55f, 0.78f, 1f);

        Light sun = null;
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                sun = lights[i];
                break;
            }
        }

        if (sun == null)
        {
            GameObject sunObject = new GameObject("Sun");
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        sun.color = new Color(1f, 0.94f, 0.84f);
        sun.intensity = 1.05f;
        sun.shadows = LightShadows.None;
        RenderSettings.sun = sun;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.83f, 0.86f, 0.96f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 135f;
        RenderSettings.fogColor = sky;

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].clearFlags = CameraClearFlags.SolidColor;
            cameras[i].backgroundColor = sky;
        }
    }

    private static Vector3 RockPosition(int index, Vector3[] zoneCenters)
    {
        int zone = index % 3;
        int ring = index / 3;
        float angle = (ring * 55f + zone * 20f) * Mathf.Deg2Rad;
        float radius = 3.5f + (ring % 4) * 1.8f;
        return zoneCenters[zone] + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    private static bool NearAny(Vector3 point, Vector3[] centers, float distance)
    {
        for (int i = 0; i < centers.Length; i++)
        {
            if ((centers[i] - point).magnitude < distance)
            {
                return true;
            }
        }

        return false;
    }

    private static float Rand(float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    private static Material Mat(string materialName, string hex, float emission)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "RippedMap");
        }

        string path = MaterialFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        ColorUtility.TryParseHtmlString(hex, out Color color);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.08f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * emission);
            }
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Box(string objectName, Transform parent, Vector3 position, Vector3 scale, float yaw, Material material, bool collider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;
        box.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        box.layer = groundLayer;
        GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        if (!collider)
        {
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        return box;
    }
}
