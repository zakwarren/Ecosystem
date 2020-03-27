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
        [SerializeField] float timeCanBeStuck = 10f;
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
        Vector3 lastPosition;
        float timeSinceMoved = 0f;
        bool isPaused = false;

        public delegate void DoingActionDelegate(GameObject target, List<Effects> afterEffects);
        public event DoingActionDelegate onDoingAction;

        [System.Serializable]
        private class Goal
        {
            public Effects goal = default;
            public int priority = 1;
            public bool removable = false;

            public Goal(Effects newGoal, int newPriority, bool newRemovable)
            {
                goal = newGoal;
                priority = newPriority;
                removable = newRemovable;
            }
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
            if (isPaused) { return; }

            if (actionQueue == null && !isDoingAction)
            {
                SetActionQueue();
            }

            if (!isDoingAction && actionQueue != null && actionQueue.Count > 0)
            {
                SetCurrentAction();
            }

            if (isDoingAction && !isSearching && currentAction.GetHasTarget())
            {
                SearchBehaviour();
            }

            if (currentAction != null && isDoingAction && currentAction.GetHasTarget())
            {
                CheckIfCloseToAction();
            }

            if (currentAction != null && isDoingAction && !currentAction.GetHasTarget())
            {
                StartCoroutine(DoAction());
            }

            ResetIfStuck();
        }

        private void OnTriggerStay(Collider other)
        {
            if (currentAction == null) { return; }
            if (targetObject != null) { return; }

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

        private void ResetIfStuck()
        {
            float durationStuck = timeCanBeStuck;
            if (currentAction != null && currentAction.GetDuration() > timeCanBeStuck)
            {
                durationStuck += currentAction.GetDuration();
            }

            if (transform.position.z.ToString("F2") == lastPosition.z.ToString("F2"))
            {
                timeSinceMoved += Time.deltaTime;
            }
            else
            {
                timeSinceMoved = 0;
                lastPosition = transform.position;
            }

            if (currentAction == null && timeSinceMoved > durationStuck)
            {
                timeSinceMoved = 0f;
                CancelCurrentGoal();
                SetActionQueue();
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
            if (!currentAction.GetHasTarget()) { return; }

            if (currentAction.GetShouldKnowTarget())
            {
                targetObject = GameObject.FindWithTag(currentAction.GetTargetTag());
                if (targetObject == null) { return; }

                if (CanMoveTo(targetObject.transform.position))
                {
                    currentDestination = targetObject.transform.position;
                    isSearching = true;
                    MoveTo(currentDestination);
                }
            }
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
            List<Effects> afterEffects = currentAction.GetAfterEffects();
            float duration = currentAction.GetDuration();

            onDoingAction(targetObject, afterEffects);
            targetObject = null;
            yield return new WaitForSeconds(duration);
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

            if (currentGoal != null && states.Contains(currentGoal.goal) && currentGoal.removable)
            {
                goals.Remove(currentGoal);
            }
            if (actionQueue != null && actionQueue.Count == 0)
            {
                actionQueue = null;
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

        public bool CanMoveTo(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            bool hasPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
            if (!hasPath) { return false; }
            if (path.status != NavMeshPathStatus.PathComplete) { return false; }
            return true;
        }

        public void MoveTo(Vector3 newDestination)
        {
            currentDestination = newDestination;
            navMeshAgent.destination = currentDestination;
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

        public Effects GetCurrentGoal()
        {
            if (currentGoal == null) { return default; }
            return currentGoal.goal;
        }

        public void AddNewGoal(Effects goal, int priority, bool removable)
        {
            Goal newGoal = new Goal(goal, priority, removable);
            if (!goals.Contains(newGoal))
            {
                goals.Add(newGoal);
            }
        }

        public void CancelCurrentGoal()
        {
            currentGoal = null;
            currentAction = null;
            targetObject = null;
            isDoingAction = false;
            isSearching = false;
            navMeshAgent.isStopped = false;

            SearchBehaviour();
        }

        public void RemoveTarget()
        {
            targetObject = null;
        }

        public void PauseAgent(bool shouldPause)
        {
            isPaused = shouldPause;
            if (isPaused)
            {
                navMeshAgent.isStopped = true;
            }
            else
            {
                navMeshAgent.isStopped = false;
            }
        }
    }
}