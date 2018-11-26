using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    private Player player;                          // Reference to the player.
    private Rigidbody2D enemyRb;                    // Reference to the enemy rigidbody2D component.
    private Animator enemyRig;                      // Reference to the enemy animator component.    
    private Transform directionCheck;               // Reference to the position marking where to check if the enemy should turn.
    private Transform groundCheck;                  // Reference to the position marking where to check if the enemy is grounded.

    [Header("Stats")]
    public Canvas enemyHUD;                         // Reference to the enemy canvas.
    public Image lifeBar;                           // Reference to the enemy life bar.
    public float life;                              // The amount of life the enemy currently has.
    public float maxLife;                          // The maximum amount of life of the enemy.

    [Header("Movement")]
    public float walkSpeed = 2f;                    // The speed of the enemy in the x axis while patrolling.
    public float runSpeedMultiplier = 2f;           // The value to modify the speed of the enemy in the x axis while aggro.
    private float directionLength = 3f;             // Length of the raycast to determine if should turn.
    private bool facingRight = true;                // For determining which way the enemy is currently facing.      
    public LayerMask ground;                        // A mask determining what is ground to the enemy.
    private bool grounded;                          // Whether or not the enemy is grounded.
    private float groundedRadius = .25f;            // Radius of the overlap circle to determine if grounded.

    [Header("Patroll")]
    [Range(0f, 1f)]
    public float waitChance = 0.2f;                 // The chance to stop moving in a stop chance roll.
    private float waitChanceTimer;                  // A timer to manage wait between stop chances.
    public float waitChanceSeconds = 4f;            // Seconds to wait between stop chances.    
    private float waitTimer;                        // A timer to manage wait between movements.
    public float waitSeconds = 2f;                  // Seconds to wait between movements.
    
    [Header("Chase")]
    public float maxAggroRange = 6f;                // The max distance to look for the player.
    public float minAggroRange = 2f;                // The distance to force aggro, move until, and start attacking.    
    private float loseAggroTimer;                   // A timer to wait before losing aggro.
    public float loseAggroWait = 2f;                // Seconds to wait before losing aggro.
    private bool aggro;                             // Whether or not the enemy is chasing the player.
    private bool losingAggro;                       // Whether or not the enemy is losing aggro;
    Vector3 playerDirection;                        // The direction of the player relative to the enemy.

    [Header("Attack")]    
    public Weapon weapon;                           // Reference to the weapon of the enemy.    
    private float attackTimer;                      // A timer to wait before attacking.
    public float attackWait = 1f;                   // Seconds to wait before attacking.

    // Misc
    private bool attack;                            // Whether or not the enemy is attacking.
    private bool damaged;                           // Whether or not the enemy has been atacked.

    private void Awake()
    {
        GetReferences();
    }

    private void GetReferences()
    {
        // Get references here.
        player = GameObject.Find("Player").GetComponent<Player>();
        enemyRb = GetComponent<Rigidbody2D>();
        enemyRig = GetComponent<Animator>();
        directionCheck = transform.Find("DirectionCheck");
        groundCheck = transform.Find("GroundCheck");
    }

    private void Update()
    {
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        // Rotate the canvas so it always look right.
        enemyHUD.transform.rotation = Quaternion.Euler(0, 0, 0);

        // Check whether or not to show the canvas.
        if (life <= 0 || life >= maxLife)
            enemyHUD.enabled = false;
        else if (life < maxLife)
            enemyHUD.enabled = true;

        // Make sure the life is in the correct range.
        life = Mathf.Clamp(life, 0f, maxLife);

        // Fill the life bar acording to its current percentage.
        lifeBar.fillAmount = life / maxLife;
    }

    private void FixedUpdate()
    {
        GroundCheck();
        if (!damaged && grounded && life > 0)
        {
            ScanForPlayer();
            if (!aggro)
                Patroll();
            else if (aggro)
                Chase();
        }
    }

    private void GroundCheck()
    {
        grounded = false;
        // The enemy is grounded if a circlecast to the groundcheck position hits anything designated as ground.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundedRadius, ground);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
                grounded = true;
        }
        enemyRig.SetBool("grounded", grounded);
    }

    private void ScanForPlayer()
    {
        // If player is dead stop scanning.
        if (player.life <= 0)
        {
            aggro = false;
            losingAggro = false;
            return;
        }

        // Direction of the player.
        playerDirection = player.transform.position - transform.position;

        // Reset the aggro states.
        if (losingAggro && loseAggroTimer < Time.time)
        {
            aggro = false;
            losingAggro = false;
        }

        // Check for aggro states.
        if (!aggro || losingAggro)
        {
            // Aggro if the player is inside min range.
            if (playerDirection.sqrMagnitude <= minAggroRange * minAggroRange)
            {
                aggro = true;
                losingAggro = false;
            }
            else
            {
                // Check if player is within max range.
                RaycastHit2D hit = Physics2D.Raycast(transform.position, playerDirection, maxAggroRange);
                if (hit.collider != null && hit.collider.tag == "Player")
                {
                    // Aggro if the player is in your current direction
                    if (facingRight && playerDirection.x >= 0)
                    {
                        aggro = true;
                        losingAggro = false;
                    }
                    else if (!facingRight && playerDirection.x < 0)
                    {
                        aggro = true;
                        losingAggro = false;
                    }
                }
            }
        }
        else if (aggro && !losingAggro)
        {
            // Lose aggro if the player is outside the max range.
            if (playerDirection.sqrMagnitude > maxAggroRange * maxAggroRange)
            {
                losingAggro = true;
                loseAggroTimer = Time.time + loseAggroWait;
            }
            if (true)
            {
                // Lose aggro if there is an obstacle between the player and the enemy
                RaycastHit2D hit = Physics2D.Raycast(transform.position, playerDirection, maxAggroRange);
                if (hit.collider != null && hit.collider.tag != "Player")
                {                    
                    losingAggro = true;
                    loseAggroTimer = Time.time + loseAggroWait;
                }
            }
        }        
    }

    private void Patroll()
    {
        DirectionCheck();
        StopChance();
        Walk();
    }

    private void DirectionCheck()
    {
        // The enemy should turn if a raycast from the directioncheck position doesnt hits anything designated as ground.
        RaycastHit2D hit = Physics2D.Raycast(directionCheck.position, Vector2.down, directionLength, ground);
        if (hit.collider == null)
        {
            if (facingRight)
            {
                facingRight = false;
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else if (!facingRight)
            {
                facingRight = true;
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }
            
    private void StopChance()
    {
        // If the chance timer isnt on cooldown...
        if (waitChanceTimer < Time.time && waitTimer < Time.time)
        {
            // ... make a roll to see if the enemy should stop.
            if (Random.value <= waitChance)
                waitTimer = Time.time + waitSeconds;
        }

        // Reset the chance timer if it and the wait timer arent on colldown
        if (waitChanceTimer < Time.time && waitTimer < Time.time)
            waitChanceTimer = Time.time + waitChanceSeconds;        
    }

    private void Walk()
    {
        // Only move the enemy if not waiting.
        if (waitTimer < Time.time)
        {
            // Reset the constraints of the rigidbody.            
            enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Move the character.
            if (facingRight)
                enemyRb.velocity = new Vector2(walkSpeed, enemyRb.velocity.y);
            else if (!facingRight)
                enemyRb.velocity = new Vector2(-walkSpeed, enemyRb.velocity.y);

            // Set the walk animation.
            enemyRig.SetInteger("State", 1);                       
        }
        else
        {
            // In wait, dont move.
            enemyRb.velocity = new Vector2(0, enemyRb.velocity.y);            

            // Constrain the x movement of the rigidbody to prevent sliding in slopes.            
            enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

            // Set the idle animation.
            enemyRig.SetInteger("State", 0);
        }
    }

    private void Chase()
    {
        if (!losingAggro)
        {
            if (!attack)
            {
                // Turn in the direction of the player.
                if (playerDirection.x >= 0)
                {
                    facingRight = true;
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                else if (playerDirection.x < 0)
                {
                    facingRight = false;
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                }

                // Should move if away from player.
                if (Mathf.Abs(playerDirection.x) > minAggroRange)
                {
                    // Move if a raycast from the directioncheck position hits anything designated as ground.
                    RaycastHit2D hit = Physics2D.Raycast(directionCheck.position, Vector2.down, directionLength, ground);
                    if (hit.collider != null)
                    {
                        // Move the character to the player.
                        if (playerDirection.x > minAggroRange)
                            enemyRb.velocity = new Vector2(walkSpeed * runSpeedMultiplier, enemyRb.velocity.y);
                        else if (playerDirection.x < -minAggroRange)
                            enemyRb.velocity = new Vector2(-walkSpeed * runSpeedMultiplier, enemyRb.velocity.y);

                        // Reset the constraints of the rigidbody.            
                        enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation;

                        // Set the run animation.
                        enemyRig.SetInteger("State", 2);
                    }
                    else
                    {
                        // No ground to keep moving
                        enemyRb.velocity = new Vector2(0, enemyRb.velocity.y);

                        // Constrain the x movement of the rigidbody to prevent sliding in slopes.            
                        enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

                        // Set the idle animation.
                        enemyRig.SetInteger("State", 0);
                    }                    
                }
                else
                {
                    // In attack range, dont move.
                    enemyRb.velocity = new Vector2(0, enemyRb.velocity.y);

                    // Constrain the x movement of the rigidbody to prevent sliding in slopes.            
                    enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

                    // Set the idle animation.
                    enemyRig.SetInteger("State", 0);

                    // Wait for the attack timer to attack.
                    if (attackTimer < Time.time)
                        enemyRig.SetBool("attacking", true);
                }                
            }
        }
        else
        {
            // Losing aggro, dont move
            enemyRb.velocity = new Vector2(0, enemyRb.velocity.y);

            // Constrain the x movement of the rigidbody to prevent sliding in slopes.            
            enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

            // Set the idle animation.
            enemyRig.SetInteger("State", 0);
        }
    }

    // Methods called by the animations.

    private void StartAttack()
    {
        attack = true;
    }

    private void EndAttack()
    {
        attack = false;
        enemyRig.SetBool("attacking", attack);

        // Stop movement.
        enemyRb.velocity = new Vector2(0, enemyRb.velocity.y);

        // Set the idle animation.
        enemyRig.SetInteger("State", 0);

        // Constrain the x movement of the rigidbody after attacking to prevent sliding in slopes.   
        enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        // Reset the attack timer.
        attackTimer = Time.time + attackWait;
    }

    private void ActivateWeaponCollider()
    {
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
            enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (facingRight)
                enemyRb.AddForce(new Vector2(weapon.wForce, 0f));
            else
                enemyRb.AddForce(new Vector2(-weapon.wForce, 0f));
        }
    }

    private void StartDamaged()
    {
        damaged = true;

        attack = false;
        enemyRig.SetBool("attacking", attack);

        // Turn off the weapon collider.
        weapon.wCollider.enabled = false;

        // Stop movement.
        enemyRb.velocity = new Vector2(0, enemyRig.velocity.y);

        // Set the idle animation.
        enemyRig.SetInteger("State", 0);

        // Constrain the x movement of the rigidbody after attacking to prevent sliding in slopes.   
        enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

    }

    private void EndDamaged()
    {
        damaged = false;
        enemyRig.SetBool("damaged", damaged);
    }

    private void SetDead()
    {
        attack = false;
        enemyRig.SetBool("attacking", attack);

        // Turn off the weapon collider.
        weapon.wCollider.enabled = false;

        // Stop movement.
        enemyRb.velocity = new Vector2(0, enemyRb.velocity.y);

        // Set the idle animation.
        enemyRig.SetInteger("State", 0);

        // Constrain the rigidbody.   
        enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        // Switch colliders to prevent collision with enemies
        GetComponent<CapsuleCollider2D>().enabled = false;
        transform.Find("DeadCollider").GetComponent<CapsuleCollider2D>().enabled = true;
    }

    // Methods called by collisions.

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If a player weapon hit the enemy...
        if (other.tag == "Weapon")
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
                    enemyRig.SetBool("damaged", true);
                }
                else if (life <= 0)
                {
                    // Set the dead animation.
                    enemyRig.SetBool("dead", true);
                }
            }
        }
    }
}
