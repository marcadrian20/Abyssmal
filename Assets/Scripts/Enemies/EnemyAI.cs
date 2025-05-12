using Pathfinding;
using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
    [Header("Pathfinding")]
    public Transform target;
    public float speed = 3f;
    public float nextWaypointDistance = 0.1f;

    protected Path path;
    protected int currentWaypoint = 0;
    protected bool reachedEndOfPath = false;

    protected Seeker seeker;
    protected Rigidbody2D rb;
    protected bool isFacingRight = true;

    protected virtual void Start()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        InvokeRepeating(nameof(UpdatePath), 0f, 0.5f);
    }

    protected virtual void UpdatePath()
    {
        if (target != null && seeker.IsDone())
            seeker.StartPath(rb.position, target.position, OnPathComplete);
    }

    protected virtual void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (path == null) return;
        if (currentWaypoint >= path.vectorPath.Count)
        {
            reachedEndOfPath = true;
            return;
        }
        else
        {
            reachedEndOfPath = false;
        }

        Vector2 nextWaypoint = path.vectorPath[currentWaypoint];
        Vector2 direction = (nextWaypoint - rb.position).normalized;

        // Only move on X axis
        Vector2 force = new Vector2(direction.x * speed, rb.linearVelocity.y);

        // Call jump logic (does nothing in base)
        TryJump(nextWaypoint);

        Move(force);

        float distance = Vector2.Distance(rb.position, nextWaypoint);
        if (distance < nextWaypointDistance)
        {
            currentWaypoint++;
        }
    }
    protected virtual void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    // Overridable jump logic
    protected virtual void TryJump(Vector2 nextWaypoint) { }
    // Allow derived classes to override movement (e.g., for jumping, flying, etc.)
    protected virtual void Move(Vector2 force)
    {
        rb.linearVelocity = new Vector2(force.x, rb.linearVelocity.y);
        // Flip the enemy if necessary
        if ((force.x > 0 && !isFacingRight) || (force.x < 0 && isFacingRight))
        {
            Flip();
        }
    }

    // Virtual attack method
    public abstract void Attack();
}