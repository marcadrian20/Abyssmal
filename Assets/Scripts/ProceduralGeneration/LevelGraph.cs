using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ProceduralGeneration
{
    public class LevelGraph
    {
        public Dictionary<int, Room> rooms = new Dictionary<int, Room>();
        public List<GraphRule> rules = new List<GraphRule>();

        private List<AbilityType> abilityProgression = new List<AbilityType>()
        {
            AbilityType.WallJump,
            AbilityType.Dash,
            AbilityType.DoubleJump,
            AbilityType.Grapple
        };

        public void AddRule(GraphRule rule)
        {
            rules.Add(rule);
            rules = rules.OrderByDescending(r => r.priority).ToList();
        }

        public void ApplyRules()
        {
            foreach (var rule in rules.Where(r => r.isEnabled))
            {
                foreach (var room in rooms.Values.ToList())
                {
                    if (rule.CanApply(this, room))
                    {
                        rule.Apply(this, room);
                    }
                }
            }
        }
        public Room GetStartRoom()
        {
            return rooms.Values.FirstOrDefault(r => r.type == RoomType.Start);
        }

        public void AddConnection(int fromRoomId, int toRoomId)
        {
            if (rooms.ContainsKey(fromRoomId) && rooms.ContainsKey(toRoomId))
            {
                rooms[fromRoomId].AddConnection(toRoomId);
                rooms[toRoomId].AddConnection(fromRoomId);
            }
            else
            {
                Debug.LogWarning($"Cannot add connection from {fromRoomId} to {toRoomId}: one of the rooms does not exist.");
            }
        }
        public List<Room> GetRoomByType(RoomType type)
        {
            return rooms.Values.Where(r => r.type == type).ToList();
        }

        public List<AbilityType> GetAbilityProgression()
        {
            return new List<AbilityType>(abilityProgression);
        }
        public List<Room> GetShortestPath(int fromRoomId, int toRoomId)
        {//BFS search
            var queue = new Queue<int>();
            var visited = new HashSet<int>();
            var parentMap = new Dictionary<int, int>();
            queue.Enqueue(fromRoomId);
            visited.Add(fromRoomId);
            while (queue.Count > 0)
            {
                int currentRoomId = queue.Dequeue();
                if (currentRoomId == toRoomId)
                {
                    // Reconstruct path
                    var path = new List<Room>();
                    int pathId = toRoomId;
                    while (pathId != fromRoomId)
                    {
                        path.Add(rooms[pathId]);
                        pathId = parentMap[pathId];
                    }
                    path.Add(rooms[fromRoomId]);
                    path.Reverse();
                    return path;
                }
                if (rooms.ContainsKey(currentRoomId))
                {
                    // Visit all neighbors
                    foreach (var neighborId in rooms[currentRoomId].connections)
                    {
                        if (!visited.Contains(neighborId))
                        {
                            visited.Add(neighborId);
                            parentMap[neighborId] = currentRoomId;
                            queue.Enqueue(neighborId);
                        }
                    }
                }
            }
            return new List<Room>(); // No path found

        }
    }

}