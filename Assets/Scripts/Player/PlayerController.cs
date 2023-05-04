using System;
using Health;
using Inventory;
using Photon.Pun;
using UnityEngine;

//[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IPunInstantiateMagicCallback
{
    [SerializeField] private float walkSpeed = 8.0f;
    [SerializeField] private float crouchSpeed = 4.0f;
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float maxSpeed = 15f;
    
    [SerializeField] private float               jumpHeight   = 1.0f;
    [SerializeField] private float               gravityValue = -10;
    [SerializeField] private InventoryController inventory;
    [SerializeField] private ShopController shop;

    private float mouseYVelocity;

    private DamageBehavior damageBehavior;
    private CharacterController controller;
    private Transform playerTransform;
    private PhotonView pv;
    private Vector3 playerVelocity;
    private bool groundedPlayer;

    private bool isInInventory;
    
    public static bool isLoadingScene = false;
    public static bool applyGravity = true;
    
    #region Animations

    private static readonly int IdleAnimation = Animator.StringToHash("idle");
    private static readonly int FrontWalkAnimation = Animator.StringToHash("walk");
    private static readonly int BackWalkAnimation = Animator.StringToHash("back_walk");
    private static readonly int FrontRunAnimation = Animator.StringToHash("front_run");
    private static readonly int BackRunAnimation = Animator.StringToHash("back_run");
    
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

        if (InputManager.Instance.PlayerOpenInventory())
        {
            // change action maps
            InputManager.Instance.OpenInventory();
            // open inventory
            inventory.OpenOrCloseInventory();
            
            Cursor.visible = true;
            isInInventory = true;
        }
        
        if (InputManager.Instance.PlayerOpenShop())
        {
            // change action maps
            InputManager.Instance.OpenShop();
            // open inventory
            shop.OpenOrCloseInventory();
            
            Cursor.visible = true;
            isInInventory  = true;
        }

        
        groundedPlayer = controller.isGrounded;
        mouseYVelocity = InputManager.Instance.OnRotate();
        
        isRunning = InputManager.Instance.PlayerIsRunning();
        isCrouching = InputManager.Instance.PlayerIsSneaking();
        isAttacking = InputManager.Instance.PlayerIsAttacking();
        isShielding = InputManager.Instance.PlayerIsShield();
        isJumping = InputManager.Instance.PlayerJumpedThisFrame();
        
        var state = GetState();
        
        if (!isAttacking)
        {
            UpdateOrientation();
            MovePlayer();
        }

        if (state == currentState) return;

        animator.CrossFade(state, 0, 0);
        currentState = state;
    }
    
    #endregion
    
    #region Animations
    
    private int GetState()
    {
        if (currentState == AttackAnimation) isAttacking = false;
        if (currentState == HitAnimation) isHit = false;

        if (Time.time < lockedUntil) return currentState;
        
        // ATTACK PRESSED
        if (isAttacking) return LockState(isCrouching ? CrouchAttackAnimation : AttackAnimation, 1.5f);

        if (isShielding)
        {
            // TODO : shield while crouch
            
            // SHIELD STAY STAND UP
            if (currentState == ShieldEnterAnimation || currentState == ShieldStayAnimation) return ShieldStayAnimation;
            // CROUCH ENTER STAND UP
            return LockState(ShieldEnterAnimation, .46f);
        }
        
        if (currentState == ShieldStayAnimation) return LockState(ShieldLeaveAnimation, .46f);

        if (isJumping) return LockState(JumpAnimation, .7f);
        
        // PLAYER HIT
        if (isHit) return LockState(HitAnimation, .7f);

        Vector2 movement = InputManager.Instance.GetPlayerMovement();
        
        if (isCrouching)
        {
            // CROUCH STAY
            if (currentState == CrouchEnterAnimation || currentState == CrouchStayAnimation) return CrouchStayAnimation;
            // CROUCH ENTER
            return LockState(CrouchEnterAnimation, .56f);
        }
        // CROUCH LEAVE
        if (currentState == CrouchStayAnimation) return LockState(CrouchLeaveAnimation, .45f);
        
        // PLAYER IS NOT MOVING
        if (movement.x == 0 && movement.y == 0) return IdleAnimation;

        // TODO : movement direction 
        return isRunning ? FrontRunAnimation : FrontWalkAnimation;
        
        int LockState(int s, float t)
        {
            lockedUntil = Time.time + t;
            return s;
        }
    }
    
    #endregion
    
    #region Methods

    private float Speed()
    {
        var moveSpeed = isCrouching
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
        
        if (InputManager.Instance.PlayerCloseShop())
        {
            // change action map
            InputManager.Instance.CloseShop();
            // close inventory
            shop.OpenOrCloseInventory();
            
            Cursor.visible = false;
            isInInventory  = false;
        }
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