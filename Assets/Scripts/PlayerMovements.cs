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

    [Header("Audio — Pas")]
    public AudioClip[] footstepSounds;
    public float stepInterval = 0.45f;
    public float sprintStepInterval = 0.3f;

    private float _footstepTimer = 0f;

    private CharacterController _cc;
    private Vector3 _velocity;
    private bool _isGrounded;

    private PlayerBonuses _bonuses;

    private readonly NetworkVariable<Color> _playerColor =
        new NetworkVariable<Color>(
            Color.white,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        _cc = GetComponent<CharacterController>();
        _bonuses = GetComponent<PlayerBonuses>();

        _playerColor.OnValueChanged += OnColorChanged;

        if (IsServer)
            _playerColor.Value = IsOwner ? Color.cyan : Color.red;

        OnColorChanged(_playerColor.Value, _playerColor.Value);

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
        HandleFootsteps();
    }

    private void HandleGroundCheck()
    {
        Vector3 checkPos = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * 0.9f;

        int mask = groundMask.value != 0 ? (int)groundMask : ~(1 << 2);
        _isGrounded = Physics.CheckSphere(checkPos, groundDistance, mask);

        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        float speedMultiplier = _bonuses != null
            ? _bonuses.SpeedMultiplier.Value
            : 1f;

        float currentSpeed = (isSprinting ? sprintSpeed : speed) * speedMultiplier;

        Vector3 move = (transform.right * h + transform.forward * v).normalized;
        _cc.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleJump()
    {
        bool canJump = _isGrounded && _velocity.y <= 0f;

        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            float jumpMultiplier = _bonuses != null
                ? _bonuses.JumpMultiplier.Value
                : 1f;

            _velocity.y = Mathf.Sqrt(
                2f * Mathf.Abs(gravity) * (jumpHeight * jumpMultiplier)
            );
        }
    }

    private void ApplyGravity()
    {
        float currentGravity = _velocity.y < 0f
            ? gravity * fallMultiplier
            : gravity;

        _velocity.y += currentGravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // ── Bruits de pas ────────────────────────────────────────────────────────
    private void HandleFootsteps()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool inputMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        // Ne joue rien si on ne bouge pas ou qu'on n'est pas au sol,
        // mais on NE remet PAS le timer à zéro pour éviter qu'un clignotement
        // de _isGrounded ne redéclenche le son en boucle
        if (!inputMoving || !_isGrounded)
            return;

        bool sprinting = Input.GetKey(KeyCode.LeftShift);
        float interval = sprinting ? sprintStepInterval : stepInterval;

        _footstepTimer -= Time.deltaTime;
        if (_footstepTimer <= 0f)
        {
            _footstepTimer = interval;
            PlayFootstepServerRpc();
        }
    }

    [ServerRpc]
    private void PlayFootstepServerRpc()
    {
        PlayFootstepClientRpc();
    }

    [ClientRpc]
    private void PlayFootstepClientRpc()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        AudioSource.PlayClipAtPoint(clip, transform.position, 0.6f); // un peu plus discret que les tirs
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