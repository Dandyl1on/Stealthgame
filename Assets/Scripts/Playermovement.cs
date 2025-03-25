using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Playermovement : MonoBehaviour
{   
    //the horizontal float is for movement either 1 for right or -1 for left and 0 for no movement
    private float horizontal;
    public float speed = 8f;
    public float jumpPower;
    //determines if what way the player is facing, might be used for flipping the sprite
    private bool isFacingRight = true;
    
    //dashing mechanics 
    public bool canDash = true;
    private bool isDashing;
    public float dashPower = 24f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;
    [SerializeField] private Slider dashCooldownSlider;
    
    //slider color image when full
    public Image fillImage;
    //players sprite renderer
    public SpriteRenderer sprite;
    
    //wall jumping mechanics
    private bool isWallSliding;
    private float wallSlideSpeed = 2f;
    private bool isWallJumping;
    private float wallJumpingDirection;
    private bool wallJumping;
    private float wallJumpDuration = 0.4f; //sets the moment/duration of the wall jump
    private Vector2 wallJumpingPower = new Vector2(10f, 16f);
    
    
    //below all are used for basic player stuff
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    //a trail rendere for sick ass tail effects
    [SerializeField] private TrailRenderer tr;
    //wall checks and what not
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private LayerMask wallLayer;

    private void Start()
    {
        // makes the dash slider go from 0 to 1 smoothly
        if (dashCooldownSlider != null)
        {
            dashCooldownSlider.minValue = 0;
            dashCooldownSlider.maxValue = dashCooldown;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //stops movement while dashing, can be removed?
        if (isDashing)
        {
             return;
        }
        
        // gets the horizontal RawAxis so its either 1, 0 or -1
        horizontal = Input.GetAxisRaw("Horizontal");
        
        // jumps the player according to its x direction and jump power (is in update to not miss button press)
        if (Input.GetButtonDown("Jump") && isGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpPower);
        }
        // starts the dash mechanic/cooldown timer thingy if left shift is pressed
        if (Input.GetButtonDown("Fire3") && canDash)
        {
            StartCoroutine(Dash());
        }
        //makes sure the player isnt flipped during the wall jump
        if (!isWallJumping)
        {
            Flip();    
        }
        
        WallSlide();
        Walljump();
    }

    private void FixedUpdate()
    {
        // handles the movements 
        if (!isWallJumping)
        {
            rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);    
        }
    }

    // returns a true or false depending on if the overlap circle is well overlapping with the groundlayer integer on the layer list (set this in the editor)
    private bool isGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }
    
    // returns a true or false depending on if the overlap circle is well overlapping with the walllayer integer on the layer list (set this in the editor)
    private bool isWalled()
    {
        return Physics2D.OverlapCircle(wallCheckRight.position, 0.2f, wallLayer) || //the or handle is used to check if its the right or left side, since its only the sprite that gets flipped and not the gameobject
               Physics2D.OverlapCircle(wallCheckLeft.position, 0.2f, wallLayer);
    }

    private void Walljump()
    {
        //checks if the player is sliding on a wall and not wall jumping
        if (isWallSliding && !isWallJumping)
        {
            //checks the overlapping circle right for overlaps and sets the walljumping direction to -1 (left) to jump the opposite way of the wall
            if (Physics2D.OverlapCircle(wallCheckRight.position, 0.2f, wallLayer))
            {
                wallJumpingDirection = -1f;
            }
            //checks the overlapping circle right for overlaps and sets the walljumping direction to 1 (right) to jump the opposite way of the wall
            if (Physics2D.OverlapCircle(wallCheckLeft.position, 0.2f, wallLayer))
            {
                wallJumpingDirection = 1f;
            }
            //stops the wall jumping boost by setting the bool to false
            CancelInvoke(nameof(stopWallJumping));
        }
        
        // starts the wall jump if player is wall sliding
        if (Input.GetButtonDown("Jump") && isWallSliding)
        {
            isWallJumping = true;
            Debug.Log(isWallJumping+" jump button");
            //gives the player the jump boost
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            //calls the stop so the jump boost gets removed after wallJumpDuration (0.4f) change this for longer wall jump
            Invoke(nameof(stopWallJumping), wallJumpDuration);
        }
    }

    private void WallSlide()
    {
        //checks if the player is not on the ground and starts to slide
        if (isWalled() && !isGrounded())
        {
            //starts the wall slide and sets bool to true
            isWallSliding = true;
            //clamps the y (vertical movement) the minimum limit of y movement is -wallSlideSpeed, but it can still move upwards
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }
    
    private void stopWallJumping()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        //flips sprite left
        if (isFacingRight && horizontal <0f)
        {
            sprite.flipX = true;
            isFacingRight = false;
        }
        //flips sprite right
        if (!isFacingRight && horizontal >0f)
        {
            sprite.flipX = false;
            isFacingRight = true;
        }
    }

    private IEnumerator Dash()
    {
        //sets bools for canDash to false and isDashing to true
        canDash = false;
        isDashing = true;
        //stores the current gravity setting so it can be restored later
        float originalGravity = rb.gravityScale;
        //disables gravity
        rb.gravityScale = 0f;
        //sets the horizontal movement to the direction*dashpower and 10f upwards
        rb.velocity = new Vector2(horizontal * dashPower, 10f);
        //trail renderer is now true
        tr.emitting = true;
        //waits for the dashTime
        yield return new WaitForSeconds(dashTime);
        //stops the trail renderer
        tr.emitting = false;
        //sets the horizontal movement to input of player meaning you can dash right and switch to fall left
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        //sets the gravity to the stored gravity from before
        rb.gravityScale = originalGravity;
        //isDashing to false and starts the timer thingy for the cooldown slider
        isDashing = false;
        StartCoroutine(DashCooldownTimer());
    }

    private IEnumerator DashCooldownTimer()
    {
        //sets the cooldownTime to 0f so its empty
        float cooldownTime = 0f;
        //while loop so when the cooldownTime is less than 1f the loop happens (happens a few times doesnt go from 0 to 1 instantly since its a float
        while (cooldownTime < dashCooldown)
        {
            //updates the cooldownTime each frame
            cooldownTime += Time.deltaTime;
            //sets the slider to the cooldownTime
            dashCooldownSlider.value = cooldownTime;
            //sets the slider color to red
            fillImage.color = Color.red;
            //waits for new frame before next while loop activation
            yield return null;
        }
        //sets the canDash to true and ensures the slider is filled fully and color to green
        canDash = true;
        dashCooldownSlider.value = dashCooldown;
        fillImage.color = Color.green;
        
    }
}
