
using UnityEngine;

public class PatrolRoute
{
    public Transform[] waypoints; // Array of waypoints for patrolling
    public float patrolSpeed = 2f; // Speed at which the enemy moves between waypoints
    private int currentWaypointIndex = 0; // Index of the current waypoint
    public int CurrentWaypointIndex => currentWaypointIndex;
    public Transform CurrentWaypoint => waypoints[currentWaypointIndex];
    public void MoveToNextWaypoint(Transform enemyTransform)
    {
        if (waypoints.Length == 0) return;

        // Move towards the current waypoint
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        enemyTransform.position = Vector2.MoveTowards(enemyTransform.position, targetWaypoint.position, patrolSpeed * Time.deltaTime);

        // Check if the enemy has reached the current waypoint
        if (Vector2.Distance(enemyTransform.position, targetWaypoint.position) < 0.1f)
        {
            // Update to the next waypoint
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }
}