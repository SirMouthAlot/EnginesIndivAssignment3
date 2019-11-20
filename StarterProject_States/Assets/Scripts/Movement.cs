// Base taken from Mix and Jam: https://www.youtube.com/watch?v=STyY26a_dPY

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Added for class 
public enum PlayerState
{
    IDLE,
    MOVING,
    CLIMBING,
    ON_WALL,
    JUMPING,
    FALLING,
    DASHING,
    WALL_JUMPING,
}

// Other states to consider: ON_WALL, JUMPING, FALLING, DASHING, WALL_JUMPING
// You may also need to move code into the states I've already made

// An alternative idea would be to make a few larger states like GROUNDED, AIRBORN, ON_WALL
// Then each state has a larger chunk of code that deals with each area

// How you choose to implement the states is up to you
// The goal is to make the code easier to understand and easier to expand on

public class Movement : MonoBehaviour
{
    // Use this to check the state
    [SerializeField]
    private PlayerState currentState = PlayerState.IDLE;

    // Custom collision script
    private Collision coll;

    [HideInInspector]
    public Rigidbody2D rb;
    private AnimationScript anim;

    [Space] // Adds some space in the inspector
    [Header("Stats")] // Adds a header in the inspector 
    public float speed = 10;
    public float jumpForce = 50;
    public float slideSpeed = 5;
    public float wallJumpLerp = 10;
    public float dashSpeed = 20;

    [Space]
    [Header("Booleans")]

    // These were originally used to switch between movement
    // They also control the animation system in unity
    public bool canMove;
    public bool wallGrab;
    public bool wallJumped;
    public bool wallSlide;
    public bool isDashing;

    [Space]

    private bool groundTouch;
    private bool hasDashed;

    public int side = 1;

    // Input Variables
    private float xInput;
    private float yInput;
    private float xRaw;
    private float yRaw;
    private Vector2 inputDirection;

    private void SetInputVariables()
    {
        xInput = Input.GetAxis("Horizontal");
        yInput = Input.GetAxis("Vertical");
        xRaw = Input.GetAxisRaw("Horizontal");
        yRaw = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector2(xInput, yInput);
    }

    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<AnimationScript>();

        SetInputVariables();
    }



    // Update is called once per frame
    void Update()
    {
        // Set input data for easy access
        SetInputVariables();

        // Reset Gravity
        rb.gravityScale = 3;

        // Use the statemachine
        StateMachine(currentState);

        // Return if on a wall
        if (wallGrab || wallSlide || !canMove)
            return;

        // Otherwise use the horizontal input to flip the sprite
        if (xInput > 0)
        {
            side = 1;
            anim.Flip(side);
        }
        if (xInput < 0)
        {
            side = -1;
            anim.Flip(side);
        }
    }

    private void StateMachine(PlayerState state)
    {
        // This is where the code for each state goes
        switch (state)
        {
            //The default state
            case PlayerState.IDLE:
                //Things that happen in the IDLE state
                #region IDLE Tasks
                // If not on the wall and on the ground
                if (!coll.onWall || coll.onGround)
                    wallSlide = false;

                //Reset all dashing stuff based on if you are touching the ground
                if (coll.onGround && !groundTouch)
                {
                    GroundTouch();
                    groundTouch = true;
                }

                // When on the ground and not dashing
                if (coll.onGround && !isDashing)
                {
                    wallJumped = false;
                    GetComponent<BetterJumping>().enabled = true;
                }
                #endregion

                //ENTER DIFFERENT STATES
                #region IDLE Switches
                // Condition: Horizontal input, go to RUNNING state
                if (xInput > 0.01f || xInput < -0.01f)
                {
                    currentState = PlayerState.MOVING;
                }

                // Try to jump when hitting space bar
                if (Input.GetButtonDown("Jump"))
                {
                    // Sets the jump animation
                    anim.SetTrigger("jump");

                    //Condition: On the ground + jump button, go to JUMPING state
                    if (coll.onGround)
                    {
                        Jump(Vector2.up, false);
                        currentState = PlayerState.JUMPING;
                    }
                }

                // Condition: On wall, go to ON WALL state
                if (coll.onWall)
                {
                    currentState = PlayerState.ON_WALL;
                }
                #endregion
                break;

            case PlayerState.MOVING:

                //Things that happen in the MOVING state
                #region MOVING Tasks
                // When on the ground and not dashing
                if (coll.onGround && !isDashing)
                {
                    wallJumped = false;
                    GetComponent<BetterJumping>().enabled = true;
                }

                // Use input direction to move and change the animation
                Walk(inputDirection);
                anim.SetHorizontalMovement(xInput, yInput, rb.velocity.y);
                #endregion
               
                //ENTER DIFFERENT STATES
                #region MOVING Switches
                 // Condition: No horizontal input, go to IDLE state
                if (xInput <= 0.01f || xInput >= 0.01f)
                {
                    currentState = PlayerState.IDLE;
                }

                // If left click and if dash is not on cooldown
                if (Input.GetButtonDown("Fire1") && !hasDashed)
                {
                    //Condition: Directional Input (and Fire1), go into DASHING state
                    if (xRaw != 0 || yRaw != 0)
                    {
                        // Dash using raw input values
                        Dash(xRaw, yRaw);
                        currentState = PlayerState.DASHING;
                    }
                }

                // Jump when hitting the space bar
                if (Input.GetButtonDown("Jump"))
                {
                    // Sets the jump animation
                    anim.SetTrigger("jump");

                    //Condition: On the ground + jump button, go to JUMPING state
                    if (coll.onGround)
                    {
                        Jump(Vector2.up, false);
                        currentState = PlayerState.JUMPING;
                    }
                }
                #endregion

                break;

            case PlayerState.CLIMBING:
                //Things that happen in the CLIMBING state
                #region CLIMBING Tasks
                // Stop gravity
                rb.gravityScale = 0;

                // Limit horizontal movement
                if (xInput > .2f || xInput < -.2f)
                {
                    rb.velocity = new Vector2(rb.velocity.x, 0);
                }

                // Vertical Movement, slower when climbing
                float speedModifier = yInput > 0 ? .5f : 1;
                rb.velocity = new Vector2(rb.velocity.x, yInput * (speed * speedModifier));
                #endregion

                //ENTER DIFFERENT STATES
                #region CLIMBING Switches
                //Condition: Player no longer on wall OR releases the FIRE button, go to IDLE state
                if (!coll.onWall || !Input.GetButton("Fire2"))
                {
                    currentState = PlayerState.IDLE;

                    wallGrab = false;
                    wallSlide = false;

                    //Condition: player still on wall but no longer climbing, go to ON_WALL state
                    if (coll.onWall)
                    {
                        currentState = PlayerState.ON_WALL;
                    }
                    // Reset Gravity
                    rb.gravityScale = 3;
                }
                #endregion
                break;

            case PlayerState.ON_WALL:
                //Things that happen in the ON_WALL state
                #region ON_WALL Tasks
                // When on the wall and not on the gorund
                if (coll.onWall && !coll.onGround)
                {
                    // If the player is moving towards the wall
                    if (xInput != 0 && !wallGrab)
                    {
                        // Slide down the wall
                        wallSlide = true;
                        WallSlide();
                    }
                }
                #endregion

                //ENTER DIFFERENT STATES
                #region ON_WALL Switches
                // Condition: Input is being given to (horizontally) move, go to MOVING state
                if (xInput > 0.01f || xInput < -0.01f)
                {
                    currentState = PlayerState.MOVING;
                }

                // Condition: On wall and hold Fire2, go to CLIMBING state
                if (coll.onWall && Input.GetButton("Fire2") && canMove)
                {
                    // Change state
                    currentState = PlayerState.CLIMBING;

                    // Flips sprite based on which wall
                    if (side != coll.wallSide)
                        anim.Flip(side * -1);

                    // Bools for movement and animation
                    wallGrab = true;
                    wallSlide = false;
                }

                // Condition: Jump input, perform checks
                if (Input.GetButtonDown("Jump"))
                {
                    // Sets the jump animation
                    anim.SetTrigger("jump");

                    //Condition: On the ground, go to JUMPING state
                    if (coll.onGround)
                    {
                        Jump(Vector2.up, false);
                        currentState = PlayerState.JUMPING;
                    }

                    //Condition: On the wall and not on the ground, go to WALL_JUMPING state
                    if (coll.onWall && !coll.onGround)
                    {
                        WallJump();
                        currentState = PlayerState.WALL_JUMPING;
                    }
                }
                #endregion
                break;

            case PlayerState.JUMPING:
                //Things that happen in the JUMPING state
                #region JUMPING Tasks
                groundTouch = false;
                #endregion

                //ENTER DIFFERENT STATES
                #region JUMPING Switches
                // If left click and if dash is not on cooldown
                if (Input.GetButtonDown("Fire1") && !hasDashed)
                {
                    //Condition: Directional Input (and Fire1), go into DASHING state
                    if (xRaw != 0 || yRaw != 0)
                    {
                        // Dash using raw input values
                        Dash(xRaw, yRaw);
                        currentState = PlayerState.DASHING;
                    }
                }

                //Condition: Vertical velocity less than 0.1, go to FALLING state
                if (rb.velocity.y <= 0.1)
                {
                    currentState = PlayerState.FALLING;
                }
                #endregion
                break;

            case PlayerState.FALLING:
                //Things that happen in the FALLING state
                #region FALLING Tasks

                #endregion

                //ENTER DIFFERENT STATES
                #region FALLING Switches
                //Condition: Hit the ground, exit the FALLING state
                if (coll.onGround && !groundTouch)
                {
                    currentState = PlayerState.IDLE;
                }

                //Condition: Collider on wall, go to ON_WALL state
                if (coll.onWall)
                {
                    currentState = PlayerState.ON_WALL;
                }

                // Condition: Horizontal input, go to RUNNING state
                if (xInput > 0.01f || xInput < -0.01f)
                {
                    currentState = PlayerState.MOVING;
                }

                if (Input.GetButtonDown("Fire1") && !hasDashed)
                {
                    // As long as there is some directional input
                    if (xRaw != 0 || yRaw != 0)
                    {
                        //Condition: Directional Input (and Fire1), go into DASHING state
                        Dash(xRaw, yRaw);
                        currentState = PlayerState.DASHING;
                        return;
                    }
                }
                #endregion
                break;

            case PlayerState.DASHING:
                //Things that happen in the DASHING state
                #region DASHING Tasks

                #endregion

                //ENTER DIFFERENT STATES
                #region DASHING Switches
                //Condition: You're in the air after dashing, enter the JUMPING state (JUMPING will check if FALLING)
                if (!coll.onGround)
                {
                    currentState = PlayerState.JUMPING;
                    return;
                }
                //CONDITION: You're still on the ground after initiating dash, enter the MOVING state 
                else
                {
                    currentState = PlayerState.MOVING;
                }
                #endregion
                break;

            case PlayerState.WALL_JUMPING:

                //Things that happen in the WALL_JUMPING state
                #region WALL_JUMPING Tasks
                groundTouch = false;
                #endregion
                
                //ENTER DIFFERENT STATES
                #region WALL_JUMPING Switches
                // If left click and if dash is not on cooldown
                if (Input.GetButtonDown("Fire1") && !hasDashed)
                {
                    //Condition: Directional Input (and Fire1), go into DASHING state
                    if (xRaw != 0 || yRaw != 0)
                    {
                        // Dash using raw input values
                        Dash(xRaw, yRaw);
                        currentState = PlayerState.DASHING;
                    }
                }

                //Condition: Vertical velocity is less than 0.1 (margin of error), go to FALLING state
                if (rb.velocity.y <= 0.1)
                {
                    currentState = PlayerState.FALLING;
                }
                #endregion
                break;
        }
    }

    void GroundTouch()
    {
        // Reset dash
        hasDashed = false;
        isDashing = false;

        side = anim.sr.flipX ? -1 : 1;
    }


    private void Dash(float x, float y)
    {
        // Graphics effects
        Camera.main.transform.DOComplete();
        Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));

        // Put dash on cooldown
        hasDashed = true;

        anim.SetTrigger("dash");


        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);

        rb.velocity += dir.normalized * dashSpeed;
        StartCoroutine(DashWait());
    }

    IEnumerator DashWait()
    {
        // Graphics effect for trail
        FindObjectOfType<GhostTrail>().ShowGhost();

        // Resets dash right away if on ground 
        StartCoroutine(GroundDash());

        // Changes drag over time
        DOVirtual.Float(14, 0, .8f, SetRigidbodyDrag);

        // Stop gravity
        rb.gravityScale = 0;

        // Disable better jumping script
        GetComponent<BetterJumping>().enabled = false;

        wallJumped = true;
        isDashing = true;

        // Wait for dash to end
        yield return new WaitForSeconds(.3f);

        // Reset gravity
        rb.gravityScale = 3;

        // Turn better jumping back on
        GetComponent<BetterJumping>().enabled = true;

        wallJumped = false;
        isDashing = false;
    }

    IEnumerator GroundDash()
    {
        // Resets dash right away
        yield return new WaitForSeconds(.15f);
        if (coll.onGround)
            hasDashed = false;
    }

    private void WallJump()
    {
        // Flip sprite if needed
        if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
        {
            side *= -1;
            anim.Flip(side);
        }

        // Disable movement while wall jumping
        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(.1f));

        // Set direction based on which wall
        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;

        // Jump using the direction
        Jump((Vector2.up / 1.5f + wallDir / 1.5f), true);

        wallJumped = true;
    }

    private void WallSlide()
    {
        // Flip if needed
        if (coll.wallSide != side)
            anim.Flip(side * -1);

        if (!canMove)
            return;

        // If the player is holding towards the wall...
        bool pushingWall = false;
        if ((rb.velocity.x > 0 && coll.onRightWall) || (rb.velocity.x < 0 && coll.onLeftWall))
        {
            pushingWall = true;
        }
        float push = pushingWall ? 0 : rb.velocity.x;

        // Move down
        rb.velocity = new Vector2(push, -slideSpeed);
    }

    private void Walk(Vector2 dir)
    {
        // Do we need these if statements anymore?
        if (!canMove)
            return;
        if (wallGrab)
            return;

        if (!wallJumped)
        {
            rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(dir.x * speed, rb.velocity.y)), wallJumpLerp * Time.deltaTime);
        }
    }

    private void Jump(Vector2 dir, bool wall)
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;
    }

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    void SetRigidbodyDrag(float x)
    {
        rb.drag = x;
    }

}
