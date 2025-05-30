using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace ProceduralGeneration
{
    [System.Serializable]
    public class GraphRule
    {
        [System.NonSerialized]
        public string name;
        public bool isEnabled = true;
        public int priority = 0;

        public virtual bool CanApply(LevelGraph graph, Room room) { return true; }
        public virtual void Apply(LevelGraph graph, Room room)
        {
            // Default implementation does nothing
            Debug.Log($"Applying rule {name} to room {room.id}");
        }
    }

    [System.Serializable]
    public class ProgressionRule : GraphRule
    {
        [Header("Progression Settings")]
        public AbilityType requiredAbility;
        public int minDistanceFromStart = 1;
        public bool requiresAllPreviousAbilities = true;

        public override bool CanApply(LevelGraph graph, Room room)
        {
            if (room.type != RoomType.Ability)
                return false;

            return ValidateAbilityProgression(graph, room);
        }

        private bool ValidateAbilityProgression(LevelGraph graph, Room room)
        {
            //Progression logic TODO

            var startRoom = graph.GetStartRoom();
            if (startRoom == null)
                return false;
            int distanceFromStart = graph.GetShortestPath(startRoom.id, room.id).Count;
            return distanceFromStart >= minDistanceFromStart;
        }
    }

    [System.Serializable]
    public class ConnectionRule : GraphRule
    {
        [Header("Connection Settings")]
        public RoomType fromRoomType;
        public RoomType toRoomType;
        public int maxConnections = 3;

        public float connectionProbability = 0.7f;

        public override bool CanApply(LevelGraph graph, Room room)
        {
            return room.type == fromRoomType && room.connections.Count < maxConnections;
        }

        public override void Apply(LevelGraph graph, Room room)
        {
            var candidateRooms = graph.GetRoomByType(toRoomType).
                                Where(r => r.id != room.id && !room.connections.Contains(r.id));
            foreach (var candidate in candidateRooms)
            {
                if (UnityEngine.Random.Range(0f, 1f) < connectionProbability)
                {
                    graph.AddConnection(room.id, candidate.id);
                    Debug.Log($"Connecting {room.id} to {candidate.id} via rule {name}");
                    break;
                }
            }
        }
    }

    [System.Serializable]
    public class AccessibilityRule : GraphRule
    {
        [Header("Accessibility Settings")]
        public bool ensureAllRoomsAccessible = true;
        public int maxAbilityRequirements = 2;


        public override void Apply(LevelGraph graph, Room room)
        {
            if (room.abilitiesRequired.Count > maxAbilityRequirements)
            {
                Debug.LogWarning($"Room {room.id} has too many ability requirements, removing excess.");
                room.abilitiesRequired = room.abilitiesRequired.Take(maxAbilityRequirements).ToList();
            }
            ValidateAccessibility(graph, room);
        }

        private void ValidateAccessibility(LevelGraph graph, Room room)
        {
            //check accessibility whether its reachable with a valid ability progression
            var progression = graph.GetAbilityProgression();
            var reachableWithProgression = false;

            var currentAbilities = new HashSet<AbilityType>();
            foreach (var ability in progression)
            {
                if (room.CanAccess(currentAbilities))
                {
                    reachableWithProgression = true;
                    break;
                }
                currentAbilities.Add(ability);
            }
            if (!reachableWithProgression && room.abilitiesRequired.Count > 0)
            {
                Debug.LogWarning($"Room {room.id} is not accessible with current abilities, removing it.");
                // graph.RemoveRoom(room.id);
                room.abilitiesRequired.RemoveAt(room.abilitiesRequired.Count - 1); // Remove last ability requirement
            }
        }
    }

    [System.Serializable]
    public class CriticalPathRule : GraphRule
    {
        [Header("Critical Path Settings")]

        public List<AbilityType> abilityOrder = new List<AbilityType>();
        public bool enforceLinearProgression = true;


        public override void Apply(LevelGraph graph, Room room)
        {
            if (room.type == RoomType.Ability && room.abilityGranted.HasValue)
            {
                EnforceCriticalPath(graph, room);
            }
        }

        private void EnforceCriticalPath(LevelGraph graph, Room room)
        {
            if (!enforceLinearProgression) return;

            var ability = room.abilityGranted.Value;
            var abilityIndex = abilityOrder.IndexOf(ability);

            if (abilityIndex > 0)
            {
                // This ability should require the previous one
                var previousAbility = abilityOrder[abilityIndex - 1];
                if (!room.abilitiesRequired.Contains(previousAbility))
                {
                    room.abilitiesRequired.Add(previousAbility);
                }
            }
        }
    }
}