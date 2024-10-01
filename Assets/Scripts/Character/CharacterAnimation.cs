using UnityEngine;

// I included two different ways to control animation in this script--from this script by testing values, which is
// less cluttered but more difficult (that's how dust is done), or by calling functions in this script from
// the physics script (a bit easier if more cluttered). Wanted to have more options!

/// <summary>
/// Controls the character's animations and particle effects.
/// </summary>
public class CharacterAnimation : MonoBehaviour
{
    /// <summary>
    /// The character's physics script.
    /// </summary>
    public CharacterPhysics character;
    /// <summary>
    /// The particle system for the dust effect.
    /// </summary>
    public ParticleSystem dust;
    /// <summary>
    /// The hat game object. (This is a placeholder for now.)
    /// </summary>
    public GameObject hat;
    /// <summary>
    /// The character's still position. (This is vestigial, but probably will be useful for hat-esque animations.)
    /// </summary>
    private Vector3 stillPosition;
    /// <summary>
    /// The character's previous input. Used to determine if the character has changed direction.
    /// </summary>
    private Vector2 previousInput;

    void FixedUpdate()
    {
        if (Mathf.Sign(character.horizontal) == -1) {
            dust.transform.rotation = new Quaternion(0, 0, 180, 1);
        } else if (character.horizontal != 0) {
            dust.transform.rotation = new Quaternion(0, 0, 0, 1);
        }

        if (character.CurrentState == character.StateGrounded) {
            if (Mathf.Sign(previousInput.x) != Mathf.Sign(character.horizontal)) {
                dust.Play();
            }
            if (character.horizontal != 0 && previousInput.x == 0) {
                dust.Play();
            }
        }

        previousInput = new Vector2(character.horizontal, character.vertical);
    }

    /// <summary>
    /// Sets the game objects of the sprites this animation script controls to active or inactive.
    /// </summary>
    /// <param name="hide"> True if the sprites should be active, just like SetActive </param>
    public void SetSpritesActive(bool hide) {
        hat.SetActive(hide);
    }
}
