using UnityEngine;

public sealed class Monster : MonoBehaviour
{
    private enum State
    {
        Wander,
        Chase,
        Cooldown
    }

    [SerializeField] private ArmLocomotion player;
    [SerializeField] private Transform visual;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float wanderSpeed = 1.4f;
    [SerializeField] private float chaseSpeed = 3.4f;
    [SerializeField] private float detectRange = 14f;
    [SerializeField] private float loseRange = 24f;
    [SerializeField] private float catchDistance = 0.9f;
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float cooldownSeconds = 4f;
    [SerializeField] private float modelYawOffset;
    [SerializeField] private float bodyRadius = 0.4f;
    [SerializeField] private float chaseAnimationSpeed = 1f;

    private State state;
    private Vector3 home;
    private Vector3 wanderTarget;
    private float nextWanderTime;
    private float cooldownEnd;
    private Transform head;
    private Animation legacyAnimation;
    private AnimationState chaseState;
    private Animator animator;
    private AudioSource audioSource;
    private AudioClip growlClip;
    private float nextGrowlTime;

    public void Configure(ArmLocomotion playerRig, Transform model, LayerMask mask, float yawOffset)
    {
        player = playerRig;
        visual = model;
        groundMask = mask;
        modelYawOffset = yawOffset;
    }

    private void Start()
    {
        home = transform.position;
        wanderTarget = home;
        state = State.Wander;
        Camera camera = Camera.main;
        head = camera != null ? camera.transform : null;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 30f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.playOnAwake = false;
        growlClip = PhysicsCompat.CreateThud(70f, 0.8f, 0.7f);
        SetupAnimation();
        ApplyChaseAnimation(false);
    }

    private void SetupAnimation()
    {
        if (visual == null)
        {
            return;
        }

        legacyAnimation = visual.GetComponentInChildren<Animation>(true);
        if (legacyAnimation != null)
        {
            legacyAnimation.playAutomatically = false;
            legacyAnimation.cullingType = AnimationCullingType.AlwaysAnimate;
            AnimationState chosen = null;
            foreach (AnimationState animationState in legacyAnimation)
            {
                string lower = animationState.name.ToLowerInvariant();
                if (chosen == null || lower.Contains("run") || lower.Contains("walk") || lower.Contains("chase"))
                {
                    chosen = animationState;
                }
            }

            if (chosen != null)
            {
                chosen.wrapMode = WrapMode.Loop;
                chosen.time = 0f;
                chosen.speed = 0f;
                legacyAnimation.Play(chosen.name);
                chaseState = chosen;
            }
            return;
        }

        animator = visual.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("Monster has an Animator with no controller, so it cannot animate. Set glTFast's Animation Method to Legacy on monster.glb and reimport.", this);
            }
            return;
        }

        Debug.LogWarning("Monster found no Animation or Animator on its model. Set glTFast's Animation Method to Legacy on monster.glb and reimport.", this);
    }

    private void ApplyChaseAnimation(bool chasing)
    {
        float speed = chasing ? chaseAnimationSpeed : 0f;
        if (chaseState != null)
        {
            chaseState.speed = speed;
            if (!chasing)
            {
                chaseState.time = 0f;
            }
        }
        else if (animator != null)
        {
            animator.speed = speed;
        }
    }

    private void EnterState(State next)
    {
        if (state == next)
        {
            return;
        }

        state = next;
        ApplyChaseAnimation(next == State.Chase);
    }

    private void Update()
    {
        if (head == null)
        {
            Camera camera = Camera.main;
            head = camera != null ? camera.transform : null;
            if (head == null)
            {
                return;
            }
        }

        Vector3 toPlayer = head.position - transform.position;
        Vector3 flatToPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        float flatDistance = flatToPlayer.magnitude;
        switch (state)
        {
            case State.Wander:
                UpdateWander(flatDistance);
                if (flatDistance < detectRange && HasLineOfSight(toPlayer))
                {
                    EnterState(State.Chase);
                    Growl();
                }
                break;
            case State.Chase:
                MoveToward(flatToPlayer, chaseSpeed);
                if (flatDistance > loseRange)
                {
                    EnterState(State.Wander);
                }
                else if (flatDistance < catchDistance && Mathf.Abs(toPlayer.y) < 2.4f)
                {
                    CatchPlayer();
                }
                break;
            case State.Cooldown:
                if (Time.time >= cooldownEnd)
                {
                    EnterState(State.Wander);
                }
                break;
        }

        SnapToGround();
    }

    private void UpdateWander(float flatDistance)
    {
        if (Time.time >= nextWanderTime || (wanderTarget - transform.position).sqrMagnitude < 0.5f)
        {
            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            wanderTarget = home + new Vector3(circle.x, 0f, circle.y);
            nextWanderTime = Time.time + Random.Range(4f, 8f);
        }

        MoveToward(Vector3.ProjectOnPlane(wanderTarget - transform.position, Vector3.up), wanderSpeed);
    }

    private bool HasLineOfSight(Vector3 toPlayer)
    {
        Vector3 origin = transform.position + Vector3.up * 1.4f;
        Vector3 direction = head.position - origin;
        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude, groundMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    private void MoveToward(Vector3 flatDirection, float speed)
    {
        if (flatDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 direction = flatDirection.normalized;
        float step = speed * Time.deltaTime;
        if (!PathIsClear(direction, step))
        {
            Vector3 left = Quaternion.Euler(0f, -60f, 0f) * direction;
            Vector3 right = Quaternion.Euler(0f, 60f, 0f) * direction;
            if (PathIsClear(left, step))
            {
                direction = left;
            }
            else if (PathIsClear(right, step))
            {
                direction = right;
            }
            else
            {
                nextWanderTime = 0f;
                return;
            }
        }

        transform.position += direction * step;
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, modelYawOffset, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    private bool PathIsClear(Vector3 direction, float step)
    {
        Vector3 chest = transform.position + Vector3.up * 0.9f;
        if (Physics.SphereCast(chest, bodyRadius, direction, out RaycastHit wall, step + 0.3f, groundMask, QueryTriggerInteraction.Ignore) && wall.normal.y < 0.5f)
        {
            return false;
        }

        Vector3 ahead = transform.position + direction * (step + 0.6f) + Vector3.up * 1f;
        return Physics.Raycast(ahead, Vector3.down, 2.5f, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 4f, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 position = transform.position;
            position.y = Mathf.Lerp(position.y, hit.point.y, 1f - Mathf.Exp(-12f * Time.deltaTime));
            transform.position = position;
        }
    }

    private void CatchPlayer()
    {
        Growl();
        if (player != null)
        {
            player.Respawn();
        }

        transform.position = home;
        EnterState(State.Cooldown);
        cooldownEnd = Time.time + cooldownSeconds;
    }

    private void Growl()
    {
        if (Time.time < nextGrowlTime || audioSource == null)
        {
            return;
        }

        nextGrowlTime = Time.time + 3f;
        audioSource.PlayOneShot(growlClip, 1f);
    }
}
