using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

// Ensure this runs before the TankShooting component
[DefaultExecutionOrder(-10)]
public class TankMovement : MonoBehaviour
{
    [Tooltip("The player number. Without a tank selection menu, Player 1 is left keyboard control, Player 2 is right keyboard")]
    public int playerNumber = 1;

    [Tooltip("The speed in unity unit/second the tank move at")]
    public float speed = 12f;

    [Tooltip("The speed in deg/s that tank will rotate at")]
    public float turnSpeed = 180f;

    [Tooltip("If set to true, the tank will auto orient and move toward the pressed direction instead of rotating on left/right and move forward on up")]
    public bool isDirectControl;

    [Tooltip("Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.")]
    public AudioSource movementAudio;

    [Tooltip("Audio to play when the tank isn't moving.")]
    public AudioClip engineIdling;

    [Tooltip("Audio to play when the tank is moving.")]
    public AudioClip engineDriving;

    [Tooltip("The amount by which the pitch of the engine noises can vary.")]
    public float pitchRange = 0.2f;

    [Tooltip("Is set to true this will be controlled by the computer and not a player")]
    public bool isComputerControlled = false;

    [HideInInspector]
    public TankInputUser inputUser;

    public Rigidbody Rigidbody => _rigidbody;

    // Defines the index of the control (1 = left keyboard, 2 = right keyboard, -1 = no control)
    public int ControlIndex { get; set; } = -1;

    // The name of the input axis for moving forward and back.
    private string _movementAxisName;

    // The name of the input axis for turning.
    private string _turnAxisName;

    // Reference used to move the tank.
    private Rigidbody _rigidbody;

    // The current value of the movement input.
    private float _movementInputValue;

    // The current value of the turn input.
    private float _turnInputValue;

    // The pitch of the audio source at the start of the scene.
    private float _originalPitch;

    // References to all the particles systems used by the Tanks
    private ParticleSystem[] _particleSystems;

    // The InputAction used to move, retrieved from TankInputUser
    private InputAction _moveAction;

    // The InputAction used to shot, retrieved from TankInputUser
    private InputAction _turnAction;

    // In Direct Control mode, store the direction the user *wants* to go toward
    private Vector3 _requestedDirection;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        inputUser = GetComponent<TankInputUser>();
        if (inputUser == null)
            inputUser = gameObject.AddComponent<TankInputUser>();
    }


    private void OnEnable()
    {
        // Computer controlled tank are kinematic
        _rigidbody.isKinematic = false;

        // Also reset the input values.
        _movementInputValue = 0f;
        _turnInputValue = 0f;

        // We grab all the Particle systems child of that Tank to be able to Stop/Play them on Deactivate/Activate
        // It is needed because we move the Tank when spawning it, and if the Particle System is playing while we do that
        // it "think" it move from (0,0,0) to the spawn point, creating a huge trail of smoke
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < _particleSystems.Length; ++i)
        {
            _particleSystems[i].Play();
        }
    }


    private void OnDisable()
    {
        // When the tank is turned off, set it to kinematic so it stops moving.
        _rigidbody.isKinematic = true;

        // Stop all particle system so it "reset" it's position to the actual one instead of thinking we moved when spawning
        for (int i = 0; i < _particleSystems.Length; ++i)
        {
            _particleSystems[i].Stop();
        }
    }


    private void Start()
    {
        // If this is computer controlled...
        if (isComputerControlled)
        {
            // but it doesn't have an AI component...
            var ai = GetComponent<TankAI>();
            if (ai == null)
            {
                gameObject.AddComponent<TankAI>();
            }
        }

        if (ControlIndex == -1 && !isComputerControlled)
        {
            ControlIndex = playerNumber;
        }


        if (ControlIndex == 1)
        {
            inputUser.ActivateScheme(ControlIndex == 1 ? "KeyboardLeft" : "KeyboardRight");
        }

        _movementAxisName = "Vertical";
        _turnAxisName = "Horizontal";

        _moveAction = inputUser.ActionAsset.FindAction(_movementAxisName);
        _turnAction = inputUser.ActionAsset.FindAction(_turnAxisName);

        // Actions need to be enabled before they can react to input
        _moveAction.Enable();
        _turnAction.Enable();

        _originalPitch = movementAudio.pitch;
    }


    private void Update()
    {
        if (!isComputerControlled)
        {
            _movementInputValue = _moveAction.ReadValue<float>();
            _turnInputValue = _turnAction.ReadValue<float>();
        }

        EngineAudio();
    }


    /// <summary>
    /// Determines and plays the appropriate engine sound based on movement state.
    /// </summary>
    private void EngineAudio()
    {
        // No input (the tank is stationary)
        if (Mathf.Abs(_movementInputValue) < 0.1f && Mathf.Abs(_turnInputValue) < 0.1f)
        {
            if (movementAudio.clip == engineDriving)
            {
                movementAudio.clip = engineIdling;
                movementAudio.pitch = Random.Range(_originalPitch - pitchRange, _originalPitch + pitchRange);
                movementAudio.Play();
            }
        }
        else
        {
            if (movementAudio.clip == engineIdling)
            {
                movementAudio.clip = engineDriving;
                movementAudio.pitch = Random.Range(_originalPitch - pitchRange, _originalPitch + pitchRange);
                movementAudio.Play();
            }
        }
    }


    private void FixedUpdate()
    {
        if (isDirectControl)
        {
            var camForward = Camera.main.transform.forward;
            camForward.y = 0;
            camForward.Normalize();
            var camRight = Vector3.Cross(Vector3.up, camForward);

            // Creates a vector based on camera look (e.g. pressing up mean we want to go up in the direction of the
            // camera, not forward in the direction of the tank)
            _requestedDirection = camForward * _movementInputValue + camRight * _turnInputValue;
        }

        Move();
        Turn();
    }


    private void Move()
    {
        float speedInput;

        // In direct control mode, the speed will depend on how far from the desired direction we are.
        if (isDirectControl)
        {
            speedInput = _requestedDirection.magnitude;

            // If we are direct control, the speed of the move is based angle between current direction and the wanted
            // direction. If under 90, full speed, then speed reduced between 90 and 180
            speedInput *= 1.0f - Mathf.Clamp01((Vector3.Angle(_requestedDirection, transform.forward) - 90) / 90.0f);
        }
        else
        {
            speedInput = _movementInputValue;
        }

        Vector3 movement = transform.forward * speedInput * speed * Time.deltaTime;

        _rigidbody.MovePosition(_rigidbody.position + movement);
    }


    private void Turn()
    {
        Quaternion turnRotation;
        if (isDirectControl)
        {
            // Compute the rotation needed to reach the desired direction
            float angleTowardTarget = Vector3.SignedAngle(_requestedDirection, transform.forward, transform.up);
            var rotatingAngle = Mathf.Sign(angleTowardTarget) * Mathf.Min(Mathf.Abs(angleTowardTarget), turnSpeed * Time.deltaTime);
            turnRotation = Quaternion.AngleAxis(-rotatingAngle, Vector3.up);
        }
        else
        {
            float turn = _turnInputValue * turnSpeed * Time.deltaTime;

            turnRotation = Quaternion.Euler(0f, turn, 0f);
        }

        _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
    }
}