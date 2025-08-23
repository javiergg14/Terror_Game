using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CameramanController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.6f;
    public float jumpForce = 5f;

    [Header("Referencias")]
    public Transform cameraTransform; // arrastra aquí la Main Camera

    [Header("Headbob")]
    public float bobFrequency = 6f;              // pasos por segundo aprox.
    public float bobAmplitude = 0.04f;           // altura base del headbob
    public float bobAmplitudeSprintMultiplier = 1.5f;
    public float bobFrequencySprintMultiplier = 1.2f;
    public float bobReturnSpeed = 8f;            // velocidad al volver a 0 cuando paras

    [Header("Ground Check")]
    public LayerMask groundMask = ~0;
    public float extraGroundDistance = 0.2f;     // margen debajo del collider para detectar suelo

    private Rigidbody rb;
    private CapsuleCollider col;
    private VHSLikeCamera vhsCam;

    private float bobTimer;
    private bool isGrounded;
    private bool isSprinting;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true; // que la física no tumbe el cuerpo

        if (cameraTransform != null)
            vhsCam = cameraTransform.GetComponent<VHSLikeCamera>();
    }

    void Update()
    {
        GroundCheck();

        Move();          // WASD + sprint
        Jump();          // Space

        HandleHeadbob(); // calcula y manda offset a la cámara
    }

    void GroundCheck()
    {
        // SphereCast desde el centro del capsule hacia abajo
        Vector3 origin = transform.position + col.center;
        float castDistance = col.height * 0.5f + extraGroundDistance;
        float radius = col.radius * 0.95f;

        isGrounded = Physics.SphereCast(origin, radius, Vector3.down, out _, castDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        // dirección relativa a la cámara, en plano XZ
        Vector3 fwd = cameraTransform ? cameraTransform.forward : transform.forward;
        fwd.y = 0f; fwd.Normalize();

        Vector3 right = cameraTransform ? cameraTransform.right : transform.right;
        right.y = 0f; right.Normalize();

        Vector3 direction = (fwd * v + right * h).normalized;

        // Sprint con LeftShift
        isSprinting = Input.GetKey(KeyCode.LeftShift) && direction.sqrMagnitude > 0.01f && isGrounded;

        float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        Vector3 velocity = direction * speed;
        velocity.y = rb.velocity.y; // mantener gravedad/impulso
        rb.velocity = velocity;
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // reset Y para salto consistente
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void HandleHeadbob()
    {
        if (vhsCam == null || cameraTransform == null) return;

        // magnitud horizontal de la velocidad
        Vector3 velXZ = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float speed = velXZ.magnitude;

        if (isGrounded && speed > 0.1f)
        {
            float freq = bobFrequency * (isSprinting ? bobFrequencySprintMultiplier : 1f);
            float amp = bobAmplitude * (isSprinting ? bobAmplitudeSprintMultiplier : 1f);

            bobTimer += Time.deltaTime * freq;
            float y = Mathf.Sin(bobTimer * 2f) * amp; // *2f para que un ciclo = paso izquierdo+derecho

            // puedes añadir un ligero vaivén lateral si quieres:
            // float x = Mathf.Sin(bobTimer) * amp * 0.3f;
            // vhsCam.SetExternalOffset(new Vector3(x, y, 0f));

            vhsCam.SetExternalOffset(new Vector3(0f, y, 0f));
        }
        else
        {
            // volver suave a 0 cuando paras o estás en el aire
            Vector3 current = Vector3.zero;
            vhsCam.SetExternalOffset(Vector3.Lerp(current, Vector3.zero, Time.deltaTime * bobReturnSpeed));
            bobTimer = 0f;
        }
    }
}
