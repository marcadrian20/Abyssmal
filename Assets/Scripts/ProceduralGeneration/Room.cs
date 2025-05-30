using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace ProceduralGeneration
{
    public enum RoomType
    {
        Start, Hub, Corridor, Branch, Ability, End
    }

    public enum AbilityType
    {
        WallJump, Dash, DoubleJump, Grapple
    }
    public struct Doorway
    {
        public int roomA_Id;
        public int roomB_Id;
        public Vector2 position; // Center of the doorway
        public enum Orientation { Horizontal, Vertical }
        public Orientation orientation;
        public float size; // Actual size of the door opening (e.g., width for vertical, height for horizontal)

        public Doorway(int idA, int idB, Vector2 pos, Orientation ori, float doorSize)
        {
            roomA_Id = idA;
            roomB_Id = idB;
            position = pos;
            orientation = ori;
            size = doorSize;
        }
    }
    [System.Serializable]
    public class Room
    {
        public int id;
        public RoomType type;
        public Vector2 position;
        public List<Vector2> voronoiPoints = new List<Vector2>();
        public HashSet<int> connections = new HashSet<int>();
        public List<AbilityType> abilitiesRequired = new List<AbilityType>();
        public AbilityType? abilityGranted;

        // Additional properties for metroidvania progression
        public bool isVisited = false;
        public bool isAccessible = false;
        public int difficulty = 1;
        public bool isFixed = false;
        public Vector2 size = Vector2.one;

        public Room(int id, RoomType type)
        {
            this.id = id;
            this.type = type;
            this.position = Vector2.zero;
        }

        // Check if player can access this room with current abilities
        public bool CanAccess(HashSet<AbilityType> playerAbilities)
        {
            return abilitiesRequired.All(ability => playerAbilities.Contains(ability));
        }

        // Add a connection to another room
        public void AddConnection(int roomId)
        {
            connections.Add(roomId);
        }
        // public bool CanAddMoreConnections()
        // {
        //     return connections.Count < maxTotalConnections;
        // }
        // Remove a connection
        public void RemoveConnection(int roomId)
        {
            connections.Remove(roomId);
        }

        public Rect GetBounds()
        {
            float minX = position.x - size.x / 2f;
            float minY = position.y - size.y / 2f;
            return new Rect(minX, minY, size.x, size.y);
        }
    }
}