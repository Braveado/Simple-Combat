using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    private Rigidbody2D playerRb;               // Reference to the player rigidbody2D component.
    private Animator playerRig;                 // Reference to the player animator component.    
    private Transform groundCheck;              // Reference to the position marking where to check if the player is grounded.

    private float groundedRadius = .25f;        // Radius of the overlap circle to determine if grounded.
    private bool grounded;                      // Whether or not the player is grounded.
    private bool facingRight = true;            // For determining which way the player is currently facing.  

    [Header("Stats")]
    public Image lifeBar;                       // Reference to the player life bar.
    public float life;                          // The amount of life the player currently has.
    public float maxLife;                       // The maximum amount of life of the player.
    public Image energyBar;                     // Reference to the player energy bar.
    public float energy;                        // The amount of energy the player currently has.
    public float limitedEnergy;                 // The amount of energy the player can use.
    public float maxEnergy;                     // The maximum amount of energy of the player.
    public float energyRegen;                   // The energy regenerated in a second.

    [Header("Movement")]
    public float maxSpeed = 6f;                 // The fastest the player can travel in the x axis.
    public float jumpForce = 600f;              // Amount of force added when the player jumps.
    public bool airControl = true;              // Whether or not a player can move while jumping.
    public LayerMask ground;                    // A mask determining what is ground to the character.    

    [Header("Weapon")]
    public Weapon weapon;                       // Reference to the weapon of the player.    

    // Misc
    private float xInput;                       // Value of the Xmovement input.
    private bool jump;                          // Whether or not the player is jumping.
    private bool attack;                        // Whether or not the player is attacking.
    private bool damaged;                       // Whether or not the player has been atacked.

    private void Awake()
    {
        GetReferences();
    }

    private void GetReferences()
    {
        // Get references here.
        playerRb = GetComponent<Rigidbody2D>();
        playerRig = GetComponent<Animator>();
        groundCheck = transform.Find("GroundCheck");
    }

    private void Update()
    {        
        GetMovementInputs();        
        if(!damaged && life > 0)
            Attacks();
        ManageEnergy();
        UpdateHUD();

        // Stitched restart.
        if (Input.GetKeyDown(KeyCode.N))
            SceneManager.LoadScene(0);
    }

    private void GetMovementInputs()
    {
        // Get value of inputs here to not miss anything
        xInput = Input.GetAxis("Xmovement");
        jump = Input.GetButtonDown("Jump");        
    }    

    private void Attacks()
    {
        if(Input.GetButtonDown("Attack") && !attack)
        {            
            // Check energy levels.
            if(energy >= weapon.wEnergy || (limitedEnergy <= weapon.wEnergy && energy == limitedEnergy))
            playerRig.SetBool("attacking", true);
        }
    }

    private void ManageEnergy()
    {
        // Clamp the max energy to the player current life.
        limitedEnergy = life;

        // Regenerate energy while not attacking and not dead.
        if (!attack && life > 0)
            energy += energyRegen * Time.deltaTime;
    }

    private void UpdateHUD()
    {        
        // Make sure the life and energy are in the correct range.
        life = Mathf.Clamp(life, 0f, maxLife);
        energy = Mathf.Clamp(energy, 0f, limitedEnergy);

        // Fill the life and energy bars acording to their current percentage.
        lifeBar.fillAmount = life / maxLife;
        energyBar.fillAmount = energy / maxEnergy;
    }

    private void FixedUpdate()
    {
        GroundCheck(); 
        if(!damaged && life > 0)
            Movement();        
    }

    private void GroundCheck()
    {
        grounded = false;
        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundedRadius, ground);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
                grounded = true;
        }
        playerRig.SetBool("grounded", grounded);
    }

    private void Movement()
    {
        // The ySpeed animator parameter is set to the value of the y velocity of the rigidbody.
        playerRig.SetFloat("ySpeed", playerRb.velocity.y);
        //only control the player if grounded or airControl is turned on.
        if (grounded || airControl)
        {
            if (!attack)
            {
                // Move the character if its not attacking.
                playerRb.velocity = new Vector2(xInput * maxSpeed, playerRb.velocity.y);

                // The xSpeed animator parameter is set to the absolute value of the horizontal input.
                playerRig.SetFloat("xSpeed", Mathf.Abs(xInput));

                // Reset the constraints of the rigidbody.
                if (xInput != 0)
                    playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
                // Constrain the x movement of the rigidbody when no xInput is recieved to prevent sliding in slopes.
                else
                    playerRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

                // If the input is moving the player right and the player is facing left...
                if (xInput > 0 && !facingRight)
                {
                    // ... flip the player.
                    facingRight = true;
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                // Otherwise if the input is moving the player left and the player is facing right...
                else if (xInput < 0 && facingRight)
                {
                    // ... flip the player.
                    facingRight = false;
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                }
            }            
        }

        // If the player should jump...
        if (grounded && jump)
        {
            // ... add a vertical force to the player.
            grounded = false;
            playerRb.AddForce(new Vector2(0f, jumpForce));
            playerRig.SetBool("grounded", grounded);            
        }
        // Turn off jump to prevent repeats.
        jump = false;
    }

    // Methods called by the animations.

    private void StartAttack()
    {
        attack = true;

        // Stop movement.
        playerRb.velocity = new Vector2(0, playerRb.velocity.y);

        // The xSpeed animator parameter is set to 0.
        playerRig.SetFloat("xSpeed", 0);

        // Constrain the x movement of the rigidbody when attacking to prevent sliding in slopes.                
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
    }

    private void EndAttack()
    {
        attack = false;
        playerRig.SetBool("attacking", attack);

        // Stop movement.
        playerRb.velocity = new Vector2(0, playerRb.velocity.y);

        // The xSpeed animator parameter is set to 0.
        playerRig.SetFloat("xSpeed", 0);

        // Constrain the x movement of the rigidbody after attacking to prevent sliding in slopes.   
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
    }

    private void ActivateWeaponCollider()
    {
        // Consume the energy cost.
        energy -= weapon.wEnergy;

        // Turn on the weapon collider.
        weapon.wCollider.enabled = true;
        // Reset the hit count to enable new collisions.
        weapon.hitCount = 0;
    }

    private void DisableWeaponCollider()
    {
        // Turn off the weapon collider.
        weapon.wCollider.enabled = false;
    }

    private void AddAttackForce()
    {
        // To prevent launching into infinity by accident.
        if (life > 0)
        {
            // Reset the constraints of the rigidbody.        
            playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (facingRight)
                playerRb.AddForce(new Vector2(weapon.wForce, 0f));
            else
                playerRb.AddForce(new Vector2(-weapon.wForce, 0f));
        }
    }

    private void StartDamaged()
    {
        damaged = true;

        attack = false;
        playerRig.SetBool("attacking", attack);

        // Turn off the weapon collider.
        weapon.wCollider.enabled = false;

        // Stop movement.
        playerRb.velocity = new Vector2(0, playerRb.velocity.y);

        // The xSpeed animator parameter is set to 0.
        playerRig.SetFloat("xSpeed", 0);

        // Constrain the x movement of the rigidbody to prevent sliding in slopes.   
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        
    }

    private void EndDamaged()
    {
        damaged = false;
        playerRig.SetBool("damaged", damaged);
    }

    private void SetDead()
    {
        attack = false;
        playerRig.SetBool("attacking", attack);

        // Turn off the weapon collider.
        weapon.wCollider.enabled = false;

        // Stop movement.
        playerRb.velocity = new Vector2(0, playerRb.velocity.y);

        // The xSpeed animator parameter is set to 0.
        playerRig.SetFloat("xSpeed", 0);

        // Constrain the rigidbody.   
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        // Switch colliders to prevent collision with enemies
        GetComponent<CapsuleCollider2D>().enabled = false;
        transform.Find("DeadCollider").GetComponent<CapsuleCollider2D>().enabled = true;        
    }

    // Methods called by collisions.

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If an enemy weapon hit the player...
        if (other.tag == "EnemyWeapon")
        {
            // ... get its stats.
            Weapon wTrigger = other.GetComponent<Weapon>();

            // Account for multiple collisions.
            wTrigger.hitCount += 1;
            if (wTrigger.hitCount == 1)
            {
                // Make the damage.
                life -= wTrigger.wDamage;

                // Chek wich animation to play.
                if (life > 0)
                {
                    // Set the damaged animation.
                    playerRig.SetBool("damaged", true);
                }
                else if(life <= 0)
                {
                    // Set the dead animation.
                    playerRig.SetBool("dead", true);
                }
            }            
        }
    }
}
