using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Tooltip("Approximate time for the camera to refocus.")]
    public float dampTime = 0.2f;

    [Tooltip("Space between the top/bottom most target and the screen edge.")]
    public float screenEdgeBuffer = 4f;

    [Tooltip("The smallest orthographic size the camera can be.")]
    public float minSize = 6.5f;

    [Tooltip("All the targets the camera needs to encompass.")]
    public Transform[] targets;

    /// <summary>
    /// Used for referencing the camera.
    /// </summary>
    private Camera _camera;

    /// <summary>
    /// Reference speed for the smooth damping of the orthographic size.
    /// </summary>
    private float _zoomSpeed;

    /// <summary>
    /// Reference velocity for the smooth damping of the position.
    /// </summary>
    private Vector3 _moveVelocity;

    /// <summary>
    /// The position the camera is moving towards.
    /// </summary>
    private Vector3 _desiredPosition;

    /// <summary>
    /// The offset to apply to the position so the child camera aim at the desired point 
    /// </summary>
    private Vector3 _aimToRig;

    private void Awake()
    {
        _camera = GetComponentInChildren<Camera>();

        // Plane in which the camera rig is in.
        Plane p = new(Vector3.up, transform.position);
        Ray r = new(_camera.transform.position, _camera.transform.forward);
        p.Raycast(r, out float d);

        // This is where the camera aim on the rig plane
        Vector3 aimTarget = r.GetPoint(d);

        _aimToRig = transform.position - aimTarget;
    }


    private void FixedUpdate()
    {
        Move();
        Zoom();
    }


    private void Move()
    {
        FindAveragePosition();

        // Smoothly transition to the found position.
        transform.position = Vector3.SmoothDamp(transform.position, _desiredPosition + _aimToRig, ref _moveVelocity, dampTime);
    }


    private void FindAveragePosition()
    {
        Vector3 averagePosition = new();
        int numTargets = 0;

        // Go through all the targets and add their positions together.
        for (int i = 0; i < targets.Length; i++)
        {
            if (!targets[i].gameObject.activeSelf)
                continue;

            averagePosition += targets[i].position;
            numTargets++;
        }

        if (numTargets > 0)
            averagePosition /= numTargets;

        averagePosition.y = transform.position.y;
        _desiredPosition = averagePosition;
    }


    private void Zoom()
    {
        float requiredSize = FindRequiredSize();
        _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, requiredSize, ref _zoomSpeed, dampTime);
    }


    private float FindRequiredSize()
    {
        Vector3 desiredLocalPosition = _camera.transform.InverseTransformPoint(_desiredPosition);
        float size = 0f;

        for (int i = 0; i < targets.Length; i++)
        {
            if (!targets[i].gameObject.activeSelf)
                continue;

            Vector3 targetLocalPosition = _camera.transform.InverseTransformPoint(targets[i].position);
            Vector3 desiredPositionToTarget = targetLocalPosition - desiredLocalPosition;

            size = Mathf.Max(size, Mathf.Abs(desiredPositionToTarget.y));
            size = Mathf.Max(size, Mathf.Abs(desiredPositionToTarget.x) / _camera.aspect);
        }

        size += screenEdgeBuffer;
        size = Mathf.Max(size, minSize);

        return size;
    }


    public void SetStartPositionAndSize()
    {
        FindAveragePosition();
        transform.position = _desiredPosition;
        _camera.orthographicSize = FindRequiredSize();
    }
}