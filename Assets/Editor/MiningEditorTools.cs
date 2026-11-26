using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR;

public static class MiningEditorTools
{
    private const string RockModelPath = "Assets/Models/Mining/rock.glb";
    private const string CrystalModelPath = "Assets/Models/Mining/crystal.glb";
    private const string PickaxeModelPath = "Assets/Models/Mining/pickaxe.glb";
    private const string PrefabFolder = "Assets/Prefabs/Mining";
    private const string ChunkPrefabPath = PrefabFolder + "/CrystalChunk.prefab";
    private const string RockPrefabPath = PrefabFolder + "/MiningRock.prefab";
    private const float ChunkScale = 0.3f;
    private const float DecorCrystalScale = 0.4f;
    private const int DecorCrystalCount = 5;

    [MenuItem("RIPPED/4 - Add Mining Rocks And Pickaxe To Open Scene")]
    public static void AddToOpenScene()
    {
        PopulateMiningContent();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("RIPPED/5 - Make Selected Model Into Pickaxe")]
    public static void MakeSelectedIntoPickaxe()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null)
        {
            EditorUtility.DisplayDialog("RIPPED", "Select your pickaxe object in the Hierarchy first.", "OK");
            return;
        }

        ConfigurePickaxe(target);
        EditorSceneManager.MarkSceneDirty(target.scene);
    }

    public static void PopulateMiningContent()
    {
        GameObject rockPrefab = BuildPrefabs();
        if (rockPrefab == null)
        {
            return;
        }

        AttachGrabHands();
        GameObject pickaxePrefab = BuildPickaxePrefab();

        GameObject root = new GameObject("MiningArea");

        GameObject manager = new GameObject("GameManager");
        manager.transform.SetParent(root.transform, false);
        manager.AddComponent<Wallet>();
        manager.AddComponent<WalletHud>();

        Vector3[] rockPositions =
        {
            new Vector3(-4f, 0f, 3.5f),
            new Vector3(-7f, 0f, 2f),
            new Vector3(-8f, 0f, -2f),
            new Vector3(-6.5f, 0f, 7f)
        };

        for (int i = 0; i < rockPositions.Length; i++)
        {
            Quaternion rotation = Quaternion.Euler(0f, 70f * i + 20f, 0f);

            GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, root.transform);
            rock.name = "MiningRock " + (i + 1);
            rock.transform.SetPositionAndRotation(rockPositions[i], rotation);

            GameObject spawnObject = new GameObject("RockSpawn " + (i + 1));
            spawnObject.transform.SetParent(root.transform, false);
            spawnObject.transform.SetPositionAndRotation(rockPositions[i], rotation);
            RockSpawnPoint spawnPoint = spawnObject.AddComponent<RockSpawnPoint>();
            SerializedObject spawnSerialized = new SerializedObject(spawnPoint);
            spawnSerialized.FindProperty("rockPrefab").objectReferenceValue = rockPrefab;
            spawnSerialized.FindProperty("currentRock").objectReferenceValue = rock.GetComponent<MineableRock>();
            spawnSerialized.ApplyModifiedProperties();
        }

        BuildShop(root.transform, pickaxePrefab);
    }

    private static GameObject BuildPickaxePrefab()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Mining");

        GameObject pickaxe = CreatePickaxe();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(pickaxe, PrefabFolder + "/Pickaxe.prefab");
        Object.DestroyImmediate(pickaxe);
        return prefab;
    }

    private static void BuildShop(Transform parent, GameObject pickaxePrefab)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");

        GameObject root = new GameObject("Shop");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(0f, 0f, 3f);
        Transform shopRoot = root.transform;

        Color wood = new Color(0.45f, 0.3f, 0.18f);
        Color board = new Color(0.08f, 0.1f, 0.14f);

        ShopBlock(shopRoot, "Counter", new Vector3(0f, 0.5f, -0.05f), new Vector3(2.4f, 1f, 0.5f), "ShopWood", wood, groundLayer);
        ShopBlock(shopRoot, "Screen", new Vector3(0f, 1.95f, 0.35f), new Vector3(2.6f, 1.6f, 0.1f), "ShopScreen", board, groundLayer);

        Color gold = new Color(1f, 0.85f, 0.3f);
        Color white = new Color(0.95f, 0.95f, 0.95f);
        TextMesh title = TextFactory.Create(shopRoot, "Title", "RIPPED SHOP", new Vector3(0f, 2.5f, 0.25f), 0.02f, gold, TextAnchor.MiddleCenter);
        TextMesh price = TextFactory.Create(shopRoot, "ItemPrice", "PICKAXE - 50 COINS", new Vector3(0f, 2.15f, 0.25f), 0.0125f, white, TextAnchor.MiddleCenter);
        TextMesh coins = TextFactory.Create(shopRoot, "Coins", "COINS: 0", new Vector3(0f, 1.85f, 0.25f), 0.0125f, gold, TextAnchor.MiddleCenter);
        TextMesh status = TextFactory.Create(shopRoot, "Status", "TOUCH BUY TO GET A PICKAXE", new Vector3(0f, 1.55f, 0.25f), 0.009f, new Color(0.6f, 0.9f, 1f), TextAnchor.MiddleCenter);
        title.gameObject.name = "Title";

        GameObject spawn = new GameObject("PickaxeSpawn");
        spawn.transform.SetParent(shopRoot, false);
        spawn.transform.localPosition = new Vector3(0f, 1.1f, -0.05f);
        spawn.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        Shop shop = root.AddComponent<Shop>();
        SerializedObject shopSerialized = new SerializedObject(shop);
        shopSerialized.FindProperty("pickaxePrefab").objectReferenceValue = pickaxePrefab;
        shopSerialized.FindProperty("pickaxeSpawn").objectReferenceValue = spawn.transform;
        shopSerialized.FindProperty("coinText").objectReferenceValue = coins;
        shopSerialized.FindProperty("priceText").objectReferenceValue = price;
        shopSerialized.FindProperty("statusText").objectReferenceValue = status;
        shopSerialized.ApplyModifiedProperties();

        CreateShopButton(shopRoot, shop, ShopAction.BuyPickaxe, "BUY", new Vector3(-0.7f, 1.25f, 0.22f), new Color(0.15f, 0.65f, 0.25f));
        CreateShopButton(shopRoot, shop, ShopAction.RecallPickaxe, "RETURN PICKAXE", new Vector3(0.7f, 1.25f, 0.22f), new Color(0.85f, 0.55f, 0.1f));

        GameObject bin = new GameObject("SellBin");
        bin.transform.SetParent(shopRoot, false);
        bin.transform.localPosition = new Vector3(2.4f, 0f, -0.4f);
        Color binColor = new Color(0.25f, 0.25f, 0.3f);
        ShopBlock(bin.transform, "BinFloor", new Vector3(0f, 0.025f, 0f), new Vector3(0.9f, 0.05f, 0.9f), "ShopBin", binColor, groundLayer);
        ShopBlock(bin.transform, "BinWallLeft", new Vector3(-0.425f, 0.25f, 0f), new Vector3(0.05f, 0.5f, 0.9f), "ShopBin", binColor, groundLayer);
        ShopBlock(bin.transform, "BinWallRight", new Vector3(0.425f, 0.25f, 0f), new Vector3(0.05f, 0.5f, 0.9f), "ShopBin", binColor, groundLayer);
        ShopBlock(bin.transform, "BinWallBack", new Vector3(0f, 0.25f, 0.425f), new Vector3(0.9f, 0.5f, 0.05f), "ShopBin", binColor, groundLayer);
        ShopBlock(bin.transform, "BinWallFront", new Vector3(0f, 0.25f, -0.425f), new Vector3(0.9f, 0.5f, 0.05f), "ShopBin", binColor, groundLayer);
        TextFactory.Create(bin.transform, "BinLabel", "SELL CRYSTALS\n+5 COINS EACH", new Vector3(0f, 1.0f, 0f), 0.0125f, gold, TextAnchor.MiddleCenter);

        SellBin sellBin = bin.AddComponent<SellBin>();
        SerializedObject binSerialized = new SerializedObject(sellBin);
        binSerialized.FindProperty("boxCenter").vector3Value = new Vector3(0f, 0.3f, 0f);
        binSerialized.FindProperty("boxSize").vector3Value = new Vector3(0.8f, 0.6f, 0.8f);
        binSerialized.ApplyModifiedProperties();
    }

    private static GameObject ShopBlock(Transform parent, string blockName, Vector3 localPosition, Vector3 scale, string materialName, Color color, int layer)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = blockName;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = scale;
        block.GetComponent<MeshRenderer>().sharedMaterial = SolidMaterial(materialName, color);
        if (layer >= 0)
        {
            block.layer = layer;
        }

        return block;
    }

    private static void CreateShopButton(Transform parent, Shop shop, ShopAction action, string label, Vector3 localPosition, Color color)
    {
        GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
        button.name = "Button " + label;
        button.transform.SetParent(parent, false);
        button.transform.localPosition = localPosition;
        button.transform.localScale = new Vector3(0.9f, 0.26f, 0.12f);
        button.GetComponent<MeshRenderer>().sharedMaterial = SolidMaterial("ShopButton" + action, color);
        button.GetComponent<BoxCollider>().isTrigger = true;

        ShopButton shopButton = button.AddComponent<ShopButton>();
        shopButton.Configure(shop, action);
        EditorUtility.SetDirty(shopButton);

        TextFactory.Create(parent, "Label " + label, label, localPosition + new Vector3(0f, 0f, -0.08f), 0.0085f, Color.white, TextAnchor.MiddleCenter);
    }

    private static GameObject BuildPrefabs()
    {
        GameObject rockModel = AssetDatabase.LoadAssetAtPath<GameObject>(RockModelPath);
        GameObject crystalModel = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalModelPath);

        if (rockModel == null || crystalModel == null)
        {
            Debug.LogError("rock.glb or crystal.glb did not import. Install the glTFast package (com.unity.cloud.gltfast) from the Package Manager, then run this menu item again.");
            return null;
        }

        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Mining");

        GameObject chunkRoot = new GameObject("CrystalChunk");
        GameObject chunkVisual = (GameObject)Object.Instantiate(crystalModel, chunkRoot.transform);
        chunkVisual.name = "Visual";
        chunkVisual.transform.localPosition = Vector3.zero;
        chunkVisual.transform.localRotation = Quaternion.identity;
        chunkVisual.transform.localScale = Vector3.one * ChunkScale;

        Bounds chunkBounds = LocalBounds(chunkRoot.transform);
        BoxCollider chunkCollider = chunkRoot.AddComponent<BoxCollider>();
        chunkCollider.center = chunkBounds.center;
        chunkCollider.size = new Vector3(Mathf.Max(chunkBounds.size.x, 0.045f), Mathf.Max(chunkBounds.size.y, 0.045f), Mathf.Max(chunkBounds.size.z, 0.045f));

        Rigidbody chunkBody = chunkRoot.AddComponent<Rigidbody>();
        chunkBody.mass = 0.05f;
#if UNITY_6000_0_OR_NEWER
        chunkBody.linearDamping = 0.1f;
        chunkBody.angularDamping = 0.3f;
#else
        chunkBody.drag = 0.1f;
        chunkBody.angularDrag = 0.3f;
#endif
        chunkBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        chunkRoot.AddComponent<CrystalChunk>();

        GameObject chunkPrefab = PrefabUtility.SaveAsPrefabAsset(chunkRoot, ChunkPrefabPath);
        Object.DestroyImmediate(chunkRoot);

        int groundLayer = LayerMask.NameToLayer("Ground");

        GameObject rockRoot = new GameObject("MiningRock");
        GameObject rockVisual = (GameObject)Object.Instantiate(rockModel, rockRoot.transform);
        rockVisual.name = "RockVisual";
        rockVisual.transform.localPosition = Vector3.zero;
        rockVisual.transform.localRotation = Quaternion.identity;
        rockVisual.transform.localScale = Vector3.one;

        MeshFilter[] filters = rockVisual.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filters.Length; i++)
        {
            MeshCollider meshCollider = filters[i].gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filters[i].sharedMesh;
        }

        List<Transform> decor = new List<Transform>();
        Random.State previous = Random.state;
        Random.InitState(1337);
        List<Vector3> points = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        SampleTopSurface(rockRoot.transform, filters, DecorCrystalCount, points, normals);
        Random.state = previous;

        for (int i = 0; i < points.Count; i++)
        {
            GameObject crystal = (GameObject)Object.Instantiate(crystalModel, rockRoot.transform);
            crystal.name = "RockCrystal " + (i + 1);
            Collider[] stray = crystal.GetComponentsInChildren<Collider>();
            for (int c = 0; c < stray.Length; c++)
            {
                Object.DestroyImmediate(stray[c]);
            }

            Vector3 up = Vector3.Slerp(normals[i], Vector3.up, 0.45f).normalized;
            crystal.transform.localScale = Vector3.one * DecorCrystalScale * Random.Range(0.8f, 1.2f);
            crystal.transform.localRotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            crystal.transform.localPosition = points[i] + up * (0.246f * crystal.transform.localScale.y - 0.02f);
            decor.Add(crystal.transform);
        }

        SetLayerRecursive(rockRoot, groundLayer);

        MineableRock mineable = rockRoot.AddComponent<MineableRock>();
        SerializedObject serialized = new SerializedObject(mineable);
        serialized.FindProperty("chunkPrefab").objectReferenceValue = chunkPrefab;
        serialized.FindProperty("visualRoot").objectReferenceValue = rockVisual.transform;
        SerializedProperty crystalsProperty = serialized.FindProperty("attachedCrystals");
        crystalsProperty.arraySize = decor.Count;
        for (int i = 0; i < decor.Count; i++)
        {
            crystalsProperty.GetArrayElementAtIndex(i).objectReferenceValue = decor[i];
        }

        serialized.ApplyModifiedProperties();

        GameObject rockPrefab = PrefabUtility.SaveAsPrefabAsset(rockRoot, RockPrefabPath);
        Object.DestroyImmediate(rockRoot);
        AssetDatabase.SaveAssets();
        return rockPrefab;
    }

    private static void SampleTopSurface(Transform root, MeshFilter[] filters, int count, List<Vector3> points, List<Vector3> normals)
    {
        List<Vector3> trianglePoints = new List<Vector3>();
        List<float> cumulative = new List<float>();
        float total = 0f;

        for (int f = 0; f < filters.Length; f++)
        {
            Mesh mesh = filters[f].sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Matrix4x4 toRoot = root.worldToLocalMatrix * filters[f].transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;

            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                Vector3 a = toRoot.MultiplyPoint3x4(vertices[indices[i]]);
                Vector3 b = toRoot.MultiplyPoint3x4(vertices[indices[i + 1]]);
                Vector3 c = toRoot.MultiplyPoint3x4(vertices[indices[i + 2]]);
                Vector3 cross = Vector3.Cross(b - a, c - a);
                float area = cross.magnitude * 0.5f;
                Vector3 normal = cross.normalized;

                if (area <= 0.00001f || normal.y < 0.45f || (a.y + b.y + c.y) / 3f < 0.2f)
                {
                    continue;
                }

                total += area;
                cumulative.Add(total);
                trianglePoints.Add(a);
                trianglePoints.Add(b);
                trianglePoints.Add(c);
            }
        }

        if (total <= 0f)
        {
            return;
        }

        int attempts = 0;
        while (points.Count < count && attempts < count * 30)
        {
            attempts++;
            float pick = Random.value * total;
            int triangle = cumulative.BinarySearch(pick);
            if (triangle < 0)
            {
                triangle = ~triangle;
            }

            triangle = Mathf.Clamp(triangle, 0, cumulative.Count - 1);
            Vector3 a = trianglePoints[triangle * 3];
            Vector3 b = trianglePoints[triangle * 3 + 1];
            Vector3 c = trianglePoints[triangle * 3 + 2];

            float u = Random.value;
            float v = Random.value;
            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }

            Vector3 point = a + (b - a) * u + (c - a) * v;
            bool tooClose = false;
            for (int i = 0; i < points.Count; i++)
            {
                if ((points[i] - point).sqrMagnitude < 0.04f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                continue;
            }

            points.Add(point);
            normals.Add(Vector3.Cross(b - a, c - a).normalized);
        }
    }

    private static GameObject CreatePickaxe()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PickaxeModelPath);
        if (model == null)
        {
            Debug.LogWarning("pickaxe.glb did not import, using the placeholder pickaxe.");
            return CreatePlaceholderPickaxe();
        }

        GameObject root = new GameObject("Pickaxe");
        GameObject visual = (GameObject)Object.Instantiate(model, root.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        if (!BuildPickaxeFromShape(root))
        {
            Debug.LogWarning("Could not analyse the pickaxe mesh, using a single bounds collider.");
            ConfigurePickaxe(root);
        }

        return root;
    }

    private static bool BuildPickaxeFromShape(GameObject root)
    {
        List<Vector3> vertices = new List<Vector3>();
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>();
        for (int f = 0; f < filters.Length; f++)
        {
            Mesh mesh = filters[f].sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Matrix4x4 toRoot = root.transform.worldToLocalMatrix * filters[f].transform.localToWorldMatrix;
            Vector3[] source = mesh.vertices;
            for (int i = 0; i < source.Length; i++)
            {
                vertices.Add(toRoot.MultiplyPoint3x4(source[i]));
            }
        }

        if (vertices.Count < 8)
        {
            return false;
        }

        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        for (int i = 1; i < vertices.Count; i++)
        {
            bounds.Encapsulate(vertices[i]);
        }

        float[] size = { bounds.size.x, bounds.size.y, bounds.size.z };
        float[] center = { bounds.center.x, bounds.center.y, bounds.center.z };
        float[] min = { bounds.min.x, bounds.min.y, bounds.min.z };
        float[] max = { bounds.max.x, bounds.max.y, bounds.max.z };

        int handleIndex = 0;
        for (int i = 1; i < 3; i++)
        {
            if (size[i] > size[handleIndex])
            {
                handleIndex = i;
            }
        }

        int headIndex = -1;
        for (int i = 0; i < 3; i++)
        {
            if (i != handleIndex && (headIndex < 0 || size[i] > size[headIndex]))
            {
                headIndex = i;
            }
        }

        int thirdIndex = 3 - handleIndex - headIndex;
        float quarter = size[handleIndex] * 0.25f;
        float lowSpread = 0f;
        float highSpread = 0f;
        int lowCount = 0;
        int highCount = 0;

        for (int i = 0; i < vertices.Count; i++)
        {
            float along = vertices[i][handleIndex];
            float spread = Mathf.Abs(vertices[i][headIndex] - center[headIndex]);
            if (along < min[handleIndex] + quarter)
            {
                lowSpread += spread;
                lowCount++;
            }
            else if (along > max[handleIndex] - quarter)
            {
                highSpread += spread;
                highCount++;
            }
        }

        float lowMean = lowCount > 0 ? lowSpread / lowCount : 0f;
        float highMean = highCount > 0 ? highSpread / highCount : 0f;
        float headSign = highMean >= lowMean ? 1f : -1f;
        float buttCoordinate = headSign > 0f ? min[handleIndex] : max[handleIndex];
        float headEndCoordinate = headSign > 0f ? max[handleIndex] : min[handleIndex];

        Vector3 tipA = vertices[0];
        Vector3 tipB = vertices[0];
        for (int i = 1; i < vertices.Count; i++)
        {
            if (vertices[i][headIndex] > tipA[headIndex])
            {
                tipA = vertices[i];
            }

            if (vertices[i][headIndex] < tipB[headIndex])
            {
                tipB = vertices[i];
            }
        }

        const float headThickness = 0.12f;
        const float handleThickness = 0.07f;

        BoxCollider handleBox = root.AddComponent<BoxCollider>();
        float handleLength = Mathf.Max(size[handleIndex] - headThickness, 0.05f);
        float[] handleCenter = { center[0], center[1], center[2] };
        handleCenter[handleIndex] = (buttCoordinate + (headEndCoordinate - headSign * headThickness)) * 0.5f;
        float[] handleSize = new float[3];
        handleSize[handleIndex] = handleLength;
        handleSize[headIndex] = handleThickness;
        handleSize[thirdIndex] = Mathf.Max(handleThickness, size[thirdIndex]);
        handleBox.center = new Vector3(handleCenter[0], handleCenter[1], handleCenter[2]);
        handleBox.size = new Vector3(handleSize[0], handleSize[1], handleSize[2]);

        BoxCollider headBox = root.AddComponent<BoxCollider>();
        float[] headCenter = { center[0], center[1], center[2] };
        headCenter[handleIndex] = headEndCoordinate - headSign * headThickness * 0.5f;
        float[] headSize = new float[3];
        headSize[handleIndex] = headThickness;
        headSize[headIndex] = size[headIndex];
        headSize[thirdIndex] = Mathf.Max(handleThickness, size[thirdIndex]);
        headBox.center = new Vector3(headCenter[0], headCenter[1], headCenter[2]);
        headBox.size = new Vector3(headSize[0], headSize[1], headSize[2]);

        GameObject hitA = new GameObject("HitPointA");
        hitA.transform.SetParent(root.transform, false);
        hitA.transform.localPosition = tipA;
        GameObject hitB = new GameObject("HitPointB");
        hitB.transform.SetParent(root.transform, false);
        hitB.transform.localPosition = tipB;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 1.5f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        Pickaxe pickaxe = root.AddComponent<Pickaxe>();

        Vector3 handleAxis = Vector3.zero;
        handleAxis[handleIndex] = headSign;
        Vector3 tipAxis = Vector3.zero;
        tipAxis[headIndex] = 1f;

        float tilt = 45f * Mathf.Deg2Rad;
        Vector3 handHeadDirection = new Vector3(0f, Mathf.Sin(tilt), Mathf.Cos(tilt));
        Vector3 handTipAxis = new Vector3(0f, Mathf.Cos(tilt), -Mathf.Sin(tilt));
        Quaternion modelBasis = Quaternion.LookRotation(handleAxis, tipAxis);
        Quaternion handBasis = Quaternion.LookRotation(handHeadDirection, handTipAxis);
        Quaternion held = handBasis * Quaternion.Inverse(modelBasis);

        float[] gripPoint = { center[0], center[1], center[2] };
        gripPoint[handleIndex] = buttCoordinate + headSign * Mathf.Min(0.22f, size[handleIndex] * 0.3f);
        Vector3 grip = new Vector3(gripPoint[0], gripPoint[1], gripPoint[2]);

        SerializedObject serialized = new SerializedObject(pickaxe);
        SerializedProperty points = serialized.FindProperty("hitPoints");
        points.arraySize = 2;
        points.GetArrayElementAtIndex(0).objectReferenceValue = hitA.transform;
        points.GetArrayElementAtIndex(1).objectReferenceValue = hitB.transform;
        serialized.FindProperty("snapToHand").boolValue = true;
        serialized.FindProperty("heldLocalPosition").vector3Value = -(held * grip);
        serialized.FindProperty("heldLocalEuler").vector3Value = held.eulerAngles;
        serialized.ApplyModifiedProperties();
        return true;
    }

    private static GameObject CreatePlaceholderPickaxe()
    {
        GameObject root = new GameObject("Pickaxe");

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localScale = new Vector3(0.035f, 0.35f, 0.035f);
        handle.transform.localPosition = new Vector3(0f, 0f, 0f);
        handle.GetComponent<Renderer>().sharedMaterial = SolidMaterial("PickaxeWood", new Color(0.45f, 0.28f, 0.12f));
        Object.DestroyImmediate(handle.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localScale = new Vector3(0.45f, 0.05f, 0.06f);
        head.transform.localPosition = new Vector3(0f, 0.33f, 0f);
        head.GetComponent<Renderer>().sharedMaterial = SolidMaterial("PickaxeIron", new Color(0.6f, 0.62f, 0.66f));
        Object.DestroyImmediate(head.GetComponent<Collider>());

        GameObject tip = new GameObject("HitPoint");
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = new Vector3(0.23f, 0.33f, 0f);

        ConfigurePickaxe(root);
        return root;
    }

    private static void ConfigurePickaxe(GameObject target)
    {
        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = target.AddComponent<Rigidbody>();
        }

        body.mass = 1.5f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (target.GetComponentInChildren<Collider>() == null)
        {
            Bounds bounds = LocalBounds(target.transform);
            BoxCollider box = target.AddComponent<BoxCollider>();
            box.center = bounds.center;
            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, 0.06f);
            size.y = Mathf.Max(size.y, 0.06f);
            size.z = Mathf.Max(size.z, 0.06f);
            box.size = size;
        }

        Transform hit = target.transform.Find("HitPoint");
        if (hit == null)
        {
            GameObject point = new GameObject("HitPoint");
            point.transform.SetParent(target.transform, false);
            Bounds bounds = LocalBounds(target.transform);
            point.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            hit = point.transform;
        }

        Pickaxe pickaxe = target.GetComponent<Pickaxe>();
        if (pickaxe == null)
        {
            pickaxe = target.AddComponent<Pickaxe>();
        }

        SerializedObject serialized = new SerializedObject(pickaxe);
        SerializedProperty points = serialized.FindProperty("hitPoints");
        points.arraySize = 1;
        points.GetArrayElementAtIndex(0).objectReferenceValue = hit;
        serialized.ApplyModifiedProperties();
    }

    private static void AttachGrabHands()
    {
        ArmLocomotion[] found = Object.FindObjectsByType<ArmLocomotion>(FindObjectsSortMode.None);
        if (found.Length == 0)
        {
            Debug.LogWarning("No ArmLocomotion player found in the open scene, so no grab hands were added.");
            return;
        }

        SerializedObject locomotion = new SerializedObject(found[0]);
        AddGrabHand(locomotion.FindProperty("leftHandVisual").objectReferenceValue as Transform, locomotion.FindProperty("leftHandTransform").objectReferenceValue as Transform, XRNode.LeftHand);
        AddGrabHand(locomotion.FindProperty("rightHandVisual").objectReferenceValue as Transform, locomotion.FindProperty("rightHandTransform").objectReferenceValue as Transform, XRNode.RightHand);
    }

    private static void AddGrabHand(Transform visual, Transform tracked, XRNode node)
    {
        if (visual == null)
        {
            return;
        }

        GrabHand hand = visual.GetComponent<GrabHand>();
        if (hand == null)
        {
            hand = visual.gameObject.AddComponent<GrabHand>();
        }

        hand.Configure(node, visual, tracked);
        EditorUtility.SetDirty(hand);
    }

    private static Bounds LocalBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;

        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds world = renderers[i].bounds;
            Vector3 min = world.min;
            Vector3 max = world.max;
            for (int c = 0; c < 8; c++)
            {
                Vector3 corner = new Vector3((c & 1) == 0 ? min.x : max.x, (c & 2) == 0 ? min.y : max.y, (c & 4) == 0 ? min.z : max.z);
                Vector3 local = root.InverseTransformPoint(corner);
                if (first)
                {
                    result = new Bounds(local, Vector3.zero);
                    first = false;
                }
                else
                {
                    result.Encapsulate(local);
                }
            }
        }

        return result;
    }

    private static void SetLayerRecursive(GameObject target, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        Transform[] all = target.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            all[i].gameObject.layer = layer;
        }
    }

    private static Material SolidMaterial(string materialName, Color color)
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Mining");
        string path = "Assets/Materials/Mining/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + folderName))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
