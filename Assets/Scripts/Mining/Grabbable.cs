using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    [SerializeField] private bool snapToHand;
    [SerializeField] private Vector3 heldLocalPosition;
    [SerializeField] private Vector3 heldLocalEuler;
    [Header("Grip tuning (added on top of the held pose, in hand space)")]
    [SerializeField] private Vector3 gripPositionOffset;
    [SerializeField] private Vector3 gripEulerOffset;
    [SerializeField] private bool mirrorOffsetForLeftHand = true;
    [SerializeField] private float throwMultiplier = 1f;
    [SerializeField] private float maxThrowSpeed = 12f;

    private Rigidbody body;
    private GrabHand holder;
    private Collider[] colliders;

    public bool IsHeld => holder != null;
    public GrabHand Holder => holder;
    public Rigidbody Body => body;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
    }

    public void BeginGrab(GrabHand hand)
    {
        if (holder != null && holder != hand)
        {
            holder.ForceRelease();
        }

        holder = hand;
        body.isKinematic = true;
        body.useGravity = false;
        transform.SetParent(hand.HandTransform, true);
        if (snapToHand)
        {
            Vector3 positionOffset = gripPositionOffset;
            Vector3 eulerOffset = gripEulerOffset;
            if (mirrorOffsetForLeftHand && hand.Node == XRNode.LeftHand)
            {
                positionOffset.x = -positionOffset.x;
                eulerOffset.y = -eulerOffset.y;
                eulerOffset.z = -eulerOffset.z;
            }

            Quaternion offset = Quaternion.Euler(eulerOffset);
            transform.localRotation = offset * Quaternion.Euler(heldLocalEuler);
            transform.localPosition = offset * heldLocalPosition + positionOffset;
        }

        OnGrabbed(hand);
    }

    public void EndGrab(Vector3 velocity, Vector3 angularVelocity)
    {
        if (holder == null)
        {
            return;
        }

        GrabHand previous = holder;
        holder = null;
        transform.SetParent(null, true);
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        PhysicsCompat.SetVelocity(body, Vector3.ClampMagnitude(velocity * throwMultiplier, maxThrowSpeed));
        body.angularVelocity = angularVelocity;
        OnReleased(previous);
    }

    protected virtual void OnGrabbed(GrabHand hand)
    {
    }

    protected virtual void OnReleased(GrabHand hand)
    {
    }

    public float SqrDistanceTo(Vector3 point)
    {
        float best = float.MaxValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null || !c.enabled)
            {
                continue;
            }

            Vector3 closest = c.ClosestPoint(point);
            float d = (closest - point).sqrMagnitude;
            if (d < best)
            {
                best = d;
            }
        }

        return best;
    }
}
