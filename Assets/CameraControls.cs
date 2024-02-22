using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public float panSpeed = 20f;
    public float zoomSpeed = 5f;
    public float orbitSpeed = 10f;
    public float lookSpeed = 5000f;

    private Vector3 lastMousePos;

    void Update()
    {
        // Pan
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            transform.Translate(-delta.x * panSpeed * Time.deltaTime, -delta.y * panSpeed * Time.deltaTime, 0);
        }

        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.Translate(0, 0, scroll * zoomSpeed, Space.Self);

        // Translate
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        transform.Translate(horizontal * panSpeed * Time.deltaTime, 0, vertical * panSpeed * Time.deltaTime);

        // Orbit
        if (Input.GetMouseButton(2)) // Middle mouse button
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            transform.RotateAround(transform.position, Vector3.up, delta.x * orbitSpeed * Time.deltaTime);
            transform.RotateAround(transform.position, transform.right, -delta.y * orbitSpeed * Time.deltaTime);
        }

        // Look Around
        if (Input.GetMouseButton(0)) // Left mouse button
        {
            float yaw = 5000 * Input.GetAxis("Mouse X") * Time.deltaTime;
            float pitch = 5000 * Input.GetAxis("Mouse Y") * Time.deltaTime;

            transform.eulerAngles += new Vector3(-pitch, yaw, 0);
        }

        lastMousePos = Input.mousePosition;
    }
}
