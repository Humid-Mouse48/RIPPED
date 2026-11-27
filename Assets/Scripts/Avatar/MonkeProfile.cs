using System;
using UnityEngine;

// Saved player look: monke color + name. The in-game computer edits this,
// MonkeAvatar reads it and listens for changes.
public static class MonkeProfile
{
    const string KeyColor = "ripped_monke_color";
    const string KeyName = "ripped_monke_name";

    static UnityEngine.Color _color = new UnityEngine.Color(0.55f, 0.36f, 0.2f);
    static string _name = "MONKE";
    static bool _loaded;

    // Both events fire so either name works for subscribers.
    public static event Action OnChanged;
    public static event Action Changed;

    public static UnityEngine.Color Color
    {
        get { Load(); return _color; }
        set { Load(); _color = value; Save(); Raise(); }
    }

    // Alias
    public static UnityEngine.Color BodyColor
    {
        get { return Color; }
        set { Color = value; }
    }

    public static string Name
    {
        get { Load(); return _name; }
        set
        {
            Load();
            _name = string.IsNullOrEmpty(value) ? "MONKE" : value;
            Save();
            Raise();
        }
    }

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;
        string hex = PlayerPrefs.GetString(KeyColor, "");
        UnityEngine.Color c;
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString("#" + hex, out c)) _color = c;
        _name = PlayerPrefs.GetString(KeyName, _name);
    }

    public static void Save()
    {
        PlayerPrefs.SetString(KeyColor, ColorUtility.ToHtmlStringRGB(_color));
        PlayerPrefs.SetString(KeyName, _name);
        PlayerPrefs.Save();
    }

    static void Raise()
    {
        if (OnChanged != null) OnChanged();
        if (Changed != null) Changed();
    }
}
