using UnityEngine;

public enum BulletTeam
{
    Player,
    Enemy
}

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public float lifeTime = 4f;
    public BulletTeam team = BulletTeam.Player;
    public GameObject owner;

    private float _t = 0f;

    private void Update()
    {
        _t += Time.deltaTime;
        if (_t >= lifeTime)
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (owner != null && collision.collider.gameObject == owner)
        {
            return;
        }

        if (team == BulletTeam.Player)
        {
            var enemy = collision.collider.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
        else
        {
            var pc = collision.collider.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
