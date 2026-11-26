using UnityEngine;

public sealed class WalletHud : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset = new Vector3(0.2f, -0.18f, 0.8f);
    [SerializeField] private float characterSize = 0.006f;

    private TextMesh text;
    private Wallet wallet;

    private void Start()
    {
        Camera camera = Camera.main;
        wallet = Wallet.Instance;
        if (camera == null || wallet == null)
        {
            enabled = false;
            return;
        }

        text = TextFactory.Create(camera.transform, "CoinHud", string.Empty, localOffset, characterSize, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter);
        wallet.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (wallet != null)
        {
            wallet.Changed -= Refresh;
        }
    }

    private void Refresh()
    {
        if (text != null)
        {
            text.text = "COINS " + wallet.Coins;
        }
    }
}
