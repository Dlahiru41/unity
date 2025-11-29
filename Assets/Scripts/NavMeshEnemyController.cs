using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshEnemyController : EnemyBase
{
    public float rePathInterval = 0.25f;

    private NavMeshAgent _agent;
    private float _pathTimer;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.angularSpeed = 360f;
        _agent.stoppingDistance = attackRange * 0.6f;
        _agent.autoBraking = true;
    }

    public float moveSpeed = 3.5f;

    protected override void HandleBehaviour()
    {
        if (playerTarget == null) return;

        _pathTimer -= Time.deltaTime;
        if (_pathTimer <= 0f)
        {
            _agent.SetDestination(playerTarget.position);
            _pathTimer = rePathInterval;
        }

        if (_agent.velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized, Vector3.up);
        }
        else
        {
            Vector3 toPlayer = (playerTarget.position - transform.position);
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            }
        }

        TryFire();
    }
}
