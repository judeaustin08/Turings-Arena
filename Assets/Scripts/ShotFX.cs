using Fusion;
using System.Collections;
using UnityEngine;

public class ShotFX : NetworkBehaviour
{
    public float startingWidth;
    public float lifetime;

    public IEnumerator StartFX(Vector3 start, Vector3 end)
    {
        LineRenderer lr = GetComponent<LineRenderer>();

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lr.endWidth = startingWidth;

        for (float t = 0; t < lifetime; t += Time.deltaTime)
        {
            lr.startWidth = lr.endWidth = Mathf.Lerp(startingWidth, 0, t / lifetime);
            yield return null;
        }

        Runner.Despawn(Object);
    }
}
