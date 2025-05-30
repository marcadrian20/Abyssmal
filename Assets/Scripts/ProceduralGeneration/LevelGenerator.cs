using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pathfinding;
using ProceduralGeneration.GraphGrammar;
namespace ProceduralGeneration
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private TilemapGenerator tilemapGenerator;

        [SerializeField] private int targetRoomCount = 25;
        [SerializeField] private float roomSpacing = 5f;
        [SerializeField] private int seed = 12345;

        [Header("Layout Settings")]
        [SerializeField] private int layoutIterations = 500; // Iterations for initial spread
        [SerializeField] private int glueingPasses = 100;     // Iterations for pulling connected rooms together
        [SerializeField] private float glueingStepFactor = 0.5f; // How much to move rooms per glueing pass (0 to 1)
        [SerializeField] private int overlapResolutionPasses = 500;
        [SerializeField] private float minSeparation = 0.1f; // Target gap between glued rooms & after overlap resolution

        [Header("Doorway Settings")]
        [SerializeField] private float doorVisualSize = 1.0f; // Preferred size of the door opening (e.g., 1 unit for a 1-tile wide door)
        [SerializeField] private float doorPlacementTolerance = 0.25f; // How close room edges need to be to be considered touching

        private List<Doorway> generatedDoorways = new List<Doorway>();

        // [SerializeField] private float roomAttractionForce = 0.1f;
        // [SerializeField] private int overlapResolutionPasses = 10;
        // [SerializeField] private float minSeparation = 0.1f;

        [Header("Graph Rules")]
        [SerializeField] private List<ProgressionRule> progressionRules = new List<ProgressionRule>();
        [SerializeField] private List<ConnectionRule> connectionRules = new List<ConnectionRule>();
        [SerializeField] private List<AccessibilityRule> accessibilityRules = new List<AccessibilityRule>();
        [SerializeField] private List<CriticalPathRule> criticalPathRules = new List<CriticalPathRule>();

        [Header("A* Integration")]
        [SerializeField] private GridGraph gridGraph;
        [SerializeField] private float nodeSize = 0.5f; // Node size to match tilemap

        [Header("Voronoi Settings")]
        [SerializeField] private int voronoiPointsPerRoom = 8;
        [SerializeField] private float roomSizeVariation = 0.3f;
        private Dictionary<int, Vector2> tempPositions; // For layout calculations
        // public Dictionary<int, Room> GeneratedRooms => levelGraph?.rooms;
        private LevelGraph levelGraph;
        private Dictionary<int, VoronoiRoomGenerator.RoomGeometry> roomGeometries = new Dictionary<int, VoronoiRoomGenerator.RoomGeometry>();

        private int nextRoomId = 0;
        [Header("Runtime Regeneration")]
        public bool useNewSeedOnRegenerate = false;

        // Public method to trigger regeneration
        public void RegenerateLevelAtRuntime()
        {
            Debug.Log("Regenerating level at runtime...");
            ClearCurrentLevelData();

            if (useNewSeedOnRegenerate)
            {
                seed = Random.Range(0, int.MaxValue);
                Debug.Log($"Using new seed for regeneration: {seed}");
            }
            else
            {
                Debug.Log($"Reusing seed for regeneration: {seed}");
            }

            GenerateLevel();
            Debug.Log("Level regeneration complete.");
        }

        private void ClearCurrentLevelData()
        {
            Debug.Log("Clearing current level data...");
            if (levelGraph != null && levelGraph.rooms != null)
            {
                levelGraph.rooms.Clear();
            }
            if (generatedDoorways != null)
            {
                generatedDoorways.Clear();
            }
            if (tempPositions != null)
            {
                tempPositions.Clear();
            }

            // If you are instantiating GameObjects for rooms, tiles, etc.,
            // you would need to destroy them here. For example:
            // foreach (Transform child in roomContainer) // Assuming you have a container for room GameObjects
            // {
            //     Destroy(child.gameObject);
            // }

            // The A* graph will be cleared and rescanned by UpdatePathfindingGraph()
            // when GenerateLevel() is called. If you need more explicit clearing:
            if (AstarPath.active != null && AstarPath.active.data.gridGraph != null)
            {
                // You could potentially clear nodes, but a full rescan is often cleaner.
                // Forcing a scan of an empty area before GenerateLevel could also work.
                // However, GenerateLevel calls UpdatePathfindingGraph which does a Scan,
                // so this might be redundant if room data is properly cleared.
            }
            Debug.Log("Current level data cleared.");
        }
        void Start()
        {
            GenerateLevel();
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) // Press 'R' to regenerate
            {
                RegenerateLevelAtRuntime();
            }
        }
        public void GenerateLevel()
        {
            Random.InitState(seed);
            if (tempPositions == null) tempPositions = new Dictionary<int, Vector2>();

            var graphGrammar = new LevelGraphGrammar();
            SetupGraphGrammarRules(graphGrammar);
            graphGrammar.SetAttribute("target_node_count", targetRoomCount);
            graphGrammar.GenerateGraph(100);
            PerformGraphLayout(graphGrammar);


            levelGraph = new LevelGraph();
            var voronoiGenerator = new VoronoiRoomGenerator();
            voronoiGenerator.InitializeTemplates();

            // Phase 2a: Create Room Shells
            foreach (var node in graphGrammar.GetNodes().Values)
            {
                var room = voronoiGenerator.CreateRoomShell(node); // Use the new shell method
                room.isFixed = node.isFixed;
                levelGraph.rooms[room.id] = room;
                room.position = node.position; // Set initial position from graph node for layout start

                foreach (var edge in node.edges.Where(e => e.fromNodeId == node.id))
                {
                    room.AddConnection(edge.toNodeId);
                    if (levelGraph.rooms.TryGetValue(edge.toNodeId, out var connectedRoom))
                    {
                        connectedRoom.AddConnection(room.id);
                    }
                }
            }
            Debug.Log($"Initial {levelGraph.rooms.Count} room shells created.");

            // Phase 3: Spatial Layout (Concrete Rooms with Sizes)
            DetermineRoomSizes();       // Sets final room.size for all rooms in levelGraph
            InitializeRoomPositions();  // Uses room.position as a starting point if available, then randomizes into tempPositions
            PositionRoomsInSpace();     // Operates on tempPositions
            GlueConnectedRooms();       // Operates on tempPositions
            ResolveRoomOverlaps();      // Operates on tempPositions
            ApplyTempPositionsToRooms(); // Applies tempPositions to room.position - final positions set

            // Phase 3b: Populate Voronoi Details (after final size and position)
            foreach (var room in levelGraph.rooms.Values)
            {
                var originalGraphNode = graphGrammar.GetNode(room.id);
                if (originalGraphNode != null)
                {
                    voronoiGenerator.PopulateRoomVoronoiDetails(room, originalGraphNode);
                }
                else
                {
                    Debug.LogWarning($"Could not find original GraphNode for room ID {room.id} to populate Voronoi details.");
                }
            }
            Debug.Log("Populated Voronoi details for rooms.");
            roomGeometries.Clear();
            foreach (var room in levelGraph.rooms.Values)
            {
                var geometry = voronoiGenerator.GenerateRoomGeometry(room);
                roomGeometries[room.id] = geometry;
                Debug.Log($"Generated geometry for room {room.id}: {geometry.voronoiCells.Count} cells, {geometry.walls.Count} walls");
            }
            Debug.Log("Generated room geometry from Voronoi diagrams using Delaunator.");
            if (tilemapGenerator != null)
            {
                tilemapGenerator.InitializeTilemaps();
                tilemapGenerator.GenerateTilemaps(roomGeometries, levelGraph);
                Debug.Log("Generated tilemaps from room geometries.");
            }
            else
            {
                Debug.LogWarning("TilemapGenerator not assigned!");
            }
            // Phase 4: Finalization
            SnapRoomsToGrid();
            PlaceDoorways();
            // GenerateCorridorsForDisconnectedRooms();
            // PlaceDoorways();
            // InsertCorridorsForDisconnectedRooms();
            int maxAttempts = 3;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int expectedConnections = GetExpectedConnectionCount();
                int actualDoors = generatedDoorways.Count;
                float connectivityRatio = (float)actualDoors / expectedConnections;

                Debug.Log($"Layout attempt {attempt + 1}: {actualDoors}/{expectedConnections} doors ({connectivityRatio:P})");

                if (connectivityRatio >= 0.8f) // 80% success rate is acceptable
                {
                    Debug.Log("Layout successful!");
                    break;
                }

                if (attempt < maxAttempts - 1)
                {
                    Debug.Log("Poor connectivity, retrying layout...");
                    RetryLayoutWithBetterSettings();
                    SnapRoomsToGrid();
                    PlaceDoorways();
                }
            }

            // Only use corridors as absolute last resort for critical connections
            // if (generatedDoorways.Count < GetExpectedConnectionCount() * 0.9f)
            // {
            //     AddCorridorsForAllDisconnectedRooms();
            //     Debug.Log("Added minimal corridors for critical paths.");
            // }
            UpdatePathfindingGraph();
            Debug.Log($"Generated {levelGraph.rooms.Count} rooms. Found {generatedDoorways.Count} doorways.");
        }
        public VoronoiRoomGenerator.RoomGeometry GetRoomGeometry(int roomId)
        {
            return roomGeometries.TryGetValue(roomId, out var geometry) ? geometry : null;
        }
        private int GetExpectedConnectionCount()
        {
            // Count unique connections (avoid double-counting A->B and B->A)
            var uniqueConnections = new HashSet<string>();
            foreach (var room in levelGraph.rooms.Values)
            {
                foreach (int connectedId in room.connections)
                {
                    string connectionKey = room.id < connectedId ? $"{room.id}-{connectedId}" : $"{connectedId}-{room.id}";
                    uniqueConnections.Add(connectionKey);
                }
            }
            return uniqueConnections.Count;
        }

        private void RetryLayoutWithBetterSettings()
        {
            Debug.Log("Retrying layout with improved settings...");

            // Increase glueing aggressiveness
            glueingPasses = Mathf.Min(glueingPasses + 50, 200);
            glueingStepFactor = Mathf.Min(glueingStepFactor + 0.2f, 1.0f);
            // doorPlacementTolerance = Mathf.Min(doorPlacementTolerance + 0.5f, 2.0f);

            // Retry the layout process with more aggressive settings
            GlueConnectedRooms();
            ResolveRoomOverlaps();
            ApplyTempPositionsToRooms();

            // Clear old doorways
            generatedDoorways.Clear();
        }

        private void AddCorridorsForAllDisconnectedRooms()
        {
            Debug.Log("Adding corridors for ALL disconnected room pairs...");

            var disconnectedPairs = GetAllDisconnectedPairs();
            var addedCorridors = 0;

            // Sort by priority: critical connections first, then by distance (shorter first)
            var sortedPairs = disconnectedPairs.OrderBy(pair => GetConnectionPriority(pair.roomA, pair.roomB))
                                               .ThenBy(pair => Vector2.Distance(pair.roomA.position, pair.roomB.position))
                                               .ToList();

            foreach (var (roomA, roomB) in sortedPairs)
            {
                Debug.Log($"Adding corridor between {roomA.type} room {roomA.id} and {roomB.type} room {roomB.id}");

                float distance = Vector2.Distance(roomA.position, roomB.position);

                if (distance > roomSpacing * 2.5f)
                {
                    // Long distance - create L-shaped corridor chain
                    CreateLongDistanceCorridorChain(roomA, roomB);
                }
                else
                {
                    // Short distance - single small corridor
                    AddSingleSmartCorridor(roomA, roomB);
                }

                addedCorridors++;

                // Optional: limit total corridors to prevent excessive clutter
                if (addedCorridors >= 15) // Reasonable upper limit
                {
                    Debug.Log($"Reached corridor limit ({addedCorridors}), stopping to prevent clutter");
                    break;
                }
            }

            if (addedCorridors > 0)
            {
                GlueConnectedRooms();        // Pull new corridors into proper positions
                ResolveRoomOverlaps();       // Fix any overlaps caused by corridor placement
                ApplyTempPositionsToRooms(); // Apply the integrated positions
                SnapRoomsToGrid();           // Ensure grid alignment
                PlaceDoorways();             // Re-run door placement for new layout
                Debug.Log($"Added {addedCorridors} corridors for disconnected pairs");
            }
        }

        private List<(Room roomA, Room roomB)> GetAllDisconnectedPairs()
        {
            var disconnectedPairs = new List<(Room roomA, Room roomB)>();
            var roomsSnapshot = levelGraph.rooms.Values.ToList();

            foreach (var roomA in roomsSnapshot)
            {
                var connectionsSnapshot = roomA.connections.ToList();

                foreach (int connectedId in connectionsSnapshot)
                {
                    if (roomA.id >= connectedId) continue; // Avoid duplicates

                    if (levelGraph.rooms.TryGetValue(connectedId, out Room roomB))
                    {
                        // Check if these rooms have a doorway
                        bool hasDoor = generatedDoorways.Any(d =>
                            (d.roomA_Id == roomA.id && d.roomB_Id == connectedId) ||
                            (d.roomA_Id == connectedId && d.roomB_Id == roomA.id));

                        if (!hasDoor)
                        {
                            float distance = Vector2.Distance(roomA.position, roomB.position);
                            Debug.Log($"Found disconnected pair: {roomA.id}({roomA.type}) -> {roomB.id}({roomB.type}), distance: {distance:F2}");
                            disconnectedPairs.Add((roomA, roomB));
                        }
                    }
                }
            }

            return disconnectedPairs;
        }

        private int GetConnectionPriority(Room roomA, Room roomB)
        {
            // Priority system: lower numbers = higher priority

            // Highest priority: Start room connections
            if (roomA.type == RoomType.Start || roomB.type == RoomType.Start)
                return 1;

            // High priority: Ability room connections
            if (roomA.type == RoomType.Ability || roomB.type == RoomType.Ability)
                return 2;

            // High priority: End room connections
            if (roomA.type == RoomType.End || roomB.type == RoomType.End)
                return 3;

            // Medium priority: Hub connections
            if (roomA.type == RoomType.Hub || roomB.type == RoomType.Hub)
                return 4;

            // Lower priority: Branch-to-branch connections
            return 5;
        }

        private void CreateLongDistanceCorridorChain(Room roomA, Room roomB)
        {
            Debug.Log($"Creating corridor chain for long distance connection: {roomA.id} -> {roomB.id}");

            Vector2 startPos = roomA.position;
            Vector2 endPos = roomB.position;
            Vector2 direction = (endPos - startPos).normalized;

            // Determine path strategy
            bool goHorizontalFirst = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);

            List<Room> corridorChain = new List<Room>();

            if (goHorizontalFirst)
            {
                // Create horizontal then vertical path
                Vector2 cornerPoint = new Vector2(endPos.x, startPos.y);
                Vector2 horizontalMidpoint = (startPos + cornerPoint) / 2f;
                Vector2 verticalMidpoint = (cornerPoint + endPos) / 2f;

                // Horizontal corridor
                var horizontalCorridor = CreateCorridorAtPosition(horizontalMidpoint, new Vector2(6f, 3f));
                corridorChain.Add(horizontalCorridor);

                // Vertical corridor (if path is long enough to need it)
                if (Vector2.Distance(cornerPoint, endPos) > roomSpacing * 1.5f)
                {
                    var verticalCorridor = CreateCorridorAtPosition(verticalMidpoint, new Vector2(3f, 6f));
                    corridorChain.Add(verticalCorridor);
                }
            }
            else
            {
                // Create vertical then horizontal path
                Vector2 cornerPoint = new Vector2(startPos.x, endPos.y);
                Vector2 verticalMidpoint = (startPos + cornerPoint) / 2f;
                Vector2 horizontalMidpoint = (cornerPoint + endPos) / 2f;

                // Vertical corridor
                var verticalCorridor = CreateCorridorAtPosition(verticalMidpoint, new Vector2(3f, 6f));
                corridorChain.Add(verticalCorridor);

                // Horizontal corridor (if needed)
                if (Vector2.Distance(cornerPoint, endPos) > roomSpacing * 1.5f)
                {
                    var horizontalCorridor = CreateCorridorAtPosition(horizontalMidpoint, new Vector2(6f, 3f));
                    corridorChain.Add(horizontalCorridor);
                }
            }

            // Connect the chain: roomA -> corridor1 -> corridor2 -> roomB
            ConnectRoomChain(roomA, corridorChain, roomB);
        }

        private Room CreateCorridorAtPosition(Vector2 position, Vector2 size)
        {
            var corridor = new Room(GetNextRoomId(), RoomType.Corridor);
            corridor.position = position;
            corridor.size = size;

            // Add to level data
            levelGraph.rooms[corridor.id] = corridor;
            tempPositions[corridor.id] = corridor.position;

            // Generate minimal Voronoi points
            corridor.voronoiPoints.Clear();
            for (int i = 0; i < 2; i++)
            {
                Vector2 point = new Vector2(
                    Random.Range(-size.x * 0.3f, size.x * 0.3f),
                    Random.Range(-size.y * 0.3f, size.y * 0.3f)
                );
                corridor.voronoiPoints.Add(point);
            }

            return corridor;
        }

        private void ConnectRoomChain(Room startRoom, List<Room> corridors, Room endRoom)
        {
            // Remove direct connection
            startRoom.connections.Remove(endRoom.id);
            endRoom.connections.Remove(startRoom.id);

            // Connect chain: start -> corridor1 -> corridor2 -> ... -> end
            Room currentRoom = startRoom;
            foreach (var corridor in corridors)
            {
                currentRoom.AddConnection(corridor.id);
                corridor.AddConnection(currentRoom.id);
                currentRoom = corridor;
            }

            // Connect last corridor to end room
            currentRoom.AddConnection(endRoom.id);
            endRoom.AddConnection(currentRoom.id);

            Debug.Log($"Connected {corridors.Count}-corridor chain from room {startRoom.id} to room {endRoom.id}");
        }

        private void AddSingleSmartCorridor(Room roomA, Room roomB)
        {
            // Create a small, strategically placed corridor
            Vector2 midpoint = (roomA.position + roomB.position) / 2f;

            // Make corridor small and oriented towards connecting the rooms
            Vector2 direction = (roomB.position - roomA.position).normalized;
            bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);

            var corridor = new Room(GetNextRoomId(), RoomType.Corridor);
            corridor.position = midpoint;
            corridor.size = isHorizontal ? new Vector2(3f, 2f) : new Vector2(2f, 3f); // Small, oriented corridor

            // Add to level
            levelGraph.rooms[corridor.id] = corridor;
            tempPositions[corridor.id] = corridor.position;

            // Update connections: A -> Corridor -> B instead of A -> B
            roomA.connections.Remove(roomB.id);
            roomB.connections.Remove(roomA.id);

            roomA.AddConnection(corridor.id);
            corridor.AddConnection(roomA.id);
            corridor.AddConnection(roomB.id);
            roomB.AddConnection(corridor.id);

            // Generate minimal Voronoi points
            for (int i = 0; i < 2; i++)
            {
                corridor.voronoiPoints.Add(Vector2.zero);
            }
        }
        private void SetupGraphGrammarRules(LevelGraphGrammar grammar)
        {
            grammar.AddRule(new StartExpansionRule { name = "StartExpansion", priority = 100 });

            // Use spatial-aware rules
            var abilityOrder = new List<AbilityType> { AbilityType.WallJump, AbilityType.Dash, AbilityType.DoubleJump, AbilityType.Grapple };

            foreach (var ability in abilityOrder)
            {
                grammar.AddRule(new SpatialHubExpansionRule
                {
                    name = $"SpatialHubExpansion_{ability}",
                    priority = 80,
                    abilityToAdd = ability,
                    probability = 0.8f,
                    maxConnectionDistance = roomSpacing * 1.5f, // Ensure adjacency
                    enforceAdjacency = true
                });
            }

            grammar.AddRule(new BranchExpansionRule { name = "BranchExpansion", priority = 60, probability = 0.7f });

            // Add connectivity validation as final rule
            grammar.AddRule(new ConnectivityValidationRule { name = "ConnectivityValidation", priority = 10 });
        }

        private void PerformGraphLayout(LevelGraphGrammar grammar)
        {
            var nodes = grammar.GetNodes().Values.ToList();
            float attractionStrength = 0.5f;
            float repulsionScale = 0.5f; // Reduced impact of roomSpacing for abstract layout
            float hierarchicalPushStrength = 0.02f;
            // Initialize positions
            foreach (var node in nodes)
            {
                if (!node.isFixed)
                {
                    node.position = Random.insideUnitCircle * 50f;
                }
            }

            // Force-directed layout with hierarchy respect
            for (int iteration = 0; iteration < layoutIterations; iteration++)
            {
                foreach (var node in nodes.Where(n => !n.isFixed))
                {
                    Vector2 force = Vector2.zero;
                    int generation = node.GetAttribute("generation", 0);

                    // Repulsion from other nodes
                    foreach (var other in nodes.Where(n => n.id != node.id))
                    {
                        Vector2 diff = node.position - other.position;
                        if (diff.magnitude > 0.001f)
                        {
                            force += diff.normalized * ((roomSpacing * repulsionScale) / Mathf.Max(diff.magnitude, 1f));
                        }
                    }

                    // Attraction to connected nodes (weaker)
                    foreach (var edge in node.edges)
                    {
                        var connected = grammar.GetNode(edge.toNodeId);
                        if (connected != null)
                        {
                            Vector2 diff = connected.position - node.position;
                            force += diff.normalized * attractionStrength; ;
                        }
                    }

                    // Hierarchical positioning (push later generations down/right)
                    force += new Vector2(generation * 5f, -generation * 8f) * hierarchicalPushStrength;

                    node.position += force * 0.05f;
                }
            }
        }
        private void DetermineRoomSizes()
        {
            foreach (var room in levelGraph.rooms.Values)
            {
                Vector2 baseSize = GetRoomSize(room.type);
                // Apply variation to the base size
                float variationX = Random.Range(1f - roomSizeVariation, 1f + roomSizeVariation);
                float variationY = Random.Range(1f - roomSizeVariation, 1f + roomSizeVariation);
                room.size = new Vector2(baseSize.x * variationX, baseSize.y * variationY);

                // Ensure size is a multiple of nodeSize for perfect grid alignment if desired (optional)
                // room.size.x = Mathf.Round(room.size.x / nodeSize) * nodeSize;
                // room.size.y = Mathf.Round(room.size.y / nodeSize) * nodeSize;
                // if (room.size.x < nodeSize) room.size.x = nodeSize;
                // if (room.size.y < nodeSize) room.size.y = nodeSize;
            }
            Debug.Log("Determined final room sizes with variation.");
        }
        // private void InitializeRoomPositions()
        // {
        //     tempPositions.Clear();
        //     foreach (var room in levelGraph.rooms.Values)
        //     {
        //         tempPositions[room.id] = new Vector2(
        //             Random.Range(-50f, 50f) * (targetRoomCount / 10f), // Spread initial positions a bit more
        //             Random.Range(-50f, 50f) * (targetRoomCount / 10f)
        //         );
        //     }
        // }
        private void InitializeRoomPositions()
        {
            tempPositions.Clear();
            float initialSpreadMagnitude = 50f * (targetRoomCount / 10f);
            foreach (var room in levelGraph.rooms.Values)
            {
                // Use the room's current position (from GraphNode) as a base, then add some randomness
                // This helps if PerformGraphLayout (if it were called) gave a decent initial layout.
                // If room.position is zero, it just becomes a random point.
                tempPositions[room.id] = room.position + new Vector2(
                    Random.Range(-initialSpreadMagnitude, initialSpreadMagnitude) * 0.1f,
                    Random.Range(-initialSpreadMagnitude, initialSpreadMagnitude) * 0.1f
                );
            }
            Debug.Log("Initialized temporary room positions.");
        }

        private void SetupGraphRules()
        {
            // Add progression rules
            foreach (var rule in progressionRules)
            {
                if (rule.isEnabled)
                {
                    rule.name = $"ProgressionRule_{rule.requiredAbility}";
                    levelGraph.AddRule(rule);
                }
            }

            // Add connection rules
            foreach (var rule in connectionRules)
            {
                if (rule.isEnabled)
                {
                    rule.name = $"ConnectionRule_{rule.fromRoomType}_to_{rule.toRoomType}";
                    levelGraph.AddRule(rule);
                }
            }

            // Add accessibility rules
            foreach (var rule in accessibilityRules)
            {
                if (rule.isEnabled)
                {
                    rule.name = "AccessibilityRule";
                    levelGraph.AddRule(rule);
                }
            }

            // Add critical path rules
            foreach (var rule in criticalPathRules)
            {
                if (rule.isEnabled)
                {
                    rule.name = "CriticalPathRule";
                    levelGraph.AddRule(rule);
                }
            }

            Debug.Log($"Setup {levelGraph.rules.Count} graph rules");
        }

        private void GenerateCriticalPath()
        {
            // Create start room
            Room startRoom = CreateRoom(RoomType.Start);
            Room currentRoom = startRoom;

            // Create ability progression path
            var abilityOrder = new List<AbilityType>
            {
                AbilityType.WallJump,
                AbilityType.Dash,
                AbilityType.DoubleJump,
                AbilityType.Grapple
            };

            foreach (var ability in abilityOrder)
            {
                // Create hub room before ability
                Room hubRoom = CreateRoom(RoomType.Hub);
                levelGraph.AddConnection(currentRoom.id, hubRoom.id);

                // Create ability room
                Room abilityRoom = CreateRoom(RoomType.Ability);
                abilityRoom.abilityGranted = ability;
                levelGraph.AddConnection(hubRoom.id, abilityRoom.id);

                currentRoom = abilityRoom;
            }

            // Create end room
            Room endRoom = CreateRoom(RoomType.End);
            endRoom.abilitiesRequired.AddRange(abilityOrder);
            levelGraph.AddConnection(currentRoom.id, endRoom.id);

            Debug.Log($"Generated critical path with {abilityOrder.Count} abilities");
        }

        private void GenerateOptionalBranches()
        {
            int branchCount = targetRoomCount - levelGraph.rooms.Count;
            var existingRooms = levelGraph.rooms.Values.ToList();

            for (int i = 0; i < branchCount; i++)
            {
                // Pick random existing room to branch from
                Room parentRoom = existingRooms[Random.Range(0, existingRooms.Count)];

                // Create branch room
                Room branchRoom = CreateRoom(GetRandomBranchRoomType());

                // Assign some ability requirements based on progression
                AssignAbilityRequirements(branchRoom, parentRoom);

                levelGraph.AddConnection(parentRoom.id, branchRoom.id);
                existingRooms.Add(branchRoom);
            }

            Debug.Log($"Generated {branchCount} optional branch rooms");
        }

        private RoomType GetRandomBranchRoomType()
        {
            var branchTypes = new RoomType[] { RoomType.Branch, RoomType.Corridor, RoomType.Hub };
            return branchTypes[Random.Range(0, branchTypes.Length)];
        }

        private void AssignAbilityRequirements(Room room, Room parentRoom)
        {
            // Copy some requirements from parent
            room.abilitiesRequired.AddRange(parentRoom.abilitiesRequired);

            // 30% chance to add an additional requirement
            if (Random.Range(0f, 1f) < 0.3f && room.abilitiesRequired.Count < 2)
            {
                var allAbilities = new List<AbilityType>
                {
                    AbilityType.WallJump, AbilityType.Dash,
                    AbilityType.DoubleJump, AbilityType.Grapple
                };

                var availableAbilities = allAbilities
                    .Where(a => !room.abilitiesRequired.Contains(a))
                    .ToList();

                if (availableAbilities.Count > 0)
                {
                    room.abilitiesRequired.Add(
                        availableAbilities[Random.Range(0, availableAbilities.Count)]
                    );
                }
            }
        }
        private int GetNextRoomId()
        {
            return nextRoomId++;
        }

        private void PositionRoomsInSpace()
        {
            tempPositions = new Dictionary<int, Vector2>();

            // Start with the START room at origin
            var startRoom = levelGraph.rooms.Values.First(r => r.type == RoomType.Start);
            tempPositions[startRoom.id] = Vector2.zero;
            startRoom.isFixed = true;

            // Use breadth-first search to place rooms based on graph structure
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(startRoom.id);
            visited.Add(startRoom.id);

            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                var currentRoom = levelGraph.rooms[currentId];
                Vector2 currentPos = tempPositions[currentId];

                var unvisitedConnections = currentRoom.connections.Where(id => !visited.Contains(id)).ToList();

                // Place connected rooms in a circle around current room
                for (int i = 0; i < unvisitedConnections.Count; i++)
                {
                    int connectedId = unvisitedConnections[i];
                    var connectedRoom = levelGraph.rooms[connectedId];

                    // Calculate position based on connection index (spread around circle)
                    float angle = (float)i / unvisitedConnections.Count * 2f * Mathf.PI;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 targetPos = currentPos + direction * roomSpacing;

                    // Avoid overlaps with existing rooms
                    targetPos = FindNearestValidPosition(targetPos, connectedRoom.size);

                    tempPositions[connectedId] = targetPos;
                    visited.Add(connectedId);
                    queue.Enqueue(connectedId);
                }
            }
        }
        private void InsertCorridorsForDisconnectedRooms()
        {
            Debug.Log("Checking for disconnected room pairs and inserting corridors...");

            var disconnectedPairs = FindDisconnectedPairs();

            foreach (var (roomA, roomB) in disconnectedPairs)
            {
                Debug.Log($"Inserting corridor between Room {roomA.id} and Room {roomB.id}");
                InsertCorridorWithSpaceMaking(roomA, roomB);
            }

            if (disconnectedPairs.Count > 0)
            {
                // After inserting corridors, update positions and re-place doors
                ApplyTempPositionsToRooms();
                PlaceDoorways();
                Debug.Log($"Inserted {disconnectedPairs.Count} corridors and updated layout");
            }
        }

        private List<(Room roomA, Room roomB)> FindDisconnectedPairs()
        {
            var disconnectedPairs = new List<(Room roomA, Room roomB)>();

            foreach (Room roomA in levelGraph.rooms.Values)
            {
                foreach (int connectedId in roomA.connections)
                {
                    if (roomA.id >= connectedId) continue; // Avoid duplicates

                    if (levelGraph.rooms.TryGetValue(connectedId, out Room roomB))
                    {
                        // Check if these rooms have a doorway
                        bool hasDoor = generatedDoorways.Any(d =>
                            (d.roomA_Id == roomA.id && d.roomB_Id == roomB.id) ||
                            (d.roomA_Id == roomB.id && d.roomB_Id == roomA.id));

                        if (!hasDoor)
                        {
                            float distance = Vector2.Distance(roomA.position, roomB.position);
                            Debug.Log($"Found disconnected pair: {roomA.id}({roomA.type}) -> {roomB.id}({roomB.type}), distance: {distance:F2}");
                            disconnectedPairs.Add((roomA, roomB));
                        }
                    }
                }
            }

            return disconnectedPairs;
        }

        private void InsertCorridorWithSpaceMaking(Room roomA, Room roomB)
        {
            Vector2 posA = roomA.position;
            Vector2 posB = roomB.position;

            // Determine corridor placement strategy
            Vector2 direction = (posB - posA).normalized;
            float distance = Vector2.Distance(posA, posB);

            // Calculate corridor position (midpoint between rooms)
            Vector2 corridorPos = (posA + posB) / 2f;

            // Determine corridor orientation based on room layout
            bool isHorizontalConnection = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
            Vector2 corridorSize = isHorizontalConnection ?
                new Vector2(distance * 0.4f, 4f) :  // Horizontal corridor
                new Vector2(4f, distance * 0.4f);   // Vertical corridor

            // Create the corridor room
            var corridor = CreateCorridorRoom(corridorPos, corridorSize);

            // Check what rooms would be displaced by this corridor
            var roomsToDisplace = FindRoomsIntersectingCorridor(corridor);

            // Push away intersecting rooms
            foreach (var displacedRoom in roomsToDisplace)
            {
                PushRoomAwayFromCorridor(displacedRoom, corridor, roomA, roomB);
            }

            // Update connections: remove direct connection, add corridor connections
            roomA.connections.Remove(roomB.id);
            roomB.connections.Remove(roomA.id);

            // Connect rooms through corridor
            ConnectRoomsThroughCorridor(roomA, roomB, corridor);

            Debug.Log($"Inserted corridor {corridor.id} between rooms {roomA.id} and {roomB.id}");
        }

        private Room CreateCorridorRoom(Vector2 position, Vector2 size)
        {
            var corridor = new Room(GetNextRoomId(), RoomType.Corridor);
            corridor.position = position;
            corridor.size = size;

            // Add to level data
            levelGraph.rooms[corridor.id] = corridor;
            tempPositions[corridor.id] = corridor.position;

            // Generate minimal Voronoi points for corridors
            for (int i = 0; i < 3; i++)
            {
                Vector2 point = new Vector2(
                    Random.Range(-corridor.size.x / 4f, corridor.size.x / 4f),
                    Random.Range(-corridor.size.y / 4f, corridor.size.y / 4f)
                );
                corridor.voronoiPoints.Add(point);
            }

            return corridor;
        }

        private List<Room> FindRoomsIntersectingCorridor(Room corridor)
        {
            var intersectingRooms = new List<Room>();
            Rect corridorBounds = corridor.GetBounds();

            foreach (var room in levelGraph.rooms.Values)
            {
                if (room.id == corridor.id) continue;

                Rect roomBounds = room.GetBounds();
                if (corridorBounds.Overlaps(roomBounds))
                {
                    intersectingRooms.Add(room);
                }
            }

            return intersectingRooms;
        }

        private void PushRoomAwayFromCorridor(Room roomToPush, Room corridor, Room roomA, Room roomB)
        {
            // Don't push the rooms we're trying to connect
            if (roomToPush.id == roomA.id || roomToPush.id == roomB.id) return;

            Vector2 corridorPos = corridor.position;
            Vector2 roomPos = roomToPush.position;

            // Calculate push direction (away from corridor center)
            Vector2 pushDirection = (roomPos - corridorPos).normalized;

            // Calculate minimum distance needed to avoid overlap
            float corridorRadius = Mathf.Max(corridor.size.x, corridor.size.y) / 2f;
            float roomRadius = Mathf.Max(roomToPush.size.x, roomToPush.size.y) / 2f;
            float minDistance = corridorRadius + roomRadius + minSeparation;

            float currentDistance = Vector2.Distance(corridorPos, roomPos);

            if (currentDistance < minDistance)
            {
                float pushAmount = minDistance - currentDistance;
                Vector2 newPosition = roomPos + pushDirection * pushAmount;

                // Update both actual position and temp position
                roomToPush.position = newPosition;
                tempPositions[roomToPush.id] = newPosition;

                Debug.Log($"Pushed room {roomToPush.id} away from corridor by {pushAmount:F2} units");
            }
        }

        private void ConnectRoomsThroughCorridor(Room roomA, Room roomB, Room corridor)
        {
            // Check which room is closer to which end of the corridor
            Vector2 corridorStart = corridor.position - corridor.size / 4f;
            Vector2 corridorEnd = corridor.position + corridor.size / 4f;

            float distAtoStart = Vector2.Distance(roomA.position, corridorStart);
            float distAtoEnd = Vector2.Distance(roomA.position, corridorEnd);

            // Connect rooms to corridor
            roomA.AddConnection(corridor.id);
            corridor.AddConnection(roomA.id);

            roomB.AddConnection(corridor.id);
            corridor.AddConnection(roomB.id);

            Debug.Log($"Connected rooms {roomA.id} and {roomB.id} through corridor {corridor.id}");
        }
        private bool OverlapsWithExistingRooms(Vector2 testPos, Vector2 roomSize)
        {
            Rect testBounds = new Rect(testPos - roomSize / 2f, roomSize);

            foreach (var existingRoomId in tempPositions.Keys)
            {
                if (levelGraph.rooms.TryGetValue(existingRoomId, out Room existingRoom))
                {
                    Vector2 existingPos = tempPositions[existingRoomId];
                    Rect existingBounds = new Rect(existingPos - existingRoom.size / 2f, existingRoom.size);

                    if (testBounds.Overlaps(existingBounds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private Vector2 FindNearestValidPosition(Vector2 preferredPos, Vector2 roomSize)
        {
            Vector2 bestPos = preferredPos;
            float minDistance = float.MaxValue;

            // Try positions in a spiral around the preferred position
            for (int radius = 0; radius < 20; radius++)
            {
                for (int angle = 0; angle < 360; angle += 30)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    Vector2 testPos = preferredPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

                    if (!OverlapsWithExistingRooms(testPos, roomSize))
                    {
                        float dist = Vector2.Distance(testPos, preferredPos);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestPos = testPos;
                        }
                    }
                }

                if (minDistance < float.MaxValue) break; // Found a valid position
            }

            return bestPos;
        }
        private void ResolveRoomOverlaps()
        {
            Debug.Log("Starting overlap resolution...");
            List<Room> allRooms = levelGraph.rooms.Values.ToList();
            bool overlapFoundInLastPass;

            for (int pass = 0; pass < overlapResolutionPasses; pass++)
            {
                overlapFoundInLastPass = false;
                for (int i = 0; i < allRooms.Count; i++)
                {
                    for (int j = i + 1; j < allRooms.Count; j++)
                    {
                        Room roomA = allRooms[i];
                        Room roomB = allRooms[j];
                        // If both rooms are fixed, no point in trying to resolve overlap between them by moving them
                        if (roomA.isFixed && roomB.isFixed) continue;
                        // Use tempPositions for current positions if layout is iterative
                        // For this example, assuming room.position is the one to check and modify
                        // If using tempPositions consistently, get bounds from tempPositions + room.size
                        Rect boundsA = new Rect(tempPositions[roomA.id] - roomA.size / 2f, roomA.size);
                        Rect boundsB = new Rect(tempPositions[roomB.id] - roomB.size / 2f, roomB.size);


                        if (boundsA.Overlaps(boundsB))
                        {
                            overlapFoundInLastPass = true;

                            Vector2 posA = tempPositions[roomA.id];
                            Vector2 posB = tempPositions[roomB.id];

                            float currentOverlapX = (boundsA.width / 2f + boundsB.width / 2f) - Mathf.Abs(posA.x - posB.x);
                            float currentOverlapY = (boundsA.height / 2f + boundsB.height / 2f) - Mathf.Abs(posA.y - posB.y);

                            Vector2 separationDirectionA = Vector2.zero;
                            float pushAmount = 0;

                            if (currentOverlapX > -minSeparation && (currentOverlapX < currentOverlapY || currentOverlapY <= -minSeparation))
                            {
                                pushAmount = (currentOverlapX + minSeparation) / 2f;
                                if (posA.x < posB.x)
                                    separationDirectionA.x = -pushAmount;
                                else
                                    separationDirectionA.x = pushAmount;
                            }
                            else if (currentOverlapY > -minSeparation)
                            {
                                pushAmount = (currentOverlapY + minSeparation) / 2f;
                                if (posA.y < posB.y)
                                    separationDirectionA.y = -pushAmount;
                                else
                                    separationDirectionA.y = pushAmount;
                            }

                            if (Mathf.Abs(pushAmount) > 0.001f)
                            {
                                // Only move non-fixed rooms
                                if (!roomA.isFixed && !roomB.isFixed) // Both can move
                                {
                                    tempPositions[roomA.id] = posA + separationDirectionA;
                                    tempPositions[roomB.id] = posB - separationDirectionA;
                                }
                                else if (!roomA.isFixed) // Only A can move
                                {
                                    tempPositions[roomA.id] = posA + separationDirectionA * 2f; // A moves full amount
                                }
                                else if (!roomB.isFixed) // Only B can move
                                {
                                    tempPositions[roomB.id] = posB - separationDirectionA * 2f; // B moves full amount
                                }
                            }
                        }
                    }
                }
                if (!overlapFoundInLastPass && pass > 0)
                {
                    Debug.Log($"Overlap resolution completed in {pass + 1} passes.");
                    // ApplyTempPositionsToRooms(); // Apply after resolution is stable
                    return;
                }
            }
            // ApplyTempPositionsToRooms(); // Apply after max passes
            Debug.Log($"Overlap resolution finished after {overlapResolutionPasses} passes (max).");
        }
        private void GlueConnectedRooms()
        {
            Debug.Log("Starting to glue connected rooms...");

            for (int pass = 0; pass < glueingPasses; pass++)
            {
                foreach (Room roomA in levelGraph.rooms.Values)
                {
                    foreach (int connectedId in roomA.connections)
                    {
                        if (!levelGraph.rooms.TryGetValue(connectedId, out Room roomB) || roomA.id >= roomB.id)
                            continue;
                        if (roomA.isFixed && roomB.isFixed) continue;

                        Vector2 posA = tempPositions[roomA.id];
                        Vector2 posB = tempPositions[roomB.id];
                        Vector2 direction = (posB - posA).normalized;

                        // Target distance for rooms to be touching (no gap)
                        float targetDistance = (roomA.size.x + roomB.size.x) / 2f + minSeparation * 0.5f; // Small gap
                        float currentDistance = Vector2.Distance(posA, posB);

                        if (Mathf.Abs(currentDistance - targetDistance) > 0.1f) // Tolerance
                        {
                            float moveAmount = (currentDistance - targetDistance) * glueingStepFactor;
                            Vector2 moveVector = direction * moveAmount * 0.5f; // Each room moves half

                            if (!roomA.isFixed && !roomB.isFixed)
                            {
                                tempPositions[roomA.id] = posA + moveVector;
                                tempPositions[roomB.id] = posB - moveVector;
                            }
                            else if (!roomA.isFixed)
                            {
                                tempPositions[roomA.id] = posA + moveVector * 2f;
                            }
                            else if (!roomB.isFixed)
                            {
                                tempPositions[roomB.id] = posB - moveVector * 2f;
                            }
                        }
                    }
                }
            }
            Debug.Log("Glueing connected rooms finished.");
        }

        private void ApplyTempPositionsToRooms()
        {
            foreach (var room in levelGraph.rooms.Values)
            {
                if (tempPositions.TryGetValue(room.id, out Vector2 newPos))
                {
                    room.position = newPos;
                }
            }
            Debug.Log("Applied temporary layout positions to rooms.");
        }

        // private void GenerateRoomGeometry()
        // {
        //     foreach (var room in levelGraph.rooms.Values)
        //     {
        //         GenerateVoronoiGeometry(room);
        //     }

        //     Debug.Log("Generated Voronoi geometry for all rooms");
        // }
        private void GenerateVoronoiPointsForAllRooms()
        {
            foreach (var room in levelGraph.rooms.Values)
            {
                GenerateVoronoiPoints(room); // Changed method name
            }
            Debug.Log("Generated Voronoi points for all rooms");
        }
        private void GenerateVoronoiPoints(Room room)
        {
            room.voronoiPoints.Clear();
            // room.size is already set by DetermineRoomSizes()

            int pointCount = voronoiPointsPerRoom + Random.Range(-2, 3);
            if (pointCount < 4) pointCount = 4;

            for (int i = 0; i < pointCount; i++)
            {
                Vector2 point = new Vector2(
                    Random.Range(-room.size.x * 0.45f, room.size.x * 0.45f), // Slightly inset
                    Random.Range(-room.size.y * 0.45f, room.size.y * 0.45f)
                );
                room.voronoiPoints.Add(point);
            }

            float marginFactor = 0.4f; // Ensure these are within the room bounds
            room.voronoiPoints.Add(new Vector2(-room.size.x * marginFactor, -room.size.y * marginFactor));
            room.voronoiPoints.Add(new Vector2(room.size.x * marginFactor, -room.size.y * marginFactor));
            room.voronoiPoints.Add(new Vector2(room.size.x * marginFactor, room.size.y * marginFactor));
            room.voronoiPoints.Add(new Vector2(-room.size.x * marginFactor, room.size.y * marginFactor));
        }

        private Vector2 GetRoomSize(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Start => new Vector2(10f, 10f),
                RoomType.End => new Vector2(12.5f, 12.5f),
                RoomType.Ability => new Vector2(9f, 9f),
                RoomType.Hub => new Vector2(11f, 11f),
                RoomType.Branch => new Vector2(7.5f, 7.5f),
                RoomType.Corridor => new Vector2(6f, 4f),
                _ => new Vector2(7.5f, 7.5f)
            };
        }

        private void UpdatePathfindingGraph()
        {
            if (gridGraph == null)
            {
                // Try to find A* pathfinding graph
                var astarPath = FindFirstObjectByType<AstarPath>();
                if (astarPath != null && astarPath.data.graphs.Length > 0)
                {
                    gridGraph = astarPath.data.graphs[0] as GridGraph;
                }
            }

            if (gridGraph != null)
            {
                UpdateGridForRooms();
                gridGraph.Scan();
                Debug.Log("Updated A* pathfinding graph");
            }
            else
            {
                Debug.LogWarning("No GridGraph found for pathfinding integration");
            }
        }

        private void UpdateGridForRooms()
        {
            if (levelGraph.rooms.Count == 0) { Debug.LogWarning("No rooms to calculate grid bounds."); return; }
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var room in levelGraph.rooms.Values)
            {
                min.x = Mathf.Min(min.x, room.position.x - room.size.x / 2);
                min.y = Mathf.Min(min.y, room.position.y - room.size.y / 2);
                max.x = Mathf.Max(max.x, room.position.x + room.size.x / 2);
                max.y = Mathf.Max(max.y, room.position.y + room.size.y / 2);
            }

            Vector2 padding = Vector2.one * Mathf.Max(roomSpacing / 5f, nodeSize * 4); // Adjusted padding
            min -= padding;
            max += padding;

            Vector2 graphSize = max - min;
            Vector3 graphCenter = new Vector3((min.x + max.x) / 2, (min.y + max.y) / 2, 0);

            gridGraph.center = graphCenter;
            gridGraph.SetDimensions(
                Mathf.RoundToInt(graphSize.x / nodeSize),
                Mathf.RoundToInt(graphSize.y / nodeSize),
                nodeSize
            );
            Debug.Log($"Updated grid graph: {Mathf.RoundToInt(graphSize.x / nodeSize)}x{Mathf.RoundToInt(graphSize.y / nodeSize)} nodes, nodeSize {nodeSize}, center {graphCenter}");
        }
        private void SnapRoomsToGrid()
        {
            if (nodeSize <= 0) { Debug.LogError("Node size is zero or negative, cannot snap to grid."); return; }
            foreach (var room in levelGraph.rooms.Values)
            {
                room.position.x = Mathf.Round(room.position.x / nodeSize) * nodeSize;
                room.position.y = Mathf.Round(room.position.y / nodeSize) * nodeSize;
            }
            Debug.Log("Snapped all room positions to grid");
        }
        // public LevelGraph GetLevelGraph() => levelGraph;
        private void PlaceDoorways()
        {
            generatedDoorways.Clear();
            if (nodeSize <= 0)
            {
                Debug.LogError("NodeSize is not set correctly, cannot accurately place doorways.");
                return;
            }

            // Ensure doorSize is at least one nodeSize and a multiple of it for grid alignment
            float actualDoorOpeningSize = Mathf.Max(nodeSize, Mathf.Round(doorVisualSize / nodeSize) * nodeSize);

            foreach (Room roomA in levelGraph.rooms.Values)
            {
                Rect boundsA = roomA.GetBounds();

                foreach (int connectedId in roomA.connections)
                {
                    // Process each connection pair only once (e.g., A-B, not B-A again)
                    if (roomA.id >= connectedId) continue;

                    if (!levelGraph.rooms.TryGetValue(connectedId, out Room roomB)) continue;
                    Rect boundsB = roomB.GetBounds();

                    bool doorPlacedForThisPair = false;

                    // Scenario 1: RoomA is to the LEFT of RoomB (Vertical Door on RoomA's Right edge)
                    if (Mathf.Abs(boundsA.xMax - boundsB.xMin) < doorPlacementTolerance)
                    {
                        float overlapMinY = Mathf.Max(boundsA.yMin, boundsB.yMin);
                        float overlapMaxY = Mathf.Min(boundsA.yMax, boundsB.yMax);
                        float sharedLength = overlapMaxY - overlapMinY;

                        if (sharedLength >= actualDoorOpeningSize)
                        {
                            float doorCenterY = overlapMinY + sharedLength / 2f; // Place in middle of shared segment
                            Vector2 doorPosition = new Vector2(boundsA.xMax, doorCenterY);

                            if (!IsDoorwayObstructed(doorPosition, roomA, roomB, actualDoorOpeningSize, Doorway.Orientation.Vertical))
                            {
                                generatedDoorways.Add(new Doorway(roomA.id, roomB.id, doorPosition, Doorway.Orientation.Vertical, actualDoorOpeningSize));
                                doorPlacedForThisPair = true;
                            }
                        }
                    }

                    // Scenario 2: RoomA is to the RIGHT of RoomB (Vertical Door on RoomA's Left edge)
                    if (!doorPlacedForThisPair && Mathf.Abs(boundsA.xMin - boundsB.xMax) < doorPlacementTolerance)
                    {
                        float overlapMinY = Mathf.Max(boundsA.yMin, boundsB.yMin);
                        float overlapMaxY = Mathf.Min(boundsA.yMax, boundsB.yMax);
                        float sharedLength = overlapMaxY - overlapMinY;

                        if (sharedLength >= actualDoorOpeningSize)
                        {
                            float doorCenterY = overlapMinY + sharedLength / 2f;
                            Vector2 doorPosition = new Vector2(boundsA.xMin, doorCenterY);
                            if (!IsDoorwayObstructed(doorPosition, roomA, roomB, actualDoorOpeningSize, Doorway.Orientation.Vertical))
                            {
                                generatedDoorways.Add(new Doorway(roomA.id, roomB.id, doorPosition, Doorway.Orientation.Vertical, actualDoorOpeningSize));
                                doorPlacedForThisPair = true;
                            }
                        }
                    }

                    // Scenario 3: RoomA is BELOW RoomB (Horizontal Door on RoomA's Top edge)
                    if (!doorPlacedForThisPair && Mathf.Abs(boundsA.yMax - boundsB.yMin) < doorPlacementTolerance)
                    {
                        float overlapMinX = Mathf.Max(boundsA.xMin, boundsB.xMin);
                        float overlapMaxX = Mathf.Min(boundsA.xMax, boundsB.xMax);
                        float sharedLength = overlapMaxX - overlapMinX;

                        if (sharedLength >= actualDoorOpeningSize)
                        {
                            float doorCenterX = overlapMinX + sharedLength / 2f;
                            Vector2 doorPosition = new Vector2(doorCenterX, boundsA.yMax);
                            if (!IsDoorwayObstructed(doorPosition, roomA, roomB, actualDoorOpeningSize, Doorway.Orientation.Horizontal))
                            {
                                generatedDoorways.Add(new Doorway(roomA.id, roomB.id, doorPosition, Doorway.Orientation.Horizontal, actualDoorOpeningSize));
                                doorPlacedForThisPair = true;
                            }
                        }
                    }

                    // Scenario 4: RoomA is ABOVE RoomB (Horizontal Door on RoomA's Bottom edge)
                    if (!doorPlacedForThisPair && Mathf.Abs(boundsA.yMin - boundsB.yMax) < doorPlacementTolerance)
                    {
                        float overlapMinX = Mathf.Max(boundsA.xMin, boundsB.xMin);
                        float overlapMaxX = Mathf.Min(boundsA.xMax, boundsB.xMax);
                        float sharedLength = overlapMaxX - overlapMinX;

                        if (sharedLength >= actualDoorOpeningSize)
                        {
                            float doorCenterX = overlapMinX + sharedLength / 2f;
                            Vector2 doorPosition = new Vector2(doorCenterX, boundsA.yMin);
                            if (!IsDoorwayObstructed(doorPosition, roomA, roomB, actualDoorOpeningSize, Doorway.Orientation.Horizontal))
                            {
                                generatedDoorways.Add(new Doorway(roomA.id, roomB.id, doorPosition, Doorway.Orientation.Horizontal, actualDoorOpeningSize));
                                // doorPlacedForThisPair = true; // Last check, not strictly needed to set
                            }
                        }
                    }
                    if (!doorPlacedForThisPair && roomA.connections.Contains(roomB.id)) // Check if still connected
                    {
                        // This means a connection exists in the graph, but no physical adjacency was found for a door.
                        // This is where your "7 to 3 goes through 6 and 16" issue manifests.
                        // Debug.LogWarning($"Could not place a direct door between Room {roomA.id} and Room {roomB.id}. They are connected in graph but not physically adjacent or suitable for a door.");
                    }
                }
            }
            Debug.Log($"Found {generatedDoorways.Count} valid doorway positions.");
        }

        private bool IsDoorwayObstructed(Vector2 doorCenter, Room roomA, Room roomB, float doorOpeningSize, Doorway.Orientation orientation)
        {
            // Create a small bounding box representing the doorway space to check for obstructions by a *third* room.
            // The door is on the boundary, so we check a thin box that slightly overlaps where the two rooms meet.
            Rect doorCheckBounds;
            float checkThickness = nodeSize * 0.5f; // How "thick" the check area for the door is

            if (orientation == Doorway.Orientation.Vertical)
            {
                // Door is vertical, its "length" is doorOpeningSize (height), "thickness" is checkThickness (width)
                doorCheckBounds = new Rect(doorCenter.x - checkThickness / 2f, doorCenter.y - doorOpeningSize / 2f, checkThickness, doorOpeningSize);
            }
            else // Horizontal
            {
                // Door is horizontal, its "length" is doorOpeningSize (width), "thickness" is checkThickness (height)
                doorCheckBounds = new Rect(doorCenter.x - doorOpeningSize / 2f, doorCenter.y - checkThickness / 2f, doorOpeningSize, checkThickness);
            }

            foreach (Room otherRoom in levelGraph.rooms.Values)
            {
                // Don't check against the two rooms the door connects
                if (otherRoom.id == roomA.id || otherRoom.id == roomB.id) continue;

                if (otherRoom.GetBounds().Overlaps(doorCheckBounds))
                {
                    // Debug.LogWarning($"Potential doorway at {doorCenter} between {roomA.id} and {roomB.id} is obstructed by room {otherRoom.id}");
                    return true; // Obstructed by a third room
                }
            }
            return false; // Not obstructed
        }

        public List<Room> GetAccessibleRooms(HashSet<AbilityType> playerAbilities)
        {
            return levelGraph.rooms.Values.Where(room => room.CanAccess(playerAbilities)).ToList();
        }
        private Room CreateRoom(RoomType type)
        {
            Room room = new Room(nextRoomId++, type);
            levelGraph.rooms[room.id] = room;
            return room;
        }


        // Gizmos for visualization
        void OnDrawGizmos()
        {
            if (levelGraph?.rooms == null) return;

            foreach (var room in levelGraph.rooms.Values)
            {
                // Color based on room type
                Gizmos.color = GetRoomColor(room.type);

                // Vector3 pos = new Vector3(room.position.x, 0, room.position.y);
                Vector3 pos2D = new Vector3(room.position.x, room.position.y, 0); // Use X,Y,0 for 2D

                //this is 3D    Gizmos.DrawWireCube(pos, new Vector3(room.size.x, 2, room.size.y));

                Gizmos.DrawWireCube(pos2D, new Vector3(room.size.x, room.size.y, 1));
                // Draw room ID
#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos2D + Vector3.forward * 2, $"{room.id}\n{room.type}");
#endif

                // Draw connections
                Gizmos.color = Color.gray;
                foreach (int connectedId in room.connections)
                {
                    if (levelGraph.rooms.ContainsKey(connectedId))
                    {
                        Vector3 otherPos2D = new Vector3(
                            levelGraph.rooms[connectedId].position.x,
                            levelGraph.rooms[connectedId].position.y,
                            0
                        );
                        Gizmos.DrawLine(pos2D, otherPos2D);
                    }
                }

                // Draw Voronoi points
                Gizmos.color = Color.red;
                foreach (var point in room.voronoiPoints)
                {
                    // Vector3 worldPoint = pos + new Vector3(point.x, 1, point.y);
                    // Gizmos.DrawSphere(worldPoint, 0.5f);
                    Vector3 worldPoint = pos2D + new Vector3(point.x, point.y, 0.5f);
                    Gizmos.DrawSphere(worldPoint, 0.25f);

                }
                if (roomGeometries.TryGetValue(room.id, out var geometry))
                {
                    // Draw Voronoi cell polygons
                    Gizmos.color = Color.yellow;
                    foreach (var cell in geometry.voronoiCells)
                    {
                        for (int i = 0; i < cell.vertices.Count; i++)
                        {
                            int nextIndex = (i + 1) % cell.vertices.Count;
                            Vector3 start = new Vector3(cell.vertices[i].x, cell.vertices[i].y, 0);
                            Vector3 end = new Vector3(cell.vertices[nextIndex].x, cell.vertices[nextIndex].y, 0);
                            Gizmos.DrawLine(start, end);
                        }
                    }

                    // Draw walls
                    Gizmos.color = geometry.walls.Any(w => w.isExterior) ? Color.black : Color.gray;
                    foreach (var wall in geometry.walls)
                    {
                        Vector3 start = new Vector3(wall.start.x, wall.start.y, 0);
                        Vector3 end = new Vector3(wall.end.x, wall.end.y, 0);
                        Gizmos.color = wall.isExterior ? Color.black : Color.gray;
                        Gizmos.DrawLine(start, end);
                    }

                    // Draw platforms
                    Gizmos.color = Color.green;
                    foreach (var platform in geometry.platforms)
                    {
                        Vector3 platformPos = new Vector3(platform.position.x, platform.position.y, platform.height);
                        Gizmos.DrawCube(platformPos, new Vector3(platform.size.x, platform.size.y, 0.2f));
                    }
                }
            }
            if (generatedDoorways != null)
            {
                Gizmos.color = Color.white; // Color for doorways
                foreach (var door in generatedDoorways)
                {
                    Vector3 doorPos3D = new Vector3(door.position.x, door.position.y, 0);
                    Vector3 doorGizmoSize;
                    float gizmoThickness = nodeSize * 0.2f; // Make Gizmo thin

                    if (door.orientation == Doorway.Orientation.Vertical)
                    {
                        // Vertical door: thin in X, 'door.size' in Y
                        doorGizmoSize = new Vector3(gizmoThickness, door.size, gizmoThickness);
                    }
                    else // Horizontal
                    {
                        // Horizontal door: 'door.size' in X, thin in Y
                        doorGizmoSize = new Vector3(door.size, gizmoThickness, gizmoThickness);
                    }
                    Gizmos.DrawCube(doorPos3D, doorGizmoSize);
                }
            }
        }

        private Color GetRoomColor(RoomType type)
        {
            return type switch
            {
                RoomType.Start => Color.green,
                RoomType.End => Color.red,
                RoomType.Ability => Color.blue,
                RoomType.Hub => Color.yellow,
                RoomType.Branch => Color.cyan,
                RoomType.Corridor => Color.magenta,
                _ => Color.white
            };
        }
    }
}