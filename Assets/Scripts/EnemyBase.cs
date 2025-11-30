using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 50;
    public float attackRange = 8f;
    public float fireCooldown = 1.25f;
    public GameObject bulletPrefab;
    public float bulletSpeed = 12f;
    public float bulletLifetime = 3f;
    public int bulletDamage = 10;

    [Header("ID")]
    public string enemyID;

    protected Transform playerTarget;
    public float currentHP; // Public for health display
    protected float fireCooldownTimer;
    private static int _nextEnemyID = 1;

    protected virtual void Awake()
    {
        currentHP = maxHealth;
        var playerController = FindObjectOfType<PlayerController>();
        playerTarget = playerController != null ? playerController.transform : null;

        // Generate unique enemy ID
        if (string.IsNullOrEmpty(enemyID))
        {
            enemyID = $"E{_nextEnemyID++}";
        }

        // Ensure we have a Rigidbody for collision detection
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = 1f;
        rb.drag = 5f; // Helps prevent pushing

        // Ensure collider exists and is set up for collision
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false; // Ensure physical collisions
        }
    }

    protected virtual void Update()
    {
        if (playerTarget == null) return;

        fireCooldownTimer -= Time.deltaTime;
        HandleBehaviour();
    }

    protected abstract void HandleBehaviour();

    protected void TryFire()
    {
        if (fireCooldownTimer > 0f) return;
        if (playerTarget == null) return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        if (toPlayer.sqrMagnitude > attackRange * attackRange) return;

        Vector3 spawnPos = transform.position + transform.forward * 0.8f + Vector3.up * 0.6f;
        GameObject projectile;
        if (bulletPrefab != null)
        {
            projectile = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(toPlayer.normalized));
        }
        else
        {
            projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.position = spawnPos;
            projectile.transform.localScale = Vector3.one * 0.2f;
        }

        Bullet bullet = projectile.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = projectile.AddComponent<Bullet>();
        }
        bullet.damage = bulletDamage;
        bullet.lifeTime = bulletLifetime;
        bullet.team = BulletTeam.Enemy;
        bullet.owner = gameObject;

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.velocity = toPlayer.normalized * bulletSpeed;

        fireCooldownTimer = fireCooldown;
    }

    public virtual void TakeDamage(int dmg)
    {
        currentHP -= dmg;
        if (currentHP <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
