using System.Collections.Generic;
using UnityEngine;

public sealed class SellBin : MonoBehaviour
{
    [SerializeField] private Vector3 boxCenter = new Vector3(0f, 0.3f, 0f);
    [SerializeField] private Vector3 boxSize = new Vector3(0.8f, 0.6f, 0.8f);
    [SerializeField] private int coinsPerCrystal = 5;
    [SerializeField] private float checkIntervalSeconds = 0.25f;

    private readonly Collider[] hits = new Collider[32];
    private readonly HashSet<CrystalChunk> counted = new HashSet<CrystalChunk>();
    private AudioSource audioSource;
    private AudioClip coinClip;
    private float nextCheck;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        coinClip = PhysicsCompat.CreateThud(1200f, 0.15f, 0.2f);
    }

    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + checkIntervalSeconds;

        Wallet wallet = Wallet.Instance;
        if (wallet == null)
        {
            return;
        }

        Vector3 center = transform.TransformPoint(boxCenter);
        Vector3 half = Vector3.Scale(boxSize, transform.lossyScale) * 0.5f;
        int count = Physics.OverlapBoxNonAlloc(center, half, hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);

        counted.Clear();
        int earned = 0;

        for (int i = 0; i < count; i++)
        {
            CrystalChunk chunk = hits[i].GetComponentInParent<CrystalChunk>();
            if (chunk == null || chunk.IsHeld || !counted.Add(chunk))
            {
                continue;
            }

            earned += coinsPerCrystal;
            FloatingText.Spawn(chunk.transform.position + Vector3.up * 0.2f, "+" + coinsPerCrystal);
            Destroy(chunk.gameObject);
        }

        if (earned > 0)
        {
            wallet.Add(earned);
            audioSource.pitch = Random.Range(0.95f, 1.1f);
            audioSource.PlayOneShot(coinClip, 1f);
        }
    }
}
