using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    [Tooltip("The amount of health each tank starts with.")]
    public float startingHealth = 200f;

    [Tooltip("The slider to represent how much health the tank currently has.")]
    public Slider slider;

    [Tooltip("The image component of the slider.")]
    public Image fillImage;

    [Tooltip("The color the health bar will be when on full health.")]
    public Color fullHealthColor = Color.green;

    [Tooltip("The color the health bar will be when on no health.")]
    public Color zeroHealthColor = Color.red;

    [Tooltip("A prefab that will be instantiated in Awake, then used whenever the tank dies.")]
    public GameObject explosionPrefab;

    /// <summary>
    /// The audio source to play when the tank explodes.
    /// </summary>
    private AudioSource _explosionAudio;

    /// <summary>
    /// The particle system the will play when the tank is destroyed.
    /// </summary>
    private ParticleSystem _explosionParticles;

    /// <summary>
    /// How much health the tank currently has.
    /// </summary>
    private float _currentHealth;

    /// <summary>
    /// Has the tank been reduced beyond zero health yet?
    /// </summary>
    private bool _dead;

    private void Awake()
    {
        _explosionParticles = Instantiate(explosionPrefab).GetComponent<ParticleSystem>();
        _explosionAudio = _explosionParticles.GetComponent<AudioSource>();
        _explosionParticles.gameObject.SetActive(false);
        slider.maxValue = startingHealth;
    }

    private void OnDestroy()
    {
        if (_explosionParticles != null)
            Destroy(_explosionParticles.gameObject);
    }

    private void OnEnable()
    {
        // When the tank is enabled, reset the tank's health and whether or not it's dead.
        _currentHealth = startingHealth;
        _dead = false;

        SetHealthUI();
    }


    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        SetHealthUI();

        if (_currentHealth <= 0f && !_dead)
        {
            OnDeath();
        }
    }


    public void IncreaseHealth(float amount)
    {
        if (_currentHealth + amount <= startingHealth)
        {
            _currentHealth += amount;
        }
        else
        {
            _currentHealth = startingHealth;
        }

        SetHealthUI();
    }

    private void SetHealthUI()
    {
        slider.value = _currentHealth;

        fillImage.color = Color.Lerp(zeroHealthColor, fullHealthColor, _currentHealth / startingHealth);
    }


    private void OnDeath()
    {
        _dead = true;

        _explosionParticles.transform.position = transform.position;
        _explosionParticles.gameObject.SetActive(true);
        _explosionParticles.Play();
        _explosionAudio.Play();

        gameObject.SetActive(false);
    }
}