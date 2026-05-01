using Fusion;
using UnityEngine;

public class FirstPersonCamera : NetworkBehaviour
{
    public PlayerController target;

    private void LateUpdate()
    {
        if (target == null) return;

        Transform t_target = target.camAnchor.transform;

        transform.position = t_target.position;
        transform.rotation = Quaternion.Euler(-target.LookAngles.y, target.LookAngles.x, 0);
    }
}