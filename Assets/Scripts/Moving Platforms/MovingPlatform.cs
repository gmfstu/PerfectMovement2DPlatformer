using UnityEngine;

// NOTES:
// This script will change if I implement the Maddy Thorson MoveX/Y carrying thing, though at this point I will probably make that
// a whole seperate project one day. 
// I will probably update this one day so that there can be more than 2 points that the platforms can move to, but make it just as 
// readable & visible as this one.

/// <summary>
/// This class is responsible for moving the platform from one point to another. 
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    /// <summary>
    /// The platform that will be moved.
    /// </summary>
    public GameObject Platform;
    /// <summary>
    /// The beginning point of the platform.
    /// </summary>
    public Transform Begin; // can make it go to multiple spots by making this an array, use the video still open
    /// <summary>
    /// The end point of the platform.
    /// </summary>
    public Transform End; 
    /// <summary>
    /// The speed at which the platform will move.
    /// </summary>
    public float Speed;
    /// <summary>
    /// The target that the platform is moving towards. (Switches between the End and Begin points).
    /// </summary>
    private Vector3 target;
    /// <summary>
    /// Whether the platform is going towards the end point or the beginning point.
    /// </summary>
    private bool goingTowardsEnd = true;

    void Start()
    {
        target = End.position;
    }

    /// <summary>
    /// Draws the lines between the start & end position, to make the platform's path visible.
    /// </summary>
    private void OnDrawGizmos() {
        if (Begin != null && End != null && Platform.transform != null) {
            Gizmos.DrawLine(Platform.transform.position, End.position);
            Gizmos.DrawLine(Platform.transform.position, Begin.position);
        }
    }

    /// <summary>
    /// Moves the platform towards the target.
    /// </summary>
    void FixedUpdate()
    {
        Platform.transform.position = Vector3.MoveTowards(Platform.transform.position, target, Speed * Time.deltaTime);

        if (Vector3.Distance(Platform.transform.position, target) < 0.05) {
            target = GetNextTarget(); 
        }
    }

    /// <summary>
    /// Returns the next target that the platform will move towards. Called when the platform reaches the target, & needs to switch.
    /// </summary>
    /// <returns>The position of the point that the platform just came from.</returns>
    private Vector3 GetNextTarget() {
        if (goingTowardsEnd) {
             goingTowardsEnd = false;
             return Begin.position;
        } else {
            goingTowardsEnd = true;
            return End.position;
        }
    }
}
