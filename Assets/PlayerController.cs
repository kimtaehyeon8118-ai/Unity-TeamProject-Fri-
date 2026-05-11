using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private enum MovementState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Dash,
        Grapple,
        Hit
    }

    [Header("Move")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float groundAcceleration = 65f;
    [SerializeField] private float groundDeceleration = 75f;
    [SerializeField] private float airAcceleration = 45f;
    [SerializeField] private float airDeceleration = 40f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float fallGravityMultiplier = 1.9f;
    [SerializeField] private float lowJumpGravityMultiplier = 1.6f;
    [SerializeField] private float maxFallSpeed = 22f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Grapple")]
    [SerializeField] private float grappleRange = 11f;
    [SerializeField] private float grappleMinVerticalOffset = 1.1f;
    [SerializeField] private float grappleInitialSlack = 1.02f;
    [SerializeField] private float grappleMinRopeLength = 2.4f;
    [SerializeField] private float grappleGravityScale = 0.92f;
    [SerializeField] private float grappleSwingForce = 28f;
    [SerializeField] private float grapplePullForce = 16f;
    [SerializeField] private float grappleReelSpeed = 4.5f;
    [SerializeField] private float grappleReleaseHorizontalBoost = 4.75f;
    [SerializeField] private float grappleReleaseVerticalBoost = 4.25f;
    [SerializeField] private float grappleReleaseVelocityRetention = 1.08f;
    [SerializeField] private float grappleReleaseSwingBonusMultiplier = 0.35f;
    [SerializeField] private float grappleReleaseMinForwardSpeed = 8.5f;
    [SerializeField] private float grappleMaxSpeed = 20f;
    [SerializeField] private float grappleForwardBias = 2.6f;
    [SerializeField] private Vector3 grappleLineOffset = new Vector3(0f, 0.65f, 0f);

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Feedback")]
    [SerializeField] private float hitLockDuration = 0.2f;
    [SerializeField] private float respawnInvulnerabilityDuration = 1f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private PlayerInput playerInput;
    private Collider2D[] colliders;
    private DistanceJoint2D grappleJoint;
    private LineRenderer grappleLine;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;

    private Vector2 moveInput;
    private Vector2 currentGrapplePoint;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isDashing;
    private bool isInvulnerable;
    private bool controlsLocked;
    private bool jumpQueued;
    private bool dashQueued;
    private bool grappleQueued;
    private bool isGrappling;
    private bool dashAvailable = true;
    private bool jumpHeldFallback;
    private bool dashHeldFallback;
    private bool grappleHeldFallback;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashCooldownTimer;
    private float defaultGravityScale;
    private Coroutine dashRoutine;
    private Coroutine invulnerabilityRoutine;
    private MovementState movementState;
    private GrappleAnchor currentGrappleAnchor;
    private GrappleAnchor previewGrappleAnchor;

    public event System.Action Dashed;
    public event System.Action GrappleAttached;
    public event System.Action<bool> GrappleReleased;

    public bool CanTakeDamage => !isInvulnerable && !controlsLocked;
    public bool IsGrounded => isGrounded;
    public bool IsGrappling => isGrappling;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>(true);
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        playerInput = GetComponent<PlayerInput>();
        colliders = GetComponents<Collider2D>();
        defaultGravityScale = rb.gravityScale;

        EnsureGrappleComponents();
    }

    private void Start()
    {
        CacheActions();
        GameManager.Instance?.RegisterPlayer(this);
    }

    private void OnEnable()
    {
        CacheActions();
        BindActions();
    }

    private void OnDisable()
    {
        UnbindActions();
        ReleaseGrapple(false);
        ClearGrapplePreview();
    }

    private void Update()
    {
        UpdateGroundedState();
        UpdateTimers();
        ReadMoveInput();
        ReadJumpFallback();
        ReadDashFallback();
        ReadGrappleFallback();
        UpdateGrapplePreview();

        if (controlsLocked || GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            UpdateAnimation();
            return;
        }

        if (jumpQueued)
        {
            jumpQueued = false;
            jumpBufferTimer = jumpBufferTime;
        }

        if (grappleQueued)
        {
            grappleQueued = false;
            TryStartGrapple();
        }

        if (dashQueued)
        {
            dashQueued = false;
            TryStartDash();
        }

        TryConsumeJumpBuffer();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (controlsLocked || isDashing || GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            return;
        }

        if (isGrappling)
        {
            ApplyGrappleMovement();
            return;
        }

        ApplyHorizontalMovement();
        ApplyBetterJumpGravity();
    }

    private void LateUpdate()
    {
        UpdateGrappleLine();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTakeContactDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryTakeContactDamage(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTakeContactDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTakeContactDamage(other);
    }

    public void SetControlEnabled(bool enabled)
    {
        controlsLocked = !enabled;

        if (!enabled)
        {
            ReleaseGrapple(false);
            moveInput = Vector2.zero;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    public void SetVisible(bool visible)
    {
        ReleaseGrapple(false);

        spriteRenderer.enabled = visible;

        foreach (Collider2D hitbox in colliders)
        {
            hitbox.enabled = visible;
        }
    }

    public void RespawnAt(Vector3 worldPosition)
    {
        ReleaseGrapple(false);

        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        isDashing = false;
        dashAvailable = true;
        dashCooldownTimer = 0f;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = Vector2.zero;
        transform.position = worldPosition;
    }

    public void ApplyRespawnInvulnerability()
    {
        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
        }

        invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine(respawnInvulnerabilityDuration));
    }

    private void TryTakeContactDamage(Collider2D other)
    {
        if (other == null || GameManager.Instance == null || !CanTakeDamage)
        {
            return;
        }

        Obstacle obstacle = other.GetComponentInParent<Obstacle>();
        if (obstacle != null && obstacle.CanDamagePlayer)
        {
            GameManager.Instance.DamagePlayer(obstacle.Damage, obstacle.transform.position);
            return;
        }

        PatrolHazard patrolHazard = other.GetComponentInParent<PatrolHazard>();
        if (patrolHazard != null)
        {
            GameManager.Instance.DamagePlayer(patrolHazard.Damage, patrolHazard.transform.position);
        }
    }

    public void ReceiveDamage(Vector2 hazardPosition)
    {
        if (!CanTakeDamage)
        {
            return;
        }

        ReleaseGrapple(false);
        StartCoroutine(HitLockRoutine(hazardPosition));
    }

    private void CacheActions()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        if (playerInput.currentActionMap == null)
        {
            playerInput.SwitchCurrentActionMap("Player");
        }

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Sprint"];
        attackAction = playerInput.actions["Attack"];
    }

    private void BindActions()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;
            jumpAction.performed += OnJumpPerformed;
            jumpAction.canceled += OnJumpCanceled;
        }

        if (dashAction != null)
        {
            dashAction.performed -= OnDashPerformed;
            dashAction.performed += OnDashPerformed;
        }

        if (attackAction != null)
        {
            attackAction.performed -= OnGrapplePerformed;
            attackAction.canceled -= OnGrappleCanceled;
            attackAction.performed += OnGrapplePerformed;
            attackAction.canceled += OnGrappleCanceled;
        }
    }

    private void UnbindActions()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;
        }

        if (dashAction != null)
        {
            dashAction.performed -= OnDashPerformed;
        }

        if (attackAction != null)
        {
            attackAction.performed -= OnGrapplePerformed;
            attackAction.canceled -= OnGrappleCanceled;
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isGrappling)
        {
            ReleaseGrapple(true);
            return;
        }

        jumpQueued = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        if (isGrappling)
        {
            return;
        }

        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        dashQueued = true;
    }

    private void OnGrapplePerformed(InputAction.CallbackContext context)
    {
        if (!isGrappling)
        {
            grappleQueued = true;
        }
    }

    private void OnGrappleCanceled(InputAction.CallbackContext context)
    {
        if (isGrappling)
        {
            ReleaseGrapple(false);
        }
    }

    private void ReadMoveInput()
    {
        if (moveAction == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void ReadDashFallback()
    {
        if (Keyboard.current == null)
        {
            dashHeldFallback = false;
            return;
        }

        bool dashPressed = Keyboard.current.leftShiftKey.wasPressedThisFrame ||
                           Keyboard.current.rightShiftKey.wasPressedThisFrame ||
                           Keyboard.current.qKey.wasPressedThisFrame;

        if (Mouse.current != null)
        {
            dashPressed |= Mouse.current.rightButton.wasPressedThisFrame;
        }

        if (dashPressed && !dashHeldFallback)
        {
            dashQueued = true;
        }

        dashHeldFallback = Keyboard.current.leftShiftKey.isPressed ||
                           Keyboard.current.rightShiftKey.isPressed ||
                           Keyboard.current.qKey.isPressed;

        if (Mouse.current != null)
        {
            dashHeldFallback |= Mouse.current.rightButton.isPressed;
        }
    }

    private void ReadJumpFallback()
    {
        if (Keyboard.current == null)
        {
            jumpHeldFallback = false;
            return;
        }

        bool isPressed = Keyboard.current.leftAltKey.wasPressedThisFrame ||
                         Keyboard.current.rightAltKey.wasPressedThisFrame ||
                         Keyboard.current.leftCommandKey.wasPressedThisFrame;

        bool isHeld = Keyboard.current.leftAltKey.isPressed ||
                      Keyboard.current.rightAltKey.isPressed ||
                      Keyboard.current.leftCommandKey.isPressed;

        if (isPressed && !jumpHeldFallback)
        {
            if (isGrappling)
            {
                ReleaseGrapple(true);
            }
            else
            {
                jumpQueued = true;
            }
        }
        else if (!isHeld && jumpHeldFallback && !isGrappling && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        jumpHeldFallback = isHeld;
    }

    private void ReadGrappleFallback()
    {
        bool isHeld = false;
        bool isPressed = false;

        if (Keyboard.current != null)
        {
            isPressed |= Keyboard.current.zKey.wasPressedThisFrame;
            isHeld |= Keyboard.current.zKey.isPressed;
        }

        if (Mouse.current != null)
        {
            isPressed |= Mouse.current.leftButton.wasPressedThisFrame;
            isHeld |= Mouse.current.leftButton.isPressed;
        }

        if (isPressed && !grappleHeldFallback && !isGrappling)
        {
            grappleQueued = true;
        }
        else if (!isHeld && grappleHeldFallback && isGrappling)
        {
            ReleaseGrapple(false);
        }

        grappleHeldFallback = isHeld;
    }

    private void UpdateGroundedState()
    {
        wasGrounded = isGrounded;
        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;

            if (!wasGrounded)
            {
                dashAvailable = true;
            }
        }
    }

    private void UpdateTimers()
    {
        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    private void TryConsumeJumpBuffer()
    {
        if (jumpBufferTimer <= 0f || coyoteTimer <= 0f || isDashing || isGrappling)
        {
            return;
        }

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        dashAvailable = true;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void ApplyHorizontalMovement()
    {
        float targetSpeed = moveInput.x * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        float acceleration = Mathf.Abs(targetSpeed) > 0.01f
            ? isGrounded ? groundAcceleration : airAcceleration
            : isGrounded ? groundDeceleration : airDeceleration;

        float movement = speedDifference * acceleration * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    private void ApplyBetterJumpGravity()
    {
        if (isGrounded || isGrappling)
        {
            rb.gravityScale = defaultGravityScale;
            return;
        }

        if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0f && jumpAction != null && !jumpAction.IsPressed())
        {
            rb.gravityScale = defaultGravityScale * lowJumpGravityMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }

        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    private void ApplyGrappleMovement()
    {
        if (currentGrappleAnchor == null)
        {
            ReleaseGrapple(false);
            return;
        }

        currentGrapplePoint = currentGrappleAnchor.WorldPosition;
        grappleJoint.connectedAnchor = currentGrapplePoint;
        rb.gravityScale = defaultGravityScale * grappleGravityScale;

        Vector2 toAnchor = currentGrapplePoint - rb.position;
        float ropeDistance = toAnchor.magnitude;
        if (ropeDistance <= 0.001f)
        {
            return;
        }

        Vector2 ropeDirection = toAnchor / ropeDistance;
        Vector2 tangent = new Vector2(-ropeDirection.y, ropeDirection.x);
        float reelInput = Mathf.Clamp(moveInput.y, -1f, 1f);

        grappleJoint.distance = Mathf.Clamp(
            grappleJoint.distance - reelInput * grappleReelSpeed * Time.fixedDeltaTime,
            grappleMinRopeLength,
            grappleRange);

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector2 desiredHorizontal = Vector2.right * Mathf.Sign(moveInput.x);
            if (Vector2.Dot(tangent, desiredHorizontal) < 0f)
            {
                tangent = -tangent;
            }

            float swingStrength = Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(grappleJoint.distance * 0.45f, grappleJoint.distance, ropeDistance));
            rb.AddForce(tangent * grappleSwingForce * Mathf.Abs(moveInput.x) * swingStrength, ForceMode2D.Force);
        }

        float ropeStretch = Mathf.Max(0f, ropeDistance - grappleJoint.distance);
        if (ropeStretch > 0.015f)
        {
            rb.AddForce(ropeDirection * ropeStretch * grapplePullForce, ForceMode2D.Force);
        }

        if (rb.linearVelocity.sqrMagnitude > grappleMaxSpeed * grappleMaxSpeed)
        {
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, grappleMaxSpeed);
        }
    }

    private void TryStartDash()
    {
        if (!dashAvailable || dashCooldownTimer > 0f || isDashing || isGrappling)
        {
            return;
        }

        dashRoutine = StartCoroutine(DashRoutine());
    }

    private void TryStartGrapple()
    {
        if (isDashing || isGrappling || controlsLocked)
        {
            return;
        }

        GrappleAnchor anchor = previewGrappleAnchor != null
            ? previewGrappleAnchor
            : GrappleAnchor.FindBestAnchor(rb.position, GetFacingDirection(), grappleRange, grappleMinVerticalOffset, grappleForwardBias);

        if (anchor == null)
        {
            return;
        }

        currentGrappleAnchor = anchor;
        currentGrapplePoint = anchor.WorldPosition;
        isGrappling = true;
        jumpBufferTimer = 0f;
        dashAvailable = true;
        rb.gravityScale = defaultGravityScale * grappleGravityScale;

        grappleJoint.connectedAnchor = currentGrapplePoint;
        grappleJoint.distance = Mathf.Clamp(
            Vector2.Distance(rb.position, currentGrapplePoint) * grappleInitialSlack,
            grappleMinRopeLength,
            grappleRange);
        grappleJoint.enabled = true;

        if (grappleLine != null)
        {
            grappleLine.enabled = true;
        }

        currentGrappleAnchor.SetHighlighted(true);
        GrappleAttached?.Invoke();
    }

    private void ReleaseGrapple(bool applyBoost)
    {
        if (!isGrappling)
        {
            return;
        }

        Vector2 releasedPoint = currentGrapplePoint;

        isGrappling = false;
        rb.gravityScale = defaultGravityScale;

        if (grappleJoint != null)
        {
            grappleJoint.enabled = false;
        }

        if (grappleLine != null)
        {
            grappleLine.enabled = false;
        }

        if (currentGrappleAnchor != null)
        {
            currentGrappleAnchor.SetHighlighted(false);
        }

        currentGrappleAnchor = null;

        if (applyBoost)
        {
            ApplyGrappleReleaseBoost(releasedPoint);
        }

        GrappleReleased?.Invoke(applyBoost);
    }

    private void ApplyGrappleReleaseBoost(Vector2 grapplePoint)
    {
        Vector2 ropeDirection = (grapplePoint - rb.position).normalized;
        Vector2 tangent = new Vector2(-ropeDirection.y, ropeDirection.x);
        float tangentialSpeed = Vector2.Dot(rb.linearVelocity, tangent);
        if (tangentialSpeed < 0f)
        {
            tangent = -tangent;
            tangentialSpeed = -tangentialSpeed;
        }

        Vector2 preservedVelocity = rb.linearVelocity * grappleReleaseVelocityRetention;
        float swingBonus = tangentialSpeed * grappleReleaseSwingBonusMultiplier;
        float verticalBoost = Mathf.Lerp(
            grappleReleaseVerticalBoost * 0.45f,
            grappleReleaseVerticalBoost,
            Mathf.InverseLerp(3.5f, grappleMaxSpeed, tangentialSpeed));

        Vector2 boost = tangent * (grappleReleaseHorizontalBoost + swingBonus) + Vector2.up * verticalBoost;
        Vector2 finalVelocity = preservedVelocity + boost;

        if (Mathf.Abs(tangent.x) > 0.01f)
        {
            float forwardDirection = Mathf.Sign(tangent.x);
            if (Mathf.Sign(finalVelocity.x) == forwardDirection)
            {
                finalVelocity.x = forwardDirection * Mathf.Max(Mathf.Abs(finalVelocity.x), grappleReleaseMinForwardSpeed);
            }
        }

        rb.linearVelocity = Vector2.ClampMagnitude(finalVelocity, grappleMaxSpeed);
        dashAvailable = true;
    }

    private IEnumerator DashRoutine()
    {
        dashAvailable = false;
        isDashing = true;
        isInvulnerable = true;
        movementState = MovementState.Dash;

        float dashDirection = Mathf.Abs(moveInput.x) > 0.01f ? Mathf.Sign(moveInput.x) : GetFacingDirection().x;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
        spriteRenderer.color = new Color(0.7f, 0.95f, 1f, 1f);
        Dashed?.Invoke();

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = defaultGravityScale;
        isDashing = false;
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.45f, rb.linearVelocity.y);
        isInvulnerable = false;
        spriteRenderer.color = Color.white;
        dashRoutine = null;
    }

    private IEnumerator HitLockRoutine(Vector2 hazardPosition)
    {
        ReleaseGrapple(false);
        controlsLocked = true;
        isInvulnerable = true;
        movementState = MovementState.Hit;

        float horizontalDirection = Mathf.Sign(transform.position.x - hazardPosition.x);
        if (Mathf.Approximately(horizontalDirection, 0f))
        {
            horizontalDirection = -GetFacingDirection().x;
        }

        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = new Vector2(horizontalDirection * moveSpeed * 0.6f, jumpForce * 0.35f);

        yield return new WaitForSeconds(hitLockDuration);

        controlsLocked = false;

        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
        }

        invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine(respawnInvulnerabilityDuration));
    }

    private IEnumerator InvulnerabilityRoutine(float duration)
    {
        isInvulnerable = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            spriteRenderer.color = spriteRenderer.color.a > 0.9f
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;

            elapsed += 0.12f;
            yield return new WaitForSeconds(0.12f);
        }

        spriteRenderer.color = Color.white;
        isInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    private void UpdateAnimation()
    {
        if (moveInput.x > 0.05f)
        {
            spriteRenderer.flipX = true;
        }
        else if (moveInput.x < -0.05f)
        {
            spriteRenderer.flipX = false;
        }

        if (isDashing)
        {
            movementState = MovementState.Dash;
        }
        else if (isGrappling)
        {
            movementState = MovementState.Grapple;
        }
        else if (!isGrounded)
        {
            movementState = rb.linearVelocity.y > 0.1f ? MovementState.Jump : MovementState.Fall;
        }
        else if (Mathf.Abs(rb.linearVelocity.x) > 0.2f)
        {
            movementState = MovementState.Run;
        }
        else
        {
            movementState = MovementState.Idle;
        }

        if (anim == null)
        {
            return;
        }

        anim.SetBool("isRun", movementState == MovementState.Run);
        anim.SetBool("isJump",
            movementState == MovementState.Jump ||
            movementState == MovementState.Fall ||
            movementState == MovementState.Dash ||
            movementState == MovementState.Grapple ||
            movementState == MovementState.Hit);
    }

    private void UpdateGrapplePreview()
    {
        if (controlsLocked || isGrappling || isDashing)
        {
            ClearGrapplePreview();
            return;
        }

        GrappleAnchor candidate = GrappleAnchor.FindBestAnchor(
            rb.position,
            GetFacingDirection(),
            grappleRange,
            grappleMinVerticalOffset,
            grappleForwardBias);

        if (candidate == previewGrappleAnchor)
        {
            return;
        }

        if (previewGrappleAnchor != null)
        {
            previewGrappleAnchor.SetPreview(false);
        }

        previewGrappleAnchor = candidate;

        if (previewGrappleAnchor != null)
        {
            previewGrappleAnchor.SetPreview(true);
        }
    }

    private void ClearGrapplePreview()
    {
        if (previewGrappleAnchor != null)
        {
            previewGrappleAnchor.SetPreview(false);
            previewGrappleAnchor = null;
        }
    }

    private void UpdateGrappleLine()
    {
        if (grappleLine == null)
        {
            return;
        }

        if (!isGrappling || currentGrappleAnchor == null)
        {
            grappleLine.enabled = false;
            return;
        }

        currentGrapplePoint = currentGrappleAnchor.WorldPosition;
        grappleLine.enabled = true;
        grappleLine.SetPosition(0, transform.position + grappleLineOffset);
        grappleLine.SetPosition(1, currentGrapplePoint);
    }

    private Vector2 GetFacingDirection()
    {
        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            return new Vector2(Mathf.Sign(moveInput.x), 0f);
        }

        return spriteRenderer != null && spriteRenderer.flipX ? Vector2.right : Vector2.left;
    }

    private void EnsureGrappleComponents()
    {
        grappleJoint = GetComponent<DistanceJoint2D>();
        if (grappleJoint == null)
        {
            grappleJoint = gameObject.AddComponent<DistanceJoint2D>();
        }

        grappleJoint.enabled = false;
        grappleJoint.autoConfigureConnectedAnchor = false;
        grappleJoint.autoConfigureDistance = false;
        grappleJoint.enableCollision = true;
        grappleJoint.maxDistanceOnly = true;

        grappleLine = GetComponent<LineRenderer>();
        if (grappleLine == null)
        {
            grappleLine = gameObject.AddComponent<LineRenderer>();
        }

        grappleLine.enabled = false;
        grappleLine.positionCount = 2;
        grappleLine.useWorldSpace = true;
        grappleLine.widthMultiplier = 0.08f;
        grappleLine.numCapVertices = 6;
        grappleLine.sortingOrder = 12;
        grappleLine.startColor = new Color(0.3f, 0.95f, 1f, 0.95f);
        grappleLine.endColor = new Color(0.85f, 1f, 1f, 0.95f);

        if (grappleLine.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                grappleLine.material = new Material(shader);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = new Color(0.3f, 0.95f, 1f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, grappleRange);
    }
}
