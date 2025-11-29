using UnityEngine;

// Simple camera follower: positions the main camera relative to the Player and follows smoothly.
[ExecuteInEditMode]
public class CameraFollower : MonoBehaviour
{
    public Transform target; // if null, will search for GameObject named "Player" or tagged MainCamera's follow
    public Vector3 offset = new Vector3(0f, 15f, -15f);
    public float smoothTime = 0.12f;

    private Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        if (target == null)
        {
            var go = GameObject.Find("Player");
            if (go != null) target = go.transform;
        }

        if (target == null)
        {
            // try find any PlayerController in scene
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null)
                target = pc.transform;
        }

        // if still null, we won't do anything until a player exists
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        // smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, smoothTime);

        // look at the player with a small upward bias
        Vector3 lookPoint = target.position + Vector3.up * 1.0f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPoint - transform.position, Vector3.up), 0.2f);
    }
}

