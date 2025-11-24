using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 10f;

    [Header("Поворот вокруг точки")]
    public float rotationSpeed = 120f;   // скорость поворота при зажатой ПКМ

    [Header("Зум (расстояние до точки)")]
    public float zoomSpeed = 20f;
    public float minDistance = 5f;
    public float maxDistance = 60f;

    // Точка, вокруг которой вращаемся и к которой зумимся
    private Vector3 focusPoint;

    private void Start()
    {
        // Пытаемся найти точку на "земле" (плоскость Y = 0), куда сейчас смотрит камера
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = new Ray(transform.position, transform.forward);

        if (groundPlane.Raycast(ray, out float enter))
        {
            focusPoint = ray.GetPoint(enter);
        }
        else
        {
            // Если не получилось — берём (0,0,0)
            focusPoint = Vector3.zero;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    // Движение вперёд/назад/влево/вправо относительно направления камеры
    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D или стрелки
        float v = Input.GetAxisRaw("Vertical");   // W/S или стрелки

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDir = forward * v + right * h;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Vector3 move = moveDir * moveSpeed * Time.deltaTime;
            // Двигаем и камеру, и фокусную точку
            transform.position += move;
            focusPoint += move;
        }
    }

    // Поворот вокруг focusPoint при зажатой ПКМ
    private void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // правая кнопка мыши
        {
            float mouseX = Input.GetAxis("Mouse X");

            // Вращаем камеру вокруг фокусной точки по оси Y
            transform.RotateAround(focusPoint, Vector3.up, mouseX * rotationSpeed * Time.deltaTime);
        }
    }

    // Зум: приближаемся/отдаляемся к focusPoint
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            // Вектор от фокусной точки до камеры
            Vector3 dir = (transform.position - focusPoint).normalized;
            float distance = Vector3.Distance(transform.position, focusPoint);

            // Меняем расстояние
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            // Новая позиция камеры на том же луче
            transform.position = focusPoint + dir * distance;
        }
    }
}
