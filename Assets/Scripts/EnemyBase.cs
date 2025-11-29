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

    protected Transform playerTarget;
    protected float currentHP;
    protected float fireCooldownTimer;

    protected virtual void Awake()
    {
        currentHP = maxHealth;
        var playerController = FindObjectOfType<PlayerController>();
        playerTarget = playerController != null ? playerController.transform : null;
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
