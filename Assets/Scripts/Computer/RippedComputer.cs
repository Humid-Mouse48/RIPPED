using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RippedLeaderboard
{
    public struct Entry
    {
        public string session;
        public string name;
        public int score;
    }

    private const string Key = "ripped.leaderboard";

    public static List<Entry> Load()
    {
        List<Entry> list = new List<Entry>();
        string raw = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            return list;
        }

        string[] rows = raw.Split(';');
        for (int i = 0; i < rows.Length; i++)
        {
            string[] parts = rows[i].Split('|');
            if (parts.Length == 3 && int.TryParse(parts[2], out int score))
            {
                list.Add(new Entry { session = parts[0], name = parts[1], score = score });
            }
        }

        return list;
    }

    public static void Submit(string name, int score, string session)
    {
        if (score <= 0)
        {
            return;
        }

        List<Entry> list = Load();
        list.RemoveAll(e => e.session == session);
        list.Add(new Entry { session = session, name = Clean(name), score = score });
        list = list.OrderByDescending(e => e.score).Take(10).ToList();
        PlayerPrefs.SetString(Key, string.Join(";", list.Select(e => e.session + "|" + e.name + "|" + e.score)));
        PlayerPrefs.Save();
    }

    public static List<Entry> Top(int count)
    {
        return Load().OrderByDescending(e => e.score).Take(count).ToList();
    }

    private static string Clean(string value)
    {
        return (value ?? "MONKE").Replace(";", string.Empty).Replace("|", string.Empty);
    }
}

public sealed class RippedComputer : MonoBehaviour
{
    private enum Page
    {
        Leaderboard,
        Palette,
        Settings
    }

    private sealed class Btn
    {
        public Transform visual;
        public Renderer renderer;
        public Color baseColor;
        public Action onPress;
        public Vector3 restPosition;
        public bool inside;
        public bool selected;
        public float nextPress;
        public float pushed;
    }

    private const string VolumeKey = "ripped.volume";
    private const string SpeedKey = "ripped.armspeed";

    private static readonly Color BezelColor = new Color(0.02f, 0.025f, 0.04f);
    private static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.11f);
    private static readonly Color HeaderColor = new Color(0.09f, 0.12f, 0.19f);
    private static readonly Color ButtonColor = new Color(0.14f, 0.18f, 0.27f);
    private static readonly Color Accent = new Color(1f, 0.69f, 0.18f);
    private static readonly Color TextColor = new Color(0.92f, 0.95f, 1f);
    private static readonly Color DimColor = new Color(0.55f, 0.62f, 0.75f);

    private static readonly Color[] Swatches =
    {
        Color.white,
        new Color(0.2f, 0.2f, 0.23f),
        new Color(0.95f, 0.3f, 0.26f),
        new Color(0.98f, 0.62f, 0.15f),
        new Color(0.95f, 0.85f, 0.25f),
        new Color(0.3f, 0.8f, 0.4f),
        new Color(0.25f, 0.6f, 0.98f),
        new Color(0.72f, 0.4f, 0.92f)
    };

    private static readonly string[] Names =
    {
        "MONKE", "GORILLA", "CHIMP", "BONOBO", "ORANGUTAN", "LEMUR", "BABOON", "MACAQUE"
    };

    private readonly List<Btn> tabs = new List<Btn>();
    private readonly List<Btn> pageButtons = new List<Btn>();
    private readonly string session = Guid.NewGuid().ToString("N");

    private Transform screen;
    private Transform content;
    private TextMesh coinsText;
    private GrabHand[] hands = new GrabHand[0];
    private ArmLocomotion player;
    private Wallet wallet;
    private int best;

    private void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);

        GameObject screenObject = new GameObject("Screen");
        screenObject.transform.SetParent(transform, false);
        screenObject.transform.localPosition = new Vector3(0f, 1.45f, 0.15f);
        screenObject.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        screen = screenObject.transform;

        Quad("Bezel", screen, new Vector3(0f, 0f, 0.004f), new Vector2(1.78f, 1.08f), BezelColor, -3);
        Quad("Panel", screen, Vector3.zero, new Vector2(1.7f, 1f), PanelColor, -2);
        Quad("Header", screen, new Vector3(0f, 0.43f, -0.002f), new Vector2(1.7f, 0.1f), HeaderColor, -2);

        TextFactory.Create(screen, "Title", "RIPPED TERMINAL", new Vector3(-0.8f, 0.43f, -0.01f), Size(0.06f), Accent, TextAnchor.MiddleLeft);
        coinsText = TextFactory.Create(screen, "Coins", "COINS --", new Vector3(0.8f, 0.43f, -0.01f), Size(0.05f), TextColor, TextAnchor.MiddleRight);

        AddTab("LEADERBOARD", -0.55f, Page.Leaderboard);
        AddTab("COLOR", 0f, Page.Palette);
        AddTab("SETTINGS", 0.55f, Page.Settings);

        ShowPage(Page.Leaderboard);
    }

    private void Start()
    {
        hands = FindObjectsByType<GrabHand>(FindObjectsSortMode.None);
        player = FindFirstObjectByType<ArmLocomotion>();
        if (player != null)
        {
            player.SpeedMultiplier = PlayerPrefs.GetFloat(SpeedKey, 1f);
        }

        wallet = Wallet.Instance;
        if (wallet != null)
        {
            wallet.Changed += OnCoinsChanged;
            OnCoinsChanged();
        }
    }

    private void OnDestroy()
    {
        if (wallet != null)
        {
            wallet.Changed -= OnCoinsChanged;
        }

        SubmitBest();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SubmitBest();
        }
    }

    private void OnApplicationQuit()
    {
        SubmitBest();
    }

    private void OnCoinsChanged()
    {
        if (wallet == null)
        {
            return;
        }

        coinsText.text = "COINS " + wallet.Coins;
        if (wallet.Coins > best)
        {
            best = wallet.Coins;
        }
    }

    private void SubmitBest()
    {
        if (best > 0)
        {
            RippedLeaderboard.Submit(MonkeProfile.CurrentName, best, session);
        }
    }

    private void AddTab(string label, float x, Page page)
    {
        Btn tab = Button(tabs, screen, label, new Vector2(x, 0.3f), new Vector2(0.5f, 0.11f), () => ShowPage(page), 0.04f);
        tab.selected = false;
    }

    private void ShowPage(Page page)
    {
        SubmitBest();

        if (content != null)
        {
            Destroy(content.gameObject);
        }

        pageButtons.Clear();
        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(screen, false);
        content = contentObject.transform;

        for (int i = 0; i < tabs.Count; i++)
        {
            tabs[i].selected = i == (int)page;
        }

        if (page == Page.Leaderboard)
        {
            BuildLeaderboard();
        }
        else if (page == Page.Palette)
        {
            BuildPalette();
        }
        else
        {
            BuildSettings();
        }
    }

    private void BuildLeaderboard()
    {
        TextFactory.Create(content, "Heading", "YOUR BEST BALANCES", new Vector3(0f, 0.17f, -0.01f), Size(0.045f), DimColor, TextAnchor.MiddleCenter);
        List<RippedLeaderboard.Entry> top = RippedLeaderboard.Top(5);
        if (top.Count == 0)
        {
            TextFactory.Create(content, "Empty", "NO RUNS YET\nSELL SOME CRYSTALS", new Vector3(0f, -0.08f, -0.01f), Size(0.06f), TextColor, TextAnchor.MiddleCenter);
            return;
        }

        for (int i = 0; i < top.Count; i++)
        {
            float y = 0.07f - i * 0.1f;
            Color rowColor = top[i].session == session ? Accent : TextColor;
            TextFactory.Create(content, "Name" + i, (i + 1) + ".  " + top[i].name, new Vector3(-0.78f, y, -0.01f), Size(0.055f), rowColor, TextAnchor.MiddleLeft);
            TextFactory.Create(content, "Score" + i, top[i].score.ToString(), new Vector3(0.78f, y, -0.01f), Size(0.055f), rowColor, TextAnchor.MiddleRight);
        }
    }

    private void BuildPalette()
    {
        TextFactory.Create(content, "Heading", "PICK YOUR MONKE COLOR", new Vector3(0f, 0.17f, -0.01f), Size(0.045f), DimColor, TextAnchor.MiddleCenter);
        TextFactory.Create(content, "Hint", "WHITE = ORIGINAL", new Vector3(0f, -0.4f, -0.01f), Size(0.035f), DimColor, TextAnchor.MiddleCenter);

        for (int i = 0; i < Swatches.Length; i++)
        {
            Color swatch = Swatches[i];
            float x = ((i % 4) - 1.5f) * 0.27f;
            float y = 0.02f - (i / 4) * 0.25f;
            GameObject quad = Quad("Swatch" + i, content, new Vector3(x, y, -0.004f), new Vector2(0.22f, 0.2f), swatch, -1);
            pageButtons.Add(new Btn
            {
                visual = quad.transform,
                renderer = quad.GetComponent<Renderer>(),
                baseColor = swatch,
                restPosition = quad.transform.localPosition,
                onPress = () => MonkeProfile.CurrentColor = swatch
            });
        }
    }

    private void BuildSettings()
    {
        TextFactory.Create(content, "VolumeLabel", "VOLUME", new Vector3(-0.78f, 0.1f, -0.01f), Size(0.055f), TextColor, TextAnchor.MiddleLeft);
        TextMesh volumeText = TextFactory.Create(content, "VolumeValue", Percent(AudioListener.volume), new Vector3(0.3f, 0.1f, -0.01f), Size(0.055f), Accent, TextAnchor.MiddleCenter);
        Button(pageButtons, content, "-", new Vector2(0.05f, 0.1f), new Vector2(0.15f, 0.11f), () => ChangeVolume(-0.1f, volumeText), 0.07f);
        Button(pageButtons, content, "+", new Vector2(0.55f, 0.1f), new Vector2(0.15f, 0.11f), () => ChangeVolume(0.1f, volumeText), 0.07f);

        float speed = player != null ? player.SpeedMultiplier : 1f;
        TextFactory.Create(content, "SpeedLabel", "ARM SPEED", new Vector3(-0.78f, -0.05f, -0.01f), Size(0.055f), TextColor, TextAnchor.MiddleLeft);
        TextMesh speedText = TextFactory.Create(content, "SpeedValue", speed.ToString("0.0") + "X", new Vector3(0.3f, -0.05f, -0.01f), Size(0.055f), Accent, TextAnchor.MiddleCenter);
        Button(pageButtons, content, "-", new Vector2(0.05f, -0.05f), new Vector2(0.15f, 0.11f), () => ChangeSpeed(-0.1f, speedText), 0.07f);
        Button(pageButtons, content, "+", new Vector2(0.55f, -0.05f), new Vector2(0.15f, 0.11f), () => ChangeSpeed(0.1f, speedText), 0.07f);

        TextFactory.Create(content, "NameLabel", "NAME", new Vector3(-0.78f, -0.2f, -0.01f), Size(0.055f), TextColor, TextAnchor.MiddleLeft);
        TextMesh nameText = TextFactory.Create(content, "NameValue", MonkeProfile.CurrentName, new Vector3(0.3f, -0.2f, -0.01f), Size(0.055f), Accent, TextAnchor.MiddleCenter);
        Button(pageButtons, content, "<", new Vector2(-0.12f, -0.2f), new Vector2(0.15f, 0.11f), () => CycleName(-1, nameText), 0.07f);
        Button(pageButtons, content, ">", new Vector2(0.72f, -0.2f), new Vector2(0.15f, 0.11f), () => CycleName(1, nameText), 0.07f);

        TextFactory.Create(content, "Footer", "SETTINGS SAVE AUTOMATICALLY", new Vector3(0f, -0.4f, -0.01f), Size(0.035f), DimColor, TextAnchor.MiddleCenter);
    }

    private static string Percent(float value)
    {
        return Mathf.RoundToInt(value * 100f) + "%";
    }

    private void ChangeVolume(float delta, TextMesh label)
    {
        float value = Mathf.Clamp01(Mathf.Round((AudioListener.volume + delta) * 10f) / 10f);
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        label.text = Percent(value);
    }

    private void ChangeSpeed(float delta, TextMesh label)
    {
        if (player == null)
        {
            return;
        }

        float value = Mathf.Clamp(Mathf.Round((player.SpeedMultiplier + delta) * 10f) / 10f, 0.5f, 2f);
        player.SpeedMultiplier = value;
        PlayerPrefs.SetFloat(SpeedKey, value);
        label.text = value.ToString("0.0") + "X";
    }

    private void CycleName(int direction, TextMesh label)
    {
        int index = Array.IndexOf(Names, MonkeProfile.CurrentName);
        index = (index + direction + Names.Length) % Names.Length;
        MonkeProfile.CurrentName = Names[index];
        label.text = Names[index];
    }

    private void Update()
    {
        if (screen == null)
        {
            return;
        }

        Camera viewer = Camera.main;
        if (viewer == null || (viewer.transform.position - screen.position).sqrMagnitude > 36f)
        {
            return;
        }

        UpdateButtons(tabs);
        UpdateButtons(pageButtons);
    }

    private void UpdateButtons(List<Btn> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Btn button = list[i];
            if (button.visual == null || button.renderer == null)
            {
                continue;
            }

            GrabHand touching = null;
            for (int h = 0; h < hands.Length; h++)
            {
                GrabHand hand = hands[h];
                if (hand == null || hand.HandTransform == null)
                {
                    continue;
                }

                Vector3 local = button.visual.InverseTransformPoint(hand.HandTransform.position);
                if (Mathf.Abs(local.x) < 0.5f && Mathf.Abs(local.y) < 0.5f && local.z < 0.03f && local.z > -0.12f)
                {
                    touching = hand;
                    break;
                }
            }

            bool inside = touching != null;
            Color target = button.selected ? Accent : button.baseColor;
            if (inside)
            {
                target = Color.Lerp(target, Color.white, 0.35f);
            }

            button.renderer.sharedMaterial.color = target;
            button.pushed = Mathf.MoveTowards(button.pushed, 0f, Time.deltaTime * 6f);
            button.visual.localPosition = button.restPosition + Vector3.forward * (0.012f * button.pushed);

            if (inside && !button.inside && Time.time >= button.nextPress)
            {
                button.nextPress = Time.time + 0.4f;
                button.pushed = 1f;
                touching.Pulse(0.6f, 0.08f);
                button.onPress?.Invoke();
            }

            button.inside = inside;
        }
    }

    private static float Size(float height)
    {
        return height / 8f;
    }

    private static GameObject Quad(string objectName, Transform parent, Vector3 localPosition, Vector2 size, Color color, int order)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = objectName;
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        Material material = new Material(shader);
        material.color = color;
        MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingOrder = order;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        return quad;
    }

    private Btn Button(List<Btn> list, Transform parent, string label, Vector2 position, Vector2 size, Action onPress, float textHeight)
    {
        GameObject quad = Quad("Button " + label, parent, new Vector3(position.x, position.y, -0.004f), size, ButtonColor, -1);
        TextFactory.Create(parent, "Label " + label, label, new Vector3(position.x, position.y, -0.012f), Size(textHeight), TextColor, TextAnchor.MiddleCenter);
        Btn button = new Btn
        {
            visual = quad.transform,
            renderer = quad.GetComponent<Renderer>(),
            baseColor = ButtonColor,
            restPosition = quad.transform.localPosition,
            onPress = onPress
        };
        list.Add(button);
        return button;
    }
}
