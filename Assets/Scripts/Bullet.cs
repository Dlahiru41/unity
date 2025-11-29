using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public float lifeTime = 4f;

    private float _t = 0f;

    private void Update()
    {
        _t += Time.deltaTime;
        if (_t >= lifeTime)
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var pc = collision.collider.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(damage);
        }

        // destroy bullet on any collision
        Destroy(gameObject);
    }
}

