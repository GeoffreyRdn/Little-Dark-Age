using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Health;
using Inventory;
using NaughtyAttributes;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

//[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IPunInstantiateMagicCallback
{
    [BoxGroup("Player")] [SerializeField] private float walkSpeed = 8.0f;
    [BoxGroup("Player")] [SerializeField] private float crouchSpeed = 4.0f;
    [BoxGroup("Player")] [SerializeField] private float animationSpeed = 2.0f;
    [BoxGroup("Player")] [SerializeField] private float runSpeed = 12f;
    [BoxGroup("Player")] [SerializeField] private float maxSpeed = 15f;
    
    [SerializeField] private string bossScene;
    
    [SerializeField] private InventoryController inventory;
    [SerializeField] private PauseMenu pauseMenu;
    
    [BoxGroup("Camera")] [SerializeField] Camera cam;
    // [BoxGroup("Camera")] [SerializeField] GameObject displayCamera;


    private float mouseYVelocity;
    
    private CharacterController controller;
    private Transform playerTransform;
    private PhotonView pv;
    private Vector3 playerVelocity;


    public static bool isInInventory = false;
    public static bool isInPauseMenu = false;
    
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
    private float delay;

    private Animator animator;

    #endregion
    
    #region Events

    private void OnEnable()
    {
        HealthController.onPlayerDeath += HandlePlayerDeath;
        HealthController.onDungeonComplete += HandleDungeonComplete;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        HealthController.onPlayerDeath -= HandlePlayerDeath;
        HealthController.onDungeonComplete -= HandleDungeonComplete;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region Start - Update

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        
        if (PhotonNetwork.IsConnectedAndReady && !pv.IsMine)
        {
            cam.gameObject.SetActive(false);
            gameObject.GetComponent<PlayerInput>().enabled = false;
            enabled = false;
            return;
        }

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

        delay = 0;
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        pv = GetComponent<PhotonView>();
        playerTransform = controller.gameObject.transform;

        Cursor.visible = false;
    }

    void Update()
    {
        if (!pv.IsMine) return;
        if (isLoadingScene) delay = 1f;

        if (isInInventory && InputManager.Instance.PlayerCloseInventory())
        {
            CloseInventory();
            return;
        }
        if (InputManager.Instance.PlayerOpenInventory()) OpenInventory();
        
        if (isInPauseMenu && InputManager.Instance.PlayerClosePauseMenu())
        {
            ClosePauseMenu();
            return;
        }
        if (InputManager.Instance.PlayerOpenPauseMenu()) OpenPauseMenu();

        if (InputManager.Instance.BossTeleport()) LoadBossScene();
        
        
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
    
    public void LoadBossScene()
    {
        PhotonNetwork.LoadLevel(bossScene);
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


    private void HandleDungeonComplete()
    {
        pv.RPC(nameof(EnableWinMenu), RpcTarget.AllBuffered);
        StartCoroutine(DelayLoadBossScene());
    }

    private IEnumerator DelayLoadBossScene()
    {
        yield return new WaitForSeconds(5);
        LoadBossScene();
    } 

    [PunRPC]
    private void EnableWinMenu()
    {
        // TODO : enable countdown prefab for each player
    }
    
    private void CloseInventory()
    {
        // change action map
        InputManager.Instance.CloseInventory();
        // close inventory
        inventory.OpenOrCloseInventory();
        
        Cursor.visible = false;
        isInInventory = false;
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

    private void OpenPauseMenu()
    {
        // change action maps
        InputManager.Instance.OpenPauseMenu();
        // open inventory
        pauseMenu.OpenOrClosePauseMenu();
            
        Cursor.visible = true;
        isInPauseMenu = true;
    }

    private void ClosePauseMenu()
    {
        // change action maps
        InputManager.Instance.ClosePauseMenu();
        // open inventory
        pauseMenu.OpenOrClosePauseMenu();
            
        Cursor.visible = false;
        isInPauseMenu = false;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        => ResetCameras();

    private void ResetCameras()
    {
        var players = PhotonNetwork.CurrentRoom.Players.Values
            .Select(player => player.TagObject as GameObject)
            .ToList();
        
        var currPlayer = PhotonNetwork.LocalPlayer.TagObject as GameObject;

        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i].GetComponent<PlayerController>();
            player.cam.gameObject.SetActive(currPlayer == players[i]);
        }
    }

    #endregion
    
    #region Multiplayer
    public void OnPhotonInstantiate(PhotonMessageInfo info)
        => info.Sender.TagObject = gameObject;
    
    #endregion
}