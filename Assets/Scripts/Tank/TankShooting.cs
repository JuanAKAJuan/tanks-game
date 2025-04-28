using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TankShooting : MonoBehaviour
{
    [Tooltip("Prefab of the shell.")]
    public Rigidbody shell;

    [Tooltip("A child of the tank where the shells are spawned.")]
    public Transform fireTransform;

    [Tooltip("A child of the tank that displays the current launch force.")]
    public Slider aimSlider;

    [Tooltip("Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.")]
    public AudioSource shootingAudio;

    [Tooltip("Audio that plays when each shot is charging up.")]
    public AudioClip chargingClip;

    [Tooltip("Audio that plays when each shot is fired.")]
    public AudioClip fireClip;

    [Tooltip("The speed in unit/second the shell have when fired at minimum charge")]
    public float minLaunchForce = 5f;

    [Tooltip("The speed in unit/second the shell have when fired at max charge")]
    public float maxLaunchForce = 20f;

    [Tooltip("The maximum time spent charging. When charging reach that time, the shell is fired at MaxLaunchForce")]
    public float maxChargeTime = 0.75f;

    [Tooltip("The time that must pass before being able to shoot again after a shot")]
    public float shotCooldown = 1.0f;

    [Header("Shell Properties")]
    [Tooltip("The amount of health removed to a tank if they are exactly on the landing spot of a shell")]
    public float maxDamage = 100f;

    [Tooltip("The force of the explosion at the shell position. It is in newton, so it need to be high, so keep it 500 and above")]
    public float explosionForce = 1000f;

    [Tooltip("The radius of the explosion in Unity unit. Force decrease with distance to the center, and an tank further than this from the shell explosion won't be impacted by the explosion")]
    public float explosionRadius = 5f;

    [HideInInspector]
    public TankInputUser inputUser;

    [Tooltip("The charging amount between 0-1")]
    public float CurrentChargeRatio =>
        (_currentLaunchForce - minLaunchForce) / (maxLaunchForce - minLaunchForce);

    public bool IsCharging => _isCharging;

    public bool IsComputerControlled { get; set; } = false;

    /// <summary>
    /// The input axis that is used for launching shells.
    /// </summary>
    private string _fireButton;

    /// <summary>
    /// The force that will be given to the shell when the fire button is released.
    /// </summary>
    private float _currentLaunchForce;

    /// <summary>
    /// How fast the launch force increases, based on the max charge time.
    /// </summary>
    private float _chargeSpeed;

    /// <summary>
    /// Whether or not the shell has been launched with this button press.
    /// </summary>
    private bool _fired;

    /// <summary>
    /// The Input Action for shooting, retrieve from TankInputUser.
    /// </summary>
    private InputAction _fireAction;

    /// <summary>
    /// Are we currently charging the shot?
    /// </summary>
    private bool _isCharging = false;

    /// <summary>
    /// The initial value of m_MinLaunchForce
    /// </summary>
    private float _baseMinLaunchForce;

    /// <summary>
    /// The timer counting down before a shot is allowed again
    /// </summary>
    private float _shotCooldownTimer;

    private void OnEnable()
    {
        _currentLaunchForce = minLaunchForce;
        _baseMinLaunchForce = minLaunchForce;
        aimSlider.value = _baseMinLaunchForce;

        aimSlider.minValue = minLaunchForce;
        aimSlider.maxValue = maxLaunchForce;
    }

    private void Awake()
    {
        inputUser = GetComponent<TankInputUser>();
        if (inputUser == null)
            inputUser = gameObject.AddComponent<TankInputUser>();
    }

    private void Start()
    {
        // The fire axis is based on the player number.
        _fireButton = "Fire";
        _fireAction = inputUser.ActionAsset.FindAction(_fireButton);

        _fireAction.Enable();

        // The rate that the launch force charges up is the range of possible forces by the max charge time.
        _chargeSpeed = (maxLaunchForce - minLaunchForce) / maxChargeTime;
    }


    private void Update()
    {
        if (!IsComputerControlled)
        {
            HumanUpdate();
        }
        else
        {
            ComputerUpdate();
        }
    }

    /// <summary>
    /// Used by AI to start charging.
    /// </summary>
    public void StartCharging()
    {
        _isCharging = true;
        _fired = false;
        _currentLaunchForce = minLaunchForce;

        shootingAudio.clip = chargingClip;
        shootingAudio.Play();
    }

    public void StopCharging()
    {
        if (_isCharging)
        {
            Fire();
            _isCharging = false;
        }
    }

    void ComputerUpdate()
    {
        aimSlider.value = _baseMinLaunchForce;

        if (_currentLaunchForce >= maxLaunchForce && !_fired)
        {
            _currentLaunchForce = maxLaunchForce;
            Fire();
        }
        else if (_isCharging && !_fired)
        {
            _currentLaunchForce += _chargeSpeed * Time.deltaTime;

            aimSlider.value = _currentLaunchForce;
        }
        else if (_fireAction.WasReleasedThisFrame() && !_fired)
        {
            Fire();
            _isCharging = false;
        }
    }

    void HumanUpdate()
    {
        if (_shotCooldownTimer > 0.0f)
        {
            _shotCooldownTimer -= Time.deltaTime;
        }

        aimSlider.value = _baseMinLaunchForce;

        if (_currentLaunchForce >= maxLaunchForce && !_fired)
        {
            _currentLaunchForce = maxLaunchForce;
            Fire();
        }
        else if (_shotCooldownTimer <= 0 && _fireAction.WasPressedThisFrame())
        {
            _fired = false;
            _currentLaunchForce = minLaunchForce;

            shootingAudio.clip = chargingClip;
            shootingAudio.Play();
        }
        else if (_fireAction.IsPressed() && !_fired)
        {
            _currentLaunchForce += _chargeSpeed * Time.deltaTime;

            aimSlider.value = _currentLaunchForce;
        }
        else if (_fireAction.WasReleasedThisFrame() && !_fired)
        {
            Fire();
        }
    }


    private void Fire()
    {
        // Set the fired flag so only Fire is only called once.
        _fired = true;

        Rigidbody shellInstance =
            Instantiate(shell, fireTransform.position, fireTransform.rotation) as Rigidbody;

        // Set the shell's velocity to the launch force in the fire position's forward direction.
        shellInstance.linearVelocity = _currentLaunchForce * fireTransform.forward;

        ShellExplosion explosionData = shellInstance.GetComponent<ShellExplosion>();
        explosionData.m_ExplosionForce = explosionForce;
        explosionData.m_ExplosionRadius = explosionRadius;
        explosionData.m_MaxDamage = maxDamage;

        shootingAudio.clip = fireClip;
        shootingAudio.Play();

        _currentLaunchForce = minLaunchForce;

        _shotCooldownTimer = shotCooldown;
    }


    /// <summary>
    /// Return the estyimated position the projectile will have with the charging level (between 0 & 1)
    /// </summary>
    /// <param name="chargingLevel">The fire charging level between 0 - 1</param>
    /// <returns>The position at which the projectile will be (ignore obstacle)</returns>
    public Vector3 GetProjectilePosition(float chargingLevel)
    {
        float chargeLevel = Mathf.Lerp(minLaunchForce, maxLaunchForce, chargingLevel);
        Vector3 velocity = chargeLevel * fireTransform.forward;

        float a = 0.5f * Physics.gravity.y;
        float b = velocity.y;
        float c = fireTransform.position.y;

        float sqrtContent = b * b - 4 * a * c;
        if (sqrtContent <= 0)
        {
            return fireTransform.position;
        }

        float answer1 = (-b + Mathf.Sqrt(sqrtContent)) / (2 * a);
        float answer2 = (-b - Mathf.Sqrt(sqrtContent)) / (2 * a);

        float answer = answer1 > 0 ? answer1 : answer2;

        Vector3 position = fireTransform.position +
                           new Vector3(velocity.x, 0, velocity.z) *
                           answer;
        position.y = 0;

        return position;
    }
}