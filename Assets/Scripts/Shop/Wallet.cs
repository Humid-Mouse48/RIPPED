using System;
using UnityEngine;

public sealed class Wallet : MonoBehaviour
{
    private const string CoinsKey = "ripped.coins";
    private const string PickaxeKey = "ripped.pickaxe";

    [SerializeField] private int startingCoins = 60;

    private int coins;
    private bool ownsPickaxe;

    public static Wallet Instance { get; private set; }

    public event Action Changed;

    public int Coins => coins;
    public bool OwnsPickaxe => ownsPickaxe;

    private void Awake()
    {
        Instance = this;
        coins = PlayerPrefs.GetInt(CoinsKey, startingCoins);
        ownsPickaxe = PlayerPrefs.GetInt(PickaxeKey, 0) == 1;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Add(int amount)
    {
        coins += amount;
        Save();
    }

    public bool TrySpend(int amount)
    {
        if (coins < amount)
        {
            return false;
        }

        coins -= amount;
        Save();
        return true;
    }

    public void SetOwnsPickaxe(bool owns)
    {
        ownsPickaxe = owns;
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.SetInt(PickaxeKey, ownsPickaxe ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
