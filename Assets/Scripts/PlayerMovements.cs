using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovements : NetworkBehaviour
{
    [Header("Déplacement")]
    public float speed = 6f;
    public float sprintSpeed = 10f;

    [Header("Saut & Gravité")]
    public float jumpHeight = 1.4f;
    public float gravity = -100f;
    public float fallMultiplier = 42f;

    [Header("Détection du sol")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private CharacterController _cc;
    private Vector3 _velocity;
    private bool _isGrounded;

    private readonly NetworkVariable<Color> _playerColor =
        new NetworkVariable<Color>(
            Color.white,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        _cc = GetComponent<CharacterController>();
        _playerColor.OnValueChanged += OnColorChanged;

        if (IsServer)
            _playerColor.Value = IsOwner ? Color.cyan : Color.red;

        if (!IsOwner)
            enabled = false;
    }

    public override void OnNetworkDespawn()
    {
        _playerColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color previous, Color current)
    {
        if (TryGetComponent<Renderer>(out var rend))
            rend.material.color = current;
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    private void HandleGroundCheck()
    {
        Vector3 checkPos = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * 0.9f;

        int mask = groundMask.value != 0 ? (int)groundMask : ~(1 << 2);
        _isGrounded = Physics.CheckSphere(checkPos, groundDistance, mask);

        Debug.Log($"isGrounded: {_isGrounded} | velocity.y: {_velocity.y:F2} | checkPos.y: {checkPos.y:F2}");

        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSprinting ? sprintSpeed : speed;

        Vector3 move = (transform.right * h + transform.forward * v).normalized;
        _cc.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleJump()
    {
        // _velocity.y > 0 = on est en train de monter = on vient de sauter
        // Cette condition est le vrai verrou anti-saut infini
        bool canJump = _isGrounded && _velocity.y <= 0f;

        if (Input.GetKeyDown(KeyCode.Space) && canJump)
            _velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
    }

    private void ApplyGravity()
    {
        float currentGravity = _velocity.y < 0f
            ? gravity * fallMultiplier
            : gravity;

        _velocity.y += currentGravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * 0.9f;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(pos, groundDistance);
    }
}