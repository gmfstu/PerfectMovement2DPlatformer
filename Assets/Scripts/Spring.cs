using UnityEngine;

/// <summary>
/// Simple class that stores the specific physics values for each specific spring, and checks if the character touches the spring.
/// </summary>
public class Spring : MonoBehaviour
{
    /// <summary>
    /// The force & direction that the spring will launch the character with.
    /// </summary>
    [SerializeField] private Vector2 launchForce = new(0, 10);
    /// <summary>
    /// The force modifier that will be applied to the character's jump force when they jump off the spring.
    /// </summary>
    [SerializeField] private float springJumpForceModifier = .5f;
    /// <summary>
    /// The gravity modifier that will be applied to the character's gravity when they jump off the spring, until they reach the apex of the jump.
    /// </summary>
    [SerializeField] private float springGravityModifier = 1.5f;
    /// <summary>
    /// The audiosource that plays the spring noise!
    /// </summary>
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider != null)
        {
            if (other.collider.tag == "Player")
            {
                other.collider.GetComponent<CharacterPhysics>().Spring(launchForce, springJumpForceModifier, springGravityModifier);
                audioSource.Play(); // could switch this sound to the character so you can control volume based on the jump
            }
        }
    }
}
