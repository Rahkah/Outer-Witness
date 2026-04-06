using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerController))]
public class MovementController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private CharacterController _characterController;
    private PlayerController _playerController;

    private void Awake()
    {
        // Cache required components
        _characterController = GetComponent<CharacterController>();
        _playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // 1. Get current movement input from the PlayerController
        // moveInput.x = horizontal (A/D), moveInput.y = vertical (W/S)
        Vector2 input = _playerController.MoveInput;

        // 2. Compute the world-space movement direction based on player's current orientation
        // We project the local movement axis onto world space using the player's forward and right vectors
        Vector3 moveDir = (transform.forward * input.y) + (transform.right * input.x);

        // 3. Normalize to prevent faster diagonal movement (Pythagorean Theorem: 1^2 + 1^2 = 2, sqrt(2) > 1)
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        // 4. Apply the movement using CharacterController.Move()
        // Movement = direction * speed * deltaTime for frame-rate independence
        // We do NOT modify the Y component to remain on the flat plane (no gravity yet)
        _characterController.Move(moveDir * moveSpeed * Time.deltaTime);
    }
}
