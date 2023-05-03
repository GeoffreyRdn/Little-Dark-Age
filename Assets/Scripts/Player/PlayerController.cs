using System;
using System.Collections.Generic;
using Health;
using Inventory;
using Photon.Pun;
using UnityEngine;

//[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IPunInstantiateMagicCallback
{
    [SerializeField] private float walkSpeed = 8.0f;
    [SerializeField] private float crouchSpeed = 4.0f;
    [SerializeField] private float animationSpeed = 2.0f;
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float maxSpeed = 15f;
    
    [SerializeField] private InventoryController inventory;

    private float mouseYVelocity;

    private DamageBehavior damageBehavior;
    private CharacterController controller;
    private Transform playerTransform;
    private PhotonView pv;
    private Vector3 playerVelocity;

    private bool isInInventory;
    
    public static bool isLoadingScene = false;
    public static bool applyGravity = true;
    
    #region Animations

    private static readonly int IdleAnimation = Animator.StringToHash("idle");
    
    private static readonly int FrontWalkAnimation = Animator.StringToHash("walk");
    private static readonly int FrontRunAnimation = Animator.StringToHash("front_run");
    
    private static readonly int BackWalkAnimation = Animator.StringToHash("back_walk");
    private static readonly int BackRunAnimation = Animator.StringToHash("back_run");
    
    private static readonly int BackRightWalkAnimation = Animator.StringToHash("back_right_walk");
    private static readonly int BackRightRunAnimation = Animator.StringToHash("back_right_run");
    
    private static readonly int BackLeftWalkAnimation = Animator.StringToHash("back_left_walk");
    private static readonly int BackLeftRunAnimation = Animator.StringToHash("back_left_run");
    
    private static readonly int FrontRightWalkAnimation = Animator.StringToHash("front_right_walk");
    private static readonly int FrontRightRunAnimation = Animator.StringToHash("front_right_run");
    
    private static readonly int FrontLeftWalkAnimation = Animator.StringToHash("front_left_walk");
    private static readonly int FrontLeftRunAnimation = Animator.StringToHash("front_left_run");
    
    
    private static readonly int AttackAnimation = Animator.StringToHash("attack");
    private static readonly int HitAnimation = Animator.StringToHash("hit");
    private static readonly int DeathAnimation = Animator.StringToHash("death");
    private static readonly int JumpAnimation = Animator.StringToHash("jump");
    
    private static readonly int CrouchEnterAnimation = Animator.StringToHash("crouch_enter");
    private static readonly int CrouchStayAnimation = Animator.StringToHash("crouch_stay");
    private static readonly int CrouchAttackAnimation = Animator.StringToHash("crouch_attack");
    private static readonly int CrouchHitAnimation = Animator.StringToHash("crouch_hit");
    private static readonly int CrouchShieldAnimation = Animator.StringToHash("crouch_shield");
    private static readonly int CrouchLeaveAnimation = Animator.StringToHash("crouch_leave");
    
    private static readonly int ShieldEnterAnimation = Animator.StringToHash("shield_enter");
    private static readonly int ShieldStayAnimation = Animator.StringToHash("shield_stay");
    private static readonly int ShieldHitAnimation = Animator.StringToHash("shield_hit");
    private static readonly int ShieldLeaveAnimation = Animator.StringToHash("shield_leave");

    private List<int> lowerSpeedAnimation;

    private bool isAttacking;
    private bool isHit;
    private bool isRunning;
    private bool isCrouching;
    private bool isShielding;
    private bool isJumping;

    public bool IsShielding => isShielding;
    
    private float lockedUntil;
    private int currentState;

    private Animator animator;

    #endregion
    
    #region Events

    private void OnEnable()
    {
        HealthController.onPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        HealthController.onPlayerDeath -= HandlePlayerDeath;
    }

    #endregion

    #region Start - Update

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        if (!pv.IsMine) return;

        lowerSpeedAnimation = new List<int>
        {
            ShieldEnterAnimation,
            ShieldHitAnimation,
            ShieldLeaveAnimation,
            ShieldStayAnimation,
            CrouchShieldAnimation,
            CrouchAttackAnimation,
            AttackAnimation
        };

        damageBehavior = GetComponentInChildren<DamageBehavior>();  
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        pv = GetComponent<PhotonView>();
        playerTransform = controller.gameObject.transform;

        Cursor.visible = false;
    }

    void Update()
    {
        if (!pv.IsMine || isLoadingScene) return;

        if (isInInventory)
        {
            HandleInventory();
            return;
        }

        if (InputManager.Instance.PlayerOpenInventory()) OpenInventory();
        
        mouseYVelocity = InputManager.Instance.OnRotate();
        isRunning = InputManager.Instance.PlayerIsRunning();
        isCrouching = InputManager.Instance.PlayerIsSneaking();
        isAttacking = InputManager.Instance.PlayerIsAttacking();
        isShielding = InputManager.Instance.PlayerIsShield();
        isJumping = InputManager.Instance.PlayerJumpedThisFrame();
        
        var state = GetState();
        
        UpdateOrientation();
        MovePlayer();

        if (state == currentState) return;

        animator.CrossFade(state, 0, 0);
        currentState = state;
    }
    
    #endregion
    
    #region Animations

    private int LockState(int s, float t)
    {
        lockedUntil = Time.time + t;
        return s;
    }

    
    private int HandleShield()
    {
        // SHIELD WHILE IN CROUCH
        if (currentState == CrouchStayAnimation || currentState == CrouchEnterAnimation || currentState == CrouchShieldAnimation)
        {
            // if (isHit) return LockState(ShieldHitAnimation, .5f);
            return CrouchShieldAnimation;
        }
            
        // SHIELD STAY STAND UP
        // CROUCH ENTER STAND UP
        return currentState == ShieldEnterAnimation || currentState == ShieldStayAnimation
            ? ShieldStayAnimation
            : LockState(ShieldEnterAnimation, .46f);
    }
    

    private int HandleCrouch()
    {
        // CROUCH STAY OR HIT
        if (currentState == CrouchEnterAnimation || currentState == CrouchStayAnimation)
        {
            return isHit ? LockState(CrouchHitAnimation, .5f) : CrouchStayAnimation;
        }
            
        // CROUCH ENTER
        return LockState(CrouchEnterAnimation, .56f);
    }
    
    
    private int GetState()
    {
        if (currentState == AttackAnimation) isAttacking = false;
        if (currentState == HitAnimation) isHit = false;

        if (Time.time < lockedUntil) return currentState;
        
        
        // ATTACK PRESSED PRIORITY
        if (isAttacking) return LockState(isCrouching ? CrouchAttackAnimation : AttackAnimation, 1.4f);

        
        // SHIELD
        if (isShielding) return HandleShield();

        // LEAVING SHIELD
        if (currentState == ShieldStayAnimation || currentState == ShieldEnterAnimation)
            return LockState(ShieldLeaveAnimation, .3f);
        
        // LEAVING SHIELD WHILE CROUCHED
        if (currentState == CrouchShieldAnimation) return CrouchStayAnimation;
        
        
        // JUMP
        if (isJumping) return LockState(JumpAnimation, .7f);
        
        
        // CROUCH
        if (isCrouching) return HandleCrouch();
        
        // CROUCH LEAVE
        if (currentState == CrouchStayAnimation || currentState == CrouchEnterAnimation)
            return LockState(CrouchLeaveAnimation, .45f);
        
        
        // PLAYER HIT
        if (isHit) return LockState(HitAnimation, .5f);
        
        
        // MOVEMENT
        Vector2 movement = InputManager.Instance.GetPlayerMovement();
        Debug.Log(movement);

        switch (movement.y)
        {
            case < 0:   
                return movement.x switch
                {
                    // BACK
                    0 => isRunning ? BackRunAnimation : BackWalkAnimation,
                    // FRONT LEFT
                    < 0 => isRunning ? FrontLeftRunAnimation : FrontLeftWalkAnimation,
                    // BACK RIGHT
                    _ => isRunning ? BackRightRunAnimation : BackRightWalkAnimation
                };
            case > 0:
                return movement.x switch
                {
                    // FRONT
                    0 => isRunning ? FrontRunAnimation : FrontWalkAnimation,
                    // BACK LEFT
                    < 0 => isRunning ? BackLeftRunAnimation : BackLeftWalkAnimation,
                    // FRONT RIGHT
                    _ => isRunning ? FrontRightRunAnimation : FrontRightWalkAnimation
                };
        }

        // RIGHT
        if (movement.x > 0) return isRunning ? FrontRightRunAnimation : FrontRightWalkAnimation;

        // LEFT
        if (movement.x < 0) return isRunning ? FrontLeftRunAnimation : FrontLeftWalkAnimation;

        return IdleAnimation;
    }
    
    #endregion
    
    #region Methods

    private float Speed()
    {
        var moveSpeed = lowerSpeedAnimation.Contains(currentState)
            ? animationSpeed
            : isCrouching
                ? crouchSpeed
                : isRunning
                    ? runSpeed
                    : walkSpeed;

        return Mathf.Clamp(moveSpeed, 0, maxSpeed);
    }

    private void UpdateOrientation()
    {
        Vector2 rotation = new Vector2(0, playerTransform.eulerAngles.y);
        rotation.y += mouseYVelocity;
        playerTransform.eulerAngles = new Vector2(0, rotation.y);
    }
    
    private void MovePlayer()
    {
        if (applyGravity) controller.SimpleMove(Vector3.zero);
        
        Vector2 movement = InputManager.Instance.GetPlayerMovement();
        Vector3 move = new Vector3(movement.x, 0f, movement.y);
        controller.Move(Speed() * Time.deltaTime * playerTransform.TransformDirection(move));
    }

    private void HandlePlayerDeath(GameObject playerDead)
    {
        Debug.Log("A player is dead");
        if (playerDead == gameObject) gameObject.SetActive(false);
    }


    private void HandleInventory()
    {
        if (InputManager.Instance.PlayerCloseInventory())
        {
            // change action map
            InputManager.Instance.CloseInventory();
            // close inventory
            inventory.OpenOrCloseInventory();
            
            Cursor.visible = false;
            isInInventory = false;
        }
    }

    private void OpenInventory()
    {
        // change action maps
        InputManager.Instance.OpenInventory();
        // open inventory
        inventory.OpenOrCloseInventory();
            
        Cursor.visible = true;
        isInInventory = true;
    }
    
    public void StartDealingDamage()
        => damageBehavior?.StartDealingDamage();
        
    public void StopDealingDamage()
        => damageBehavior?.StopDealingDamage();

    #endregion
    
    #region Multiplayer
    public void OnPhotonInstantiate(PhotonMessageInfo info)
        => info.Sender.TagObject = gameObject;
    
    #endregion
}