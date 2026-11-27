using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// RIPPED > Build Map builds a real level into the open scene:
// canyon floor, cliff ring, mesas to climb, a raised plaza, 3 mining zones, lamps, signs,
// the computer, lighting + fog, and a NavMesh for the monster.
// Your existing Shop / SellBin / RockSpawnPoint / Monster objects are moved into place (not recreated),
// and the old test course root objects are switched off (not deleted).
public static class RippedMapBuilder
{
    const string RootName = "RIPPED_Map";
    const string MatDir = "Assets/Materials/Generated";

    static Transform root;
    static System.Random rng;

    [MenuItem("RIPPED/Build Map")]
    public static void BuildFromMenu()
    {
        Build();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // Used by AutoBuild: builds the map into the scene once, then saves it.
    public static void EnsureMapInScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (GameObject.Find(RootName) != null) return;
        Build();
        EditorSceneManager.SaveScene(scene);
    }

    public static void Build()
    {
        rng = new System.Random(1337);
        var scene = SceneManager.GetActiveScene();

        foreach (var go in scene.GetRootGameObjects())
            if (go.name == RootName) Object.DestroyImmediate(go);

        // ---- gather existing gameplay objects ----
        var shops = FindByType("Shop");
        var bins = FindByType("SellBin");
        var rocks = FindByType("RockSpawnPoint");
        var monsters = FindByType("Monster");
        var tracked = new List<Transform>();
        tracked.AddRange(shops); tracked.AddRange(bins); tracked.AddRange(rocks); tracked.AddRange(monsters);

        foreach (var t in tracked)
            if (t.parent != null && IsOldCourse(t.root.gameObject)) t.SetParent(null, true);

        foreach (var go in scene.GetRootGameObjects())
        {
            if (!tracked.Contains(go.transform) && IsOldCourse(go))
            {
                go.SetActive(false);
                Debug.Log("[MapBuilder] Disabled old course object: " + go.name);
            }
        }

        var rootGo = new GameObject(RootName);
        root = rootGo.transform;

        // ---- materials (flat, saturated, Gorilla-Tag-ish) ----
        var sand = Mat("RM_Sand", C("#d9b77a"));
        var padMats = new[] { Mat("RM_PadA", C("#b98b52")), Mat("RM_PadB", C("#9a7a8a")), Mat("RM_PadC", C("#7a9a80")) };
        var cliffA = Mat("RM_CliffA", C("#8a5a44"));
        var cliffB = Mat("RM_CliffB", C("#6f4a3d"));
        var band = Mat("RM_Band", C("#d98c4a"));
        var plazaM = Mat("RM_Plaza", C("#3f4a66"));
        var stepM = Mat("RM_Step", C("#566086"));
        var trim = Mat("RM_Trim", C("#ffb02e"), 0.6f);
        var post = Mat("RM_Post", C("#2b2f3a"));
        var lamp = Mat("RM_Lamp", C("#ffd37a"), 2.0f);
        var board = Mat("RM_Board", C("#1d2335"));
        var mesaMats = new[] { Mat("RM_MesaA", C("#c86b4a")), Mat("RM_MesaB", C("#a9565a")), Mat("RM_MesaC", C("#d08a53")) };
        var boulder = Mat("RM_Boulder", C("#9b6b50"));
        var deskM = Mat("RM_Desk", C("#7a4a2e"));
        var deskTop = Mat("RM_DeskTop", C("#e8d9b5"));
        var kbM = Mat("RM_Keyboard", C("#2b2f3a"));

        // ---- ground ----
        Box("Floor", root, new Vector3(0, -0.5f, 0), new Vector3(120, 1, 120), sand);

        var zoneCenters = new Vector3[3];
        for (int z = 0; z < 3; z++)
        {
            float ang = (30f + 120f * z) * Mathf.Deg2Rad;
            zoneCenters[z] = new Vector3(Mathf.Cos(ang) * 28f, 0, Mathf.Sin(ang) * 28f);
            Box("ZonePad" + z, root, zoneCenters[z] + Vector3.up * 0.03f, new Vector3(20, 0.06f, 20), padMats[z], false, 20f * z);
        }

        // ---- plaza (shop / sell / computer) ----
        Box("Plaza", root, new Vector3(0, 0.5f, 0), new Vector3(20, 1, 20), plazaM);
        Box("TrimN", root, new Vector3(0, 1.04f, 9.85f), new Vector3(20, 0.08f, 0.3f), trim, false);
        Box("TrimS", root, new Vector3(0, 1.04f, -9.85f), new Vector3(20, 0.08f, 0.3f), trim, false);
        Box("TrimE", root, new Vector3(9.85f, 1.04f, 0), new Vector3(0.3f, 0.08f, 20), trim, false);
        Box("TrimW", root, new Vector3(-9.85f, 1.04f, 0), new Vector3(0.3f, 0.08f, 20), trim, false);

        for (int i = 0; i < 3; i++)
        {
            float h = (i + 1) * 0.33f;
            Box("Step" + i, root, new Vector3(0, h / 2f, -10.5f - (2 - i) * 1f), new Vector3(8, h, 1), stepM);
        }

        float[] lx = { -9f, 9f, -9f, 9f };
        float[] lz = { -9f, -9f, 9f, 9f };
        for (int i = 0; i < 4; i++)
        {
            Box("LampPost" + i, root, new Vector3(lx[i], 2.5f, lz[i]), new Vector3(0.2f, 3f, 0.2f), post);
            Box("LampHead" + i, root, new Vector3(lx[i], 4.15f, lz[i]), new Vector3(0.5f, 0.5f, 0.5f), lamp, false);
        }

        // ---- cliff ring ----
        int n = 36;
        float R = 46f;
        for (int i = 0; i < n; i++)
        {
            float a = i * 360f / n;
            Vector3 dir = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
            float h = Rand(14f, 24f);
            float yaw = -(a + 90f);
            Vector3 p = dir * R;
            Box("Cliff" + i, root, p + Vector3.up * (h / 2f), new Vector3(9.2f, h, 6f), i % 2 == 0 ? cliffA : cliffB, true, yaw);
            Box("CliffBand" + i, root, p - dir * 3.02f + Vector3.up * (h * 0.45f), new Vector3(9.3f, 0.7f, 0.12f), band, false, yaw);
        }

        // ---- mesas (climb with your arms) ----
        float[,] mesas =
        {
            { 90, 21, 9, 5.5f }, { 210, 21, 8, 4.5f }, { 330, 21, 9, 6f },
            { 90, 33, 7, 3.2f }, { 210, 33, 7, 3.8f }, { 330, 33, 8, 3f }
        };
        for (int i = 0; i < mesas.GetLength(0); i++)
        {
            float a = mesas[i, 0] * Mathf.Deg2Rad;
            float r = mesas[i, 1], w = mesas[i, 2], h = mesas[i, 3];
            Box("Mesa" + i, root, new Vector3(Mathf.Cos(a) * r, h / 2f, Mathf.Sin(a) * r), new Vector3(w, h, w * 0.8f), mesaMats[i % 3], true, Rand(0f, 90f));
        }

        // ---- boulders (decor, kept clear of the mining zones) ----
        int placed = 0, tries = 0;
        while (placed < 24 && tries < 400)
        {
            tries++;
            float a = Rand(0f, 360f) * Mathf.Deg2Rad;
            float r = Rand(13f, 40f);
            Vector3 p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            bool near = false;
            foreach (var zc in zoneCenters) if ((zc - p).magnitude < 13f) near = true;
            foreach (var m in mesas.Cast<float>().Take(0)) { }
            if (near) continue;
            float s = Rand(1f, 2.5f);
            Box("Boulder" + placed, root, p + Vector3.up * (s * 0.4f), new Vector3(s * 1.3f, s * 0.8f, s), boulder, true, Rand(0f, 360f));
            placed++;
        }

        // ---- invisible ceiling so nobody climbs out ----
        var ceil = new GameObject("WorldCeiling");
        ceil.transform.SetParent(root, false);
        ceil.transform.position = new Vector3(0, 34f, 0);
        var bc = ceil.AddComponent<BoxCollider>();
        bc.size = new Vector3(130, 1, 130);

        // ---- placement of existing objects ----
        Vector3 spawn = new Vector3(0, 1f, -5f);
        Vector3 center = new Vector3(0, 1f, 0);

        for (int i = 0; i < shops.Count; i++)
        {
            Vector3 p = new Vector3(-7f - i * 3f, 1f, 3f);
            PlaceFacing(shops[i], p, spawn);
            if (i == 0) Sign("SHOP", p + Vector3.up * 3.4f, spawn, board, C("#ffb02e"), 3f);
        }
        for (int i = 0; i < bins.Count; i++)
        {
            Vector3 p = new Vector3(7f + i * 3f, 1f, 3f);
            PlaceFacing(bins[i], p, spawn);
            if (i == 0) Sign("SELL", p + Vector3.up * 3.0f, spawn, board, C("#6fe39a"), 2.4f);
        }

        if (rocks.Count > 0)
        {
            for (int i = 0; i < rocks.Count; i++) rocks[i].position = RockPos(i, zoneCenters);
            int target = Mathf.Max(rocks.Count, 18);
            int existing = rocks.Count;
            for (int i = existing; i < target; i++)
            {
                var clone = Object.Instantiate(rocks[0].gameObject);
                clone.name = rocks[0].name + "_" + i;
                clone.transform.SetParent(null);
                clone.transform.position = RockPos(i, zoneCenters);
            }
        }
        else
        {
            Debug.LogWarning("[MapBuilder] No RockSpawnPoint found in the scene. Add one, then run RIPPED > Build Map again.");
        }

        for (int i = 0; i < monsters.Count; i++)
        {
            float a = (200f + i * 50f) * Mathf.Deg2Rad;
            monsters[i].position = new Vector3(Mathf.Cos(a) * 34f, 0f, Mathf.Sin(a) * 34f);
        }

        // ---- computer ----
        var comp = new GameObject("Computer");
        comp.transform.SetParent(root, false);
        comp.transform.position = new Vector3(0, 1f, 8f);
        Box("DeskTop", comp.transform, new Vector3(0, 0.85f, 0), new Vector3(2f, 0.08f, 0.9f), deskTop);
        float[] dx = { -0.9f, 0.9f };
        float[] dz = { -0.35f, 0.35f };
        foreach (float x in dx) foreach (float z in dz)
            Box("DeskLeg", comp.transform, new Vector3(x, 0.4f, z), new Vector3(0.1f, 0.8f, 0.1f), deskM);
        Box("Keyboard", comp.transform, new Vector3(0, 0.9f, -0.15f), new Vector3(0.7f, 0.03f, 0.25f), kbM, false);
        Box("ScreenStand", comp.transform, new Vector3(0, 1.0f, 0.15f), new Vector3(0.12f, 0.3f, 0.08f), kbM, false);
        comp.AddComponent<RippedComputer>();

        // ---- big title on the north cliff ----
        var titleGo = new GameObject("TitleSign");
        titleGo.transform.SetParent(root, false);
        titleGo.transform.position = new Vector3(0, 12f, 40f);
        Label("RIPPED", titleGo.transform, Vector3.zero, Quaternion.identity, 4.5f, C("#ffb02e"));

        // ---- light, fog, sky ----
        var sun = Object.FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        if (sun == null)
        {
            var lg = new GameObject("Sun");
            sun = lg.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        sun.color = C("#fff0d6");
        sun.intensity = 1.05f;
        sun.shadows = LightShadows.None; // Quest performance
        RenderSettings.sun = sun;

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = C("#d4dcf5");
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 135f;
        var sky = C("#8cc8ff");
        RenderSettings.fogColor = sky;
        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = sky;
        }

        // ---- player spawn ----
        var rig = FindRig(scene);
        if (rig != null)
        {
            rig.position = spawn;
            rig.rotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("[MapBuilder] Couldn't find the XR rig. Move your player to (0, 1, -5) by hand.");
        }

        // ---- NavMesh for the monster ----
        try
        {
            var surf = rootGo.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.Children;
            surf.BuildNavMesh();
            if (surf.navMeshData != null && !AssetDatabase.Contains(surf.navMeshData))
            {
                string p = "Assets/Scenes/RIPPED_NavMesh.asset";
                AssetDatabase.DeleteAsset(p);
                AssetDatabase.CreateAsset(surf.navMeshData, p);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MapBuilder] NavMesh bake skipped: " + e.Message);
        }

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = rootGo;
        Debug.Log("[MapBuilder] RIPPED map built.");
    }

    // ---------- helpers ----------
    static Vector3 RockPos(int i, Vector3[] zc)
    {
        int zone = i % 3;
        int k = i / 3;
        float ang = (k * 55f + zone * 20f) * Mathf.Deg2Rad;
        float r = 3.5f + (k % 4) * 1.8f;
        return zc[zone] + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
    }

    static void PlaceFacing(Transform t, Vector3 pos, Vector3 lookAt)
    {
        t.position = pos;
        Vector3 d = lookAt - pos;
        d.y = 0;
        if (d.sqrMagnitude > 0.001f) t.rotation = Quaternion.LookRotation(d);
    }

    static float Rand(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    static Color C(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }

    static List<Transform> FindByType(string typeName)
    {
        return Object.FindObjectsOfType<MonoBehaviour>(true)
            .Where(m => m != null && m.GetType().Name == typeName)
            .Select(m => m.transform).Distinct().OrderBy(t => t.name).ToList();
    }

    static bool IsOldCourse(GameObject go)
    {
        string n = go.name.ToLowerInvariant();
        if (go.name == RootName) return false;
        string[] skip = { "xr", "rig", "player", "camera", "light", "manager", "system", "event", "canvas", "monster", "shop", "wallet", "volume" };
        foreach (var s in skip) if (n.Contains(s)) return false;
        string[] keys = { "test", "course", "demo", "playground", "obstacle", "level", "arena", "floor", "ground", "terrain" };
        foreach (var k in keys) if (n.Contains(k)) return true;
        return false;
    }

    static Transform FindRig(Scene scene)
    {
        string[] keys = { "xr origin", "xr rig", "xrorigin", "player", "rig" };
        foreach (var k in keys)
            foreach (var go in scene.GetRootGameObjects())
                if (go.name != RootName && go.name.ToLowerInvariant().Contains(k)) return go.transform;
        return null;
    }

    static Material Mat(string name, Color c, float emission = 0f)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials", "Generated");
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emission);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = true, float yaw = 0f)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        g.GetComponent<MeshRenderer>().sharedMaterial = m;
        g.isStatic = true;
        if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static void Sign(string text, Vector3 pos, Vector3 facePoint, Material board, Color textCol, float w)
    {
        var g = new GameObject("Sign_" + text);
        g.transform.SetParent(root, false);
        g.transform.position = pos;
        Vector3 away = pos - facePoint;
        away.y = 0;
        if (away.sqrMagnitude > 0.001f) g.transform.rotation = Quaternion.LookRotation(away);
        Box("Board", g.transform, Vector3.zero, new Vector3(w, 0.9f, 0.12f), board, false);
        Label(text, g.transform, new Vector3(0, 0, -0.08f), Quaternion.identity, 0.55f, textCol);
    }

    static void Label(string text, Transform parent, Vector3 lp, Quaternion lr, float height, Color col)
    {
        var g = new GameObject("Label_" + text);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        g.transform.localRotation = lr;
        var tm = g.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        tm.text = text;
        tm.fontSize = 100;
        tm.characterSize = height / 10f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = col;
        g.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }
}
