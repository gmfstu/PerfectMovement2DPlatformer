using UnityEngine;

/// <summary>
/// A super small script that makes the player move along with the moving platform by setting the platform to the character's parent. 
/// This is totally fine for now, but might at least need more functionality if I want to standarize momentum jumps across all moving things
/// based on speed.
/// </summary>
public class MovingPlatformCollider : MonoBehaviour
{
    /// <summary>
    /// When the player collides with the moving platform, it becomes a child of the platform.
    /// </summary>
    /// <param name="other"></param>
    private void OnCollisionEnter2D(Collision2D other) {
        Debug.Log("Collision");
         if (other.collider != null) {
            Debug.Log("Collision with " + other.collider.tag);
            if (other.collider.tag == "Player") {
                Debug.Log("Player collision");
                other.collider.transform.parent = transform;
            }
        }
    }

    /// <summary>
    /// When the player stops colliding with the moving platform, it stops being a child of the platform.
    /// </summary>
    /// <param name="other"></param>
    private void OnCollisionExit2D(Collision2D other) {
        if (other.collider != null) {
            if (other.collider.tag == "Player") {
                other.collider.transform.parent = null;
            }
        }
    }
}