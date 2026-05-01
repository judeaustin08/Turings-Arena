using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    private CharacterController _cc;
    private FirstPersonCamera _fpc;

    private Vector3 _velocity;
    private bool _jumpPressed;
    private bool _firePressed;

    public float speed = 5f;

    public float jumpForce = 5f;
    public float gravityValue = -10f;

    public float lookSpeed = 100f;
    public Vector2 LookAngles { get; private set; }
    public Transform camAnchor;

    [Networked] public float Health { get; set; } = 100;

    public float damage = 10f;
    public GameObject fxPrefab;
    public Transform gunTip;
    public float cooldown = 0.2f;
    public TickTimer weaponCooldown { get; set; }

    public InputActionAsset inputAsset;
    private InputActionMap inputMap;
    private InputAction move;
    private InputAction look;
    private InputAction jump;
    private InputAction fire;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        inputMap = inputAsset.FindActionMap("Player");
        move = inputMap.FindAction("Move");
        look = inputMap.FindAction("Look");
        jump = inputMap.FindAction("Jump");
        fire = inputMap.FindAction("Attack");
    }

    private void OnEnable()
    {
        inputMap.Enable();
    }

    private void OnDisable()
    {
        inputMap.Disable();
    }

    private void Update()
    {
        // Update look angles
        Vector2 lookDelta = look.ReadValue<Vector2>();
        LookAngles += lookDelta * lookSpeed * Time.deltaTime;
        LookAngles = new()
        {
            x = LookAngles.x,
            y = Mathf.Clamp(LookAngles.y, -60f, 70f),
        };

        if (jump.WasPressedThisFrame()) _jumpPressed = true;
        if (fire.WasPressedThisFrame()) _firePressed = true;
    }

    /// <summary>
    /// Called when this NetworkBehaviour is instantiated and ready for use by Fusion
    /// </summary>
    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _fpc = FindAnyObjectByType<FirstPersonCamera>();
        _fpc.target = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        if (!HasStateAuthority) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// FixedUpdateNetwork is only executed on the State Authority
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (_cc.isGrounded)
        {
            _velocity = new()
            {
                x = 0,
                y = -1f,
                z = 0
            };
        }

        _velocity.y += gravityValue * Runner.DeltaTime;
        if (_jumpPressed && _cc.isGrounded)
        {
            _velocity.y += jumpForce;
        }

        camAnchor.rotation = Quaternion.Euler(-LookAngles.y, LookAngles.x, 0);

        Vector2 i_movement = move.ReadValue<Vector2>();
        Vector3 moveDelta = new()
        {
            x = i_movement.x,
            y = 0,
            z = i_movement.y
        };
        moveDelta *= speed;
        moveDelta = Quaternion.Euler(0, LookAngles.x, 0) * moveDelta;

        _cc.Move(moveDelta * Runner.DeltaTime + _velocity * Runner.DeltaTime);

        if (_firePressed)
        {
            Shoot();
        }

        _jumpPressed = false;
        _firePressed = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void DealDamageRPC(float damage)
    {
        Health -= damage;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void SpawnShotFxRPC(Vector3 point)
    {
        NetworkObject fx = Runner.Spawn(fxPrefab);
        StartCoroutine(fx.GetComponent<ShotFX>().StartFX(gunTip.position, point));
    }

    public void Shoot()
    {
        if (!weaponCooldown.ExpiredOrNotRunning(Runner)) return;
        weaponCooldown = TickTimer.CreateFromSeconds(Runner, cooldown);

        Ray ray = new(camAnchor.transform.position, camAnchor.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            PlayerController other;
            if (hit.collider.TryGetComponent(out other))
            {
                other.DealDamageRPC(damage);
            }

            SpawnShotFxRPC(hit.point);
        }
    }
}