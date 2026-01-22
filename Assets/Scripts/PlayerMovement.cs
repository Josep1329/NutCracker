using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform playerCamera; // assign in inspector or will fallback to Camera.main

    [Header("Input")]
    public bool inputEnabled = true;

    CharacterController controller;
    float xRotation = 0f;
    Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
        // Ensure the camera is a child of the player so yaw (left/right) comes only from the player transform
        if (playerCamera != null && playerCamera.parent != transform)
        {
            playerCamera.SetParent(transform, true); // keep world position
            playerCamera.localRotation = Quaternion.identity; // reset local rotation so pitch is relative to player
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Check grounded at start of frame (based on state from previous Move)
        bool grounded = controller.isGrounded;
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f; // small downward force to keep contact
        }

        // If input is disabled (e.g., during dialogue), only apply gravity so character stays grounded
        if (!inputEnabled)
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(new Vector3(0f, velocity.y, 0f) * Time.deltaTime);
            return;
        }

        // --- Mouse look ---
        if (playerCamera != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            // Use localRotation with Quaternion to avoid Euler wrap/free rotations
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Rotate the player (yaw) only — camera pitch stays local
            transform.Rotate(Vector3.up * mouseX);
        }

        // --- Movement ---
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        // Use Input.GetButtonDown("Jump") so it's configurable in Input settings
        if (grounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Combine horizontal movement and vertical velocity into one Move call
        Vector3 finalMove = move * walkSpeed + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);
    }

    // Allow other systems to enable/disable player input (movement + looking)
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
