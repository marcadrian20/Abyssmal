using UnityEngine;

/// <summary>
/// Static class that defines all layer constants for the game.
/// Place this file in a "Scripts/Core" or "Scripts/Utils" folder.
/// </summary>
public static class Layers
{
    // Layer numbers (make sure these match your Unity Editor settings)
    public const int EnvironmentLayer = 8;
    public const int PlayerLayer = 9;
    public const int PlayerProjectilesLayer = 10;
    public const int EnemiesLayer = 11;
    public const int EnemyProjectilesLayer = 12;
    public const int InteractiveLayer = 13;
    public const int TrapsLayer = 14;
    public const int CollectiblesLayer = 15;
    public const int TriggersLayer = 16;

    // Layer names (for more readable code)
    public const string EnvironmentLayerName = "Environment";
    public const string PlayerLayerName = "Player";
    public const string PlayerProjectilesLayerName = "PlayerProjectiles";
    public const string EnemiesLayerName = "Enemies";
    public const string EnemyProjectilesLayerName = "EnemyProjectiles";
    public const string InteractiveLayerName = "Interactive";
    public const string TrapsLayerName = "Traps";
    public const string CollectiblesLayerName = "Collectibles";
    public const string TriggersLayerName = "Triggers";

    // Layer masks for efficient collision checks
    public static readonly int PlayerMask = 1 << PlayerLayer;
    public static readonly int EnvironmentMask = 1 << EnvironmentLayer;
    public static readonly int EnemiesMask = 1 << EnemiesLayer;
    public static readonly int CollectiblesMask = 1 << CollectiblesLayer;
    public static readonly int AllProjectilesMask = (1 << PlayerProjectilesLayer) | (1 << EnemyProjectilesLayer);
    
    // Combined masks for common checks
    public static readonly int GroundMask = EnvironmentMask | (1 << InteractiveLayer);
    public static readonly int DamageSourcesMask = EnemiesMask | (1 << EnemyProjectilesLayer) | (1 << TrapsLayer);
    public static readonly int InteractableMask = (1 << InteractiveLayer) | (1 << CollectiblesLayer) | (1 << TriggersLayer);
    
    // Utility methods
    public static void SetToLayer(this GameObject gameObject, int layer)
    {
        if (gameObject == null) return;
        gameObject.layer = layer;
    }
    
    public static void SetChildrenToLayer(this GameObject gameObject, int layer, bool recursive = true)
    {
        if (gameObject == null) return;
        
        foreach (Transform child in gameObject.transform)
        {
            child.gameObject.layer = layer;
            
            if (recursive && child.childCount > 0)
            {
                SetChildrenToLayer(child.gameObject, layer, true);
            }
        }
    }
}