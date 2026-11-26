using UnityEngine;

public sealed class FloatingText : MonoBehaviour
{
    private const float Lifetime = 1.2f;

    private TextMesh mesh;
    private float age;
    private Color startColor;

    public static void Spawn(Vector3 position, string message)
    {
        TextMesh created = TextFactory.Create(null, "FloatingText", message, position, 0.0125f, new Color(1f, 0.9f, 0.3f), TextAnchor.MiddleCenter);
        created.gameObject.AddComponent<FloatingText>();
    }

    private void Awake()
    {
        mesh = GetComponent<TextMesh>();
        startColor = mesh.color;
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * (0.5f * Time.deltaTime);

        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 away = transform.position - camera.transform.position;
            if (away.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(away);
            }
        }

        Color color = startColor;
        color.a = 1f - Mathf.Clamp01(age / Lifetime);
        mesh.color = color;

        if (age >= Lifetime)
        {
            Destroy(gameObject);
        }
    }
}
