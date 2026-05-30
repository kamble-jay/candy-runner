using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    public float     moveSpeed  = 2f;
    public Transform startPoint;
    public Transform endPoint;

    private void Update()
    {
        transform.position += Vector3.left * (moveSpeed * Time.deltaTime);

        if (transform.position.x <= endPoint.position.x)
        {
            var pos = transform.position;
            pos.x = startPoint.position.x;
            transform.position = pos;
        }
    }
}
