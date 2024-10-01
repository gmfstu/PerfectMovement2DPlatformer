using System.Collections;
using UnityEngine;

/// <summary>
/// This class is responsible for controlling the camera, following the player, switching between rooms, and a reasonable amount of 
/// the times the character can die in.
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary>
    /// The target that the camera will follow (always the character rn).
    /// </summary>
    public Transform Target;
    /// <summary>
    /// The dark screen that will cover the screen when the player dies.
    /// </summary>
    public Transform DarkScreen;
    /// <summary>
    /// The y position of the dark screen when the player dies.
    /// </summary>
    private float darkScreenStart;
    /// <summary>
    /// The direction that the screen is moving in. 0 is no movement, -1 is moving down, 1 is moving up.
    /// </summary>
    private float moveScreenDirection;
    /// <summary>
    /// The curve that will be used to shake the screen when the player dies.
    /// </summary>
    public AnimationCurve ScreenShakeCurve;
    /// <summary>
    /// The room that the camera is currently in.
    /// </summary>
    public Room CurrentRoom;
    /// <summary>
    /// The room that the player will respawn in.
    /// </summary>
    private Room respawnRoom;
    /// <summary>
    /// The time that the camera will take to follow the player.
    /// </summary>
    [SerializeField] private float followTime;
    /// <summary>
    /// The vertical size of the camera.
    /// </summary>
    [SerializeField] private float verticalSize;
    /// <summary>
    /// The horizontal size of the camera.
    /// </summary>
    [SerializeField] private float horizontalSize;
    /// <summary>
    /// The position that the camera will move towards.
    /// </summary>
    private Vector3 goTo;
    /// <summary>
    /// The field of view of the camera.
    /// </summary>
    private float fieldOfView = 60;
    /// <summary>
    /// Whether the screen is shaking.
    /// </summary>
    private bool screenIsShaking = false;
    /// <summary>
    /// Whether the death animation is playing.
    /// </summary>
    private bool deathAnimationPlaying = false;
    /// <summary>
    /// Whether the player is respawning.
    /// </summary>
    private bool respawning = false;

    private void OnEnable() {
        CharacterPhysics.OnPlayerDeath += Die;
    }

    private void OnDisable() {
        CharacterPhysics.OnPlayerDeath -= Die;
    }

    void Start()
    {
        fieldOfView = GetComponent<Camera>().GetGateFittedFieldOfView(); // TODO: who knows if this works how i want it to
        Debug.Log("Field of view: " + fieldOfView);
        float distance = verticalSize/(2 * Mathf.Tan(fieldOfView * Mathf.Deg2Rad / 2));
        Debug.Log("Distance: " + distance);
        transform.position = new Vector3(transform.position.x, transform.position.y, -distance);
        goTo = transform.position;
        darkScreenStart = DarkScreen.transform.position.y;
        moveScreenDirection = 0;
    }

    void Update()
    {
        if (!respawning) {
            CheckRoomSwitch();
        }

        // controls camera follow on character on x axis
        if (Target.position.x + (horizontalSize / 2f) < CurrentRoom.RightBoundary 
        && Target.position.x - (horizontalSize / 2f) > CurrentRoom.LeftBoundary) {
            if (Target.position.x != transform.position.x) {
                goTo = new Vector3(Target.position.x, goTo.y, goTo.z);
            }
        } 

        // controls camera follow on character on y axis
        if (Target.position.y + (verticalSize / 2f) < CurrentRoom.UpBoundary 
        && Target.position.y - (verticalSize / 2f) > CurrentRoom.DownBoundary) {
            if (Target.position.y != transform.position.y) {
                goTo = new Vector3(goTo.x, Target.position.y, goTo.z);
            }
        }

        // controls the right, left, up and down boundaries in order, when the camera is switching between rooms.
        if (transform.position.x + (horizontalSize / 2f) > CurrentRoom.RightBoundary) {
            goTo = new Vector3(CurrentRoom.RightBoundary - (horizontalSize / 2f), goTo.y, goTo.z);
        }
        if (transform.position.x - (horizontalSize / 2f) < CurrentRoom.LeftBoundary) {
            goTo = new Vector3(CurrentRoom.LeftBoundary + (horizontalSize / 2f), goTo.y, goTo.z);
        }
        if (transform.position.y + (verticalSize / 2f) > CurrentRoom.UpBoundary) {
            goTo = new Vector3(goTo.x, CurrentRoom.UpBoundary - (verticalSize / 2f), goTo.z);
        }
        if (transform.position.y - (verticalSize / 2f) < CurrentRoom.DownBoundary) {
            goTo = new Vector3(goTo.x, CurrentRoom.DownBoundary + (verticalSize / 2f), goTo.z);
        }

        if (Vector3.Magnitude(goTo - transform.position) < 0.05f) {
            transform.position = goTo;
        }
        
        // lerping in Update() is not linear, it exponentially slows down, because the distance shortens & it recalculates!
        // if you don't want this effect, there are lots of switches, mostly just make it actually linear by setting a timer.
        transform.position = Vector3.Lerp(transform.position, goTo, followTime * Time.deltaTime);


        if (!screenIsShaking && deathAnimationPlaying) {
            deathAnimationPlaying = false;
            moveScreenDirection = -1;
        }

        ScreenHandler();
    }

    /// <summary>
    /// Handles the screen movement when the player dies.
    /// </summary>
    private void ScreenHandler() {
        if (moveScreenDirection < 0) {
            float difference = transform.position.y - darkScreenStart;
            DarkScreen.position = new Vector3(transform.position.x, 
                Mathf.Max(DarkScreen.position.y + (difference * Time.deltaTime / 2), transform.position.y), 1);
            if (DarkScreen.position.y == transform.position.y) {
                // move camera & character
                moveScreenDirection = 1;
                transform.position = new Vector3(respawnRoom.transform.position.x, respawnRoom.transform.position.y, 
                                                 transform.position.z);
                Target.GetComponent<CharacterPhysics>().GoToRespawnPoint();
            }
        }

        if (moveScreenDirection > 0) {
            float difference = darkScreenStart - transform.position.y;
            DarkScreen.position = new Vector3(transform.position.x, 
                Mathf.Min(DarkScreen.position.y + (difference * Time.deltaTime / 2), darkScreenStart), 1);
            if (DarkScreen.position.y == darkScreenStart) {
                // finish coroutine & resume play
                Target.GetComponent<CharacterPhysics>().RespawnFinished();
                respawning = false;
                moveScreenDirection = 0;
            }
        }
    }

    /// <summary>
    /// Makes the camera shake when the player dies.
    /// </summary>
    public void Die() {
        StartCoroutine(ScreenShake(.2f));
        screenIsShaking = true;
        deathAnimationPlaying = true;
        respawning = true;
    }

    /// <summary>
    /// Checks if the player has moved to another room. Kills the player if they leave to a room with no room in that direction.
    /// </summary>
    private void CheckRoomSwitch() {
        if (Target.position.x > CurrentRoom.RightBoundary) {
            if (CurrentRoom.right == null) {
                Target.GetComponent<CharacterPhysics>().Die();
                respawning = true;
            } else {
                CurrentRoom = CurrentRoom.right;
            }
        } 
        if (Target.position.x < CurrentRoom.LeftBoundary) {
            if (CurrentRoom.left == null) {
                Target.GetComponent<CharacterPhysics>().Die();
                respawning = true;
            } else {
                CurrentRoom = CurrentRoom.left;
            }
        }
        if (Target.position.y > CurrentRoom.UpBoundary) {
            if (CurrentRoom.up == null) {
                Target.GetComponent<CharacterPhysics>().Die();
                respawning = true;
            } else {
                CurrentRoom = CurrentRoom.up;
            }
        }
        if (Target.position.y < CurrentRoom.DownBoundary) {
            if (CurrentRoom.down == null) {
                Target.GetComponent<CharacterPhysics>().Die();
                respawning = true;
            } else {
                CurrentRoom = CurrentRoom.down;
            }
        }
    }

    /// <summary>
    /// Sets the respawn room to the current room.
    /// </summary>
    public void SetRespawnRoom() {
        respawnRoom = CurrentRoom;
    }

    /// <summary>
    /// Makes the screen shake for a given duration.
    /// </summary>
    /// <param name="duration">Length of time the screen should shake for, before the black screen comes down.</param>
    /// <returns>An ienumerator that I don't use, just needs one because I'm using coroutines cus I'm so fancy & a great programmer.
    /// If you're reading this & considering hiring me, just do it. I use coroutines like they are no big deal. I didn't
    /// even watch a YouTube video for this! Just off the dome coroutines in places where they are simpler to use. Come on. Hire me.</returns>
    public IEnumerator ScreenShake(float duration) {
        Vector3 start_position = transform.position;
        float elapsed_time = 0f;
        while (elapsed_time < duration) {
            elapsed_time += Time.deltaTime;
            float intensity = ScreenShakeCurve.Evaluate(elapsed_time / duration); 
            transform.position = start_position + (Random.insideUnitSphere * intensity);
            yield return null;
        }

        transform.position = start_position;
        screenIsShaking = false;
    }
}
