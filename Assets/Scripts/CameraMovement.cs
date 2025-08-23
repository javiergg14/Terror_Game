using UnityEngine;

public class VHSLikeCamera : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 100f;
    [Min(0.001f)] public float smoothTime = 0.05f; // suavizado

    [Header("Breathing Effect")]
    public float breathingAmplitudeY = 0.05f; // vertical
    public float breathingAmplitudeX = 0.02f; // lateral sutil
    public float breathingFrequency = 1.5f;   // velocidad

    [Header("Camera Lag")]
    public float lagAmount = 0.1f;   // retraso al girar
    public float lagSmoothing = 5f;  // rapidez con la que se corrige el lag

    [Header("Rotation Limits")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    // ---- internos
    private Vector2 smoothMouseDelta;
    private Vector2 currentRotation;
    private Vector3 lagOffset;
    private Vector3 externalOffset;      // <-- offset que viene del headbob
    private Vector3 initialLocalPos;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        initialLocalPos = transform.localPosition;
    }

    void Update()
    {
        HandleMouseLook();
        UpdateLag();
        ApplyOffsets();
    }

    void HandleMouseLook()
    {
        Vector2 mouseDelta = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        ) * mouseSensitivity * Time.deltaTime;

        // suavizado exponencial estable
        float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothTime));
        smoothMouseDelta = Vector2.Lerp(smoothMouseDelta, mouseDelta, t);

        currentRotation.x += smoothMouseDelta.x; // yaw
        currentRotation.y -= smoothMouseDelta.y; // pitch
        currentRotation.y = Mathf.Clamp(currentRotation.y, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0f);
    }

    void UpdateLag()
    {
        Vector3 targetOffset = new Vector3(-smoothMouseDelta.y, smoothMouseDelta.x, 0f) * lagAmount;
        lagOffset = Vector3.Lerp(lagOffset, targetOffset, Time.deltaTime * lagSmoothing);
    }

    void ApplyOffsets()
    {
        // respiración
        float y = Mathf.Sin(Time.time * breathingFrequency) * breathingAmplitudeY;
        float x = Mathf.Sin(Time.time * breathingFrequency * 0.5f) * breathingAmplitudeX;

        Vector3 breathing = new Vector3(x, y, 0f);

        // posición final = base + respiración + lag + headbob externo
        transform.localPosition = initialLocalPos + breathing + lagOffset + externalOffset;
    }

    /// <summary>La usa el controlador del jugador para aplicar headbob.</summary>
    public void SetExternalOffset(Vector3 offset)
    {
        externalOffset = offset;
    }
}
