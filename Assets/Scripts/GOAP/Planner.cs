using System.Collections.Generic;
using UnityEngine;

namespace AI.GOAP
{
    public class Planner
    {
        public class Node
        {
            public Node parent = null;
            public float cost = 0;
            public Action action = null;
            public Dictionary<string, int> state = null;

            public Node(Node parent, float cost, Action action, Dictionary<string, int> allStates)
            {
                this.parent = parent;
                this.cost = cost;
                this.action = action;
                this.state = new Dictionary<string, int>(allStates);
            }
        }

        private Queue<Action> PlanActions(List<Action> actions, string goal)
        {
            Node start = new Node(null, 0, null, null);
            List<Node> leaves = new List<Node>();
            bool success = BuildGraph(start, leaves, actions, goal);
            if (!success)
            {
                return null;
            }

            Node cheapest = null;
            foreach (Node leaf in leaves)
            {
                if (cheapest == null || leaf.cost < cheapest.cost)
                {
                    cheapest = leaf;
                }
            }

            List<Action> results = new List<Action>();
            Node node = cheapest;
            while (node != null)
            {
                if (node.action != null)
                {
                    results.Insert(0, node.action);
                }
                node = node.parent;
            }

            Queue<Action> queue = new Queue<Action>();
            foreach (Action result in results)
            {
                queue.Enqueue(result);
            }
            return queue;
        }

        private bool BuildGraph(Node parent, List<Node> leaves, List<Action> actions, string goal)
        {
            bool foundPath = false;
            foreach (Action action in actions)
            {
                if (action.IsAchievable(parent.state))
                {
                    Dictionary<string, int> currentState = new Dictionary<string, int>(parent.state);
                    foreach (KeyValuePair<string, int> effect in action.GetAfterEffects())
                    {
                        if (!currentState.ContainsKey(effect.Key))
                        {
                            currentState.Add(effect.Key, effect.Value);
                        }

                        Node node = new Node(parent, parent.cost + action.GetCost(), action, null);

                        if (IsGoalAchieved(goal, currentState))
                        {
                            leaves.Add(node);
                            foundPath = true;
                        }
                        else
                        {
                            List<Action> actionSubset = GetActionSubset(actions, action);
                            bool found = BuildGraph(node, leaves, actionSubset, goal);
                            if (found)
                            {
                                foundPath = true;
                            }
                        }
                    }
                }
            }
            return foundPath;
        }

        private bool IsGoalAchieved(string goal, Dictionary<string, int> state)
        {
            if (!state.ContainsKey(goal))
            {
                return false;
            }
            return true;
        }

        private List<Action> GetActionSubset(List<Action> actions, Action actionToRemove)
        {
            List<Action> subset = new List<Action>();
            foreach (Action action in actions)
            {
                if (!action.Equals(actionToRemove))
                {
                    subset.Add(action);
                }
            }
            return subset;
        }
    }
}