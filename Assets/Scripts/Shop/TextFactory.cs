using UnityEngine;

public static class TextFactory
{
    private static Font cachedFont;

    public static Font GetFont()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        TextMesh existing = Object.FindFirstObjectByType<TextMesh>();
        if (existing != null && existing.font != null)
        {
            cachedFont = existing.font;
            return cachedFont;
        }

        try
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            cachedFont = null;
        }

        if (cachedFont == null)
        {
            try
            {
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
                cachedFont = null;
            }
        }

        return cachedFont;
    }

    public static TextMesh Create(Transform parent, string objectName, string text, Vector3 localPosition, float characterSize, Color color, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(objectName);
        if (parent != null)
        {
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
        }
        else
        {
            textObject.transform.position = localPosition;
        }

        TextMesh mesh = textObject.AddComponent<TextMesh>();
        Font font = GetFont();
        mesh.font = font;
        mesh.text = text;
        mesh.fontSize = 80;
        mesh.characterSize = characterSize;
        mesh.anchor = anchor;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;

        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
        if (font != null && meshRenderer != null)
        {
            meshRenderer.sharedMaterial = font.material;
        }

        return mesh;
    }
}
