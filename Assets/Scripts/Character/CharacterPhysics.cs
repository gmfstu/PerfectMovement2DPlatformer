using UnityEngine;
using UnityEngine.InputSystem;
using System;

// TODO: Future projects to add to this template:
// - Add a state for climbing, dashing, and idle, depending on if I need those for future projects
// - Rework the base movement away from RigidBody2D velocity stuff, and recreate the Celeste MoveX & MoveY system.
//       - (or at least, make momentum carrying interactions, switching between moves, and other unique transitions
//         consistent and easy to code! Would be great to have hyperjumps, wavedashes, and cancels working out of the box).
// - Rework the jump buffer, so there is a buffer system for all inputs in the game! Something I'd have to learn best practice
//   for, but I'm sure isn't too hard.

/// <summary>
/// This class is the main physics controller for the character. Based on a Unity-ized and simplified version of Maddy Thorson's
/// Celeste, this controls all physics and movement, but also a bit of animation and sound through the inputs it receives.
/// </summary>
public class CharacterPhysics : MonoBehaviour
{
    #region Constants
    /// <summary>
    /// The base speed at which the character runs (grounded).
    /// </summary>
    [SerializeField] private float moveSpeed = 5f;
    /// <summary>
    /// The force at which the character jumps (force is currently set, not added).
    /// </summary>
    [SerializeField] private float jumpForce = 15f;
    /// <summary>
    /// The base force of gravity that the character experiences (doesn't use deltaTime, like most of these values).
    /// </summary>
    [SerializeField] private float baseGravity = .75f;
    /// <summary>
    /// The mutiplier to baseGravity that the character experiences when falling.
    /// </summary>
    [SerializeField] private float fallingGravityModifier = 2f;
    /// <summary>
    /// The mutiplier to baseGravity that the character experiences when launched off a spring.
    /// </summary>
    private float springGravityModifier = 1.5f; // technically this isn't super constant... whoops! 
    /// <summary>
    /// The mutiplier to baseGravity that the character experiences when fast falling (holding down). This is additional to other grav modifiers.
    /// </summary>
    [SerializeField] private float fastFallingGravityModifier = 1.5f;
    /// <summary>
    /// The base terminal velocity the character can fall at.
    /// </summary>
    [SerializeField] private float baseTerminalVelocity = -15f;
    /// <summary>
    /// The terminal velocity the character can fall at when fast falling (holding down).
    /// </summary>
    [SerializeField] private float fastTerminalVelocity = -20f;
    /// <summary>
    /// The terminal velocity the character can fall at when sliding down a wall.
    /// </summary>
    [SerializeField] private float wallTerminalVelocity = -3f;
    /// <summary>
    /// The time the player must hold down jump to get a full jump (grounded). If jump is released after this, the jump is not shortened, 
    /// even if the character is still rising--emulates jump squats from fighting games, without the actual grounded time.
    /// </summary>
    [SerializeField] private float fullHopTime = .35f;
    /// <summary>
    /// The amount that the character's jump is shortened if the jump button is released before fullHopTime.
    /// </summary>
    [SerializeField] private float shortHopModifier = .5f;

    // 
    /// <summary>
    /// The rate at which the character accelerates on the ground. This value & deceleration are the T in the Lerp function from the current x 
    /// velocity to the desired velocity when grounded. 
    /// </summary>
    [SerializeField] private float acceleration = .15f;
    /// <summary>
    /// The rate at which the character decelerates on the ground. This value & acceleration are the T in the Lerp function from the current x 
    /// velocity to the desired velocity when grounded. 
    /// </summary>
    [SerializeField] private float deceleration = .05f;
    /// <summary>
    /// The rate at which the character accelerates in the air. this is more of a modifier--this is multiplied by the values above, i.e. 0.5 
    /// allows for half the acceleration/control in the air.
    /// </summary>
    [SerializeField] private float baseAirFriction = 0.5f;
    /// <summary>
    /// The time the player has after leaving the ground to still jump. I prefer a higher coyote time, because I don't make super technical/
    /// intense platformers, but instead platformers more about creativity, problem solving, and freedom of movement.
    /// </summary>
    [SerializeField] private float coyoteTime = 0.2f;
    /// <summary>
    /// The time the player has after pressing jump to still jump. 
    /// </summary>
    [SerializeField] private float jumpBufferTime = 0.2f;
    /// <summary>
    /// The time the player has after jumping to not be able to jump again. This is to prevent spamming the jump button, when the character
    /// has jumped but the TimeSinceGrounded is still 0 as the overlap box is still touching the ground. Would love a way to do this in a more 
    /// clean way, without using OnCollision2D, which is permanently my enemy.
    /// </summary>
    [SerializeField] private float jumpLockoutTime = 0.3f;
    /// <summary>
    /// The range for all edge detection, mostly used for the edge rounding raycasts to make the game feel more forgiving & fun. Also used for
    /// the bottom of the character, the top of the character, and the left and right sides for wall/ground detection, in some places. 
    /// </summary>
    [SerializeField] private float edgeDetectionRange = .125f; // this means the 1/8th of the character is "edge"
    /// <summary>
    /// The distance the character is from the ground when grounded. This is used for the grounded box, and is a bit smaller than the edge
    /// detection range.
    /// </summary>
    private float box_distance = .5f;
    /// <summary>
    /// Little bit of distance used to make detection boxes work, & not overlap with the character. Looked nicer to have this in constants 
    /// instead of just a float in the confusing, long Gizmo code, because it is based on the character's size.
    /// </summary>
    private float box_y_size = .01f;
    /// <summary>
    /// The time the player floats/gets a tiny speed boost while at the apex of the jump. Makes the jump feel so much better, even though
    /// its such a small feature. IMO this should be the mechanic that people rave about the way they talk about coyote time or jump buffer.
    /// Maybe I'll make a click bait YouTube short about it...
    /// </summary>
    [SerializeField] private float apexModifierDuration = .1f;
    /// <summary>
    /// The gravity modifier at the apex of the jump. This is a multiplier to baseGravity, so 1 is normal gravity, 0.5 is half gravity, etc.
    /// </summary>
    [SerializeField] private float apexModifierGravity = .9f;
    /// <summary>
    /// The speed boost at the apex of the jump. This is a multiplier to the character's speed, so 1 is normal speed, 1.5 is 1.5x speed, etc.
    /// </summary>
    [SerializeField] private float apexModifierSpeed = 1.1f;
    /// <summary>
    /// The jump force boost the character gets when wall jumping. This is a multiplier to the character's jump force, so 1 is normal 
    /// speed, 1.5 is 1.5x
    /// </summary>
    [SerializeField] private float wallJumpForceMultiplier = .8f;
    /// <summary>
    /// The speed boost the character gets when wall jumping. This is a multiplier to the character's speed, so 1 is normal speed, 1.5 is 1.5x
    /// speed, etc.
    /// </summary>
    [SerializeField] private float wallJumpSpeedBoost = 1.2f;
    /// <summary>
    /// The lag time before the player regains air control after walljumping. Prevents infinite scaling of walljumps, and makes the walljump 
    /// feel better/easier. 
    /// </summary>
    [SerializeField] private float wallJumpLag = .8f;
    /// <summary>
    /// This returns all the characters air mobility after the wallJumpLag period, instead of linearly during the lag. Won't feel like HK until
    /// you crank the acceleration, deceleration, and airFriction up to 11.
    /// </summary>
    [SerializeField] private bool hollowKnightWallJump = false;
    /// <summary>
    /// This removes the effect where walljumping adds to your vertical momentum, letting you jump higher.
    /// </summary>
    [SerializeField] private bool limitWallJumpVertical = false;
    /// <summary>
    /// This adds the effect where you will not slide off a platform unless you are holding that direction, & not holding down (kind of like 
    /// crouching on ledges like Minecraft). Don't know why more games don't do this, afaik I kind of invented this for platformers! Obviously
    /// Smash Ultimate does this, but that game loves to make you stick to the ground so :/.
    /// </summary>
    [SerializeField] private bool stickyPlatforms = true;
    #endregion

    #region Vars
    /// <summary>
    /// The actual value used as gravity, which is modified by the gravity modifiers. 
    /// </summary>
    private float gravity = 0;
    /// <summary>
    /// The horizontal input of the player, gotten from the new Unity input system.
    /// </summary>
    internal float horizontal = 0;
    /// <summary>
    /// The vertical input of the player, gotten from the new Unity input system.
    /// </summary>
    internal float vertical = 0;
    /// <summary>
    /// The last velocity the character had in the y direction. Used to determine if the character has reached the apex of the jump.
    /// </summary>
    private float previous_velocity_y = 0;
    /// <summary>
    /// The direction of the last wall the character jumped off of. Used to determine if the character can walljump off the same wall.
    /// Set to 0 when the character is grounded, -1 for the left wall, 1 for the right.
    /// </summary>
    private float previous_wall = 0;
    /// <summary>
    /// The air friction modifier, actually used to modify acceleration/deceleration.
    /// </summary>
    private float airFriction;
    /// <summary>
    /// Whether or not the character is currently thrown in the air because of a spring--I considered making this a state, but felt like 
    /// that would feel clunky and unnecessary at best, and would be overly complicated code at worst. One of my pet peeves in platformers is
    /// when you can tell a game is overly coded, and there are clear seems in how the game runs & feels.
    /// </summary>
    private bool springLaunched = false;
    /// <summary>
    /// Whether or not the character is pressing the jump button. This, unlike phantomJUmpPressed, is "used" when the character jumps. Set to
    /// false when the player releases the jump button, or when the character uses the jump input (any jump/walljump/cancel).
    /// </summary>
    private bool jumpPressed = false;
    /// <summary>
    /// Whether or not the character has released the jump button. This is used to determine if the character should large jump off the spring, 
    /// or other times where the character should have a jump effect by holding rather than pressing. (I guess I could name this jump held, but
    /// I want it to be clear that jumpPressed is the way inputs are handeled in this code, and this is an exception for edge cases).
    /// </summary>
    private bool phantomJumpPressed = false;
    /// <summary>
    /// Whether or not the character has released the jump button. This is used to determine if the character should cancel their jump, or
    /// if the jump buffer should be set, and things of that timed nature.
    /// </summary>
    private bool jumpReleased = false;
    /// <summary>
    /// Whether or not the character is dead. This is used to stop the character from moving, and to play the death animation.
    /// </summary>
    public bool dead { get; private set; } = false;
    /// <summary>
    /// Whether or not the game is paused. This is used to stop the character from moving, and to activate pause menus.
    /// </summary>
    public bool paused { get; private set; } = false;
    /// <summary>
    /// The respawn point of the character--set by moving through the (currently green) respawn trigger boxes. The character will respawn 
    /// at the transform of the gameObject, as the respawn boxes have no script, and only a non-trigger collider.
    /// </summary>
    public Vector3 RespawnPoint;
    #endregion

    // In order to make the state machine used primarily, no timers are internal (how I usually do it), or even have public get; methods.
    // All other scripts (like moving platforms, sound & animation) are more readable & better because they reference the state machine, and
    // I'm sure it will be easier to expand on that system when adding more states (for climbing, dashing, hookshot, float, glide, etc).
    #region Timers
    /// <summary>
    /// The time since the character was grounded (character is grounded if value == 0). Used to determine if the character is grounded, 
    /// and to set the state of the character. I decided to remove isGrounded() and bool grounded, and combine the functionality of both 
    /// into this timer.
    /// </summary>
    private float TimeSinceGrounded = 0;
    /// <summary>
    /// The time since the character was touching the left wall. On left wall if value == 0. Considered combining left & right into one 
    /// timer, but this was easier frankly, and it isn't very cluttered.
    /// </summary>
    private float TimeSinceLeftWall = 0;
    /// <summary>
    /// The time since the character was touching the right wall. On right wall if value == 0.
    /// </summary>
    private float TimeSinceRightWall = 0;
    /// <summary>
    /// The time since the character jumped. Character just jumped if time == 0, but this is less consistent than grounded, because the timer 
    /// is set to 0 when the character jumps, and so HadleStates and HandleMovement, and the first bit of HandleJump never see this value 
    /// being 0.
    /// </summary>
    private float TimeSinceJumped = 0; // i thought about making this & the jump buffer one timer, but there is 0 reason to do that, & this is more readable
    /// <summary>
    /// The time since the character pressed the jump button. Used to activate the jump buffer. Set to 1 + jumpBufferTime when jumping to 
    /// make sure buffer doesn't cause double jump glitches.
    /// </summary>
    private float TimeSinceJumpBuffered = 0;
    /// <summary>
    /// The time since the character reached the apex of the jump. Used to give the character a speed boost at the apex of the jump.
    /// </summary>
    private float TimeSinceApexReached = 0;
    /// <summary>
    /// The time since the character walljumped. Used to give the character a speed boost after walljumping, and to limit the character's
    /// air mobility according to the length of walljumpLag.
    /// </summary>
    private float TimeSinceWallJumped = 0;
    #endregion

    #region Components
    /// <summary>
    /// The layer that the ground and wall objects are on, used for collision detection.
    /// </summary>
    public LayerMask groundLayer;
    /// <summary>
    /// The layer that the respawn trigger boxes are on, used for collision detection.
    /// </summary>
    public LayerMask respawnLayer;
    /// <summary>
    /// The camera controller, right now only used once to tell the camera when a new respawn box is entered.
    /// </summary>
    public CameraController CameraController;
    /// <summary>
    /// The rigidbody attached to the character, used for all sorts of stuff! Potentially will remove this one day for a more tilebased 
    /// custom physics engine like my hero Maddy Thorson :D.
    /// </summary>
    private Rigidbody2D rb;
    /// <summary>
    /// The sprite renderer attached to the character, used to change the color of the character when they die. Most other sprite changes
    /// are done in the CharacterAnimation script.
    /// </summary>
    private SpriteRenderer sr;
    /// <summary>
    /// The trail renderer attached to the character. Used to show the character's path.
    /// </summary>
    private TrailRenderer tr;
    /// <summary>
    /// The sound controller attached to the character's children. Used to play sounds when the character jumps, lands, and other things.
    /// </summary>
    public CharacterSound sound;
    /// <summary>
    /// The animation controller attached to the character's children. Used to play animations when the character jumps, lands, and other things.
    /// </summary>
    private CharacterAnimation animator;
    /// <summary>
    /// The pause menu. Only exists as a reference here because it was easy to put another button function in this script, but will probably
    /// switch the Pause(callback) and Unpause(callback) methods to the pause menu script.
    /// </summary>
    public PauseMenu pauseMenu;

    #endregion

    #region States
    /// <summary>
    /// The current state of the character. Used to determine what the character can do, and to update the character's state.
    /// </summary>
    public ICharacterState CurrentState { get; private set; }
    /// <summary>
    /// The grounded state of the character. Used to be StateIdle and StateRunning, but are now combined because it caused too many errors
    /// for very little functionality, trying to switch between them all the time.
    /// </summary>
    public GroundedState StateGrounded { get; private set; }
    /// <summary>
    /// The jumping/airborne state of the character.
    /// </summary>
    public JumpingState StateJumping { get; private set; }
    /// <summary>
    /// The climbing state of the character. Currently vestigial, but exists as an example of how to add more states to the character.
    /// </summary>
    public ClimbState StateClimb { get; private set; }
    /// <summary>
    /// The dashing state of the character. Currently vestigial, but exists as an example of how to add more states to the character.
    /// </summary>
    public DashState StateDash { get; private set; }
    #endregion

    #region Actions
    /// <summary>
    /// The event that is called when the player dies. Used to respawn the player, and to play the death animation.
    /// </summary>
    public static event Action OnPlayerDeath;
    #endregion

    private void OnDrawGizmos() {
        // grounded box
        Gizmos.DrawCube(transform.position + (Vector3.down * box_distance), new Vector3(transform.localScale.x / 1.1f, box_y_size, 1));

        // left & right wall box
        // Gizmos.DrawCube(transform.position + (Vector3.left * box_distance),
        //                             new Vector3(box_y_size, transform.localScale.y, 1));
        // Gizmos.DrawCube(transform.position + (Vector3.right * box_distance),
        //                             new Vector3(box_y_size, transform.localScale.y, 1));

        // edge detection areas
        // Vector2 right_inside = new Vector2(transform.localScale.x / 2 - edgeDetectionRange, (transform.localScale.y - box_y_size) / 2);
        // Vector2 right_outside = new Vector2(transform.localScale.x / 2, (transform.localScale.y + box_y_size) / 2);
        // Gizmos.DrawLine(rb.position + right_outside, rb.position + right_outside + (Vector2.up * box_y_size * 3));
        // Gizmos.DrawLine(rb.position + right_inside, rb.position + right_inside + (Vector2.up * box_y_size * 3));
        // Vector2 left_inside = new Vector2(-transform.localScale.x / 2 + edgeDetectionRange, (transform.localScale.y - box_y_size) / 2);
        // Vector2 left_outside = new Vector2(-transform.localScale.x / 2, (transform.localScale.y + box_y_size) / 2);
        // Gizmos.DrawLine(rb.position + left_outside, rb.position + left_outside + (Vector2.up * box_y_size * 3));
        // Gizmos.DrawLine(rb.position + left_inside, rb.position + left_inside + (Vector2.up * box_y_size * 3));

        // Vector2 lower_bottom = new Vector2(-transform.localScale.x / 2 - edgeDetectionRange, -transform.localScale.y / 2);
        // Vector2 upper_bottom = new Vector2(-transform.localScale.x / 2 - edgeDetectionRange, -transform.localScale.y / 2 + edgeDetectionRange);
        // Gizmos.DrawLine((Vector2)transform.position + lower_bottom, (Vector2)transform.position + lower_bottom + Vector2.right * (transform.localScale.x + (edgeDetectionRange * 2)));
        // Gizmos.DrawLine((Vector2)transform.position + upper_bottom, (Vector2)transform.position + upper_bottom + Vector2.right * (transform.localScale.x + (edgeDetectionRange * 2)));
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        tr = GetComponentInChildren<TrailRenderer>();
        animator = GetComponentInChildren<CharacterAnimation>();

        StateGrounded = new GroundedState(this);
        StateJumping = new JumpingState(this);
        StateClimb = new ClimbState(this);
        StateDash = new DashState(this);

        TransitionToState(StateJumping);

        box_y_size = transform.localScale.y * 0.05f;
        box_distance = transform.localScale.x * 0.5f;
        airFriction = baseAirFriction;

        RespawnPoint = new Vector3(0, 0, 0);
    }

    private void OnEnable() {
        OnPlayerDeath += Respawn;
    }

    private void OnDisable() {
        OnPlayerDeath -= Respawn;
    }

    /// <summary>
    /// First of the three respawn functions. Begins the process: hides & stops the character, sets dead to true, and (in the future) starts 
    /// the animation.
    /// </summary>
    private void Respawn() {
        sr.color = Color.clear;
        // play animation
        rb.velocity = Vector2.zero;
        dead = true;
        animator.SetSpritesActive(false);
    }

    /// <summary>
    /// Second of the three respawn functions. Moves the character to the respawn point, and sets the color back to normal. This happens when 
    /// the black screen is fully covering the screen.
    /// </summary>
    public void GoToRespawnPoint() {
        transform.position = RespawnPoint;
        sr.color = new Color(92, 236, 255);
        tr.gameObject.SetActive(false);
        animator.SetSpritesActive(true);
    }

    /// <summary>
    /// Third of the three respawn functions. Sets the character to not dead, and makes the character visible again. This happens when the 
    /// black screen is entirely hidden, and gameplay can resume.
    /// </summary>
    public void RespawnFinished() {
        dead = false;
        tr.gameObject.SetActive(true);
    }

    void FixedUpdate()
    {
        if (!dead && !paused)
        {
            // timers!
            TimeSinceGrounded += Time.deltaTime;
            TimeSinceLeftWall += Time.deltaTime;
            TimeSinceRightWall += Time.deltaTime;
            TimeSinceJumped += Time.deltaTime;
            TimeSinceJumpBuffered += Time.deltaTime;
            TimeSinceApexReached += Time.deltaTime;
            TimeSinceWallJumped += Time.deltaTime;
            // timers that need to be checked every frame
            TimeSinceGrounded = IsGrounded();
            TimeSinceLeftWall = IsLeftWall();
            TimeSinceRightWall = IsRightWall();
            // core state machine
            HandleStates();
            // base stuff in Maddy Thorson order
            HandleMovement();
            HandleJump();
            HandleGravity();
            // fancy stuff in Maddy Thorson order
            HandleCorners();
            HandleApex();
            HandleCollect();
            // walljump is last, as will be most additional moves like dash & float.
            HandleWallJump();
            // state machine update
            CurrentState.UpdateState();
        }
    }

    /// <summary>
    /// DON"T FORGET TO CHANGE WHEN MORE STATES ARE ADDED! Transitions the character to a new state. Currently limited in functionality 
    /// because there are really only 2 important states.
    /// </summary>
    private void HandleStates() { // grounded, jump, wall, dash, climb
        if (TimeSinceGrounded == 0 && CurrentState != StateDash && CurrentState != StateClimb) {
            CurrentState = StateGrounded;
        } else {
            CurrentState = StateJumping; // TODO: remove this when more states are implemented!
        }

        if (TimeSinceJumped < 0.2f && rb.velocity.y > 0) { 
            // makes sure that IsGrounded does not tell the state machine it is grounded the frame after leaving the ground jumping
            CurrentState = StateJumping;
        }
        // don't need a check for jumping or dashing because that gets set when you jump or dash
    }

    /// <summary>
    /// Handles the horizontal movement of the character. This is the core of the character's movement, and is the most important function.
    /// This function alone I have edited more than any other HandleX functions combined, & will probably continue to. Meant to be as
    /// flexible as possible--I'd love to be able to change the total feel of the game by changing constants from the inspector. Hopefully 
    /// I added enough!
    /// </summary>
    private void HandleMovement() // TODO: lowkey where is deltatime...
    { 
        // the horizontal speed that the user is inputting
        float target_speed = horizontal * moveSpeed;
        // accelerate slower if you are not trying to move
        float acceleration_rate = (Mathf.Abs(target_speed) > 0.01f) ? acceleration : deceleration;
        if (CurrentState == StateGrounded) { // grounded 
            // is the user going faster in the right direction than they would normally (aka had they been launched?)
            if (Mathf.Sign(horizontal) == Mathf.Sign(rb.velocity.x) && Mathf.Abs(target_speed) < Mathf.Abs(rb.velocity.x)) {
                // don't do anything with the speed...? // this is currently not doing anything
            }
            // actually set the speed! because this is fixed update, should be consistent over different machines. 
            float x_speed = Mathf.Lerp(rb.velocity.x, target_speed, acceleration_rate);
            // besides the conditional below, this should be one of the only times x velocity changes!
            rb.velocity = new Vector2(x_speed, rb.velocity.y);

            // makes it so velocity actually does become 0, kind of a manual friction effect
            // this is VERY much icing on the cake and should be removed if it causes issues, but it is nice to have.
            if (Mathf.Abs(rb.velocity.x) < 0.05f && horizontal == 0) {
                rb.velocity = new Vector2(0, rb.velocity.x);
            }
        } 

        if (CurrentState == StateJumping) { // could do this stuff in the actual state machine, but this made more sense at the time.
            // add the speed boost at the apex of the jump
            if (TimeSinceApexReached < apexModifierDuration) {
                acceleration_rate *= apexModifierSpeed;
            }
            // actually set x speed, reduced by air friction modifier
            float x_speed = Mathf.Lerp(rb.velocity.x, target_speed, acceleration_rate * airFriction);
            rb.velocity = new Vector2(x_speed, rb.velocity.y);
        }
    }

    /// <summary>
    /// Handles the character's jump. This is the second most important function, and is very important to HandleMovement.
    /// </summary>
    private void HandleJump()
    { // kind of 
        if (TimeSinceGrounded < coyoteTime && (jumpPressed || TimeSinceJumpBuffered < jumpBufferTime) && TimeSinceJumped > jumpLockoutTime)
        {
            // animator.SetTrigger("Jump");
            sound.PlaySound(CharacterSound.Sounds.Jump);
            // set the state
            CurrentState = StateJumping;
            // restart the timer so we know how long ago jump worked
            TimeSinceJumped = 0;
            // "uses" up the jump input
            jumpPressed = false;
            TimeSinceJumpBuffered = jumpBufferTime + 1; 
            // actually preforms the jump
            rb.velocity = new Vector2(rb.velocity.x, jumpForce); // used to be:     rb.AddForce(jumpForceVector2, ForceMode2D.Impulse);
        }
        // checks if character is jumping, let go of button, before a certain time, and is not on the ground
        if (TimeSinceGrounded < fullHopTime && jumpReleased == true && rb.velocity.y > 0 && CurrentState == StateJumping) {
            // one of the only times y velocity changes
            jumpReleased = false;
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * shortHopModifier);
        }
        // if jump is released after fullHopTime, nothing happens
    }

    /// <summary>
    /// Handles the character's gravity. Could be totally reworked if I end up needing more gravity modifiers, but for now I like having
    /// a base gravity that gets modified by other things. Down the line, I may make all these modifiers their own values, but I think 
    /// that is substantially less intuitive.
    /// </summary>
    private void HandleGravity() {
        if (rb.velocity.y < 0) { // if you are falling
            gravity = baseGravity * fallingGravityModifier;
        } else if (springLaunched) {
            gravity = baseGravity * springGravityModifier;
        } else {
            gravity = baseGravity;
        }
        if (TimeSinceApexReached < apexModifierDuration) {
            gravity = baseGravity * apexModifierGravity;
        }
        if (vertical < 0) {
            gravity *= fastFallingGravityModifier;
        }
 
        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y - gravity);

        if (vertical < 0) {
            if (rb.velocity.y < fastTerminalVelocity) {
                rb.velocity = new Vector2(rb.velocity.x, fastTerminalVelocity);
            }
        } else {
            if (rb.velocity.y < baseTerminalVelocity) {
                rb.velocity = new Vector2(rb.velocity.x, baseTerminalVelocity);
            }
        }

        // this used to be predicated on the walled state
        if (TimeSinceLeftWall == 0 && horizontal < 0) {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(wallTerminalVelocity, rb.velocity.y));
        }
        if (TimeSinceRightWall == 0 && horizontal > 0) {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(wallTerminalVelocity, rb.velocity.y));
        }
    }

    /// <summary>
    /// Handles the corner detection for the character. These rays are drawn in OnDrawGizmos. At one point I wrote very ominously 
    /// "This function should potentially be in Update() rather than FixedUpdate()... uh...", but I have no idea why I wrote that. 
    /// Leaving it just in case I end up haunted by this down the line...
    /// </summary>
    private void HandleCorners() { 
        if (TimeSinceLeftWall > Time.fixedDeltaTime * 3 && CurrentState != StateGrounded) 
        {    
            Vector2 right_inside = new Vector2(transform.localScale.x / 2 - edgeDetectionRange, transform.localScale.y / 2 + box_y_size);
            Vector2 right_outside = new Vector2(transform.localScale.x / 2 - edgeDetectionRange / 8, transform.localScale.y / 2 + box_y_size);
            RaycastHit2D right_corner = Physics2D.Raycast(rb.position + right_outside, Vector2.up, box_y_size * 3, groundLayer);
            RaycastHit2D right_ray = Physics2D.Raycast(rb.position + right_inside, Vector2.up, box_y_size * 3, groundLayer);
            if (right_corner.collider != null && right_ray.collider == null) {
                rb.position = new Vector2(rb.position.x - edgeDetectionRange, rb.position.y);
            }

            Vector2 left_inside = new Vector2(-transform.localScale.x / 2 + edgeDetectionRange, transform.localScale.y / 2 + box_y_size);
            Vector2 left_outside = new Vector2(-transform.localScale.x / 2 + edgeDetectionRange / 8, transform.localScale.y / 2 + box_y_size);
            RaycastHit2D left_corner = Physics2D.Raycast(rb.position + left_outside, Vector2.up, box_y_size * 3, groundLayer);
            RaycastHit2D left_ray = Physics2D.Raycast(rb.position + left_inside, Vector2.up, box_y_size * 3, groundLayer);
            if (left_corner.collider != null && left_ray.collider == null) {
                rb.position = new Vector2(rb.position.x + edgeDetectionRange, rb.position.y);
            }
        }

        Vector2 lower_bottom = new Vector2(-transform.localScale.x / 2 - edgeDetectionRange, -transform.localScale.y / 2);
        Vector2 upper_bottom = new Vector2(-transform.localScale.x / 2 - edgeDetectionRange, -transform.localScale.y / 2 + edgeDetectionRange);
        RaycastHit2D lower_ray = Physics2D.Raycast((Vector2)transform.position + lower_bottom, Vector2.right, transform.localScale.x + (edgeDetectionRange * 2), groundLayer);
        RaycastHit2D upper_ray = Physics2D.Raycast((Vector2)transform.position + upper_bottom, Vector2.right, transform.localScale.x + (edgeDetectionRange * 2), groundLayer);
        if (lower_ray.collider != null && upper_ray.collider == null) {
            rb.position = new Vector2(rb.position.x, rb.position.y + edgeDetectionRange);
        }

        // stops character from running off stage if not moving or moving in opposite direction, this is sticky platforms
        if (stickyPlatforms && CurrentState == StateGrounded && (horizontal == 0 || Mathf.Sign(horizontal) != Mathf.Sign(rb.velocity.x))) {
            Vector2 middle_left = new Vector2(-edgeDetectionRange, (-transform.localScale.y / 2) - edgeDetectionRange);
            Vector2 middle_right = new Vector2(edgeDetectionRange, (-transform.localScale.y / 2) - edgeDetectionRange);
            RaycastHit2D middle_left_ray = Physics2D.Raycast(rb.position + middle_left, Vector2.down, edgeDetectionRange * 2, groundLayer);
            RaycastHit2D middle_right_ray = Physics2D.Raycast(rb.position + middle_right, Vector2.down, edgeDetectionRange * 2, groundLayer);
            if ((middle_left_ray.collider == null && middle_right_ray.collider != null) 
             || (middle_left_ray.collider != null && middle_right_ray.collider == null)) {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }

/// <summary>
/// Handles the apex of the jump. This is the point where the character is at the highest point of the jump, and gets a brief speed boost & 
/// gravity reduction. The timer is set here, but most of the functionality lives in HandleMovement and HandleGravity.
/// </summary>
    private void HandleApex() {
        if (Mathf.Sign(previous_velocity_y) == 1 && Mathf.Sign(rb.velocity.y) == -1) {
            TimeSinceApexReached = 0;
            springLaunched = false;
        }
        previous_velocity_y = rb.velocity.y;
    }

    /// <summary>
    /// Handles the collection of items/zones. Currently only checks for respawn zones, but will be expanded to include collectables,
    /// items, health, or whatever I'd need.
    /// </summary>
    private void HandleCollect() {
        RaycastHit2D respawn_zone = Physics2D.Raycast(rb.position, Vector2.up, edgeDetectionRange, respawnLayer);
        if (respawn_zone.collider != null) {
            RespawnPoint = respawn_zone.transform.position;
            CameraController.SetRespawnRoom();
        }
    }

    /// <summary>
    /// A helper function for HandleWallJump, that does the actual "walljumping".
    /// </summary>
    /// <param name="direction"></param>
    private void WallJump(int direction) {
         // animator.SetTrigger("WallJump");
        sound.PlaySound(CharacterSound.Sounds.Jump);

        // set the state
        CurrentState = StateJumping;
        // restart the timer so we know how long ago jump worked
        TimeSinceWallJumped = 0;
        // set the last wall
        previous_wall = -direction;
        // "uses" up the jump input
        jumpPressed = false;
        TimeSinceJumpBuffered = jumpBufferTime + 1; 
        // actually preforms the jump
        if (!limitWallJumpVertical) {
            rb.AddForce(new Vector2(moveSpeed * direction * wallJumpSpeedBoost, jumpForce * wallJumpForceMultiplier), ForceMode2D.Impulse); 
        } else {
            rb.velocity = new Vector2(moveSpeed * direction * wallJumpSpeedBoost, jumpForce * wallJumpForceMultiplier);
        }
    }

    /// <summary>
    /// Handles the character's walljump. Wall jumps vary a TON from game to game, so while I tried to make this as broad and 
    /// flexible as possible in my implementation, I might have to take it apart if I want to do something more custom.
    /// </summary>
    private void HandleWallJump() { // there might be some air control issue here? couldn't recreate it
        // this is the simplest implementation of walljump--why complicate it when wall jumping is SO different between games.
        if (TimeSinceGrounded == 0) {
            previous_wall = 0;
        }

        if (TimeSinceRightWall == 0 && jumpPressed && (TimeSinceWallJumped > wallJumpLag || previous_wall != 1)) {
            WallJump(-1);
        }
        if (TimeSinceLeftWall == 0 && jumpPressed && (TimeSinceWallJumped > wallJumpLag || previous_wall != -1)) {
            WallJump(1);
        }

        if (TimeSinceWallJumped < wallJumpLag) {
            if (hollowKnightWallJump) { // return air control all at once
                airFriction = 0;
            } else { // return air control gradually
                airFriction = baseAirFriction * (TimeSinceWallJumped / wallJumpLag);
            }
        } else {
            airFriction = baseAirFriction;
        }
        if ((TimeSinceRightWall == 0 && previous_wall == -1) || (TimeSinceLeftWall == 0 && previous_wall == 1)) {
            airFriction = baseAirFriction;
        }
    }

    /// <summary>
    /// Handles the character's spring bounce. Currently uses add force, but I might change it if that causes issues...
    /// </summary>
    /// <param name="velocity">Launch angle/force of the specific spring.</param>
    /// <param name="springJumpForceModifier">The additional amount that holding/pressing jump on the spring launches the character.</param>
    /// <param name="newSpringGravityModifier">Changes the gravity modifier for when the character is rising on a spring jump.</param>
    public void Spring(Vector3 velocity, float springJumpForceModifier, float newSpringGravityModifier) {
        springGravityModifier = newSpringGravityModifier;
        rb.AddForce(velocity, ForceMode2D.Impulse);
        if (phantomJumpPressed || TimeSinceJumpBuffered < jumpBufferTime) {
            rb.AddForce(new Vector2(0, jumpForce * springJumpForceModifier), ForceMode2D.Impulse);
            jumpPressed = false;
            TimeSinceJumpBuffered = jumpBufferTime + 1;
        }
        springLaunched = true;
    }

    /// <summary>
    /// Invokes the OnPlayerDeath action if it is not null.
    /// </summary>
    public void Die() {
        OnPlayerDeath?.Invoke();
    }

    #region Timer, and Input Functions
    /// <summary>
    /// Checks if the character is grounded. Returns 0 if the character is grounded, and the time since the character was grounded otherwise.
    /// </summary>
    /// <returns>How long it has been since the character has been grounded.</returns>
    private float IsGrounded()
    {
        if (Physics2D.OverlapBox(transform.position + (Vector3.down * box_distance),
                                    new Vector2(transform.localScale.x / 1.1f, box_y_size), 0, groundLayer)) {
            return 0;
        }
        else 
        {
            return TimeSinceGrounded;
        }
    }
    
    /// <summary>
    /// Checks if the character is touching the left wall. Returns 0 if the character is touching the left wall, and the time since then otherwise.
    /// </summary>
    /// <returns>How long it has been since the character was on the left wall.</returns>
    private float IsLeftWall() 
    {
        if (Physics2D.OverlapBox(transform.position + (Vector3.left * box_distance),
                                    new Vector2(box_y_size, transform.localScale.y / 1.1f), 0, groundLayer)) {
            return 0;
        }
        else 
        {
            return TimeSinceLeftWall;
        }
    }

    /// <summary>
    /// Checks if the character is touching the right wall. Returns 0 if the character is touching the right wall, and the time since then otherwise.
    /// </summary>
    /// <returns>How long it has been since the character was on the right wall.</returns>
    private float IsRightWall() 
    {
        if (Physics2D.OverlapBox(transform.position + (Vector3.right * box_distance),
                                    new Vector2(box_y_size, transform.localScale.y / 1.1f), 0, groundLayer)) {
            return 0;
        }
        else 
        {
            return TimeSinceRightWall;
        }
    }

    /// <summary>
    /// CHANGE IF MORE ACTIONS ARE ADDED! Checks if the character is idle. Returns true if the character is not moving, jumping, or doing anything else, and false otherwise.
    /// </summary>
    /// <returns>True if the character is idle, false otherwise.</returns>
    public bool IsIdle()
    { // should only return idle if nothing is being attempted by the player
        return !jumpPressed && horizontal == 0 && vertical == 0 && Mathf.Abs(rb.velocity.x) < 0.02f;
    }

    /// <summary>
    /// My recieving input system function for the x & y input from the player.
    /// </summary>
    /// <param name="context">The vector2 of the players movement input, from -1 to 1.</param>
    public void Move(InputAction.CallbackContext context) { // is this flawed that it only is called when it changes?
        horizontal = context.ReadValue<Vector2>().x;
        vertical = context.ReadValue<Vector2>().y;
    }

    /// <summary>
    /// My recieving input system function for the jump input from the player.
    /// </summary>
    /// <param name="context">The bool for whether the jump button has been pressed</param>
    public void Jump(InputAction.CallbackContext context) {
        if (context.performed) { 
            jumpPressed = true;
            phantomJumpPressed = true;
            jumpReleased = false;
        }
        if (context.canceled) { 
            if (jumpPressed) { // buffer a jump
                TimeSinceJumpBuffered = 0;
            }
            jumpPressed = false;
            phantomJumpPressed = false;
            jumpReleased = true;
        }
    }

    /// <summary>
    /// My recieving input system function for the dash input from the player.
    /// </summary>
    /// <param name="context">The bool for whether the dash button has been pressed</param>
    public void Dash(InputAction.CallbackContext context) {
        if (context.performed) { // dash buffer?

        }
        if (context.canceled) { 

        }
    }

    /// <summary>
    /// My recieving input system function for the pause input from the player.
    /// </summary>
    /// <param name="context">The bool for whether the pause button has been pressed</param>
    public void Pause(InputAction.CallbackContext context) {
        if (context.performed && paused) {
            Time.timeScale = 1;
            paused = !paused;
            pauseMenu.CloseMenu();
            // shouldnt be able to click?
        }
        else if (context.performed && !paused && GetComponent<PlayerInput>().enabled) { // last check might be redundant!
            Time.timeScale = 0;
            paused = !paused;
            pauseMenu.OpenMenu();
        }
    }

    /// <summary>
    /// Unpauses the game. Called by the pause menu. Could totally move this to the pause script if I wanted to.
    /// </summary>
    public void Unpause() {
        Time.timeScale = 1;
        paused = false;
        pauseMenu.CloseMenu();
    }

    /// <summary>
    /// My recieving input system function for the die input from the player. Used as a quick reset button, fully functional but not
    /// currently implemented.
    /// </summary>
    /// <param name="context">The bool for whether the die button has been pressed</param>
    public void InputDie(InputAction.CallbackContext context) {
        if (context.performed && !dead) {
            Die();
        }
    }

    /// <summary>
    /// Transititions to a new state. Calls the exit method of the current state, sets the current state to the new state, and calls the enter
    /// method of the new state. 
    /// </summary>
    /// <param name="newState">The state to transition to</param>
    public void TransitionToState(ICharacterState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();
    }
    #endregion





    // -------- -------- -------- -------- -------- -------- -------- -------- -------- -------- //
    // below is the state machine
    // -------- -------- -------- -------- -------- -------- -------- -------- -------- -------- //

    



    #region State Machine
    // State Interface
    public interface ICharacterState
    { // TODO: does the whole initializer slow this thing down? or is it okay
        void EnterState(); 
        void UpdateState();
        void ExitState();
    }

    // Running State
    public class GroundedState : ICharacterState
    {
        private CharacterPhysics character;

        public GroundedState(CharacterPhysics character)
        {
            this.character = character;
        }

        public void EnterState()
        {
            // character.animator.SetBool("isRunning", true);
        }

        public void UpdateState()
        {
            // if (!character.IsIdle())
            // {
            //     character.TransitionToState(character.StateIdle);
            // }
            // else if (character.TimeSinceGrounded > 0)
            // {
            //     character.TransitionToState(character.StateJumping);
            // }
            // // Handle wall check
        }

        public void ExitState()
        {
            // character.animator.SetBool("isRunning", false);
        }
    }

    // Jumping State
    public class JumpingState : ICharacterState
    {
        private CharacterPhysics character;

        public JumpingState(CharacterPhysics character)
        {
            this.character = character;
        }

        public void EnterState()
        {
            // character.animator.SetTrigger("Jump");
        }

        public void UpdateState()
        {
            
        }

        public void ExitState()
        {
            // Cleanup jumping state
        }
    }

    // Climb State
    public class ClimbState : ICharacterState
    {
        private CharacterPhysics character;

        public ClimbState(CharacterPhysics character)
        {
            this.character = character;
        }

        public void EnterState()
        {
            // Initialize climb state
        }

        public void UpdateState()
        {
            // Handle climb behavior
            // if (character.TimeSinceGrounded == 0)
            // {
            //     character.TransitionToState(character.StateIdle);
            // }
        }

        public void ExitState()
        {
            // Cleanup climb state
        }
    }

    // Dash State
    public class DashState : ICharacterState
    {
        private CharacterPhysics character;

        public DashState(CharacterPhysics character)
        {
            this.character = character;
        }

        public void EnterState()
        {
            // Initialize dash state
        }

        public void UpdateState()
        {
            // Handle dash behavior
            // if (character.TimeSinceGrounded == 0)
            // {
            //     character.TransitionToState(character.StateIdle);
            // }
        }

        public void ExitState()
        {
            // Cleanup dash state
        }
    }
    #endregion
}