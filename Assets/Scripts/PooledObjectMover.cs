using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PooledObjectMover : MonoBehaviour
{
    private float _speed;
    private bool _movingLeft;
    private string _poolEntryName;

    private Camera _cam;
    private Renderer _renderer;
    private bool _initialized;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Initialize(float speed, bool spawnedOnRight, string poolEntryName)
    {
        _speed = speed;
        _movingLeft = spawnedOnRight;
        _poolEntryName = poolEntryName;
        _cam = Camera.main;
        _initialized = true;
    }

    private void OnEnable()
    {
        _initialized = false;
    }

    private void Update()
    {
        if (!_initialized || _cam == null)
            return;

        float direction = _movingLeft ? -1f : 1f;
        transform.Translate(direction * _speed * Time.deltaTime * Vector3.right, Space.World);

        if (IsOutOfCamera())
            ReturnToPool();
    }

    private bool IsOutOfCamera()
    {
        var bounds = _renderer.bounds;
        var minViewport = _cam.WorldToViewportPoint(bounds.min);
        var maxViewport = _cam.WorldToViewportPoint(bounds.max);

        return _movingLeft ? maxViewport.x < 0f : minViewport.x > 1f;
    }

    private void ReturnToPool()
    {
        _initialized = false;

        if (ObjectPoolManager.instancePool != null)
            ObjectPoolManager.instancePool.ReturnToPool(_poolEntryName, gameObject);
        else
            gameObject.SetActive(false);
    }

    public void UpdateSpeed(float newSpeed)
    {
        _speed = newSpeed;
    }
}
