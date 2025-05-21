using UnityEngine;
public class PathfindingHelper
{
    public static Vector2[] CalculatePath(Vector2 start, Vector2 end, float stepSize)
    {
        // Placeholder for path calculation logic
        // This should return an array of waypoints from start to end
        return new Vector2[] { start, end };
    }

    public static void DrawPath(Vector2[] path)
    {
        // Placeholder for drawing the path in the editor
        // This could use Gizmos or other methods to visualize the path
    }

    public static bool IsWithinPerimeter(Vector2 position, Vector2 center, float radius)
    {
        return Vector2.Distance(position, center) <= radius;
    }
}