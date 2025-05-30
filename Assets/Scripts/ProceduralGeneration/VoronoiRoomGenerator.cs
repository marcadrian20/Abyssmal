using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralGeneration.GraphGrammar;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;

namespace ProceduralGeneration
{
    public class VoronoiRoomGenerator
    {
        [System.Serializable]
        public class RoomTemplate
        {
            public string nodeLabel;
            public Vector2 baseSize;
            public int voronoiSites;
            public float irregularity = 0.3f;
            public List<RoomFeature> features = new List<RoomFeature>();
        }

        [System.Serializable]
        public class RoomFeature
        {
            public string featureType; // "platform", "pit", "secret_area"
            public Vector2 relativePosition;
            public Vector2 size;
            public float probability = 1f;
        }

        // NEW: Data structures for actual room geometry
        [System.Serializable]
        public class RoomGeometry
        {
            public int roomId;
            public List<VoronoiCell> voronoiCells = new List<VoronoiCell>();
            public List<Vector2> walkableArea = new List<Vector2>();
            public List<WallSegment> walls = new List<WallSegment>();
            public List<Platform> platforms = new List<Platform>();
            public Rect roomBounds;
        }

        [System.Serializable]
        public class VoronoiCell
        {
            public Vector2 site;
            public List<Vector2> vertices = new List<Vector2>();
            public bool isWalkable = true;
        }

        [System.Serializable]
        public class WallSegment
        {
            public Vector2 start;
            public Vector2 end;
            public bool isExterior;

            public WallSegment(Vector2 start, Vector2 end, bool isExterior = false)
            {
                this.start = start;
                this.end = end;
                this.isExterior = isExterior;
            }
        }

        [System.Serializable]
        public class Platform
        {
            public Vector2 position;
            public Vector2 size;
            public float height;
        }

        private Dictionary<string, RoomTemplate> roomTemplates;

        public void InitializeTemplates()
        {
            roomTemplates = new Dictionary<string, RoomTemplate>
            {
                ["START"] = new RoomTemplate
                {
                    nodeLabel = "START",
                    baseSize = new Vector2(15f, 15f),
                    voronoiSites = 8,
                    irregularity = 0.2f
                },
                ["HUB"] = new RoomTemplate
                {
                    nodeLabel = "HUB",
                    baseSize = new Vector2(20f, 20f),
                    voronoiSites = 12,
                    irregularity = 0.4f
                },
                ["ABILITY_WALL_JUMP"] = new RoomTemplate
                {
                    nodeLabel = "ABILITY_WALL_JUMP",
                    baseSize = new Vector2(12f, 18f),
                    voronoiSites = 6,
                    irregularity = 0.1f,
                    features = new List<RoomFeature>
                    {
                        new RoomFeature { featureType = "tall_walls", relativePosition = Vector2.zero, size = new Vector2(2f, 10f) }
                    }
                },
                ["BRANCH"] = new RoomTemplate
                {
                    nodeLabel = "BRANCH",
                    baseSize = new Vector2(10f, 10f),
                    voronoiSites = 6,
                    irregularity = 0.3f
                },
                ["CORRIDOR"] = new RoomTemplate
                {
                    nodeLabel = "CORRIDOR",
                    baseSize = new Vector2(6f, 4f),
                    voronoiSites = 4,
                    irregularity = 0.1f
                }
            };
        }


        public Room CreateRoomShell(GraphNode node)
        {
            var room = new Room(node.id, ConvertLabelToRoomType(node.label));
            return room;
        }

        public void PopulateRoomVoronoiDetails(Room room, GraphNode abstractNode)
        {
            if (room == null || abstractNode == null)
            {
                Debug.LogError("Room or AbstractNode is null in PopulateRoomVoronoiDetails.");
                return;
            }

            string templateKey = GetTemplateKey(abstractNode.label);
            if (!roomTemplates.TryGetValue(templateKey, out var template))
            {
                if (!roomTemplates.TryGetValue("BRANCH", out template))
                {
                    if (!roomTemplates.TryGetValue("START", out template))
                    {
                        Debug.LogError($"Fallback template not found for node label {abstractNode.label}.");
                        return;
                    }
                }
            }

            GenerateVoronoiLayout(room, template);
            AddRoomFeatures(room, template, abstractNode);
        }

        public RoomGeometry GenerateRoomGeometry(Room room)
        {
            if (room.voronoiPoints.Count < 3)
            {
                Debug.LogWarning($"Room {room.id} has insufficient Voronoi points for geometry generation");
                return CreateFallbackGeometry(room);
            }

            // Convert Voronoi points to world space
            var worldPoints = room.voronoiPoints
                .Select(localPoint => room.position + localPoint)
                .ToArray();

            // Generate Delaunay triangulation
            var delaunayPoints = worldPoints.ToPoints();
            // .Select(p => new DelaunatorSharp.Point(p.x, p.y))
            // .ToArray();

            var delaunator = new Delaunator(delaunayPoints);

            // Convert Delaunay to Voronoi
            var voronoiDiagram = ComputeVoronoiFromDelaunay(delaunator, worldPoints, room);

            // Create room geometry
            var roomGeometry = new RoomGeometry
            {
                roomId = room.id,
                voronoiCells = voronoiDiagram,
                walkableArea = GenerateWalkableArea(voronoiDiagram, room),
                walls = GenerateWalls(voronoiDiagram, room),
                platforms = GeneratePlatforms(voronoiDiagram, room),
                roomBounds = room.GetBounds()
            };

            return roomGeometry;
        }

        private List<VoronoiCell> ComputeVoronoiFromDelaunay(Delaunator delaunator, Vector2[] sites, Room room)
        {
            var cells = new List<VoronoiCell>();
            var triangles = delaunator.Triangles;
            // var coords = delaunator.Coords;
            var points = delaunator.Points.ToVectors2();

            // Compute circumcenters of triangles (these become Voronoi vertices)
            var circumcenters = new List<Vector2>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = points[triangles[i]];
                var b = points[triangles[i + 1]];
                var c = points[triangles[i + 2]];
                // Calculate circumcenter of triangle (a, b, c)

                var circumcenter = GetCircumcenter(a, b, c);
                circumcenters.Add(circumcenter);
            }

            // For each Voronoi site, find adjacent triangles and build cell
            for (int siteIndex = 0; siteIndex < sites.Length; siteIndex++)
            {
                var site = sites[siteIndex];
                var cellVertices = new List<Vector2>();

                // Find triangles that contain this site
                var adjacentTriangles = new List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (triangles[i] == siteIndex || triangles[i + 1] == siteIndex || triangles[i + 2] == siteIndex)
                    {
                        adjacentTriangles.Add(i / 3); // Triangle index
                    }
                }

                // Collect circumcenters of adjacent triangles
                foreach (var triangleIndex in adjacentTriangles)
                {
                    if (triangleIndex < circumcenters.Count)
                    {
                        cellVertices.Add(circumcenters[triangleIndex]);
                    }
                }

                // Sort vertices to form a proper polygon
                if (cellVertices.Count >= 3)
                {
                    cellVertices = SortVerticesClockwise(cellVertices, site);

                    // Clip to room bounds
                    cellVertices = ClipPolygonToRoom(cellVertices, room);

                    if (cellVertices.Count >= 3)
                    {
                        cells.Add(new VoronoiCell
                        {
                            site = site,
                            vertices = cellVertices,
                            isWalkable = true
                        });
                    }
                }
            }

            return cells;
        }

        private Vector2 GetPointFromCoords(int index, IList<double> coords)
        {
            return new Vector2((float)coords[index * 2], (float)coords[index * 2 + 1]);
        }

        private Vector2 GetCircumcenter(Vector2 a, Vector2 b, Vector2 c)
        {
            float ad = a.x * a.x + a.y * a.y;
            float bd = b.x * b.x + b.y * b.y;
            float cd = c.x * c.x + c.y * c.y;
            float d = 2 * (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));

            if (Mathf.Abs(d) < 0.0001f) return (a + b + c) / 3f; // Fallback for degenerate triangles

            return new Vector2(
                (ad * (b.y - c.y) + bd * (c.y - a.y) + cd * (a.y - b.y)) / d,
                (ad * (c.x - b.x) + bd * (a.x - c.x) + cd * (b.x - a.x)) / d
            );
        }

        private List<Vector2> SortVerticesClockwise(List<Vector2> vertices, Vector2 center)
        {
            return vertices.OrderBy(v =>
            {
                Vector2 diff = v - center;
                return Mathf.Atan2(diff.y, diff.x);
            }).ToList();
        }

        private List<Vector2> ClipPolygonToRoom(List<Vector2> vertices, Room room)
        {
            var roomBounds = room.GetBounds();
            var clipped = new List<Vector2>();

            foreach (var vertex in vertices)
            {
                // Simple bounding box clipping
                if (roomBounds.Contains(vertex))
                {
                    clipped.Add(vertex);
                }
                else
                {
                    // Find intersection with room boundary
                    var clampedVertex = new Vector2(
                        Mathf.Clamp(vertex.x, roomBounds.xMin, roomBounds.xMax),
                        Mathf.Clamp(vertex.y, roomBounds.yMin, roomBounds.yMax)
                    );
                    clipped.Add(clampedVertex);
                }
            }
            // Remove duplicate points
            var uniqueClipped = new List<Vector2>();
            foreach (var vertex in clipped)
            {
                bool isDuplicate = uniqueClipped.Any(existing => Vector2.Distance(existing, vertex) < 0.1f);
                if (!isDuplicate)
                {
                    uniqueClipped.Add(vertex);
                }
            }

            return uniqueClipped;
        }

        private List<Vector2> GenerateWalkableArea(List<VoronoiCell> cells, Room room)
        {
            var walkableVertices = new List<Vector2>();

            foreach (var cell in cells.Where(c => c.isWalkable))
            {
                // Create slightly smaller walkable area within each cell
                var shrunkVertices = ShrinkPolygon(cell.vertices, 0.3f);
                if (shrunkVertices.Count >= 3)
                {
                    walkableVertices.AddRange(shrunkVertices);
                }
            }

            // Add main floor area
            var roomBounds = room.GetBounds();
            var mainFloor = new List<Vector2>
            {
                new Vector2(roomBounds.xMin + 1f, roomBounds.yMin + 0.5f),
                new Vector2(roomBounds.xMax - 1f, roomBounds.yMin + 0.5f),
                new Vector2(roomBounds.xMax - 1f, roomBounds.yMin + 1.5f),
                new Vector2(roomBounds.xMin + 1f, roomBounds.yMin + 1.5f)
            };
            walkableVertices.AddRange(mainFloor);

            return walkableVertices;
        }

        private List<Vector2> ShrinkPolygon(List<Vector2> vertices, float shrinkAmount)
        {
            if (vertices.Count < 3) return vertices;

            var center = vertices.Aggregate(Vector2.zero, (sum, v) => sum + v) / vertices.Count;
            return vertices.Select(v => Vector2.Lerp(v, center, shrinkAmount)).ToList();
        }

        private List<WallSegment> GenerateWalls(List<VoronoiCell> cells, Room room)
        {
            var walls = new List<WallSegment>();
            var roomBounds = room.GetBounds();

            // Room perimeter walls
            walls.Add(new WallSegment(
                new Vector2(roomBounds.xMin, roomBounds.yMin),
                new Vector2(roomBounds.xMax, roomBounds.yMin), true));
            walls.Add(new WallSegment(
                new Vector2(roomBounds.xMax, roomBounds.yMin),
                new Vector2(roomBounds.xMax, roomBounds.yMax), true));
            walls.Add(new WallSegment(
                new Vector2(roomBounds.xMax, roomBounds.yMax),
                new Vector2(roomBounds.xMin, roomBounds.yMax), true));
            walls.Add(new WallSegment(
                new Vector2(roomBounds.xMin, roomBounds.yMax),
                new Vector2(roomBounds.xMin, roomBounds.yMin), true));

            // Interior walls from Voronoi cell edges
            foreach (var cell in cells)
            {
                if (Random.Range(0f, 1f) < 0.2f) // 20% chance for interior walls
                {
                    for (int i = 0; i < cell.vertices.Count; i++)
                    {
                        int nextIndex = (i + 1) % cell.vertices.Count;
                        var wallSegment = new WallSegment(cell.vertices[i], cell.vertices[nextIndex], false);

                        // Only add reasonable-length walls that don't block too much
                        float wallLength = Vector2.Distance(wallSegment.start, wallSegment.end);
                        if (wallLength > 1f && wallLength < room.size.magnitude * 0.25f)
                        {
                            walls.Add(wallSegment);
                        }
                    }
                }
            }

            return walls;
        }

        private List<Platform> GeneratePlatforms(List<VoronoiCell> cells, Room room)
        {
            var platforms = new List<Platform>();

            foreach (var cell in cells)
            {
                if (Random.Range(0f, 1f) < 0.2f) // 20% chance for platforms
                {
                    var platform = new Platform
                    {
                        position = cell.site,
                        size = new Vector2(
                            Random.Range(1.5f, 3.5f),
                            Random.Range(0.3f, 0.8f)
                        ),
                        height = Random.Range(0.8f, 2.2f)
                    };

                    platforms.Add(platform);
                }
            }

            return platforms;
        }

        private RoomGeometry CreateFallbackGeometry(Room room)
        {
            // Simple rectangular room as fallback
            var roomBounds = room.GetBounds();

            return new RoomGeometry
            {
                roomId = room.id,
                voronoiCells = new List<VoronoiCell>(),
                walkableArea = new List<Vector2>
                {
                    new Vector2(roomBounds.xMin + 1f, roomBounds.yMin + 0.5f),
                    new Vector2(roomBounds.xMax - 1f, roomBounds.yMin + 0.5f),
                    new Vector2(roomBounds.xMax - 1f, roomBounds.yMax - 0.5f),
                    new Vector2(roomBounds.xMin + 1f, roomBounds.yMax - 0.5f)
                },
                walls = new List<WallSegment>
                {
                    new WallSegment(new Vector2(roomBounds.xMin, roomBounds.yMin), new Vector2(roomBounds.xMax, roomBounds.yMin), true),
                    new WallSegment(new Vector2(roomBounds.xMax, roomBounds.yMin), new Vector2(roomBounds.xMax, roomBounds.yMax), true),
                    new WallSegment(new Vector2(roomBounds.xMax, roomBounds.yMax), new Vector2(roomBounds.xMin, roomBounds.yMax), true),
                    new WallSegment(new Vector2(roomBounds.xMin, roomBounds.yMax), new Vector2(roomBounds.xMin, roomBounds.yMin), true)
                },
                platforms = new List<Platform>(),
                roomBounds = roomBounds
            };
        }

        private void GenerateVoronoiLayout(Room room, RoomTemplate template)
        {
            room.voronoiPoints.Clear();

            for (int i = 0; i < template.voronoiSites; i++)
            {
                Vector2 site = new Vector2(
                    Random.Range(-room.size.x * 0.4f, room.size.x * 0.4f),
                    Random.Range(-room.size.y * 0.4f, room.size.y * 0.4f)
                );

                site += Random.insideUnitCircle * template.irregularity * room.size.magnitude * 0.1f;
                room.voronoiPoints.Add(site);
            }

            AddBoundaryPoints(room, template);
        }

        private void AddBoundaryPoints(Room room, RoomTemplate template)
        {
            float margin = 0.35f;
            Vector2 halfSize = room.size * margin;

            room.voronoiPoints.Add(new Vector2(-halfSize.x, -halfSize.y));
            room.voronoiPoints.Add(new Vector2(halfSize.x, -halfSize.y));
            room.voronoiPoints.Add(new Vector2(halfSize.x, halfSize.y));
            room.voronoiPoints.Add(new Vector2(-halfSize.x, halfSize.y));

            if (template.irregularity > 0.3f)
            {
                room.voronoiPoints.Add(new Vector2(0, -halfSize.y));
                room.voronoiPoints.Add(new Vector2(halfSize.x, 0));
                room.voronoiPoints.Add(new Vector2(0, halfSize.y));
                room.voronoiPoints.Add(new Vector2(-halfSize.x, 0));
            }
        }

        private void AddRoomFeatures(Room room, RoomTemplate template, GraphNode node)
        {
            foreach (var feature in template.features)
            {
                if (Random.Range(0f, 1f) <= feature.probability)
                {
                    Vector2 featurePos = Vector2.Scale(feature.relativePosition, room.size);
                    room.voronoiPoints.Add(featurePos);
                }
            }
        }

        private string GetTemplateKey(string nodeLabel)
        {
            return nodeLabel.Contains("ABILITY_") ? nodeLabel : nodeLabel;
        }

        private RoomType ConvertLabelToRoomType(string label)
        {
            return label switch
            {
                "START" => RoomType.Start,
                "END" => RoomType.End,
                "HUB" => RoomType.Hub,
                "BRANCH" => RoomType.Branch,
                "CORRIDOR" => RoomType.Corridor,
                string s when s.StartsWith("ABILITY_") => RoomType.Ability,
                _ => RoomType.Branch
            };
        }

        public Room GenerateRoomGeometry(GraphNode node)
        {
            string templateKey = GetTemplateKey(node.label);
            if (!roomTemplates.TryGetValue(templateKey, out var template))
            {
                template = roomTemplates["START"];
            }

            var room = new Room(node.id, ConvertLabelToRoomType(node.label));
            room.position = node.position;
            room.size = template.baseSize;

            GenerateVoronoiLayout(room, template);
            AddRoomFeatures(room, template, node);

            return room;
        }
    }
}