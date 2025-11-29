using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Shooting")]
    public GameObject bulletPrefab; // if null, will spawn a default sphere bullet
    public float shootCooldown = 0.25f;
    public float bulletSpeed = 20f;
    public float bulletLifetime = 4f;
    public int bulletDamage = 10;

    // internals
    private Rigidbody _rb;
    private float _shootTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // prefer kinematic physics for direct control
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (currentHealth <= 0)
            currentHealth = maxHealth;
    }

    private void Update()
    {
        HandleShooting();
        _shootTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // supports WASD, arrows and diagonal via Combined axes
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0f, v).normalized * (moveSpeed * Time.fixedDeltaTime);
        if (move.sqrMagnitude > 0f)
        {
            Vector3 newPos = _rb.position + move;
            _rb.MovePosition(newPos);

            // face movement direction
            Vector3 lookDir = new Vector3(h, 0f, v);
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 0.2f));
            }
        }
    }

    private void HandleShooting()
    {
        // shoot with left mouse button or space
        if ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) && _shootTimer <= 0f)
        {
            Shoot();
            _shootTimer = shootCooldown;
        }
    }

    private void Shoot()
    {
        Vector3 spawnPos = transform.position + transform.forward * 1f + Vector3.up * 0.5f;

        GameObject bullet;

        if (bulletPrefab != null)
        {
            bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // create simple sphere bullet
            bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.transform.position = spawnPos;
            bullet.transform.localScale = Vector3.one * 0.25f;
            var rend = bullet.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = Color.yellow;
                rend.sharedMaterial = mat;
            }
            // add collider and rigidbody if not present
            if (bullet.GetComponent<Collider>() == null)
                bullet.AddComponent<SphereCollider>();
        }

        // add Bullet script
        Bullet b = bullet.GetComponent<Bullet>();
        if (b == null)
            b = bullet.AddComponent<Bullet>();

        b.damage = bulletDamage;
        b.lifeTime = bulletLifetime;
        b.team = BulletTeam.Player;
        b.owner = gameObject;

        Rigidbody br = bullet.GetComponent<Rigidbody>();
        if (br == null)
            br = bullet.AddComponent<Rigidbody>();
        br.useGravity = false;
        br.velocity = transform.forward * bulletSpeed;

        // ignore collision with shooter
        Collider playerCol = GetComponent<Collider>();
        Collider bulletCol = bullet.GetComponent<Collider>();
        if (playerCol != null && bulletCol != null)
            Physics.IgnoreCollision(playerCol, bulletCol);
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // simple death: destroy gameobject
        Destroy(gameObject);
    }

    [ContextMenu("Create Player Object (Cube)")]
    private void CreatePlayerObject()
    {
        // will create a cube with a colored front face marker and attach this script
        GameObject existing = GameObject.Find("Player");
        if (existing != null)
        {
            Debug.LogWarning("Player GameObject already exists in scene.");
            return;
        }

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cube);
        player.name = "Player";
        player.transform.position = Vector3.up * 0.5f;
        player.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);

        // replace default material color
        var rend = player.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.cyan;
            rend.sharedMaterial = mat;
        }

        // add rigidbody and collider adjustments
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        BoxCollider col = player.GetComponent<BoxCollider>();
        if (col == null)
            player.AddComponent<BoxCollider>();

        // add a small thin cube in front to mark 'front face' (use cube instead of Quad to avoid MeshCollider issues)
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "FrontMarker";
        marker.transform.SetParent(player.transform, false);
        marker.transform.localScale = new Vector3(0.6f, 0.9f, 0.05f);
        // position the marker slightly in front of the cube
        marker.transform.localPosition = new Vector3(0f, 0f, 0.51f);
        marker.transform.localRotation = Quaternion.identity;
        var mr = marker.GetComponent<Renderer>();
        if (mr != null)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = Color.red;
            mr.sharedMaterial = m;
        }

        // keep the BoxCollider on the marker — BoxCollider is convex and safe with non-kinematic Rigidbody

        // attach this PlayerController (copy component values)
        PlayerController pc = player.AddComponent<PlayerController>();
        pc.moveSpeed = moveSpeed;
        pc.maxHealth = maxHealth;
        pc.currentHealth = currentHealth > 0 ? currentHealth : maxHealth;
        pc.shootCooldown = shootCooldown;
        pc.bulletSpeed = bulletSpeed;
        pc.bulletLifetime = bulletLifetime;
        pc.bulletDamage = bulletDamage;

        // focus selection in editor (editor-only)
#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = player;
#endif
    }
}
