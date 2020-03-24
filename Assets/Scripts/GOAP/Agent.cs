using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AI.GOAP
{
    [SelectionBase]
    [RequireComponent(typeof(Rigidbody))]
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

        List<Effects> states = new List<Effects>();
        Goal currentGoal;
        Queue<Action> actionQueue;
        Action currentAction;
        Vector3 currentDestination;
        GameObject targetObject;
        bool isDoingAction = false;
        bool isSearching = false;

        public delegate void DoingActionDelegate(GameObject target, List<Effects> afterEffects);
        public event DoingActionDelegate onDoingAction;

        [System.Serializable]
        private class Goal
        {
            public Effects goal = default;
            public int priority = 1;
            public bool removable = false;
        }

        private class ActionNode
        {
            public ActionNode parent = null;
            public float cost = 0;
            public Action action = null;
            public List<Effects> state = null;

            public ActionNode(ActionNode parent, float cost, Action action, List<Effects> states)
            {
                this.parent = parent;
                this.cost = cost;
                this.action = action;
                this.state = new List<Effects>(states);
            }
        }

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            senseSphere = GetComponent<SphereCollider>();
        }

        private void Start()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            senseSphere.isTrigger = true;
            senseSphere.radius = senseRadius;
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

            if (!isDoingAction && actionQueue != null && actionQueue.Count > 0)
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
            if (other.gameObject.tag == currentAction.GetTargetTag())
            {
                Vector3 dest = other.ClosestPoint(transform.position);
                if (CanMoveTo(dest))
                {
                    targetObject = other.gameObject;
                    MoveTo(dest);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, senseRadius);

            if (targetObject != null)
            {
                Gizmos.DrawLine(transform.position, currentDestination);
            }
        }

        private bool CheckPreconditions()
        {
            List<Effects> preconditions = currentAction.GetPreconditions();
            foreach (Effects precondition in preconditions)
            {
                if (!states.Contains(precondition))
                {
                    return false;
                }
            }
            return true;
        }

        private void SetActionQueue()
        {
            goals.Sort(SortGoals);
            foreach (Goal goal in goals)
            {
                actionQueue = PlanActions(actions, states, goal);
                if (actionQueue != null)
                {
                    currentGoal = goal;
                    break;
                }
            }
        }

        private int SortGoals(Goal goal1, Goal goal2)
        {
            return goal1.priority.CompareTo(goal2.priority);
        }

        private void SetCurrentAction()
        {
            isDoingAction = true;
            currentAction = actionQueue.Dequeue();
        }

        private void SearchBehaviour()
        {
            Vector3 randomDirection = Random.insideUnitSphere * searchDistance;
            randomDirection += transform.position;
            NavMeshHit navHit;
            NavMesh.SamplePosition (randomDirection, out navHit, searchDistance, -1);

            if (CanMoveTo(navHit.position))
            {
                isSearching = true;
                MoveTo(navHit.position);
            }
        }

        private bool CanMoveTo(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            bool hasPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
            if (!hasPath) { return false; }
            if (path.status != NavMeshPathStatus.PathComplete) { return false; }
            return true;
        }

        private void MoveTo(Vector3 newDestination)
        {
            currentDestination = newDestination;
            navMeshAgent.destination = currentDestination;
        }

        private void CheckIfCloseToAction()
        {
            float distanceToTarget = Vector3.Distance(currentDestination, transform.position);
            if (distanceToTarget < withinTargetRange)
            {
                if (targetObject != null) {
                    StartCoroutine(DoAction());
                }
                else
                {
                    isSearching = false;
                }
            }
        }

        private IEnumerator DoAction()
        {
            navMeshAgent.isStopped = true;
            onDoingAction(targetObject, currentAction.GetAfterEffects());
            targetObject = null;
            yield return new WaitForSeconds(currentAction.GetDuration());
            CompleteAction();
        }

        private void CompleteAction()
        {
            if (currentAction == null) { return; }

            foreach (Effects effect in currentAction.GetPreconditions())
            {
                RemoveFromState(effect);
            }
            foreach (Effects effect in currentAction.GetAfterEffects())
            {
                AddToState(effect);
            }

            currentAction = null;
            isDoingAction = false;
            isSearching = false;
            navMeshAgent.isStopped = false;
        }

        private Queue<Action> PlanActions(List<Action> actions, List<Effects> states, Goal goal)
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

        private bool BuildGraph(ActionNode parent, List<ActionNode> actionNodes, List<Action> actions, Goal goal)
        {
            bool foundPath = false;
            foreach (Action action in actions)
            {
                if (action.IsAchievable(parent.state))
                {
                    List<Effects> currentState = new List<Effects>(parent.state);
                    foreach (Effects effect in action.GetAfterEffects())
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

        private bool IsGoalAchieved(Goal goal, List<Effects> state)
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

        public void AddToState(Effects effectToAdd)
        {
            if (!states.Contains(effectToAdd))
            {
                states.Add(effectToAdd);
            }
        }

        public void RemoveFromState(Effects effectToRemove)
        {
            if (states.Contains(effectToRemove))
            {
                states.Remove(effectToRemove);
            }
        }

        public Action GetCurrentAction()
        {
            return currentAction;
        }
    }
}