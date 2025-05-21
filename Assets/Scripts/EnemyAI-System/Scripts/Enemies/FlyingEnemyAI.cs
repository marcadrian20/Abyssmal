using UnityEngine;

public class FlyingEnemyAI : EnemyAI
{
    [Header("Flying Settings")]
    public float flyHeight = 3f;
    public float verticalSpeed = 2f;
    public float patrolSpeed = 2f;
    public float engageRange = 0.5f;


    // [Header("Patrol")]
    // public PatrolRoute patrolRoute;
    // private bool isPatrolling = true;
    private MeleeEnemyCombat meleeCombat;
    private EnemyAnimation enemyAnimation;
    void Awake()
    {
        enemyAnimation = GetComponent<EnemyAnimation>();
        meleeCombat = GetComponent<MeleeEnemyCombat>();

    }
    // protected override void Start()
    // {
    //     base.Start();
    //     meleeCombat = GetComponent<MeleeEnemyCombat>();

    //     if (patrolRoute != null && patrolRoute.waypoints != null && patrolRoute.waypoints.Length > 0)
    //     {
    //         target = patrolRoute.waypoints[0];
    //         isPatrolling = true;
    //     }
    //     else
    //     {
    //         isPatrolling = false;
    //     }
    // }

    protected void Update()
    {
        // base.Update();

        // if (isPatrolling && patrolRoute != null && patrolRoute.waypoints.Length > 0)
        // {
        //     float dist = Vector2.Distance(transform.position, target.position);
        //     if (dist < 0.2f)
        //     {
        //         patrolRoute.MoveToNextWaypoint(transform);
        //         target = patrolRoute.waypoints[patrolRoute.CurrentWaypointIndex];
        //     }
        // }
        // Aerial melee attack logic
        if (target != null && meleeCombat != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            if (distanceToTarget <= engageRange) // Expose AttackRange in MeleeEnemyCombat
            {
                meleeCombat.Attack();
            }
        }
        // else: you can add aggro logic here to chase the player if needed
    }

    // protected override void Move(Vector2 force)
    // {
    //     // Flying enemies move freely in X and Y
    //     rb.linearVelocity = new Vector2(force.x, force.y);

    //     // Flip sprite if needed
    //     if ((force.x > 0 && !isFacingRight) || (force.x < 0 && isFacingRight))
    //         Flip();
    // }

    // protected override void FixedUpdate()
    // {
    //     if (path == null) return;
    //     if (currentWaypoint >= path.vectorPath.Count)
    //     {
    //         reachedEndOfPath = true;
    //         return;
    //     }
    //     else
    //     {
    //         reachedEndOfPath = false;
    //     }

    //     Vector2 nextWaypoint = path.vectorPath[currentWaypoint];
    //     Vector2 direction = (nextWaypoint - rb.position).normalized;
    //     Vector2 force = direction * patrolSpeed;

    //     Move(force);

    //     float distance = Vector2.Distance(rb.position, nextWaypoint);
    //     if (distance < nextWaypointDistance)
    //     {
    //         currentWaypoint++;
    //     }
    // }
    void OnDrawGizmosSelected()
    {
        // Draw engage range (from enemy position)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, engageRange);

        // Draw attack range (from attackPoint, if assigned)
        Gizmos.color = Color.red;
    }
}