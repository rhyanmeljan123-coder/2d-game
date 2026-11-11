using UnityEngine;

/// <summary>
/// Smoothly follows a target and keeps the view inside the current area.
/// Areas are the outdoor map(s) and any interior rooms; the camera uses whichever area the target is in.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [System.Serializable]
    public struct Area
    {
        public Rect bounds;
        public float orthographicSize;
        public Area(Rect bounds, float orthographicSize) { this.bounds = bounds; this.orthographicSize = orthographicSize; }
    }

    public Transform target;
    public float smoothTime = 0.12f;
    /// <summary>The first area is the default (outdoors).</summary>
    public Area[] areas = { new Area(new Rect(0, 0, 80, 24), 7f) };

    /// <summary>True while the camera is in an interior area (farmhouse, dungeon): no weather or day/night tint there.</summary>
    public static bool Indoors { get; private set; }

    Camera cam;
    Vector3 velocity;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        var area = CurrentArea();
        cam.orthographicSize = area.orthographicSize;
        if (target != null) transform.position = Clamp(target.position, area);
    }

    void LateUpdate()
    {
        if (target == null) return;
        var area = CurrentArea();
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, area.orthographicSize, 1f - Mathf.Exp(-12f * Time.deltaTime));

        var goal = Clamp(target.position, area);
        if ((transform.position - goal).sqrMagnitude > 400f) { transform.position = goal; velocity = Vector3.zero; } // after a door: snap
        else transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }

    Area CurrentArea()
    {
        Indoors = false;
        for (int i = areas.Length - 1; i >= 1; i--)
            if (areas[i].bounds.Contains(target != null ? (Vector2)target.position : Vector2.zero)) { Indoors = true; return areas[i]; }
        return areas[0];
    }

    Vector3 Clamp(Vector3 pos, Area area)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        var b = area.bounds;
        // If the view is bigger than the area on an axis, just center on the area
        float x = halfW * 2f >= b.width ? b.center.x : Mathf.Clamp(pos.x, b.xMin + halfW, b.xMax - halfW);
        float y = halfH * 2f >= b.height ? b.center.y : Mathf.Clamp(pos.y, b.yMin + halfH, b.yMax - halfH);
        return new Vector3(x, y, -10f);
    }
}
