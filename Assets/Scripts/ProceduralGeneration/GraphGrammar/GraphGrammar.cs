using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ProceduralGeneration.GraphGrammar
{
    [System.Serializable]
    public class GraphNode
    {
        public int id;
        public string label; // e.g., "START", "HUB", "ABILITY_WALL_JUMP"
        public Dictionary<string, object> attributes = new Dictionary<string, object>();
        public List<GraphEdge> edges = new List<GraphEdge>();

        // Spatial properties (for layout)
        public Vector2 position;
        public Vector2 size;
        public bool isFixed = false; // Prevents movement during layout

        public GraphNode(int id, string label)
        {
            this.id = id;
            this.label = label;
        }

        public void SetAttribute(string key, object value)
        {
            attributes[key] = value;
        }

        public T GetAttribute<T>(string key, T defaultValue = default(T))
        {
            return attributes.TryGetValue(key, out var value) ? (T)value : defaultValue;
        }
    }

    [System.Serializable]
    public class GraphEdge
    {
        public int fromNodeId;
        public int toNodeId;
        public string label;
        public Dictionary<string, object> attributes = new Dictionary<string, object>();

        public GraphEdge(int from, int to, string label = "")
        {
            fromNodeId = from;
            toNodeId = to;
            this.label = label;
        }
    }

    [System.Serializable]
    public abstract class ProductionRule
    {
        public string name;
        public int priority = 0;
        public bool isEnabled = true;

        // Pattern matching
        public abstract bool CanApply(LevelGraphGrammar graph, GraphNode node);

        // Graph transformation
        public abstract List<GraphTransformation> Apply(LevelGraphGrammar graph, GraphNode node);

        // Probability for stochastic rules
        [Range(0f, 1f)]
        public float probability = 1f;

        protected bool RollProbability()
        {
            return Random.Range(0f, 1f) <= probability;
        }
    }
    public abstract class SpatialProductionRule : ProductionRule
    {
        public float maxConnectionDistance = 15f; // Maximum distance for direct connection
        public bool enforceAdjacency = true; // Whether this rule requires spatial adjacency

        protected bool AreNodesSpatiallyCompatible(GraphNode nodeA, GraphNode nodeB, LevelGraphGrammar graph)
        {
            if (!enforceAdjacency) return true;

            float distance = Vector2.Distance(nodeA.position, nodeB.position);
            return distance <= maxConnectionDistance;
        }

        protected void EnforceAdjacency(GraphNode nodeA, GraphNode nodeB, float targetDistance = 8f)
        {
            Vector2 direction = (nodeB.position - nodeA.position).normalized;
            nodeB.position = nodeA.position + direction * targetDistance;
        }
    }

    // Rule that ensures critical connections are spatially adjacent
    [System.Serializable]
    public class CriticalPathRule : SpatialProductionRule
    {
        public override bool CanApply(LevelGraphGrammar graph, GraphNode node)
        {
            // Apply to ABILITY nodes that need guaranteed connectivity
            return node.label.StartsWith("ABILITY_") &&
                   node.edges.Any(e => !AreNodesSpatiallyCompatible(node, graph.GetNode(e.toNodeId), graph));
        }

        public override List<GraphTransformation> Apply(LevelGraphGrammar graph, GraphNode node)
        {
            var transformations = new List<GraphTransformation>();

            // For each edge that's too long, insert intermediate nodes
            foreach (var edge in node.edges.ToList())
            {
                var connectedNode = graph.GetNode(edge.toNodeId);
                if (connectedNode != null && !AreNodesSpatiallyCompatible(node, connectedNode, graph))
                {
                    // Insert bridge node
                    var bridgeNode = new GraphNode(graph.GetNextNodeId(), "SPATIAL_BRIDGE");
                    bridgeNode.position = (node.position + connectedNode.position) / 2f;
                    bridgeNode.SetAttribute("generation", node.GetAttribute("generation", 0));
                    bridgeNode.SetAttribute("is_bridge", true);

                    transformations.Add(new GraphTransformation
                    {
                        type = GraphTransformation.TransformationType.AddNode,
                        newNode = bridgeNode
                    });

                    // Replace long edge with two short edges
                    transformations.Add(new GraphTransformation
                    {
                        type = GraphTransformation.TransformationType.AddEdge,
                        newEdge = new GraphEdge(node.id, bridgeNode.id, edge.label)
                    });

                    transformations.Add(new GraphTransformation
                    {
                        type = GraphTransformation.TransformationType.AddEdge,
                        newEdge = new GraphEdge(bridgeNode.id, connectedNode.id, edge.label)
                    });

                    // Remove original long edge
                    transformations.Add(new GraphTransformation
                    {
                        type = GraphTransformation.TransformationType.RemoveEdge,
                        targetEdgeFrom = edge.fromNodeId,
                        targetEdgeTo = edge.toNodeId
                    });
                }
            }

            return transformations;
        }
    }

    [System.Serializable]
    public class GraphTransformation
    {
        public enum TransformationType
        {
            AddNode,
            RemoveNode,
            AddEdge,
            RemoveEdge,
            ModifyNode,
            ModifyEdge
        }

        public TransformationType type;
        public GraphNode newNode;
        public GraphEdge newEdge;
        public int targetNodeId;
        public int targetEdgeFrom;
        public int targetEdgeTo;
        public Dictionary<string, object> modifications = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class SpatialHubExpansionRule : SpatialProductionRule
    {
        public AbilityType abilityToAdd;
        public int minBranches = 1;
        public int maxBranches = 3;

        public override bool CanApply(LevelGraphGrammar graph, GraphNode node)
        {
            if (node.label == "HUB")
            {
                // Check if this hub already has an ability connection of the type this rule would add
                foreach (var edge in node.edges)
                {
                    var connectedNode = graph.GetNode(edge.toNodeId);
                    if (connectedNode != null && connectedNode.label.StartsWith("ABILITY_"))
                    {
                        var grantedAbility = connectedNode.GetAttribute<AbilityType>("ability_granted");
                        if (grantedAbility == abilityToAdd)
                        {
                            return false; // Already has this ability path
                        }
                    }
                }
                return RollProbability();
            }
            return false;
        }

        public override List<GraphTransformation> Apply(LevelGraphGrammar graph, GraphNode node)
        {
            var transformations = new List<GraphTransformation>();
            int generation = node.GetAttribute("generation", 0);

            // Add ABILITY node
            var abilityNode = new GraphNode(graph.GetNextNodeId(), $"ABILITY_{abilityToAdd}");
            abilityNode.SetAttribute("generation", generation + 1);
            abilityNode.SetAttribute("ability_granted", abilityToAdd);
            abilityNode.SetAttribute("accessibility_level", generation);

            // Position ability node adjacent to hub (spatial awareness)
            var hubPos = node.position;
            var direction = Random.insideUnitCircle.normalized;
            abilityNode.position = hubPos + direction * maxConnectionDistance * 0.5f; // Use spatial property

            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddNode,
                newNode = abilityNode
            });

            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddEdge,
                newEdge = new GraphEdge(node.id, abilityNode.id, "ABILITY_GATE")
            });

            // Add random branches
            int branchCount = Random.Range(minBranches, maxBranches + 1);
            for (int i = 0; i < branchCount; i++)
            {
                var branchNode = new GraphNode(graph.GetNextNodeId(), "BRANCH");
                branchNode.SetAttribute("generation", generation + 1);
                branchNode.SetAttribute("accessibility_level", generation + 1);

                // Position branches around the ability room, not the hub
                var branchDirection = Random.insideUnitCircle.normalized;
                branchNode.position = abilityNode.position + branchDirection * maxConnectionDistance * 0.4f;

                transformations.Add(new GraphTransformation
                {
                    type = GraphTransformation.TransformationType.AddNode,
                    newNode = branchNode
                });

                transformations.Add(new GraphTransformation
                {
                    type = GraphTransformation.TransformationType.AddEdge,
                    newEdge = new GraphEdge(abilityNode.id, branchNode.id, "PROGRESSION")
                });
            }

            return transformations;
        }
    }
    public class ConnectivityValidationRule : ProductionRule
    {
        public override bool CanApply(LevelGraphGrammar graph, GraphNode node)
        {
            // Apply at the end of generation to validate connectivity
            return graph.GetAttribute("generation_complete", false) &&
                   HasUnreachableConnections(graph, node);
        }

        public override List<GraphTransformation> Apply(LevelGraphGrammar graph, GraphNode node)
        {
            var transformations = new List<GraphTransformation>();

            // Find unreachable connections and create spatial bridges
            foreach (var edge in node.edges)
            {
                var connectedNode = graph.GetNode(edge.toNodeId);
                if (connectedNode != null && !WouldHaveSpatialConnection(node, connectedNode))
                {
                    // Create a spatial bridge chain
                    var bridgeChain = CreateSpatialBridge(node, connectedNode, graph);
                    transformations.AddRange(bridgeChain);
                }
            }

            return transformations;
        }

        private bool HasUnreachableConnections(LevelGraphGrammar graph, GraphNode node)
        {
            foreach (var edge in node.edges)
            {
                var connectedNode = graph.GetNode(edge.toNodeId);
                if (connectedNode != null && !WouldHaveSpatialConnection(node, connectedNode))
                {
                    return true;
                }
            }
            return false;
        }

        private bool WouldHaveSpatialConnection(GraphNode nodeA, GraphNode nodeB)
        {
            // Simulate your door placement logic
            float distance = Vector2.Distance(nodeA.position, nodeB.position);
            return distance <= 12f; // Your roomSpacing * 2 or similar threshold
        }

        private List<GraphTransformation> CreateSpatialBridge(GraphNode nodeA, GraphNode nodeB, LevelGraphGrammar graph)
        {
            var transformations = new List<GraphTransformation>();

            // Create L-shaped bridge path
            var midPoint1 = new Vector2(nodeB.position.x, nodeA.position.y);
            var midPoint2 = new Vector2(nodeA.position.x, nodeB.position.y);

            // Choose the shorter path
            var chosenMidpoint = Vector2.Distance(nodeA.position, midPoint1) + Vector2.Distance(midPoint1, nodeB.position) <
                               Vector2.Distance(nodeA.position, midPoint2) + Vector2.Distance(midPoint2, nodeB.position)
                               ? midPoint1 : midPoint2;

            // Create bridge node
            var bridgeNode = new GraphNode(graph.GetNextNodeId(), "SPATIAL_BRIDGE");
            bridgeNode.position = chosenMidpoint;
            bridgeNode.SetAttribute("generation", Mathf.Max(
                nodeA.GetAttribute("generation", 0),
                nodeB.GetAttribute("generation", 0)
            ));

            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddNode,
                newNode = bridgeNode
            });

            // Replace direct edge with bridged path
            var originalEdge = nodeA.edges.FirstOrDefault(e => e.toNodeId == nodeB.id);
            if (originalEdge != null)
            {
                transformations.Add(new GraphTransformation
                {
                    type = GraphTransformation.TransformationType.RemoveEdge,
                    targetEdgeFrom = originalEdge.fromNodeId,
                    targetEdgeTo = originalEdge.toNodeId
                });

                transformations.Add(new GraphTransformation
                {
                    type = GraphTransformation.TransformationType.AddEdge,
                    newEdge = new GraphEdge(nodeA.id, bridgeNode.id, originalEdge.label)
                });

                transformations.Add(new GraphTransformation
                {
                    type = GraphTransformation.TransformationType.AddEdge,
                    newEdge = new GraphEdge(bridgeNode.id, nodeB.id, originalEdge.label)
                });
            }

            return transformations;
        }
    }
    // Rule: START -> START + HUB
    [System.Serializable]
    public class StartExpansionRule : ProductionRule
    {
        public override bool CanApply(LevelGraphGrammar graph, GraphNode node)
        {
            return node.label == "START" &&
                   node.edges.Count == 0 &&
                   RollProbability();
        }

        public override List<GraphTransformation> Apply(LevelGraphGrammar graph, GraphNode node)
        {
            var transformations = new List<GraphTransformation>();

            // Add HUB node
            var hubNode = new GraphNode(graph.GetNextNodeId(), "HUB");
            hubNode.SetAttribute("generation", 1);
            hubNode.SetAttribute("accessibility_level", 0);

            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddNode,
                newNode = hubNode
            });

            // Connect START to HUB
            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddEdge,
                newEdge = new GraphEdge(node.id, hubNode.id, "PASSAGE")
            });

            return transformations;
        }
    }

    // Rule: HUB -> HUB + ABILITY + BRANCH*
    [System.Serializable]
    public class HubExpansionRule : ProductionRule
    {
        public AbilityType abilityToAdd;
        public int minBranches = 1;
        public int maxBranches = 3;

        public override bool CanApply(LevelGraphGrammar graph, GraphNode node)
        {
            // if (node.label != "HUB") return false;

            // int generation = node.GetAttribute("generation", 0);
            // var maxGeneration = graph.GetAttribute("max_generation", 4);

            // return generation < maxGeneration &&
            //        !HasAbilityConnection(graph, node) &&
            //        RollProbability();
            if (node.label == "HUB")
            {
                // Check if this hub already has an ability connection of the type this rule would add
                // to prevent adding the same ability type multiple times from the same hub via this rule.
                foreach (var edge in node.edges)
                {
                    var connectedNode = graph.GetNode(edge.toNodeId);
                    if (connectedNode != null && connectedNode.label.StartsWith("ABILITY_"))
                    {
                        var grantedAbility = connectedNode.GetAttribute<AbilityType>("ability_granted");
                        if (grantedAbility == abilityToAdd)
                        {
                            return false; // Already has this ability path
                        }
                    }
                }
                return RollProbability();
                // It's a HUB and doesn't have this specific ability path yet
            }
            return false;
        }


        private bool HasAbilityConnection(LevelGraphGrammar graph, GraphNode hub)
        {
            // return hub.edges.Any(e =>
            // {
            //     var connected = graph.GetNode(e.toNodeId);
            //     return connected?.label.StartsWith("ABILITY_") == true;
            // });
            foreach (var edge in hub.edges)
            {
                var connectedNode = graph.GetNode(edge.toNodeId);
                if (connectedNode != null && connectedNode.label.StartsWith("ABILITY_"))
                {
                    return true;
                }
            }
            return false;

        }

        public override List<GraphTransformation> Apply(LevelGraphGrammar graph, GraphNode node)
        {
            var transformations = new List<GraphTransformation>();
            int generation = node.GetAttribute("generation", 0);

            // Add ABILITY node
            var abilityNode = new GraphNode(graph.GetNextNodeId(), $"ABILITY_{abilityToAdd}");
            abilityNode.SetAttribute("generation", generation + 1);
            abilityNode.SetAttribute("ability_granted", abilityToAdd);
            abilityNode.SetAttribute("accessibility_level", generation);

            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddNode,
                newNode = abilityNode
            });

            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddEdge,
                newEdge = new GraphEdge(node.id, abilityNode.id, "ABILITY_GATE")
            });

            // Add random branches
            int branchCount = Random.Range(minBranches, maxBranches + 1);
            for (int i = 0; i < branchCount; i++)
            {
                var branchNode = new GraphNode(graph.GetNextNodeId(), "BRANCH");
                branchNode.SetAttribute("generation", generation + 1);
                branchNode.SetAttribute("accessibility_level", generation + 1); // Requires the new ability

                transformations.Add(new GraphTransformation
                {
                    type = GraphTransformation.TransformationType.AddNode,
                    newNode = branchNode
                });

                transformations.Add(new GraphTransformation
                {
                    type = GraphTransformation.TransformationType.AddEdge,
                    newEdge = new GraphEdge(abilityNode.id, branchNode.id, "PROGRESSION")
                });
            }

            return transformations;
        }
    }

    // Rule: BRANCH -> BRANCH + (HUB | CORRIDOR | TERMINAL)
    [System.Serializable]
    public class BranchExpansionRule : ProductionRule
    {
        [System.Serializable]
        public class ExpansionOption
        {
            public string nodeType;
            [Range(0f, 1f)]
            public float weight;
        }

        public List<ExpansionOption> expansionOptions = new List<ExpansionOption>
        {
            new ExpansionOption { nodeType = "HUB", weight = 0.3f },
            new ExpansionOption { nodeType = "CORRIDOR", weight = 0.5f },
            new ExpansionOption { nodeType = "TERMINAL", weight = 0.2f }
        };

        public override bool CanApply(LevelGraphGrammar graph, GraphNode node)
        {
            // return node.label == "BRANCH" &&
            //        node.edges.Count(e => e.fromNodeId == node.id) == 0 && // No outgoing edges
            //        RollProbability();
            if (node.label != "BRANCH")
            {
                return false;
            }

            // Check if this branch node has already expanded into one of its possible child types.
            // An outgoing edge from this node (where edge.fromNodeId == node.id)
            // to a node whose label is one of the expansionOptions.nodeType indicates it has expanded.
            foreach (var edge in node.edges)
            {
                if (edge.fromNodeId == node.id) // This is an edge originating from the current branch node
                {
                    var connectedNode = graph.GetNode(edge.toNodeId);
                    // If this outgoing edge connects to a node type that this rule can produce,
                    // it means this rule (or a similar one) has already been applied to this branch.
                    if (connectedNode != null && expansionOptions.Any(opt => opt.nodeType == connectedNode.label))
                    {
                        return false; // Already expanded by this rule type
                    }
                }
            }

            return RollProbability(); // If no such outgoing edge exists, it can apply (subject to probability)

        }

        public override List<GraphTransformation> Apply(LevelGraphGrammar graph, GraphNode node)
        {
            var transformations = new List<GraphTransformation>();

            // Weighted random selection
            string selectedType = WeightedRandomSelect(expansionOptions);
            int generation = node.GetAttribute("generation", 0);
            int accessibilityLevel = node.GetAttribute("accessibility_level", 0);

            var newNode = new GraphNode(graph.GetNextNodeId(), selectedType);
            newNode.SetAttribute("generation", generation + 1);
            newNode.SetAttribute("accessibility_level", accessibilityLevel);

            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddNode,
                newNode = newNode
            });

            transformations.Add(new GraphTransformation
            {
                type = GraphTransformation.TransformationType.AddEdge,
                newEdge = new GraphEdge(node.id, newNode.id, "PASSAGE")
            });

            return transformations;
        }

        private string WeightedRandomSelect(List<ExpansionOption> options)
        {
            float totalWeight = options.Sum(o => o.weight);
            float randomValue = Random.Range(0f, totalWeight);

            float currentWeight = 0f;
            foreach (var option in options)
            {
                currentWeight += option.weight;
                if (randomValue <= currentWeight)
                    return option.nodeType;
            }

            return options.Last().nodeType;
        }
    }
}