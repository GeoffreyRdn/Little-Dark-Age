using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Health;
using Inventory;
using Items;
using NaughtyAttributes;
using Photon.Pun;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour, IPunInstantiateMagicCallback
{
    [BoxGroup("Player")] [SerializeField] private float walkSpeed = 8.0f;
    [BoxGroup("Player")] [SerializeField] private float crouchSpeed = 4.0f;
    [BoxGroup("Player")] [SerializeField] private float animationSpeed = 2.0f;
    [BoxGroup("Player")] [SerializeField] private float runSpeed = 12f;
    [BoxGroup("Player")] [SerializeField] private float maxSpeed = 15f;

    [BoxGroup("Sounds"), SerializeField] private AudioClip walkSound;
    [BoxGroup("Sounds"), SerializeField] private AudioClip runSound;
    [BoxGroup("Sounds"), SerializeField] private AudioClip jumpSound;
    [BoxGroup("Sounds"), SerializeField] private AudioClip attackSound;
    
    [BoxGroup("Sounds"), SerializeField] private AudioClip drawShieldSound;
    [BoxGroup("Sounds"), SerializeField] private AudioClip sheatheShieldSound;
    
    [BoxGroup("Sounds"), SerializeField] private AudioClip deathSound;
    [BoxGroup("Sounds"), SerializeField] private AudioClip hurtSound;
    
    [BoxGroup("Sounds"), SerializeField] private AudioClip gameOverSound;
    [BoxGroup("Sounds"), SerializeField] private AudioClip victorySound;
    
    [SerializeField] private string bossScene;
    
    [SerializeField] private InventoryController inventory;
    [SerializeField] private ShopController shop;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] public GameObject loadingScreen;
    
    [BoxGroup("Camera")] [SerializeField] Camera cam;
    // [BoxGroup("Camera")] [SerializeField] GameObject displayCamera;
    
    [BoxGroup("Weapons")] [SerializeField] private GameObject activeWeapon;
    [BoxGroup("Weapons")] [SerializeField] private GameObject axe;
    [BoxGroup("Weapons")] [SerializeField] private GameObject dagger;
    [BoxGroup("Weapons")] [SerializeField] private GameObject hammer;
    [BoxGroup("Weapons")] [SerializeField] private GameObject spear;
    [BoxGroup("Weapons")] [SerializeField] private GameObject sword;
    [BoxGroup("Weapons")] [SerializeField] private GameObject shield;
    
    [BoxGroup("UI"), SerializeField] private GameObject winDungeonText;
    [BoxGroup("UI"), SerializeField] private GameObject gameOverText;
    [BoxGroup("UI"), SerializeField] private GameObject winBossText;
    [BoxGroup("UI"), SerializeField] private TextMeshProUGUI nbEnemiesText;

    private float mouseYVelocity;
    
    private CharacterController controller;
    private Transform playerTransform;
    private PhotonView pv;
    private Vector3 playerVelocity;


    public AudioSource audioSource;
    public static bool isInInventory = false;
    public static bool isInPauseMenu = false;
    public static bool isInShop = false;
    
    public static bool applyGravity = true;
    
    #region Animations

    public static readonly int IdleAnimation = Animator.StringToHash("idle");
    
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
    
    public static readonly int ShieldEnterAnimation = Animator.StringToHash("shield_enter");
    public static readonly int ShieldStayAnimation = Animator.StringToHash("shield_stay");
    private static readonly int ShieldHitAnimation = Animator.StringToHash("shield_hit");
    private static readonly int ShieldLeaveAnimation = Animator.StringToHash("shield_leave");

    private List<int> lowerSpeedAnimation;

    private bool isAttacking;
    private bool isHit;
    private bool isRunning;
    public bool isDead;
    private bool isCrouching;
    private bool isShielding;
    private bool isJumping;

    public bool IsShielding => isShielding;
    
    private float lockedUntil;
    public int currentState;

    private Animator animator;

    #endregion
    
    #region Events

    private void OnEnable()
    {
        HealthController.onPlayerDeath += HandlePlayerDeath;
        HealthController.onDungeonComplete += HandleDungeonComplete;
        DamageBehavior.onPlayerDamaged += HandlePlayerDamaged;
        EnemyInstantiation.onEnemiesSpawned += UpdateEnemiesRemainingText;
        HealthController.onEnemyDeath += UpdateEnemiesRemainingText;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        HealthController.onPlayerDeath -= HandlePlayerDeath;
        HealthController.onDungeonComplete -= HandleDungeonComplete;
        DamageBehavior.onPlayerDamaged -= HandlePlayerDamaged;
        EnemyInstantiation.onEnemiesSpawned -= UpdateEnemiesRemainingText;
        HealthController.onEnemyDeath -= UpdateEnemiesRemainingText;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region Start - Update

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        loadingScreen.SetActive(false);
        
        var maxHealth = GetComponent<HealthController>().MaxHealth;
        var health = GetComponent<HealthController>().Health;
        UpdateHealthBar(health, maxHealth);
        
        if (PhotonNetwork.IsConnectedAndReady && !pv.IsMine)
        {
            cam.gameObject.SetActive(false);
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
        
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        pv = GetComponent<PhotonView>();
        playerTransform = controller.gameObject.transform;
        audioSource = GetComponent<AudioSource>();

        Cursor.visible = false;
    }

    void Update()
    {
        if (!pv.IsMine) return;
        if (currentState == DeathAnimation) return;
        
        if (isDead)
        {
            animator.CrossFade(DeathAnimation, 0, 0);
            currentState = DeathAnimation;
            pv.RPC(nameof(SendAnimations), RpcTarget.Others, currentState);
            return;
        }

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

        if (isInShop && InputManager.Instance.PlayerCloseShop()){
            CloseShopMenu();
            return;
        }
        if (InputManager.Instance.PlayerOpenShop()) OpenShopMenu();


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
        pv.RPC(nameof(SendAnimations), RpcTarget.Others, currentState);
    }
    
    #endregion
    
    #region Animations
    

    [PunRPC]
    private void SendAnimations(int state, PhotonMessageInfo info)
    {
        if (info.Sender.TagObject is GameObject sender)
        {
            var playerAnimator = sender.GetComponentInChildren<Animator>();
            playerAnimator.CrossFade(state, 0, 0);

            var pc = sender.GetComponent<PlayerController>();

            if (state == AttackAnimation)
            {
                pc.audioSource.loop = false;
                pc.audioSource.clip = attackSound;
                pc.audioSource.Play();
            }
            
            else if (state == HitAnimation)
            {
                pc.audioSource.loop = false;
                pc.audioSource.clip = hurtSound;
                pc.audioSource.Play();
            }
            
            else if (state == DeathAnimation)
            {
                pc.audioSource.loop = false;
                pc.audioSource.clip = deathSound;
                pc.audioSource.Play();
            }
            
            else if (state == JumpAnimation)
            {
                pc.audioSource.loop = false;
                pc.audioSource.clip = jumpSound;
                pc.audioSource.Play();
            }
            
            else if (state == ShieldEnterAnimation)
            {
                audioSource.loop = false;
                audioSource.clip = drawShieldSound;
                audioSource.Play();
            }
            
            else if (state == ShieldLeaveAnimation)
            {
                audioSource.loop = false;
                audioSource.clip = sheatheShieldSound;
                audioSource.Play();
            }
            
            else if (state == BackRunAnimation ||
                     state == FrontRunAnimation ||
                     state == BackLeftRunAnimation ||
                     state == BackRightRunAnimation ||
                     state == FrontLeftRunAnimation ||
                     state == FrontRightRunAnimation)
            {
                audioSource.loop = true;
                audioSource.clip = runSound;
                audioSource.Play();
            }

            // WALKING
            else
            {
                audioSource.loop = true;
                audioSource.clip = walkSound;
                audioSource.Play();
            }
        }
    }
    

    [PunRPC]
    private void SendDeath(PhotonMessageInfo info)
    {
        if (info.Sender.TagObject is GameObject sender)
        {
            var playerController = sender.GetComponent<PlayerController>();
            playerController.isDead = true;
        }
    }
    
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
        if (currentState == ShieldEnterAnimation || currentState == ShieldStayAnimation) return ShieldStayAnimation;

        audioSource.loop = false;
        audioSource.clip = drawShieldSound;
        audioSource.Play();
        return LockState(ShieldEnterAnimation, .46f);
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
        if (isAttacking)
        {
            audioSource.loop = false;
            audioSource.clip = attackSound;
            audioSource.Play();
            return LockState(isCrouching ? CrouchAttackAnimation : AttackAnimation, 1.4f);
        }

        
        // SHIELD
        if (isShielding) return HandleShield();

        // LEAVING SHIELD
        if (currentState == ShieldStayAnimation || currentState == ShieldEnterAnimation)
        {
            audioSource.loop = false;
            audioSource.clip = sheatheShieldSound;
            audioSource.Play();
            return LockState(ShieldLeaveAnimation, .3f);
        }
        
        // LEAVING SHIELD WHILE CROUCHED
        if (currentState == CrouchShieldAnimation) return CrouchStayAnimation;
        
        
        // JUMP
        if (isJumping)
        {
            audioSource.loop = false;
            audioSource.clip = jumpSound;
            audioSource.Play();
            return LockState(JumpAnimation, .7f);
        }
        
        
        // CROUCH
        if (isCrouching) return HandleCrouch();
        
        // CROUCH LEAVE
        if (currentState == CrouchStayAnimation || currentState == CrouchEnterAnimation)
            return LockState(CrouchLeaveAnimation, .45f);
        
        
        // PLAYER HIT
        if (isHit)
        {
            audioSource.loop = false;
            audioSource.clip = hurtSound;
            audioSource.Play();
            return LockState(HitAnimation, .5f);
        }
        
        
        // MOVEMENT
        Vector2 movement = InputManager.Instance.GetPlayerMovement();

        if (movement != Vector2.zero)
        {
            audioSource.loop = true;
            audioSource.clip = isRunning ? runSound : walkSound;
            audioSource.Play();
        }

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

        audioSource.Stop();
        return IdleAnimation;
    }
    
    #endregion
    
    #region Methods
    
    public void SetShieldActive(bool active) {
        shield.SetActive(active);
    }

    public void ChangeEquippedWeapon(ItemWeapon weapon) {
        if (weapon == null) {
            activeWeapon.SetActive(false);
            activeWeapon = null;
            return;
        }

        if (activeWeapon != null) {
            activeWeapon.SetActive(false);
        }

        switch (weapon.Id) {
            // Sword
            case 5:
                activeWeapon = sword;
                break;
            // Axe
            case 6:
                activeWeapon = axe;
                break;
            // Dagger
            case 7:
                activeWeapon = dagger;
                break;
            // Hammer
            case 8:
                activeWeapon = hammer;
                break;
            // Spear
            case 9:
                activeWeapon = spear;
                break;
        }
        activeWeapon.SetActive(true);
    }
    
    public void UpdateHealthBar(float health, float maxHealth)
        => healthBar.UpdateHealthBar(health, maxHealth);


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
    
    private void LoadBossScene()
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
        if (playerDead == gameObject)
        {
            audioSource.loop = false;
            audioSource.clip = deathSound;
            audioSource.Play();
            isDead = true;
            
            pv.RPC(nameof(SendDeath), RpcTarget.Others);
            
            if (isInInventory) CloseInventory();
            if (isInShop) CloseShopMenu();
            if (isInPauseMenu) ClosePauseMenu();
            
            InputManager.Instance.DisableActionMap();
            
            var allDead = true;
        
            foreach (var player in PhotonNetwork.PlayerList)
            {
                var currPlayer = player.TagObject as GameObject;
                
                if (currPlayer.TryGetComponent<PlayerController>(out var playerController))
                {
                    allDead &= playerController.isDead;
                }
            }

            if (allDead)
            {
                pv.RPC(nameof(EnableOrDisableLoseMenu), RpcTarget.All, true);
                StartCoroutine(HandleEndGame());
            }
            else
            {
                Debug.Log("SOME PLAYERS ARE STILL ALIVE");
            }
        }
    }
    
    private IEnumerator HandleEndGame()
    {
        yield return new WaitForSeconds(5);
        pv.RPC(nameof(EnableOrDisableLoseMenu), RpcTarget.All, false);
        pv.RPC(nameof(ReturnToLobby), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void EnableOrDisableLoseMenu(bool state)
    {
        audioSource.PlayOneShot(gameOverSound);
        gameOverText.SetActive(state);
    }

    [PunRPC]
    private void ReturnToLobby()
        => (PhotonNetwork.LocalPlayer.TagObject as GameObject)?.GetComponent<PlayerController>().pauseMenu.ReturnToLobby();

    private void HandleDungeonComplete()
    {
        pv.RPC(nameof(EnableWinMenu), RpcTarget.AllBuffered);
        StartCoroutine(DelayLoadBossScene());
    }

    private IEnumerator DelayLoadBossScene()
    {
        yield return new WaitForSeconds(5);
        pv.RPC(nameof(DisableWinMenu), RpcTarget.All);
        LoadBossScene();
    }

    private void HandlePlayerDamaged(GameObject player)
    {
        if (pv.IsMine && player == gameObject)
        {
            isHit = true;
        }
    }

    public void UpdateEnemiesRemainingText(int nbEnemies)
        => pv.RPC(nameof(UpdateEnemiesRemainingTextRPC), RpcTarget.All, nbEnemies);

    [PunRPC]
    private void UpdateEnemiesRemainingTextRPC(int nbEnemies)
    {
        nbEnemiesText.text = nbEnemies +  nbEnemies == 1 ? " Enemy Remaining" : " Enemies Remaining";
        nbEnemiesText.gameObject.SetActive(true);
    }
    
    [PunRPC]
    private void EnableWinMenu()
    {
        audioSource.PlayOneShot(victorySound);
        winDungeonText.SetActive(true);
    }

    [PunRPC]
    private void DisableWinMenu()
        => winDungeonText.SetActive(false); 
    
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
    
    private void OpenShopMenu(){
        // change action maps
        InputManager.Instance.OpenShop();
        // open inventory
        shop.OpenOrCloseInventory();
            
        Cursor.visible = true;
        isInShop  = true;
    }
    
    private void CloseShopMenu(){
        // change action maps
        InputManager.Instance.CloseShop();
        // open or close shop
        shop.OpenOrCloseShopMenu();
            
        Cursor.visible = false;
        isInShop = false;
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
    {
        ResetCameras();
        nbEnemiesText.gameObject.SetActive(scene.name == "Dungeon");
    }

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