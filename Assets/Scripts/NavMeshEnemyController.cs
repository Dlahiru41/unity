using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshEnemyController : EnemyFSM
{
    [Header("NavMesh Settings")]
    public float moveSpeed = 3.5f;
    public float strafeSpeed = 2.5f;
    public float retreatSpeed = 4.5f;
    
    [Header("Patrol Settings")]
    public float patrolRadius = 15f;
    public float patrolWaitTime = 2f;
    
    [Header("Combat Settings")]
    public float strafeDistance = 5f;
    public float optimalCombatRange = 8f;
    
    private NavMeshAgent _agent;
    private Vector3 _patrolTarget;
    private Vector3 _coverPosition;
    private Vector3 _strafeTarget;
    private float _stateActionTimer;
    private int _strafeDirection = 1;
    
    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.angularSpeed = 360f;
        _agent.acceleration = 8f;
        _agent.stoppingDistance = 1f;
        _agent.autoBraking = true;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        
        // Initialize as aggressive enemy
        aggressiveness = 0.8f;
        cautiousness = 0.4f;
        teamworkTendency = 0.5f;
        
        _patrolTarget = transform.position;
    }

    protected override void OnStateEnter(State state)
    {
        base.OnStateEnter(state);
        _stateActionTimer = 0f;
        
        switch (state)
        {
            case State.Patrol:
                _agent.speed = moveSpeed * 0.7f;
                SetNewPatrolPoint();
                break;
                
            case State.Chase:
                _agent.speed = moveSpeed;
                _agent.stoppingDistance = optimalCombatRange * 0.8f;
                break;
                
            case State.Strafe:
                _agent.speed = strafeSpeed;
                _strafeDirection = Random.value > 0.5f ? 1 : -1;
                break;
                
            case State.Retreat:
                _agent.speed = retreatSpeed;
                _agent.stoppingDistance = 0.5f;
                break;
                
            case State.TakeCover:
                _agent.speed = retreatSpeed;
                _coverPosition = FindCoverPosition();
                _agent.SetDestination(_coverPosition);
                break;
                
            case State.Flank:
                _agent.speed = moveSpeed * 1.1f;
                _agent.SetDestination(CalculateFlankPosition());
                break;
                
            case State.Ambush:
                _agent.speed = moveSpeed * 0.5f;
                _agent.isStopped = true;
                break;
        }
    }

    protected override void ExecuteState(State state)
    {
        _stateActionTimer += Time.deltaTime;
        
        switch (state)
        {
            case State.Idle:
                ExecuteIdle();
                break;
            case State.Patrol:
                ExecutePatrol();
                break;
            case State.Seek:
                ExecuteSeek();
                break;
            case State.Chase:
                ExecuteChase();
                break;
            case State.Strafe:
                ExecuteStrafe();
                break;
            case State.Retreat:
                ExecuteRetreat();
                break;
            case State.TakeCover:
                ExecuteTakeCover();
                break;
            case State.Ambush:
                ExecuteAmbush();
                break;
            case State.Flank:
                ExecuteFlank();
                break;
            case State.Dead:
                _agent.isStopped = true;
                break;
        }
        
        UpdateRotation();
    }

    private void ExecuteIdle()
    {
        _agent.isStopped = true;
        if (_stateActionTimer > 2f)
        {
            TransitionToState(State.Patrol);
        }
    }

    private void ExecutePatrol()
    {
        _agent.isStopped = false;
        
        if (!_agent.pathPending && _agent.remainingDistance < 1f)
        {
            if (_stateActionTimer > patrolWaitTime)
            {
                SetNewPatrolPoint();
                _stateActionTimer = 0f;
            }
        }
        else
        {
            _agent.SetDestination(_patrolTarget);
        }
    }

    private void ExecuteSeek()
    {
        _agent.isStopped = false;
        _agent.SetDestination(lastKnownTargetPos);
        
        if (hasLineOfSight && target != null)
        {
            TransitionToState(State.Chase);
        }
    }

    private void ExecuteChase()
    {
        if (target == null) return;
        
        _agent.isStopped = false;
        float distance = Vector3.Distance(transform.position, target.position);
        
        if (distance > optimalCombatRange * 1.2f)
        {
            _agent.SetDestination(target.position);
        }
        else if (distance < optimalCombatRange * 0.8f)
        {
            // Too close, back up slightly
            Vector3 retreatDir = (transform.position - target.position).normalized;
            _agent.SetDestination(transform.position + retreatDir * 2f);
        }
        else
        {
            // At optimal range, switch to strafe
            if (_stateActionTimer > 1.5f && Random.value > 0.6f)
            {
                TransitionToState(State.Strafe);
            }
        }
        
        TryFire();
    }

    private void ExecuteStrafe()
    {
        if (target == null) return;
        
        _agent.isStopped = false;
        
        // Strafe perpendicular to target
        Vector3 toTarget = (target.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toTarget);
        _strafeTarget = transform.position + right * _strafeDirection * strafeDistance;
        
        _agent.SetDestination(_strafeTarget);
        
        // Change strafe direction periodically
        if (_stateActionTimer > 2f)
        {
            _strafeDirection *= -1;
            _stateActionTimer = 0f;
        }
        
        TryFire();
    }

    private void ExecuteRetreat()
    {
        if (target == null) return;
        
        _agent.isStopped = false;
        Vector3 retreatDir = (transform.position - target.position).normalized;
        Vector3 retreatPos = transform.position + retreatDir * 10f;
        _agent.SetDestination(retreatPos);
        
        // Occasionally fire while retreating
        if (Random.value > 0.7f)
        {
            TryFire();
        }
    }

    private void ExecuteTakeCover()
    {
        _agent.isStopped = false;
        
        if (!_agent.pathPending && _agent.remainingDistance < 1.5f)
        {
            // Reached cover, stay hidden
            _agent.isStopped = true;
            
            // Peek and shoot occasionally
            if (_stateActionTimer > 1f && Random.value > 0.5f)
            {
                TryFire();
            }
        }
        else
        {
            _agent.SetDestination(_coverPosition);
        }
    }

    private void ExecuteAmbush()
    {
        _agent.isStopped = true;
        
        // Wait for target to get closer
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance < attackRange * 0.7f && hasLineOfSight)
            {
                // Spring the ambush!
                TryFire();
                if (_stateActionTimer > 0.5f)
                {
                    TransitionToState(State.Chase);
                }
            }
        }
    }

    private void ExecuteFlank()
    {
        _agent.isStopped = false;
        
        if (!_agent.pathPending && _agent.remainingDistance < 2f)
        {
            // Reached flank position, attack!
            TransitionToState(State.Chase);
        }
        
        // Fire if opportunity presents
        if (hasLineOfSight && Random.value > 0.8f)
        {
            TryFire();
        }
    }

    private void SetNewPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            _patrolTarget = hit.position;
        }
    }

    private void UpdateRotation()
    {
        if (target != null && (currentState == State.Chase || currentState == State.Strafe || currentState == State.Retreat))
        {
            Vector3 toTarget = (target.position - transform.position);
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
        else if (_agent.velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized, Vector3.up);
        }
    }
}
