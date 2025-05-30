using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace ProceduralGeneration
{
    public class TilemapGenerator : MonoBehaviour
    {
        [Header("Tilemap References")]
        public Tilemap solidTilemap;        // Ground, walls, platforms
        public Tilemap backgroundTilemap;   // Background decorations

        [Header("Solid Tile Assets")]
        public TileBase groundTile;
        public TileBase wallTile;
        public TileBase platformTile;
        public TileBase[] groundVariations;  // Different ground tile types
        public TileBase[] wallVariations;    // Different wall tile types

        [Header("Background Tile Assets")]
        public TileBase[] backgroundTiles;
        public TileBase[] decorationTiles;

        [Header("Generation Settings")]
        public float tileSize = 1f;
        public int backgroundDensity = 2; // Every N tiles
        public float decorationProbability = 0.15f;
        public bool generatePlatformsOnSolidTilemap = true; // Keep platforms on main tilemap

        private Dictionary<int, VoronoiRoomGenerator.RoomGeometry> roomGeometries;

        public void InitializeTilemaps()
        {
            // Create tilemap GameObjects if they don't exist
            if (solidTilemap == null)
                solidTilemap = CreateTilemap("Solid", 0, true); // Has collider
            if (backgroundTilemap == null)
                backgroundTilemap = CreateTilemap("Background", -1, false); // No collider

            Debug.Log("Tilemaps initialized");
        }

        public void GenerateTilemaps(Dictionary<int, VoronoiRoomGenerator.RoomGeometry> geometries, LevelGraph levelGraph)
        {
            roomGeometries = geometries;

            // Clear existing tiles
            ClearAllTilemaps();

            // Generate each room on the solid tilemap
            foreach (var kvp in roomGeometries)
            {
                var roomId = kvp.Key;
                var geometry = kvp.Value;
                var room = levelGraph.rooms[roomId];

                GenerateRoomTiles(room, geometry);
            }

            // Generate connections between rooms
            GenerateCorridorTiles(levelGraph);

            // Generate background separately
            GenerateBackgroundTiles(levelGraph);

            Debug.Log("Tilemap generation completed!");
        }

        private void GenerateRoomTiles(Room room, VoronoiRoomGenerator.RoomGeometry geometry)
        {
            // Generate all solid elements on the same tilemap
            GenerateRoomFloor(room, geometry);
            GenerateRoomWalls(geometry);
            GenerateRoomPlatforms(geometry);
            GenerateVoronoiEnvironmentDetails(geometry, room);
        }

        private void GenerateRoomFloor(Room room, VoronoiRoomGenerator.RoomGeometry geometry)
        {
            // Create a solid floor for the room
            var roomBounds = geometry.roomBounds;

            // Main floor area
            for (float x = roomBounds.xMin + 1; x < roomBounds.xMax - 1; x += tileSize)
            {
                Vector3Int floorPos = WorldToTilePosition(new Vector2(x, roomBounds.yMin + 0.5f));

                // Use ground variations for visual interest
                TileBase floorTile = GetGroundTileVariation();
                solidTilemap.SetTile(floorPos, floorTile);
            }

            // Fill walkable areas from Voronoi cells
            foreach (var cell in geometry.voronoiCells.Where(c => c.isWalkable))
            {
                FillVoronoiCellFloor(cell);
            }

            // Add additional walkable area
            foreach (var walkablePoint in geometry.walkableArea)
            {
                Vector3Int tilePos = WorldToTilePosition(walkablePoint);
                solidTilemap.SetTile(tilePos, GetGroundTileVariation());
            }
        }

        private void FillVoronoiCellFloor(VoronoiRoomGenerator.VoronoiCell cell)
        {
            if (cell.vertices.Count < 3) return;

            // Get bounding box of the cell
            var minX = cell.vertices.Min(v => v.x);
            var maxX = cell.vertices.Max(v => v.x);
            var minY = cell.vertices.Min(v => v.y);
            var maxY = cell.vertices.Max(v => v.y);

            // Fill the cell area with floor tiles
            for (float x = minX; x <= maxX; x += tileSize)
            {
                for (float y = minY; y <= maxY; y += tileSize)
                {
                    Vector2 point = new Vector2(x, y);
                    if (IsPointInPolygon(point, cell.vertices.ToArray()))
                    {
                        Vector3Int tilePos = WorldToTilePosition(point);
                        solidTilemap.SetTile(tilePos, GetGroundTileVariation());
                    }
                }
            }
        }

        private void GenerateRoomWalls(VoronoiRoomGenerator.RoomGeometry geometry)
        {
            foreach (var wall in geometry.walls)
            {
                GenerateWallLine(wall.start, wall.end, wall.isExterior);
            }
        }

        private void GenerateWallLine(Vector2 start, Vector2 end, bool isExterior)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.CeilToInt(distance / tileSize);

            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? (float)i / steps : 0;
                Vector2 point = Vector2.Lerp(start, end, t);
                Vector3Int tilePos = WorldToTilePosition(point);

                // Use different wall tiles for variation
                TileBase wallTileToUse = GetWallTileVariation(isExterior);
                solidTilemap.SetTile(tilePos, wallTileToUse);

                // Add wall thickness for exterior walls
                if (isExterior)
                {
                    solidTilemap.SetTile(tilePos + Vector3Int.up, wallTileToUse);

                    // Add foundation below exterior walls
                    solidTilemap.SetTile(tilePos + Vector3Int.down, wallTileToUse);
                }
            }
        }

        private void GenerateRoomPlatforms(VoronoiRoomGenerator.RoomGeometry geometry)
        {
            foreach (var platform in geometry.platforms)
            {
                GeneratePlatform(platform);
            }
        }

        private void GeneratePlatform(VoronoiRoomGenerator.Platform platform)
        {
            Vector3Int centerPos = WorldToTilePosition(platform.position);
            int widthTiles = Mathf.RoundToInt(platform.size.x / tileSize);
            int heightTiles = Mathf.Max(1, Mathf.RoundToInt(platform.height / tileSize));

            // Generate platform surface
            for (int x = -widthTiles / 2; x <= widthTiles / 2; x++)
            {
                Vector3Int surfacePos = centerPos + new Vector3Int(x, heightTiles, 0);
                solidTilemap.SetTile(surfacePos, platformTile);
            }

            // Generate platform supports (pillars)
            for (int y = 1; y < heightTiles; y++)
            {
                // Support pillars at edges
                Vector3Int leftSupport = centerPos + new Vector3Int(-widthTiles / 2, y, 0);
                Vector3Int rightSupport = centerPos + new Vector3Int(widthTiles / 2, y, 0);

                solidTilemap.SetTile(leftSupport, platformTile);
                solidTilemap.SetTile(rightSupport, platformTile);

                // Occasional middle supports for longer platforms
                if (widthTiles > 4 && y % 2 == 0)
                {
                    Vector3Int middleSupport = centerPos + new Vector3Int(0, y, 0);
                    solidTilemap.SetTile(middleSupport, platformTile);
                }
            }
        }

        private void GenerateVoronoiEnvironmentDetails(VoronoiRoomGenerator.RoomGeometry geometry, Room room)
        {
            // Add environmental details along Voronoi cell edges
            foreach (var cell in geometry.voronoiCells)
            {
                if (Random.Range(0f, 1f) < 0.2f) // 20% chance for details
                {
                    for (int i = 0; i < cell.vertices.Count; i++)
                    {
                        int nextIndex = (i + 1) % cell.vertices.Count;

                        // Occasionally add wall details along cell edges
                        if (Random.Range(0f, 1f) < 0.3f)
                        {
                            Vector2 edgeMidpoint = (cell.vertices[i] + cell.vertices[nextIndex]) * 0.5f;
                            Vector3Int tilePos = WorldToTilePosition(edgeMidpoint);

                            // Only add if there's no existing tile
                            if (!solidTilemap.HasTile(tilePos))
                            {
                                solidTilemap.SetTile(tilePos, GetWallTileVariation(false));
                            }
                        }
                    }
                }
            }
        }

        private void GenerateCorridorTiles(LevelGraph levelGraph)
        {
            foreach (var room in levelGraph.rooms.Values)
            {
                foreach (var connectionId in room.connections)
                {
                    if (levelGraph.rooms.ContainsKey(connectionId) && room.id < connectionId)
                    {
                        var otherRoom = levelGraph.rooms[connectionId];
                        GenerateCorridorBetweenRooms(room, otherRoom);
                    }
                }
            }
        }

        private void GenerateCorridorBetweenRooms(Room room1, Room room2)
        {
            Vector2 start = room1.position;
            Vector2 end = room2.position;

            // Create L-shaped corridor
            Vector2 corner = new Vector2(start.x, end.y);

            GenerateCorridorSegment(start, corner);
            GenerateCorridorSegment(corner, end);
        }

        private void GenerateCorridorSegment(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.CeilToInt(distance / tileSize);

            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? (float)i / steps : 0;
                Vector2 point = Vector2.Lerp(start, end, t);
                Vector3Int centerPos = WorldToTilePosition(point);

                // Create 3-wide corridor floor
                for (int offset = -1; offset <= 1; offset++)
                {
                    Vector3Int floorPos = centerPos + Vector3Int.right * offset;
                    solidTilemap.SetTile(floorPos, GetGroundTileVariation());

                    // Add walls on the sides
                    if (offset == -1 || offset == 1)
                    {
                        Vector3Int wallUp = floorPos + Vector3Int.up;
                        Vector3Int wallDown = floorPos + Vector3Int.down;

                        if (!solidTilemap.HasTile(wallUp))
                            solidTilemap.SetTile(wallUp, GetWallTileVariation(true));
                        if (!solidTilemap.HasTile(wallDown))
                            solidTilemap.SetTile(wallDown, GetWallTileVariation(true));
                    }
                }
            }
        }

        private void GenerateBackgroundTiles(LevelGraph levelGraph)
        {
            if (backgroundTiles.Length == 0) return;

            // Get overall level bounds
            var allRooms = levelGraph.rooms.Values;
            if (!allRooms.Any()) return;

            var minX = allRooms.Min(r => r.position.x - r.size.x / 2) - 5;
            var maxX = allRooms.Max(r => r.position.x + r.size.x / 2) + 5;
            var minY = allRooms.Min(r => r.position.y - r.size.y / 2) - 5;
            var maxY = allRooms.Max(r => r.position.y + r.size.y / 2) + 5;

            // Fill background with tiled pattern
            for (float x = minX; x <= maxX; x += tileSize * backgroundDensity)
            {
                for (float y = minY; y <= maxY; y += tileSize * backgroundDensity)
                {
                    Vector3Int tilePos = WorldToTilePosition(new Vector2(x, y));

                    // Choose background tile
                    var bgTile = backgroundTiles[Random.Range(0, backgroundTiles.Length)];
                    backgroundTilemap.SetTile(tilePos, bgTile);

                    // Occasionally add decorations
                    if (decorationTiles.Length > 0 && Random.Range(0f, 1f) < decorationProbability)
                    {
                        var decorationTile = decorationTiles[Random.Range(0, decorationTiles.Length)];
                        Vector3Int decorationPos = tilePos + Vector3Int.up;
                        backgroundTilemap.SetTile(decorationPos, decorationTile);
                    }
                }
            }
        }

        #region Utility Methods

        private TileBase GetGroundTileVariation()
        {
            if (groundVariations.Length == 0) return groundTile;

            // 80% chance for main ground tile, 20% for variations
            if (Random.Range(0f, 1f) < 0.8f)
                return groundTile;
            else
                return groundVariations[Random.Range(0, groundVariations.Length)];
        }

        private TileBase GetWallTileVariation(bool isExterior)
        {
            if (wallVariations.Length == 0) return wallTile;

            // Use different probabilities for exterior vs interior walls
            float variationChance = isExterior ? 0.1f : 0.3f;

            if (Random.Range(0f, 1f) < variationChance)
                return wallVariations[Random.Range(0, wallVariations.Length)];
            else
                return wallTile;
        }

        private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;

            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 pi = polygon[i];
                Vector2 pj = polygon[j];

                if ((pi.y > point.y) != (pj.y > point.y) &&
                    point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        private Vector3Int WorldToTilePosition(Vector2 worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / tileSize),
                Mathf.FloorToInt(worldPos.y / tileSize),
                0
            );
        }

        private Tilemap CreateTilemap(string name, int sortingOrder, bool hasCollider)
        {
            GameObject tilemapGO = new GameObject(name);
            tilemapGO.transform.SetParent(transform);

            var tilemap = tilemapGO.AddComponent<Tilemap>();
            var renderer = tilemapGO.AddComponent<TilemapRenderer>();

            renderer.sortingOrder = sortingOrder;

            // Add collider only to solid tilemap
            if (hasCollider)
            {
                var collider = tilemapGO.AddComponent<TilemapCollider2D>();
                collider.compositeOperation = Collider2D.CompositeOperation.Merge;
                // Add composite collider for better performance
                var rigidbody = tilemapGO.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Static;

                var compositeCollider = tilemapGO.AddComponent<CompositeCollider2D>();
                compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            }

            return tilemap;
        }

        private void ClearAllTilemaps()
        {
            if (solidTilemap != null)
            {
                solidTilemap.SetTilesBlock(solidTilemap.cellBounds,
                    new TileBase[solidTilemap.cellBounds.size.x * solidTilemap.cellBounds.size.y * solidTilemap.cellBounds.size.z]);
            }

            if (backgroundTilemap != null)
            {
                backgroundTilemap.SetTilesBlock(backgroundTilemap.cellBounds,
                    new TileBase[backgroundTilemap.cellBounds.size.x * backgroundTilemap.cellBounds.size.y * backgroundTilemap.cellBounds.size.z]);
            }
        }

        #endregion
    }
}