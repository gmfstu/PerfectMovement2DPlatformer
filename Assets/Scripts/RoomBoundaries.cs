using UnityEngine;

/// <summary>
/// A simple class that standardizes the room boundaries for the levels. Visually & logically simple. Super into this script.
/// </summary>
public class Room : MonoBehaviour
{
    /// <summary>
    /// The height of the room.
    /// </summary>
    [SerializeField] private float height;
    /// <summary>
    /// The width of the room.
    /// </summary>
    [SerializeField] private float width;
    /// <summary>
    /// The room that is above this room. No room means that if the player goes up, they will be killed.
    /// </summary>
    public Room up = null;
    /// <summary>
    /// The room that is below this room. No room means that if the player goes up, they will be killed.
    /// </summary>
    public Room down = null;
    /// <summary>
    /// The room that is to the left of this room. No room means that if the player goes up, they will be killed.
    /// </summary>
    public Room left = null;
    /// <summary>
    /// The room that is to the right of this room. No room means that if the player goes up, they will be killed.
    /// </summary>
    public Room right = null;
    /// <summary>
    /// The x value of the right boundary of the room.
    /// </summary>
    public float RightBoundary { get; private set; }
    /// <summary>
    /// The x value of the left boundary of the room.
    /// </summary>
    public float LeftBoundary { get; private set; }
    /// <summary>
    /// The y value of the up boundary of the room.
    /// </summary>
    public float UpBoundary { get; private set; }
    /// <summary>
    /// The y value of the down boundary of the room.
    /// </summary>
    public float DownBoundary { get; private set; }

    private void Start() {
        RightBoundary = transform.position.x + (width / 2f);
        LeftBoundary = transform.position.x - (width / 2f);
        UpBoundary = transform.position.y + (height / 2f);
        DownBoundary = transform.position.y - (height / 2f);
    }

    /// <summary>
    /// Draws the room boundaries, so they are easy to see when editing levels.
    /// </summary>
    private void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0));
    }
}
