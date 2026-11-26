using UnityEngine;

public sealed class Shop : MonoBehaviour
{
    [SerializeField] private int pickaxePrice = 50;
    [SerializeField] private GameObject pickaxePrefab;
    [SerializeField] private Transform pickaxeSpawn;
    [SerializeField] private TextMesh coinText;
    [SerializeField] private TextMesh priceText;
    [SerializeField] private TextMesh statusText;
    [SerializeField] private float fallRespawnHeight = -6f;
    [SerializeField] private float statusSeconds = 4f;

    private Wallet wallet;
    private Pickaxe pickaxe;
    private AudioSource audioSource;
    private AudioClip buyClip;
    private AudioClip denyClip;
    private float statusClearTime;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        buyClip = PhysicsCompat.CreateThud(700f, 0.25f, 0.15f);
        denyClip = PhysicsCompat.CreateThud(120f, 0.25f, 0.4f);
    }

    private void Start()
    {
        wallet = Wallet.Instance;
        if (wallet == null)
        {
            Debug.LogError("Shop needs a Wallet in the scene.", this);
            enabled = false;
            return;
        }

        wallet.Changed += Refresh;

        if (wallet.OwnsPickaxe)
        {
            SpawnPickaxe();
            SetStatus("WELCOME BACK");
        }
        else
        {
            SetStatus("TOUCH BUY TO GET A PICKAXE");
        }

        Refresh();
        statusClearTime = 0f;
    }

    private void OnDestroy()
    {
        if (wallet != null)
        {
            wallet.Changed -= Refresh;
        }
    }

    private void Update()
    {
        if (statusClearTime > 0f && Time.time >= statusClearTime)
        {
            statusClearTime = 0f;
            SetStatusImmediate(wallet.OwnsPickaxe ? "MINE ROCKS, SELL CRYSTALS" : "TOUCH BUY TO GET A PICKAXE");
        }

        if (pickaxe != null && !pickaxe.IsHeld && pickaxe.transform.position.y < fallRespawnHeight)
        {
            MovePickaxeToCounter();
        }
    }

    public void BuyPickaxe()
    {
        if (wallet.OwnsPickaxe)
        {
            SetStatus("YOU ALREADY OWN A PICKAXE");
            audioSource.PlayOneShot(denyClip);
            return;
        }

        if (!wallet.TrySpend(pickaxePrice))
        {
            SetStatus("NEED " + (pickaxePrice - wallet.Coins) + " MORE COINS");
            audioSource.PlayOneShot(denyClip);
            return;
        }

        wallet.SetOwnsPickaxe(true);
        SpawnPickaxe();
        SetStatus("PICKAXE BOUGHT");
        audioSource.PlayOneShot(buyClip);
    }

    public void RecallPickaxe()
    {
        if (!wallet.OwnsPickaxe)
        {
            SetStatus("BUY A PICKAXE FIRST");
            audioSource.PlayOneShot(denyClip);
            return;
        }

        if (pickaxe == null)
        {
            SpawnPickaxe();
        }
        else
        {
            MovePickaxeToCounter();
        }

        SetStatus("PICKAXE RETURNED");
        audioSource.PlayOneShot(buyClip);
    }

    private void SpawnPickaxe()
    {
        if (pickaxePrefab == null || pickaxeSpawn == null)
        {
            Debug.LogError("Shop is missing its pickaxe prefab or spawn point.", this);
            return;
        }

        GameObject created = Instantiate(pickaxePrefab, pickaxeSpawn.position, pickaxeSpawn.rotation);
        pickaxe = created.GetComponent<Pickaxe>();
    }

    private void MovePickaxeToCounter()
    {
        if (pickaxe == null)
        {
            return;
        }

        if (pickaxe.IsHeld && pickaxe.Holder != null)
        {
            pickaxe.Holder.ForceRelease();
        }

        PhysicsCompat.SetVelocity(pickaxe.Body, Vector3.zero);
        pickaxe.Body.angularVelocity = Vector3.zero;
        pickaxe.transform.SetPositionAndRotation(pickaxeSpawn.position, pickaxeSpawn.rotation);
    }

    private void Refresh()
    {
        if (coinText != null)
        {
            coinText.text = "COINS: " + wallet.Coins;
        }

        if (priceText != null)
        {
            priceText.text = wallet.OwnsPickaxe ? "PICKAXE - OWNED" : "PICKAXE - " + pickaxePrice + " COINS";
        }
    }

    private void SetStatus(string message)
    {
        SetStatusImmediate(message);
        statusClearTime = Time.time + statusSeconds;
    }

    private void SetStatusImmediate(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
