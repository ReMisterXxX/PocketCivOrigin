using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 12f;          // скорость перемещения фокуса (WASD)
    public float rotationSpeed = 120f;     // скорость вращения (правая кнопка мыши)
    public float zoomSpeed = 20f;          // скорость зума колёсиком

    [Header("Zoom limits")]
    public float minDistance = 6f;
    public float maxDistance = 40f;

    [Header("Pitch limits")]
    public float minPitch = 20f;
    public float maxPitch = 80f;

    // Точка, вокруг которой крутим камеру (центр обзора)
    private Vector3 focusPoint = Vector3.zero;

    // Расстояние до фокуса и углы
    private float distance = 15f;
    private float yaw = 45f;
    private float pitch = 45f;

    private void Start()
    {
        // Инициализируем фокус из текущего положения камеры
        // Берём точку "вперёд" от камеры на некоторое расстояние
        focusPoint = transform.position + transform.forward * 10f;

        Vector3 toCam = transform.position - focusPoint;
        distance = toCam.magnitude;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void LateUpdate()
    {
        HandleInput();
        UpdateCameraTransform();
    }

    private void HandleInput()
    {
        // ВРАЩЕНИЕ ВОКРУГ ФОКУСА (ПКМ зажата)
        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");

            yaw += dx * rotationSpeed * Time.deltaTime;
            pitch -= dy * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // ЗУМ КОЛЁСИКОМ
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        // ПЕРЕМЕЩЕНИЕ ФОКУСА (WASD) в горизонтальной плоскости
        Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 right   = new Vector3(transform.right.x, 0f, transform.right.z).normalized;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.A)) move -= right;

        if (move.sqrMagnitude > 0f)
        {
            move.Normalize();
            focusPoint += move * moveSpeed * Time.deltaTime;
        }
    }

    private void UpdateCameraTransform()
    {
        // Считаем позицию камеры из углов и расстояния до фокуса
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rot * new Vector3(0f, 0f, -distance); // камера позади фокуса

        transform.position = focusPoint + offset;
        transform.rotation = rot;
    }

    /// <summary>
    /// Мгновенно переносит фокус камеры к указанной точке.
    /// Камера продолжает крутиться вокруг НОВОЙ точки.
    /// </summary>
    public void JumpToPosition(Vector3 worldPos)
    {
        focusPoint = worldPos;
        // Положение и поворот пересчитаются в LateUpdate()
    }
}
