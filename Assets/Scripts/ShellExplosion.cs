using UnityEngine;

public class ShellExplosion : MonoBehaviour
{
    [Tooltip("Used to filter what the explosion affects, this should be set to \"Players\".")]
    public LayerMask m_TankMask;

    [Tooltip("Reference to the particles that will play on explosion.")]
    public ParticleSystem m_ExplosionParticles;

    [Tooltip("Reference to the audio that will play on explosion.")]
    public AudioSource m_ExplosionAudio;

    /// <summary>
    /// The time in seconds before the shell is removed.
    /// </summary>
    [HideInInspector] public float m_MaxLifeTime = 2f;

    /// <summary>
    /// The amount of damage done if the explosion is centred on a tank.
    /// </summary>
    [HideInInspector] public float m_MaxDamage = 100f;

    /// <summary>
    /// The amount of force added to a tank at the centre of the explosion.
    /// </summary>
    [HideInInspector] public float m_ExplosionForce = 1000f;

    /// <summary>
    /// The maximum distance away from the explosion tanks can be and are still affected.
    /// </summary>
    [HideInInspector] public float m_ExplosionRadius = 5f;


    private void Start()
    {
        Destroy(gameObject, m_MaxLifeTime);
    }


    private void OnTriggerEnter(Collider other)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

        for (int i = 0; i < colliders.Length; i++)
        {
            Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();

            if (!targetRigidbody)
                continue;

            targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

            TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();

            if (!targetHealth)
                continue;

            float damage = CalculateDamage(targetRigidbody.position);

            targetHealth.TakeDamage(damage);
        }

        m_ExplosionParticles.transform.parent = null;
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();

        ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;

        Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
        Destroy(gameObject);
    }


    private float CalculateDamage(Vector3 targetPosition)
    {
        Vector3 explosionToTarget = targetPosition - transform.position;
        float explosionDistance = explosionToTarget.magnitude;
        float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
        float damage = relativeDistance * m_MaxDamage;

        damage = Mathf.Max(0f, damage);

        return damage;
    }
}