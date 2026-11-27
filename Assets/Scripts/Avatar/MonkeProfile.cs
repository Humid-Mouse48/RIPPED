using System;
using UnityEngine;

public static class MonkeProfile
{
    private const string ColorKey = "ripped.monkecolor";
    private const string NameKey = "ripped.monkename";

    private static Color currentColor = Color.white;
    private static string currentName = "MONKE";
    private static bool loaded;

    public static event Action Changed;

    public static Color CurrentColor
    {
        get
        {
            Load();
            return currentColor;
        }
        set
        {
            Load();
            currentColor = value;
            Save();
            Changed?.Invoke();
        }
    }

    public static string CurrentName
    {
        get
        {
            Load();
            return currentName;
        }
        set
        {
            Load();
            currentName = string.IsNullOrEmpty(value) ? "MONKE" : value;
            Save();
            Changed?.Invoke();
        }
    }

    private static void Load()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        string hex = PlayerPrefs.GetString(ColorKey, string.Empty);
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString("#" + hex, out Color parsed))
        {
            currentColor = parsed;
        }

        currentName = PlayerPrefs.GetString(NameKey, currentName);
    }

    private static void Save()
    {
        PlayerPrefs.SetString(ColorKey, ColorUtility.ToHtmlStringRGB(currentColor));
        PlayerPrefs.SetString(NameKey, currentName);
        PlayerPrefs.Save();
    }
}
