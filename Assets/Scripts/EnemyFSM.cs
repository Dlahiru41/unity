using UnityEngine;

/// <summary>
/// Advanced Finite State Machine for enemy AI behavior.
/// Supports hierarchical states, transitions, and probability-based decisions.
/// </summary>
public abstract class EnemyFSM : EnemyBase
{
    public enum State
    {
        Idle,
        Patrol,
        Seek,
        Chase,
        Strafe,
        Retreat,
        TakeCover,
        Ambush,
        Flank,
        Dead
    }

    [Header("FSM State")]
    public State currentState = State.Idle;
    public State previousState = State.Idle;
    
    [Header("State Weights & Probabilities")]
    [Range(0f, 1f)] public float aggressiveness = 0.7f;
    [Range(0f, 1f)] public float cautiousness = 0.5f;
    [Range(0f, 1f)] public float teamworkTendency = 0.3f;
    
    [Header("State Thresholds")]
    public float retreatHealthPercent = 0.3f;
    public float aggressiveHealthPercent = 0.7f;
    public float coverSearchRadius = 10f;
    
    [Header("Timing")]
    public float stateEvaluationInterval = 0.5f;
    public float minStateTime = 1f;
    
    protected Transform target;
    protected float stateTimer;
    protected float evaluationTimer;
    protected Vector3 lastKnownTargetPos;
    protected bool hasLineOfSight;
    
    // State probabilities (updated each evaluation)
    protected float[] stateProbabilities;
    
    protected override void Awake()
    {
        base.Awake(); // Call EnemyBase.Awake()
        stateProbabilities = new float[System.Enum.GetValues(typeof(State)).Length];
    }

    protected override void Update()
    {
        base.Update(); // Call EnemyBase.Update() which handles fireCooldownTimer
        
        if (currentHP <= 0)
        {
            TransitionToState(State.Dead);
            return;
        }

        stateTimer += Time.deltaTime;
        evaluationTimer += Time.deltaTime;

        // Periodic state evaluation based on probability
        if (evaluationTimer >= stateEvaluationInterval)
        {
            EvaluateStateTransition();
            evaluationTimer = 0f;
        }

        // Execute current state behavior (replaces HandleBehaviour)
        ExecuteState(currentState);
    }

    protected override void HandleBehaviour()
    {
        // Not used in FSM - ExecuteState is called from Update instead
    }

    protected virtual void EvaluateStateTransition()
    {
        if (stateTimer < minStateTime)
            return;

        UpdateContext();
        CalculateStateProbabilities();
        
        State bestState = SelectBestState();
        if (bestState != currentState && CanTransition(currentState, bestState))
        {
            TransitionToState(bestState);
        }
    }

    protected virtual void UpdateContext()
    {
        // Update situational awareness
        if (target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            
            hasLineOfSight = !Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                dirToTarget,
                distance,
                LayerMask.GetMask("Default")
            );

            if (hasLineOfSight)
            {
                lastKnownTargetPos = target.position;
            }
        }
    }

    protected virtual void CalculateStateProbabilities()
    {
        if (target == null)
        {
            // No target - idle or patrol
            SetProbability(State.Idle, 0.3f);
            SetProbability(State.Patrol, 0.7f);
            return;
        }

        float healthPercent = currentHP / maxHealth;
        float distance = Vector3.Distance(transform.position, target.position);
        float normalizedDistance = Mathf.Clamp01(distance / attackRange);

        // Base probabilities on health
        if (healthPercent < retreatHealthPercent)
        {
            // Low health - defensive behavior
            SetProbability(State.Retreat, 0.4f * cautiousness);
            SetProbability(State.TakeCover, 0.3f * cautiousness);
            SetProbability(State.Strafe, 0.2f);
            SetProbability(State.Chase, 0.1f * (1f - cautiousness));
        }
        else if (healthPercent > aggressiveHealthPercent)
        {
            // High health - aggressive behavior
            SetProbability(State.Chase, 0.4f * aggressiveness);
            SetProbability(State.Flank, 0.2f * aggressiveness * teamworkTendency);
            SetProbability(State.Strafe, 0.2f);
            SetProbability(State.Ambush, 0.1f * (1f - aggressiveness));
            SetProbability(State.TakeCover, 0.1f * cautiousness);
        }
        else
        {
            // Medium health - balanced behavior
            SetProbability(State.Chase, 0.3f * aggressiveness);
            SetProbability(State.Strafe, 0.3f);
            SetProbability(State.TakeCover, 0.2f * cautiousness);
            SetProbability(State.Retreat, 0.1f * cautiousness);
            SetProbability(State.Flank, 0.1f * teamworkTendency);
        }

        // Distance modifiers
        if (distance < attackRange * 0.5f)
        {
            // Too close - increase retreat/strafe
            ModifyProbability(State.Retreat, 1.5f);
            ModifyProbability(State.Strafe, 1.3f);
            ModifyProbability(State.Chase, 0.5f);
        }
        else if (distance > attackRange * 1.5f)
        {
            // Too far - increase chase/seek
            ModifyProbability(State.Chase, 1.5f);
            ModifyProbability(State.Seek, 1.2f);
            ModifyProbability(State.Strafe, 0.7f);
        }

        // Line of sight modifiers
        if (!hasLineOfSight)
        {
            ModifyProbability(State.Seek, 2.0f);
            ModifyProbability(State.Ambush, 1.5f);
            ModifyProbability(State.Strafe, 0.3f);
        }

        // Normalize probabilities
        NormalizeProbabilities();
    }

    protected void SetProbability(State state, float value)
    {
        stateProbabilities[(int)state] = Mathf.Clamp01(value);
    }

    protected void ModifyProbability(State state, float multiplier)
    {
        stateProbabilities[(int)state] *= multiplier;
    }

    protected void NormalizeProbabilities()
    {
        float sum = 0f;
        foreach (float prob in stateProbabilities)
            sum += prob;

        if (sum > 0f)
        {
            for (int i = 0; i < stateProbabilities.Length; i++)
                stateProbabilities[i] /= sum;
        }
    }

    protected virtual State SelectBestState()
    {
        // Weighted random selection based on probabilities
        float random = Random.value;
        float cumulative = 0f;

        for (int i = 0; i < stateProbabilities.Length; i++)
        {
            cumulative += stateProbabilities[i];
            if (random <= cumulative)
                return (State)i;
        }

        return currentState;
    }

    protected virtual bool CanTransition(State from, State to)
    {
        // Define valid state transitions (Hierarchical FSM rules)
        switch (from)
        {
            case State.Dead:
                return false; // No transitions from dead

            case State.Idle:
                return true; // Can transition to any state

            case State.Patrol:
                return to != State.Ambush; // Can't ambush while patrolling openly

            case State.Chase:
                return to != State.Idle && to != State.Patrol;

            case State.Retreat:
                return to != State.Chase && to != State.Flank; // Can't attack while retreating

            case State.TakeCover:
                return to != State.Chase || stateTimer > 2f; // Must stay in cover briefly

            default:
                return true;
        }
    }

    protected virtual void TransitionToState(State newState)
    {
        if (newState == currentState)
            return;

        OnStateExit(currentState);
        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;
        OnStateEnter(newState);
    }

    protected virtual void OnStateEnter(State state)
    {
        // Override in derived classes for state-specific initialization
    }

    protected virtual void OnStateExit(State state)
    {
        // Override in derived classes for state-specific cleanup
    }

    protected abstract void ExecuteState(State state);

    // Utility methods for derived classes
    protected Vector3 FindCoverPosition()
    {
        Vector3 bestCover = transform.position;
        float bestScore = float.MinValue;

        Collider[] obstacles = Physics.OverlapSphere(transform.position, coverSearchRadius);
        
        foreach (Collider obstacle in obstacles)
        {
            if (obstacle.gameObject == gameObject)
                continue;

            Vector3 coverPos = obstacle.transform.position;
            Vector3 dirFromTarget = (coverPos - target.position).normalized;
            Vector3 potentialCover = coverPos + dirFromTarget * 2f;

            // Score based on distance to enemy and coverage from target
            float distToTarget = Vector3.Distance(potentialCover, target.position);
            float distToSelf = Vector3.Distance(potentialCover, transform.position);
            float score = distToTarget / (distToSelf + 1f);

            if (score > bestScore)
            {
                bestScore = score;
                bestCover = potentialCover;
            }
        }

        return bestCover;
    }

    protected Vector3 CalculateFlankPosition()
    {
        if (target == null)
            return transform.position;

        Vector3 toTarget = (target.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toTarget);
        
        // Randomly flank left or right
        float direction = Random.value > 0.5f ? 1f : -1f;
        Vector3 flankOffset = right * (direction * attackRange * 0.7f);
        
        return target.position + flankOffset;
    }

    protected void DrawStateGizmo()
    {
        // Visual debugging for current state
        Color stateColor = GetStateColor(currentState);
        Gizmos.color = stateColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
    }

    protected Color GetStateColor(State state)
    {
        switch (state)
        {
            case State.Idle: return Color.gray;
            case State.Patrol: return Color.blue;
            case State.Seek: return Color.cyan;
            case State.Chase: return Color.red;
            case State.Strafe: return Color.yellow;
            case State.Retreat: return Color.magenta;
            case State.TakeCover: return Color.green;
            case State.Ambush: return new Color(0.5f, 0f, 0.5f);
            case State.Flank: return new Color(1f, 0.5f, 0f);
            case State.Dead: return Color.black;
            default: return Color.white;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        DrawStateGizmo();
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // Helper method for FSM states to fire - calls base class TryFire
    protected new void TryFire()
    {
        base.TryFirePublic();
    }
}
