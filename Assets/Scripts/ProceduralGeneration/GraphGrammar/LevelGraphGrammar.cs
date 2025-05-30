using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ProceduralGeneration.GraphGrammar
{
    public class LevelGraphGrammar
    {
        private Dictionary<int, GraphNode> nodes = new Dictionary<int, GraphNode>();
        private List<ProductionRule> rules = new List<ProductionRule>();
        private Dictionary<string, object> graphAttributes = new Dictionary<string, object>();
        private int nextNodeId = 0;
        
        public int GetNextNodeId() => nextNodeId++;
        
        public void SetAttribute(string key, object value)
        {
            graphAttributes[key] = value;
        }
        
        public T GetAttribute<T>(string key, T defaultValue = default(T))
        {
            return graphAttributes.TryGetValue(key, out var value) ? (T)value : defaultValue;
        }
        
        public void AddNode(GraphNode node)
        {
            nodes[node.id] = node;
        }
        
        public GraphNode GetNode(int id)
        {
            return nodes.TryGetValue(id, out var node) ? node : null;
        }
        
        public void AddRule(ProductionRule rule)
        {
            rules.Add(rule);
            rules = rules.OrderByDescending(r => r.priority).ToList();
        }
        
        public void GenerateGraph(int maxIterations = 100)
        {
            // Initialize with START node
            var startNode = new GraphNode(GetNextNodeId(), "START");
            startNode.SetAttribute("generation", 0);
            startNode.isFixed = true; // START position is fixed
            AddNode(startNode);
            
            SetAttribute("max_generation", 4);
            // SetAttribute("target_node_count", 25);
            
            // Apply production rules iteratively
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                if (nodes.Count >= GetAttribute("target_node_count", 25))
                    break;
                Debug.Log($"Iteration {iteration}: {nodes.Count} nodes generated.");    
                bool ruleApplied = false;
                
                // Try to apply rules to each node
                foreach (var node in nodes.Values.ToList())
                {
                    foreach (var rule in rules.Where(r => r.isEnabled))
                    {
                        if (rule.CanApply(this, node))
                        {
                            var transformations = rule.Apply(this, node);
                            ApplyTransformations(transformations);
                            ruleApplied = true;
                            break; // Apply one rule per node per iteration
                        }
                    }
                    
                    if (ruleApplied) break;
                }
                
                if (!ruleApplied)
                {
                    Debug.Log($"No more rules can be applied. Stopping at iteration {iteration}");
                    break;
                }
            }
            
            // Add END node
            AddEndNode();
            
            Debug.Log($"Graph generation complete. {nodes.Count} nodes generated.");
        }
        
        private void ApplyTransformations(List<GraphTransformation> transformations)
        {
            foreach (var transform in transformations)
            {
                switch (transform.type)
                {
                    case GraphTransformation.TransformationType.AddNode:
                        AddNode(transform.newNode);
                        break;
                        
                    case GraphTransformation.TransformationType.AddEdge:
                        var fromNode = GetNode(transform.newEdge.fromNodeId);
                        var toNode = GetNode(transform.newEdge.toNodeId);
                        if (fromNode != null && toNode != null)
                        {
                            fromNode.edges.Add(transform.newEdge);
                            // Add reverse edge for undirected graph
                            toNode.edges.Add(new GraphEdge(toNode.id, fromNode.id, transform.newEdge.label));
                        }
                        break;
                        
                    case GraphTransformation.TransformationType.ModifyNode:
                        var nodeToModify = GetNode(transform.targetNodeId);
                        if (nodeToModify != null)
                        {
                            foreach (var mod in transform.modifications)
                            {
                                nodeToModify.SetAttribute(mod.Key, mod.Value);
                            }
                        }
                        break;
                }
            }
        }
        
        private void AddEndNode()
        {
            // Find the most advanced ability nodes
            var abilityNodes = nodes.Values
                .Where(n => n.label.StartsWith("ABILITY_"))
                .OrderByDescending(n => n.GetAttribute("generation", 0));
                
            if (abilityNodes.Any())
            {
                var endNode = new GraphNode(GetNextNodeId(), "END");
                endNode.SetAttribute("generation", abilityNodes.First().GetAttribute("generation", 0) + 1);
                
                // Require all abilities for end node
                var requiredAbilities = abilityNodes.Select(n => n.GetAttribute<AbilityType>("ability_granted")).ToList();
                endNode.SetAttribute("required_abilities", requiredAbilities);
                
                AddNode(endNode);
                
                // Connect to the last ability node
                var lastAbilityNode = abilityNodes.First();
                lastAbilityNode.edges.Add(new GraphEdge(lastAbilityNode.id, endNode.id, "FINAL_PASSAGE"));
                endNode.edges.Add(new GraphEdge(endNode.id, lastAbilityNode.id, "FINAL_PASSAGE"));
            }
        }
        
        public Dictionary<int, GraphNode> GetNodes() => new Dictionary<int, GraphNode>(nodes);
        
        public List<GraphNode> GetNodesByLabel(string label)
        {
            return nodes.Values.Where(n => n.label == label).ToList();
        }
    }
}