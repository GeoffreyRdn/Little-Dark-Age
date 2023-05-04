using UnityEngine;

public class TestCharacterController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public Transform cameraTransform;
    public float cameraRotationSpeed = 3f;

    private Rigidbody rb;
    private bool isJumping = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Player movement
        float horizontalMove = Input.GetAxis("Horizontal");
        float verticalMove = Input.GetAxis("Vertical");
        Vector3 moveDirection = (horizontalMove * cameraTransform.right + verticalMove * cameraTransform.forward).normalized;
        moveDirection.y = 0f;
        rb.velocity = moveDirection * moveSpeed + new Vector3(0f, rb.velocity.y, 0f);

        // Player jumping
        if (Input.GetButtonDown("Jump") && !isJumping)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
        }

        // Camera rotation
        float mouseX = Input.GetAxis("Mouse X") * cameraRotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * cameraRotationSpeed;
        transform.Rotate(Vector3.up * mouseX);

        float cameraRotationX = cameraTransform.rotation.eulerAngles.x - mouseY;
        cameraTransform.rotation = Quaternion.Euler(cameraRotationX, transform.rotation.eulerAngles.y, 0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isJumping = false;
        }
    }
}