using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
// ============================================================
// SkeletonAI
// Enemy movement, chasing, attack logic, and target handling. (Этот скрипт отвечает за: enemy movement, chasing, attack logic, and target handling.)
// ============================================================
public class SkeletonAI : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("References")]
    public Transform player;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Detection")]
    public float detectDistance = 10f;
    public float attackDistance = 1.9f;
    public float stopChaseDistance = 25f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Movement")]
    public float chaseMoveSpeed = 3.8f;
    public float patrolMoveSpeed = 2.0f;
    public float rotationSpeed = 7f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Patrol")]
    public bool enablePatrol = true;
    public float patrolDirectionChangeMin = 1.5f;
    public float patrolDirectionChangeMax = 3.5f;
    public float patrolPauseMin = 0.6f;
    public float patrolPauseMax = 1.4f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Attack")]
    public float damage = 10f;
    public float attackCooldown = 1.2f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Debug")]
    public bool drawDetectionRadius = true;

    private Rigidbody rb;

    private float attackTimer;
    private bool isChasing;

    private Vector3 patrolDirection = Vector3.zero;
    private float patrolTimer;
    private bool patrolWaiting;

    // Initialize runtime state. (Initialize runtime state)
    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        PickNewPatrolState(true);
    }

    // Run per-frame logic. (Run per-frame logic)
    private void Update()
    {
        if (player == null)
            return;

        UpdateChaseState();
        UpdateAttackTimer();

        if (isChasing)
        {
            Vector3 chaseDir = GetDirectionToPlayer();
            RotateToDirection(chaseDir);
        }
        else if (enablePatrol && !patrolWaiting && patrolDirection.sqrMagnitude > 0.001f)
        {
            RotateToDirection(patrolDirection);
        }

        UpdatePatrolTimer();
    }

    // Run physics-step logic. (Run physics-step logic)
    private void FixedUpdate()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (isChasing)
            HandleChase(distance);
        else
            HandlePatrol();
    }

    // Handle Chase. (Handle Chase)
    private void HandleChase(float distanceToPlayer)
    {
        Vector3 chaseDir = GetDirectionToPlayer();

        if (distanceToPlayer > attackDistance)
        {
            Move(chaseDir, chaseMoveSpeed);
        }
        else
        {
            StopHorizontalMovement();
            AttackPlayer();
        }
    }

    // Handle Patrol. (Handle Patrol)
    private void HandlePatrol()
    {
        if (!enablePatrol)
        {
            StopHorizontalMovement();
            return;
        }

        if (patrolWaiting || patrolDirection.sqrMagnitude < 0.001f)
        {
            StopHorizontalMovement();
            return;
        }

        Move(patrolDirection, patrolMoveSpeed);
    }

    // Update Chase State. (Update Chase State)
    private void UpdateChaseState()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (!isChasing && distance <= detectDistance)
            isChasing = true;

        if (isChasing && distance >= stopChaseDistance)
        {
            isChasing = false;
            PickNewPatrolState(true);
        }
    }

    // Update Attack Timer. (Update Attack Timer)
    private void UpdateAttackTimer()
    {
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
    }

    // Update Patrol Timer. (Update Patrol Timer)
    private void UpdatePatrolTimer()
    {
        if (isChasing || !enablePatrol)
            return;

        patrolTimer -= Time.deltaTime;

        if (patrolTimer <= 0f)
            PickNewPatrolState(false);
    }

    // Pick New Patrol State. (Pick New Patrol State)
    private void PickNewPatrolState(bool immediateMove)
    {
        if (!enablePatrol)
            return;

        if (!immediateMove && Random.value < 0.35f)
        {
            patrolWaiting = true;
            patrolDirection = Vector3.zero;
            patrolTimer = Random.Range(patrolPauseMin, patrolPauseMax);
            return;
        }

        patrolWaiting = false;

        Vector2 random2D = Random.insideUnitCircle.normalized;

        if (random2D.sqrMagnitude < 0.01f)
            random2D = Vector2.right;

        patrolDirection = new Vector3(random2D.x, 0f, random2D.y).normalized;
        patrolTimer = Random.Range(patrolDirectionChangeMin, patrolDirectionChangeMax);
    }

    // Get Direction To Player. (Get Direction To Player)
    private Vector3 GetDirectionToPlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return dir.normalized;
    }

    // Rotate To Direction. (Rotate To Direction)
    private void RotateToDirection(Vector3 dir)
    {
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // Move. (Move)
    private void Move(Vector3 dir, float speed)
    {
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            StopHorizontalMovement();
            return;
        }

        dir.Normalize();

        Vector3 velocity = rb.linearVelocity;

        rb.linearVelocity = new Vector3(
            dir.x * speed,
            velocity.y,
            dir.z * speed
        );
    }

    // Stop Horizontal Movement. (Stop Horizontal Movement)
    private void StopHorizontalMovement()
    {
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

    // Attack Player. (Attack Player)
    private void AttackPlayer()
    {
        if (attackTimer > 0f)
            return;

        attackTimer = attackCooldown;

        PlayerHealth hp = player.GetComponent<PlayerHealth>();

        if (hp != null)
            hp.TakeDamage(damage);
    }

    // On Draw Gizmos Selected. (On Draw Gizmos Selected)
    private void OnDrawGizmosSelected()
    {
        if (!drawDetectionRadius)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
