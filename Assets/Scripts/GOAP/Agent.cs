using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace AI.GOAP
{
    [SelectionBase]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Agent : MonoBehaviour
    {
        [SerializeField] float senseRadius = 10f;
        [SerializeField] float searchDistance = 10f;
        [SerializeField] float withinTargetRange = 2f;
        [SerializeField] List<Goal> goals = null;
        [SerializeField] List<Action> actions = null;

        NavMeshAgent navMeshAgent;
        SphereCollider senseSphere;

        List<string> states = new List<string>();
        Goal currentGoal;
        Queue<Action> actionQueue;
        Action currentAction;
        Vector3 currentDestination;
        bool isDoingAction = false;
        bool isSearching = false;
        bool foundTarget = false;

        [System.Serializable]
        private class Goal
        {
            public string goal = "";
            public int importance = 1;
            public bool removable = false;
        }

        private class ActionNode
        {
            public ActionNode parent = null;
            public float cost = 0;
            public Action action = null;
            public List<string> state = null;

            public ActionNode(ActionNode parent, float cost, Action action, List<string> states)
            {
                this.parent = parent;
                this.cost = cost;
                this.action = action;
                this.state = new List<string>(states);
            }
        }

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            senseSphere = GetComponent<SphereCollider>();
        }

        private void Start()
        {
            senseSphere.radius = senseRadius;
            states.Add("hungry");
        }

        private void LateUpdate()
        {
            if (actionQueue == null && !isDoingAction)
            {
                SetActionQueue();
            }

            if (actionQueue != null && actionQueue.Count == 0)
            {
                if (currentGoal.removable)
                {
                    goals.Remove(currentGoal);
                }
                actionQueue = null;
            }

            if (actionQueue != null && actionQueue.Count > 0)
            {
                SetCurrentAction();
            }

            if (isDoingAction && !isSearching)
            {
                SearchBehaviour();
            }

            if (currentAction != null && isDoingAction)
            {
                CheckIfCloseToAction();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentAction == null) { return; }
            if (other.gameObject.tag == currentAction.GetLocationTag())
            {
                foundTarget = true;
                Vector3 dest = other.ClosestPoint(transform.position);
                MoveTo(dest);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, senseRadius);

            if (foundTarget)
            {
                Gizmos.DrawLine(transform.position, currentDestination);
            }
        }

        private void SetActionQueue()
        {
            var sortedGoals = from entry in goals orderby entry descending select entry;
            foreach (Goal goal in sortedGoals)
            {
                actionQueue = PlanActions(actions, states, goal);
                if (actionQueue != null)
                {
                    currentGoal = goal;
                    break;
                }
            }
        }

        private void SetCurrentAction()
        {
            isDoingAction = true;
            currentAction = actionQueue.Dequeue();
            Debug.Log("New action: " + currentAction.name);
        }

        private void SearchBehaviour()
        {
            isSearching = true;
            Vector3 randomDirection = Random.insideUnitSphere * searchDistance;
            randomDirection += transform.position;
            NavMeshHit navHit;
            NavMesh.SamplePosition (randomDirection, out navHit, searchDistance, -1);
            MoveTo(navHit.position);
        }

        private void CheckIfCloseToAction()
        {
            float distanceToTarget = Vector3.Distance(currentDestination, transform.position);
            if (distanceToTarget < withinTargetRange)
            {
                isSearching = false;
                if (foundTarget) {
                    StartCoroutine(DoAction());
                }
            }
        }

        private void MoveTo(Vector3 newDestination)
        {
            currentDestination = newDestination;
            navMeshAgent.destination = currentDestination;
            navMeshAgent.isStopped = false;
        }

        private Queue<Action> PlanActions(List<Action> actions, List<string> states, Goal goal)
        {
            ActionNode start = new ActionNode(null, 0, null, states);
            List<ActionNode> actionNodes = new List<ActionNode>();
            bool success = BuildGraph(start, actionNodes, actions, goal);
            if (!success)
            {
                return null;
            }

            ActionNode cheapest = FindCheapest(actionNodes);
            List<Action> actionList = OrderActions(cheapest);
            Queue<Action> actionQueue = BuildActionQueue(actionList);
            return actionQueue;
        }

        private IEnumerator DoAction()
        {
            isDoingAction = true;
            navMeshAgent.isStopped = true;
            yield return new WaitForSeconds(currentAction.GetDuration());
            currentAction = null;
            isDoingAction = false;
            foundTarget = false;
        }

        private bool BuildGraph(ActionNode parent, List<ActionNode> actionNodes, List<Action> actions, Goal goal)
        {
            bool foundPath = false;
            foreach (Action action in actions)
            {
                if (action.IsAchievable(parent.state))
                {
                    List<string> currentState = new List<string>(parent.state);
                    foreach (string effect in action.GetAfterEffects())
                    {
                        if (!currentState.Contains(effect))
                        {
                            currentState.Add(effect);
                        }

                        ActionNode node = new ActionNode(parent, parent.cost + action.GetCost(), action, currentState);

                        if (IsGoalAchieved(goal, currentState))
                        {
                            actionNodes.Add(node);
                            foundPath = true;
                        }
                        else
                        {
                            List<Action> actionSubset = GetActionSubset(actions, action);
                            foundPath = BuildGraph(node, actionNodes, actionSubset, goal);
                        }
                    }
                }
            }
            return foundPath;
        }

        private ActionNode FindCheapest(List<ActionNode> actionNode)
        {
            ActionNode cheapest = null;
            foreach (ActionNode node in actionNode)
            {
                if (cheapest == null || node.cost < cheapest.cost)
                {
                    cheapest = node;
                }
            }
            return cheapest;
        }

        private List<Action> OrderActions(ActionNode cheapest)
        {
            List<Action> actionList = new List<Action>();
            ActionNode actionNode = cheapest;
            while (actionNode != null)
            {
                if (actionNode.action != null)
                {
                    actionList.Insert(0, actionNode.action);
                }
                actionNode = actionNode.parent;
            }
            return actionList;
        }

        private Queue<Action> BuildActionQueue(List<Action> actionList)
        {
            Queue<Action> actionQueue = new Queue<Action>();
            foreach (Action action in actionList)
            {
                actionQueue.Enqueue(action);
            }
            return actionQueue;
        }

        private bool IsGoalAchieved(Goal goal, List<string> state)
        {
            if (!state.Contains(goal.goal))
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