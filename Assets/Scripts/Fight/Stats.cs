using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parent class for player and opponent stats
/// </summary>
public class Stats : MonoBehaviour
{
    public int lifePoints;
    public int shieldCount;
    public int shootDamage;
    public int loadCapacity;
    public float dodgeSuccessRate;
    public float disarmSuccessRate;

    [HideInInspector] public bool isDodging = false;
    [HideInInspector] public bool inFight = false;
    [HideInInspector] public bool isShielding = false;
    [HideInInspector] public int loadCount;

    private float dodgeDistance = 70f;
    private float dodgeSpeed = 50f;
    private float dodgeDirection = 1.0f;
    private Vector3 initialPosition;


    public GameObject shield;
    public GameObject projectilePrefab;
    public Transform opponent;

    private AudioSource audioSource;
    private AudioClip shootSound;
    private AudioClip reloadSound;
    private AudioClip disarmSound;
    private ShakeEffect shakeEffect;
    // Start is called before the first frame update
    void Start()
    {
        if (shield != null) shield.SetActive(false);
        audioSource = GetComponentInChildren<AudioSource>();
        shootSound = Resources.Load<AudioClip>("Audio/shoot");
        reloadSound = Resources.Load<AudioClip>("Audio/reload");
        disarmSound = Resources.Load<AudioClip>("Audio/disarm");
        shakeEffect = GetComponent<ShakeEffect>();
    }

    // Update is called once per frame
    void Update()
    {
        if (inFight && (isDodging || initialPosition != transform.position))
        {
            Dodge();
        }
    }

    public void startFight()
    {
        inFight = true;
        initialPosition = transform.position;
        LoadGameSettings();
    }

    public virtual void LoadGameSettings()
    {
        Debug.Log("Called LoadGameSettings()");
    }

    /// <summary>
    /// Adds damage to this gameobject's life points.
    /// </summary>
    /// <param name="damage"> How much damage to give. </param>
    public void TakeDamage(int damage)
    {
        lifePoints -= damage;
        if (lifePoints < 0)
            lifePoints = 0;
    }

    public void Shoot(bool hit)
    {
        GameObject projectile = Instantiate(projectilePrefab, gameObject.transform.position, Quaternion.identity);

        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            Debug.Log(opponent);
            Debug.Log(opponent.localPosition.x + " " + opponent.localPosition.y + " " + opponent.localPosition.z);
            Debug.Log(opponent.position.x + " " + opponent.position.y + " " + opponent.position.z);
            Debug.Log("xxxxxxxxxxxxxxxx");
            projScript.SetTarget(opponent, hit);
        }
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }
    public void Dodge()
    {
        if (isDodging)
        {
            Vector3 dodgePosition = initialPosition + new Vector3(dodgeDistance * dodgeDirection, 0, 0);

            transform.position = Vector3.Lerp(transform.position, dodgePosition, dodgeSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, initialPosition, dodgeSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// If gamobject has shields it will be used and the flag set.
    /// </summary>
    /// <remarks>Dont forget to ResetShield() after round</remarks>
    public void UseShield()
    {
        if (shieldCount > 0)
        {
            shieldCount--;
            isShielding = true;
            shield.SetActive(true);
            shield.GetComponent<ShieldCollision>().Activate();
        }
        else
        {
            isShielding = false;
        }
    }

    /// <summary>
    /// Adds one round to the gun until max is reached.
    /// </summary>
    public void IncreaseLoad()
    {
        if (loadCount < loadCapacity)
        {
            loadCount++;
            if (audioSource != null && reloadSound != null)
                audioSource.PlayOneShot(reloadSound);
        }
    }

    /// <summary>
    /// Resets loads to 0. = gets disarmed
    /// </summary>
    public void ResetLoad()
    {
        loadCount = 0;

        if (audioSource != null && disarmSound != null)
            audioSource.PlayOneShot(disarmSound);
        shakeEffect.Shake();
    }

    /// <summary>
    /// Undoges the player
    /// </summary>
    public void ResetDodge()
    {
        isDodging = false;
        dodgeDirection = Random.value > 0.5f ? 1.0f : -1.0f;
    }

    /// <summary>
    /// Removes the shield from the player.
    /// </summary>
    /// <param name="deactivateSound"> If true, the deactivate sound will be played. Set to false if the other action is shoot because the sound comes from projectile and the blink effect deactivates the shield </param>
    public void ResetShield(bool deactivateSound = true)
    {
        isShielding = false;
        if (deactivateSound)
            shield.GetComponent<ShieldCollision>().Deactivate();
    }
}
