using System.Collections;
using UnityEngine;

public sealed class MineableRock : MonoBehaviour
{
    [SerializeField] private int hitsToBreak = 4;
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform[] attachedCrystals = new Transform[0];
    [SerializeField] private int minChunks = 8;
    [SerializeField] private int maxChunks = 12;
    [SerializeField] private float burstSpeed = 2.2f;
    [SerializeField] private float chunkSpawnRadius = 0.2f;
    [SerializeField] private float hitCooldownSeconds = 0.25f;
    [SerializeField, Range(0f, 1f)] private float chunkScaleVariance = 0.25f;
    [Tooltip("Multiplies the rock's size at runtime. Lower this to make rocks smaller.")]
    [SerializeField, Range(0.05f, 2f)] private float sizeMultiplier = 0.4f;
    [Tooltip("Multiplies the size of every crystal chunk when it spawns. Lower this to make chunks smaller.")]
    [SerializeField, Range(0.05f, 2f)] private float chunkSizeMultiplier = 0.3f;

    private int hits;
    private float nextHitTime;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private AudioSource audioSource;
    private AudioClip hitClip;
    private AudioClip breakClip;
    private bool broken;

    public int Hits => hits;
    public int HitsRemaining => Mathf.Max(0, hitsToBreak - hits);
    public bool IsBroken => broken;

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        // Crystals stuck on the rock are never shown. Chunks only appear when the rock breaks.
        if (attachedCrystals != null)
        {
            for (int i = 0; i < attachedCrystals.Length; i++)
            {
                if (attachedCrystals[i] != null)
                {
                    attachedCrystals[i].gameObject.SetActive(false);
                }
            }
        }

        visualRoot.localScale *= sizeMultiplier;
        baseLocalPosition = visualRoot.localPosition;
        baseLocalScale = visualRoot.localScale;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.maxDistance = 25f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        hitClip = PhysicsCompat.CreateThud(150f, 0.18f, 0.55f);
        breakClip = PhysicsCompat.CreateThud(80f, 0.5f, 0.75f);
    }

    public bool TryHit(Vector3 point, Vector3 direction)
    {
        if (broken || Time.time < nextHitTime)
        {
            return false;
        }

        nextHitTime = Time.time + hitCooldownSeconds;
        hits++;
        if (hits >= hitsToBreak)
        {
            Break(point);
            return true;
        }

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(hitClip, 1f);
        StopAllCoroutines();
        StartCoroutine(Shake(direction));
        return true;
    }

    private void Break(Vector3 point)
    {
        broken = true;
        StopAllCoroutines();
        if (chunkPrefab != null)
        {
            int count = Random.Range(minChunks, maxChunks + 1);
            float radius = chunkSpawnRadius * sizeMultiplier;
            Vector3 center = transform.position + Vector3.up * 0.15f * sizeMultiplier;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Random.insideUnitSphere * radius;
                offset.y = Mathf.Abs(offset.y) * 0.6f;
                Vector3 position = center + offset;
                Vector3 direction = (offset + Vector3.up * 0.8f * radius).normalized;
                SpawnChunk(position, direction * Random.Range(burstSpeed * 0.5f, burstSpeed), Random.rotation);
            }
        }

        GameObject soundObject = new GameObject("RockBreakSound");
        soundObject.transform.position = transform.position;
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.maxDistance = 30f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.PlayOneShot(breakClip, 1f);
        Destroy(soundObject, 1.5f);
        Destroy(gameObject);
    }

    private void SpawnChunk(Vector3 position, Vector3 velocity, Quaternion rotation)
    {
        GameObject chunk = Instantiate(chunkPrefab, position, rotation);
        float scale = (1f + Random.Range(-chunkScaleVariance, chunkScaleVariance)) * chunkSizeMultiplier;
        chunk.transform.localScale *= scale;
        Rigidbody body = chunk.GetComponent<Rigidbody>();
        if (body != null)
        {
            PhysicsCompat.SetVelocity(body, velocity);
            body.angularVelocity = Random.insideUnitSphere * 6f;
        }
    }

    private IEnumerator Shake(Vector3 direction)
    {
        Vector3 push = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * 0.02f;
        float duration = 0.22f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float falloff = 1f - Mathf.Clamp01(t / duration);
            Vector3 jitter = Random.insideUnitSphere * 0.012f * falloff;
            visualRoot.localPosition = baseLocalPosition + push * falloff + jitter;
            visualRoot.localScale = baseLocalScale * (1f - 0.04f * falloff);
            yield return null;
        }

        visualRoot.localPosition = baseLocalPosition;
        visualRoot.localScale = baseLocalScale;
    }
}
