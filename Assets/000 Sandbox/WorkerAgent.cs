using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(WorkerNeeds))]
public class WorkerAgent : MonoBehaviour {
    [Header("World Locations")]
    [SerializeField] Transform computer;
    [SerializeField] Transform printer;
    [SerializeField] Transform waterDispenser;
    [SerializeField] Transform bathroom;

    [Header("World Resources")]
    [SerializeField] PrinterStatus printerStatus;
    [SerializeField] BathroomStatus bathroomStatus;

    NavMeshAgent navMeshAgent;
    WorkerNeeds needs;
    IGoapPlanner planner;

    // GOAP state
    public Dictionary<string, AgentBelief> beliefs;
    public HashSet<AgentAction> actions;
    public HashSet<AgentGoal> goals;
    public ActionPlan actionPlan;
    public AgentAction currentAction;
    public AgentGoal currentGoal;
    AgentGoal lastGoal;

    // Local state flags
    bool hasDocument;
    bool documentPrinted;

    void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
        needs = GetComponent<WorkerNeeds>();
        planner = new GoapPlanner();

        SetupBeliefs();
        SetupActions();
        SetupGoals();

        if (printerStatus) printerStatus.OnStatusChanged += HandleAvailabilityChanged;
        if (bathroomStatus) bathroomStatus.OnStatusChanged += HandleAvailabilityChanged;
    }

    void OnDestroy() {
        if (printerStatus) printerStatus.OnStatusChanged -= HandleAvailabilityChanged;
        if (bathroomStatus) bathroomStatus.OnStatusChanged -= HandleAvailabilityChanged;
    }

    void Update() {
        // Plan selection
        if (currentAction == null) {
            CalculatePlan();

            if (actionPlan != null && actionPlan.Actions.Count > 0) {
                navMeshAgent.ResetPath();
                currentGoal = actionPlan.AgentGoal;
                currentAction = actionPlan.Actions.Pop();

                if (currentAction.Preconditions.All(b => b.Evaluate())) {
                    currentAction.Start();
                } else {
                    currentAction = null;
                    currentGoal = null;
                }
            }
        }

        // Action execution
        if (actionPlan != null && currentAction != null) {
            currentAction.Update(Time.deltaTime);

            if (currentAction.Complete) {
                currentAction.Stop();
                currentAction = null;

                if (actionPlan.Actions.Count == 0) {
                    lastGoal = currentGoal;
                    currentGoal = null;
                }
            }
        }
    }

    void CalculatePlan() {
        var priorityLevel = currentGoal?.Priority ?? 0;
        HashSet<AgentGoal> goalsToCheck = goals;

        if (currentGoal != null) {
            goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
        }

        var potentialPlan = planner.Plan(actions, goalsToCheck, lastGoal);
        if (potentialPlan != null) {
            actionPlan = potentialPlan;
        }
    }

    void HandleAvailabilityChanged() {
        if (currentAction != null) {
            currentAction.Stop();
        }
        currentAction = null;
        currentGoal = null;
        actionPlan = null;
        navMeshAgent.ResetPath();
    }

    void SetupBeliefs() {
        beliefs = new Dictionary<string, AgentBelief>();
        var factory = new BeliefFactory(this, beliefs);

        factory.AddBelief("Nothing", () => false);

        // Internal needs satisfied states (true when the need is handled)
        factory.AddBelief("IsThirsty", () => !needs.IsThirsty);
        factory.AddBelief("NeedsBathroom", () => !needs.NeedsBathroom);

        // Document state
        factory.AddBelief("HasDocument", () => hasDocument);
        factory.AddBelief("DocumentPrinted", () => documentPrinted);

        // Locations
        factory.AddLocationBelief("AtComputer", 2f, computer);
        factory.AddLocationBelief("AtPrinter", 2f, printer);
        factory.AddLocationBelief("AtWater", 2f, waterDispenser);
        factory.AddLocationBelief("AtBathroom", 2f, bathroom);

        // Availability
        factory.AddBelief("PrinterFree", () => printerStatus && printerStatus.IsFree);
        factory.AddBelief("BathroomFree", () => bathroomStatus && bathroomStatus.IsFree);
    }

    void SetupGoals() {
        goals = new HashSet<AgentGoal>();

        goals.Add(new AgentGoal.Builder("RelieveBathroom")
            .WithPriority(4)
            .WithDesiredEffect(beliefs["NeedsBathroom"])
            .Build());

        goals.Add(new AgentGoal.Builder("DrinkWater")
            .WithPriority(3)
            .WithDesiredEffect(beliefs["IsThirsty"])
            .Build());

        goals.Add(new AgentGoal.Builder("ProduceDocument")
            .WithPriority(2)
            .WithDesiredEffect(beliefs["HasDocument"])
            .Build());

        goals.Add(new AgentGoal.Builder("PrintDocument")
            .WithPriority(2)
            .WithDesiredEffect(beliefs["DocumentPrinted"])
            .Build());

        goals.Add(new AgentGoal.Builder("Idle")
            .WithPriority(1)
            .WithDesiredEffect(beliefs["Nothing"])
            .Build());
    }

    void SetupActions() {
        actions = new HashSet<AgentAction>();

        actions.Add(new AgentAction.Builder("GoToComputer")
            .WithStrategy(new MoveStrategy(navMeshAgent, () => computer.position))
            .AddEffect(beliefs["AtComputer"])
            .Build());

        actions.Add(new AgentAction.Builder("WriteDocument")
            .WithStrategy(new TimedStrategy(2f, () => { hasDocument = true; documentPrinted = false; }))
            .AddPrecondition(beliefs["AtComputer"])
            .AddEffect(beliefs["HasDocument"])
            .Build());

        actions.Add(new AgentAction.Builder("GoToPrinter")
            .WithStrategy(new MoveStrategy(navMeshAgent, () => printer.position))
            .AddEffect(beliefs["AtPrinter"])
            .Build());

        actions.Add(new AgentAction.Builder("Print")
            .WithStrategy(new ReservingTimedStrategy(
                2f,
                printerStatus,
                onComplete: () => { documentPrinted = true; },
                onCancel: () => { documentPrinted = false; }))
            .AddPrecondition(beliefs["AtPrinter"])
            .AddPrecondition(beliefs["HasDocument"])
            .AddPrecondition(beliefs["PrinterFree"])
            .AddEffect(beliefs["DocumentPrinted"])
            .Build());

        actions.Add(new AgentAction.Builder("GoToWater")
            .WithStrategy(new MoveStrategy(navMeshAgent, () => waterDispenser.position))
            .AddEffect(beliefs["AtWater"])
            .Build());

        actions.Add(new AgentAction.Builder("DrinkWater")
            .WithStrategy(new TimedStrategy(1f, needs.Quench))
            .AddPrecondition(beliefs["AtWater"])
            .AddEffect(beliefs["IsThirsty"])
            .Build());

        actions.Add(new AgentAction.Builder("GoToBathroom")
            .WithStrategy(new MoveStrategy(navMeshAgent, () => bathroom.position))
            .AddEffect(beliefs["AtBathroom"])
            .Build());

        actions.Add(new AgentAction.Builder("UseBathroom")
            .WithStrategy(new ReservingTimedStrategy(2f, bathroomStatus, needs.Relieve))
            .AddPrecondition(beliefs["AtBathroom"])
            .AddPrecondition(beliefs["BathroomFree"])
            .AddEffect(beliefs["NeedsBathroom"])
            .Build());

        // Fallback idle so Idle goal is achievable
        actions.Add(new AgentAction.Builder("Idle")
            .WithStrategy(new IdleStrategy(2f))
            .AddEffect(beliefs["Nothing"])
            .Build());
    }

    // --- Helper strategies -------------------------------------------------
    class TimedStrategy : IActionStrategy {
        readonly CountdownTimer timer;
        readonly Action onComplete;
        readonly Action onStart;
        bool complete;

        public bool CanPerform => true;
        public bool Complete => complete;

        public TimedStrategy(float duration, Action onComplete, Action onStart = null) {
            this.onComplete = onComplete;
            this.onStart = onStart;
            timer = new CountdownTimer(duration);
            timer.OnTimerStart += () => complete = false;
            timer.OnTimerStop += () => {
                complete = true;
                this.onComplete?.Invoke();
            };
        }

        public void Start() {
            onStart?.Invoke();
            timer.Start();
        }

        public void Update(float deltaTime) => timer.Tick(deltaTime);

        public void Stop() {
            if (!complete) onComplete?.Invoke();
            complete = true;
        }
    }

    class ReservingTimedStrategy : IActionStrategy {
        readonly CountdownTimer timer;
        readonly Action onComplete;
        readonly Action onCancel;
        readonly IReservable resource;
        bool complete;
        bool reserved;

        public bool CanPerform => resource == null || resource.IsFree;
        public bool Complete => complete;

        public ReservingTimedStrategy(float duration, IReservable resource, Action onComplete, Action onCancel = null) {
            this.resource = resource;
            this.onComplete = onComplete;
            this.onCancel = onCancel;
            timer = new CountdownTimer(duration);
            timer.OnTimerStart += () => complete = false;
            timer.OnTimerStop += () => {
                complete = true;
                this.onComplete?.Invoke();
                Release();
            };
        }

        public void Start() {
            Reserve();
            timer.Start();
        }

        public void Update(float deltaTime) => timer.Tick(deltaTime);

        public void Stop() {
            if (!complete) onCancel?.Invoke();
            Release();
            complete = true;
        }

        void Reserve() {
            if (resource != null && resource.IsFree) {
                resource.Reserve();
                reserved = true;
            }
        }

        void Release() {
            if (reserved && resource != null) {
                resource.Release();
                reserved = false;
            }
        }
    }
}
