using UnityEngine;

/// <summary>Smoothly follows a target and keeps the view inside the map.</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float orthographicSize = 7f;
    public float smoothTime = 0.12f;
    public Vector2 mapSize = new Vector2(40f, 24f);

    Camera cam;
    Vector3 velocity;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
        if (target != null) transform.position = Clamp(target.position);
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, Clamp(target.position), ref velocity, smoothTime);
    }

    Vector3 Clamp(Vector3 pos)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        // If the view is bigger than the map on an axis, just center on the map
        float x = halfW * 2f >= mapSize.x ? mapSize.x * 0.5f : Mathf.Clamp(pos.x, halfW, mapSize.x - halfW);
        float y = halfH * 2f >= mapSize.y ? mapSize.y * 0.5f : Mathf.Clamp(pos.y, halfH, mapSize.y - halfH);
        return new Vector3(x, y, -10f);
    }
}
