using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RippedVisuals
{
    public const string VolumeName = "RIPPED_PostFX";

    private const string Folder = "Assets/Materials/RippedMap";
    private const string SkyPath = Folder + "/RIPPED_Sky.mat";
    private const string ProfilePath = Folder + "/RippedPostFX.asset";

    [MenuItem("RIPPED/7 - Apply Epic Visuals")]
    public static void ApplyFromMenu()
    {
        Apply();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void Apply()
    {
        EnsureFolder();
        Light sun = ConfigureSun();
        Run("sky", () => ConfigureSky(sun));
        Run("render pipeline asset", ConfigurePipeline);
        Run("cameras", ConfigureCameras);
        Run("post processing", ConfigurePostProcessing);
        AssetDatabase.SaveAssets();
        Debug.Log("[RippedVisuals] Visuals applied.");
    }

    public static Shader FindEpicShader()
    {
        Shader shader = Shader.Find("RIPPED/EpicLit");
        if (shader == null || !shader.isSupported)
        {
            return null;
        }

        return shader;
    }

    public static Color ShadeFor(Color color)
    {
        Color shade = Color.Lerp(color * 0.5f, new Color(0.28f, 0.22f, 0.5f), 0.4f);
        shade.a = 1f;
        return shade;
    }

    private static void Run(string label, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[RippedVisuals] " + label + " skipped: " + exception.Message);
        }
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "RippedMap");
        }
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    private static Light ConfigureSun()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    sun = lights[i];
                    break;
                }
            }
        }

        if (sun == null)
        {
            GameObject sunObject = new GameObject("Sun");
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.transform.rotation = Quaternion.Euler(38f, -40f, 0f);
        sun.color = new Color(1f, 0.92f, 0.8f);
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.9f;
        sun.shadowBias = 0.05f;
        sun.shadowNormalBias = 0.4f;
        RenderSettings.sun = sun;
        return sun;
    }

    private static void ConfigureSky(Light sun)
    {
        Color horizon = Hex("#cfe6ff");

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("#9ec8ff");
        RenderSettings.ambientEquatorColor = Hex("#ffd9a8");
        RenderSettings.ambientGroundColor = Hex("#8f7650");

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 150f;
        RenderSettings.fogColor = horizon;

        Shader shader = Shader.Find("RIPPED/Sky");
        if (shader == null || !shader.isSupported)
        {
            RenderSettings.skybox = null;
            Debug.LogWarning("[RippedVisuals] RIPPED/Sky shader not available, using a solid sky color.");
            return;
        }

        Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
        if (sky == null)
        {
            sky = new Material(shader);
            AssetDatabase.CreateAsset(sky, SkyPath);
        }
        else if (sky.shader != shader)
        {
            sky.shader = shader;
        }

        Vector3 toSun = -sun.transform.forward;
        sky.SetColor("_TopColor", Hex("#3f86e8"));
        sky.SetColor("_HorizonColor", horizon);
        sky.SetColor("_GroundColor", Hex("#a88a5c"));
        sky.SetColor("_SunColor", Hex("#ffdc99"));
        sky.SetVector("_SunDirection", new Vector4(toSun.x, toSun.y, toSun.z, 0f));
        EditorUtility.SetDirty(sky);

        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();
    }

    private static void ConfigurePipeline()
    {
        List<RenderPipelineAsset> assets = new List<RenderPipelineAsset>();
        AddUnique(assets, GraphicsSettings.defaultRenderPipeline);
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
        {
            AddUnique(assets, QualitySettings.GetRenderPipelineAssetAt(i));
        }

        if (assets.Count == 0)
        {
            Debug.LogWarning("[RippedVisuals] No render pipeline asset found.");
            return;
        }

        for (int i = 0; i < assets.Count; i++)
        {
            SerializedObject serialized = new SerializedObject(assets[i]);
            SetInt(serialized, "m_MainLightRenderingMode", 1);
            SetBool(serialized, "m_MainLightShadowsSupported", true);
            SetInt(serialized, "m_MainLightShadowmapResolution", 2048);
            SetFloat(serialized, "m_ShadowDistance", 40f);
            SetInt(serialized, "m_ShadowCascadeCount", 2);
            SetFloat(serialized, "m_Cascade2Split", 0.3f);
            SetBool(serialized, "m_SoftShadowsSupported", true);
            SetFloat(serialized, "m_ShadowDepthBias", 1f);
            SetFloat(serialized, "m_ShadowNormalBias", 1f);
            SetBool(serialized, "m_SupportsHDR", true);
            SetInt(serialized, "m_MSAA", 4);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(assets[i]);
            Debug.Log("[RippedVisuals] Shadows, HDR and MSAA set on " + assets[i].name);
        }
    }

    private static void AddUnique(List<RenderPipelineAsset> list, RenderPipelineAsset asset)
    {
        if (asset != null && !list.Contains(asset))
        {
            list.Add(asset);
        }
    }

    private static void SetInt(SerializedObject serialized, string name, int value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetFloat(SerializedObject serialized, string name, float value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetBool(SerializedObject serialized, string name, bool value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void ConfigureCameras()
    {
        bool hasSky = RenderSettings.skybox != null;
        Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].clearFlags = hasSky ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            UniversalAdditionalCameraData data = cameras[i].GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            data.antialiasing = AntialiasingMode.None;
            data.volumeLayerMask = ~0;
        }
    }

    private static void ConfigurePostProcessing()
    {
        VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(ProfilePath);
        }

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "RippedPostFX";
        AssetDatabase.CreateAsset(profile, ProfilePath);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.5f);
        bloom.scatter.Override(0.6f);
        bloom.highQualityFiltering.Override(false);
        bloom.skipIterations.Override(1);

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments adjustments = profile.Add<ColorAdjustments>(true);
        adjustments.postExposure.Override(0.15f);
        adjustments.contrast.Override(14f);
        adjustments.saturation.Override(16f);

        for (int i = 0; i < profile.components.Count; i++)
        {
            AssetDatabase.AddObjectToAsset(profile.components[i], profile);
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        GameObject old = GameObject.Find(VolumeName);
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
        }

        GameObject volumeObject = new GameObject(VolumeName);
        GameObject mapRoot = GameObject.Find("RIPPED_Map");
        if (mapRoot != null)
        {
            volumeObject.transform.SetParent(mapRoot.transform, false);
        }

        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }
}
