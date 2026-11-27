using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Local-only leaderboard (swap for Photon later). Stores each session's peak cash.
public static class RippedLeaderboard
{
    public struct Entry { public string session; public string name; public int score; }

    const string Key = "ripped_leaderboard_v1";

    public static List<Entry> Load()
    {
        var list = new List<Entry>();
        string raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(raw)) return list;
        foreach (string row in raw.Split(';'))
        {
            string[] p = row.Split('|');
            int s;
            if (p.Length == 3 && int.TryParse(p[2], out s))
                list.Add(new Entry { session = p[0], name = p[1], score = s });
        }
        return list;
    }

    // One entry per session id: submitting again replaces the old score for that session.
    public static void Submit(string name, int score, string session)
    {
        if (score <= 0) return;
        var list = Load();
        list.RemoveAll(e => e.session == session);
        list.Add(new Entry { session = session, name = Clean(name), score = score });
        list = list.OrderByDescending(e => e.score).Take(10).ToList();
        PlayerPrefs.SetString(Key, string.Join(";", list.Select(e => e.session + "|" + e.name + "|" + e.score)));
        PlayerPrefs.Save();
    }

    public static List<Entry> Top(int n) { return Load().OrderByDescending(e => e.score).Take(n).ToList(); }

    static string Clean(string s) { return (s ?? "MONKE").Replace(";", "").Replace("|", ""); }
}

// Build via RIPPED > Build Map (or automatically in AutoBuild). Players stand on the -Z side.
// Poke the buttons with your hands.
public class RippedComputer : MonoBehaviour
{
    enum Page { Leaderboard, Color, Settings }

    class Btn
    {
        public Transform t;
        public Renderer rend;
        public Action press;
        public bool active;
        public float next;
    }

    static readonly UnityEngine.Color PanelCol = new UnityEngine.Color(0.05f, 0.07f, 0.11f);
    static readonly UnityEngine.Color Accent = new UnityEngine.Color(1f, 0.69f, 0.18f);
    static readonly UnityEngine.Color BtnCol = new UnityEngine.Color(0.14f, 0.18f, 0.27f);
    static readonly UnityEngine.Color TextCol = new UnityEngine.Color(0.92f, 0.95f, 1f);
    static readonly UnityEngine.Color Dim = new UnityEngine.Color(0.55f, 0.62f, 0.75f);

    static readonly UnityEngine.Color[] Swatches =
    {
        new UnityEngine.Color(0.55f, 0.36f, 0.2f), new UnityEngine.Color(0.15f, 0.15f, 0.17f),
        new UnityEngine.Color(0.9f, 0.25f, 0.22f), new UnityEngine.Color(0.98f, 0.62f, 0.15f),
        new UnityEngine.Color(0.95f, 0.85f, 0.2f), new UnityEngine.Color(0.25f, 0.75f, 0.35f),
        new UnityEngine.Color(0.2f, 0.55f, 0.95f), new UnityEngine.Color(0.7f, 0.35f, 0.9f)
    };

    static readonly string[] Names = { "MONKE", "GORILLA", "CHIMP", "BONOBO", "ORANGUTAN", "LEMUR", "BABOON", "MACAQUE" };
    static readonly string[] CashNames = { "money", "balance", "coins", "cash", "gold", "total", "amount", "credits" };

    Transform screen, content;
    readonly List<Btn> tabs = new List<Btn>();
    readonly List<Btn> pageBtns = new List<Btn>();
    readonly List<Transform> hands = new List<Transform>();
    TextMesh cashText;
    Page page = Page.Leaderboard;
    string session = Guid.NewGuid().ToString("N");
    float peak, nextCash, nextScan, nextWalletSearch;
    MemberInfo cashMember;
    object walletObj;
    Type walletType;
    static Font font;

    void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("ripped_volume", 1f);

        var s = new GameObject("Screen");
        s.transform.SetParent(transform, false);
        s.transform.localPosition = new Vector3(0f, 1.45f, 0.15f);
        s.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        screen = s.transform;

        Quad("Bezel", screen, new Vector3(0, 0, 0.004f), new Vector2(1.78f, 1.08f), new UnityEngine.Color(0.02f, 0.025f, 0.04f), -1);
        Quad("Panel", screen, Vector3.zero, new Vector2(1.7f, 1.0f), PanelCol, 0);
        Quad("HeaderBar", screen, new Vector3(0, 0.43f, -0.002f), new Vector2(1.7f, 0.1f), new UnityEngine.Color(0.09f, 0.12f, 0.19f), 1);

        Text(screen, "RIPPED TERMINAL", new Vector3(-0.8f, 0.43f, -0.006f), 0.06f, Accent, TextAnchor.MiddleLeft);
        cashText = Text(screen, "CASH --", new Vector3(0.8f, 0.43f, -0.006f), 0.05f, TextCol, TextAnchor.MiddleRight);

        AddTab("LEADERBOARD", -0.55f, Page.Leaderboard);
        AddTab("COLOR", 0f, Page.Color);
        AddTab("SETTINGS", 0.55f, Page.Settings);

        ShowPage(Page.Leaderboard);
    }

    void AddTab(string label, float x, Page p)
    {
        var b = Button(tabs, screen, label, new Vector2(x, 0.3f), new Vector2(0.5f, 0.11f), () => ShowPage(p), 0.04f);
        b.active = false;
    }

    void ShowPage(Page p)
    {
        page = p;
        SubmitPeak();
        if (content != null) Destroy(content.gameObject);
        pageBtns.Clear();
        var c = new GameObject("Content");
        c.transform.SetParent(screen, false);
        content = c.transform;

        for (int i = 0; i < tabs.Count; i++) tabs[i].active = i == (int)p;

        if (p == Page.Leaderboard) BuildLeaderboard();
        else if (p == Page.Color) BuildColor();
        else BuildSettings();
    }

    // ---------- Pages ----------
    void BuildLeaderboard()
    {
        Text(content, "TOP RUNS  (PEAK CASH)", new Vector3(0, 0.17f, -0.006f), 0.045f, Dim, TextAnchor.MiddleCenter);
        var top = RippedLeaderboard.Top(5);
        if (top.Count == 0)
        {
            Text(content, "NO RUNS YET\nSELL SOME CRYSTALS!", new Vector3(0, -0.08f, -0.006f), 0.06f, TextCol, TextAnchor.MiddleCenter);
            return;
        }
        for (int i = 0; i < top.Count; i++)
        {
            float y = 0.07f - i * 0.1f;
            bool mine = top[i].session == session;
            var col = mine ? Accent : TextCol;
            Text(content, (i + 1) + ".  " + top[i].name, new Vector3(-0.7f, y, -0.006f), 0.055f, col, TextAnchor.MiddleLeft);
            Text(content, "$" + top[i].score, new Vector3(0.7f, y, -0.006f), 0.055f, col, TextAnchor.MiddleRight);
        }
    }

    void BuildColor()
    {
        Text(content, "PICK YOUR MONKE COLOR", new Vector3(0, 0.17f, -0.006f), 0.045f, Dim, TextAnchor.MiddleCenter);
        for (int i = 0; i < Swatches.Length; i++)
        {
            int idx = i;
            float x = ((i % 4) - 1.5f) * 0.27f;
            float y = 0.02f - (i / 4) * 0.25f;
            var q = Quad("Swatch" + i, content, new Vector3(x, y, -0.004f), new Vector2(0.22f, 0.2f), Swatches[i], 1);
            pageBtns.Add(new Btn { t = q.transform, rend = q.GetComponent<Renderer>(), press = () => MonkeProfile.Color = Swatches[idx], active = false });
        }
    }

    void BuildSettings()
    {
        // Volume
        Text(content, "VOLUME", new Vector3(-0.75f, 0.08f, -0.006f), 0.055f, TextCol, TextAnchor.MiddleLeft);
        var vol = Text(content, Mathf.RoundToInt(AudioListener.volume * 100f) + "%", new Vector3(0.3f, 0.08f, -0.006f), 0.055f, Accent, TextAnchor.MiddleCenter);
        Button(pageBtns, content, "-", new Vector2(0.05f, 0.08f), new Vector2(0.15f, 0.12f), () => SetVolume(-0.1f, vol), 0.07f);
        Button(pageBtns, content, "+", new Vector2(0.55f, 0.08f), new Vector2(0.15f, 0.12f), () => SetVolume(0.1f, vol), 0.07f);

        // Name
        Text(content, "NAME", new Vector3(-0.75f, -0.12f, -0.006f), 0.055f, TextCol, TextAnchor.MiddleLeft);
        var nm = Text(content, MonkeProfile.Name, new Vector3(0.3f, -0.12f, -0.006f), 0.055f, Accent, TextAnchor.MiddleCenter);
        Button(pageBtns, content, "<", new Vector2(-0.12f, -0.12f), new Vector2(0.15f, 0.12f), () => CycleName(-1, nm), 0.07f);
        Button(pageBtns, content, ">", new Vector2(0.72f, -0.12f), new Vector2(0.15f, 0.12f), () => CycleName(1, nm), 0.07f);

        Text(content, "SETTINGS SAVE AUTOMATICALLY", new Vector3(0, -0.34f, -0.006f), 0.035f, Dim, TextAnchor.MiddleCenter);
    }

    void SetVolume(float delta, TextMesh label)
    {
        float v = Mathf.Clamp01(Mathf.Round((AudioListener.volume + delta) * 10f) / 10f);
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("ripped_volume", v);
        label.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    void CycleName(int dir, TextMesh label)
    {
        int i = Array.IndexOf(Names, MonkeProfile.Name);
        i = (i + dir + Names.Length) % Names.Length;
        MonkeProfile.Name = Names[i];
        label.text = Names[i];
    }

    // ---------- Input (hand poke) ----------
    void Update()
    {
        if (Time.unscaledTime >= nextCash) { nextCash = Time.unscaledTime + 0.5f; UpdateCash(); }

        var cam = Camera.main;
        if (cam == null) return;
        if ((cam.transform.position - screen.position).sqrMagnitude > 36f) return;

        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + (hands.Count == 0 ? 1.5f : 5f);
            ScanHands();
        }
        hands.RemoveAll(h => h == null);

        Poke(tabs);
        Poke(pageBtns);
    }

    void ScanHands()
    {
        hands.Clear();
        foreach (var t in FindObjectsOfType<Transform>())
        {
            if (!t.name.ToLowerInvariant().Contains("hand")) continue;
            if (t.IsChildOf(transform)) continue;
            hands.Add(t);
        }
    }

    void Poke(List<Btn> list)
    {
        foreach (var b in list.ToArray())
        {
            if (b.t == null || b.rend == null) continue;
            bool hover = false;
            foreach (var h in hands)
            {
                Vector3 l = b.t.InverseTransformPoint(h.position);
                if (Mathf.Abs(l.x) < 0.5f && Mathf.Abs(l.y) < 0.5f && l.z < 0.03f && l.z > -0.1f) { hover = true; break; }
            }
            if (page == Page.Color && !b.active && b.rend.sharedMaterial != null && list == pageBtns)
            {
                // swatches keep their own color; just no tint logic needed
            }
            else
            {
                var baseC = b.active ? Accent : BtnCol;
                b.rend.sharedMaterial.color = hover ? UnityEngine.Color.Lerp(baseC, UnityEngine.Color.white, 0.35f) : baseC;
            }
            if (hover && Time.unscaledTime > b.next)
            {
                b.next = Time.unscaledTime + 0.4f;
                if (b.press != null) b.press();
            }
        }
    }

    // ---------- Cash / leaderboard hook ----------
    void UpdateCash()
    {
        float v;
        if (TryReadCash(out v))
        {
            cashText.text = "CASH $" + Mathf.RoundToInt(v);
            if (v > peak) peak = v;
        }
    }

    // Reads Wallet's money by reflection so this compiles regardless of how Wallet is written.
    bool TryReadCash(out float v)
    {
        v = 0f;
        try
        {
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            if (cashMember == null)
            {
                if (Time.unscaledTime < nextWalletSearch) return false;
                nextWalletSearch = Time.unscaledTime + 3f;
                walletType = typeof(RippedComputer).Assembly.GetType("Wallet");
                if (walletType == null) return false;
                cashMember = walletType.GetMembers(F)
                    .Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property)
                    .FirstOrDefault(m => CashNames.Contains(m.Name.TrimStart('_').ToLowerInvariant()));
                if (cashMember == null) return false;
            }

            bool isStatic = cashMember is FieldInfo
                ? ((FieldInfo)cashMember).IsStatic
                : ((PropertyInfo)cashMember).GetGetMethod(true).IsStatic;

            object target = null;
            if (!isStatic)
            {
                var uo = walletObj as UnityEngine.Object;
                if (walletObj == null || (uo != null && uo == null)) { }
                if (walletObj == null || (walletObj is UnityEngine.Object && (UnityEngine.Object)walletObj == null))
                {
                    if (typeof(Component).IsAssignableFrom(walletType)) walletObj = FindObjectOfType(walletType);
                }
                target = walletObj;
                if (target == null) return false;
            }

            object raw = cashMember is FieldInfo
                ? ((FieldInfo)cashMember).GetValue(target)
                : ((PropertyInfo)cashMember).GetValue(target, null);
            v = Convert.ToSingle(raw);
            return true;
        }
        catch { return false; }
    }

    void SubmitPeak()
    {
        if (peak >= 1f) RippedLeaderboard.Submit(MonkeProfile.Name, Mathf.RoundToInt(peak), session);
    }

    void OnApplicationPause(bool paused) { if (paused) SubmitPeak(); }
    void OnApplicationQuit() { SubmitPeak(); }
    void OnDestroy() { SubmitPeak(); }

    // ---------- Drawing helpers ----------
    static Font GetFont()
    {
        if (font != null) return font;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 64);
        return font;
    }

    static GameObject Quad(string n, Transform parent, Vector3 lp, Vector2 size, UnityEngine.Color c, int order)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
        g.name = n;
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        g.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = g.GetComponent<MeshRenderer>();
        var sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("UI/Default");
        var m = new Material(sh);
        m.color = c;
        r.sharedMaterial = m;
        r.sortingOrder = order;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return g;
    }

    static TextMesh Text(Transform parent, string s, Vector3 lp, float height, UnityEngine.Color c, TextAnchor anchor)
    {
        var g = new GameObject("Text");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        var tm = g.AddComponent<TextMesh>();
        tm.font = GetFont();
        tm.text = s;
        tm.fontSize = 100;
        tm.characterSize = height / 10f;
        tm.anchor = anchor;
        tm.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left
                     : anchor == TextAnchor.MiddleRight ? TextAlignment.Right : TextAlignment.Center;
        tm.color = c;
        var r = g.GetComponent<MeshRenderer>();
        r.sharedMaterial = tm.font.material;
        r.sortingOrder = 3;
        return tm;
    }

    Btn Button(List<Btn> list, Transform parent, string label, Vector2 pos, Vector2 size, Action act, float textH)
    {
        var q = Quad("Btn_" + label, parent, new Vector3(pos.x, pos.y, -0.004f), size, BtnCol, 1);
        Text(parent, label, new Vector3(pos.x, pos.y, -0.008f), textH, TextCol, TextAnchor.MiddleCenter);
        var b = new Btn { t = q.transform, rend = q.GetComponent<Renderer>(), press = act };
        list.Add(b);
        return b;
    }
}
