using UnityEngine;

public enum ShopAction
{
    BuyPickaxe,
    RecallPickaxe
}

public sealed class ShopButton : MonoBehaviour
{
    [SerializeField] private Shop shop;
    [SerializeField] private ShopAction action;
    [SerializeField] private float pokeRadius = 0.1f;
    [SerializeField] private float cooldownSeconds = 0.6f;
    [SerializeField] private float pressDepth = 0.04f;

    private GrabHand[] hands;
    private bool[] wasInside;
    private Collider trigger;
    private Vector3 restPosition;
    private float pressAmount;
    private float nextPressTime;

    public void Configure(Shop owner, ShopAction buttonAction)
    {
        shop = owner;
        action = buttonAction;
    }

    private void Start()
    {
        hands = FindObjectsByType<GrabHand>(FindObjectsSortMode.None);
        wasInside = new bool[hands.Length];
        trigger = GetComponent<Collider>();
        restPosition = transform.localPosition;
    }

    private void Update()
    {
        if (hands != null && trigger != null && shop != null)
        {
            for (int i = 0; i < hands.Length; i++)
            {
                if (hands[i] == null || hands[i].HandTransform == null)
                {
                    continue;
                }

                Vector3 handPosition = hands[i].HandTransform.position;
                Vector3 closest = trigger.ClosestPoint(handPosition);
                bool inside = (closest - handPosition).sqrMagnitude < pokeRadius * pokeRadius;

                if (inside && !wasInside[i] && Time.time >= nextPressTime)
                {
                    Press(hands[i]);
                }

                wasInside[i] = inside;
            }
        }

        pressAmount = Mathf.MoveTowards(pressAmount, 0f, Time.deltaTime * 6f);
        transform.localPosition = restPosition + Vector3.forward * (pressDepth * pressAmount);
    }

    private void Press(GrabHand hand)
    {
        nextPressTime = Time.time + cooldownSeconds;
        pressAmount = 1f;
        hand.Pulse(0.6f, 0.08f);

        if (action == ShopAction.BuyPickaxe)
        {
            shop.BuyPickaxe();
        }
        else
        {
            shop.RecallPickaxe();
        }
    }
}
