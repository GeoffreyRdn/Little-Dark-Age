using Photon.Pun;
using UnityEngine;

public class InputManager : MonoBehaviourPunCallbacks
{
    public static InputManager Instance;
    private PlayerControls playerControls;

    private void Awake()
    {
        playerControls = new PlayerControls();
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    public bool PlayerIsAttacking()
        => playerControls.Ground.Attack.triggered;

    public bool PlayerIsShield()
        => playerControls.Ground.Shield.IsPressed();

    public Vector2 GetPlayerMovement()
        => playerControls.Ground.Movement.ReadValue<Vector2>().normalized;

    public float OnRotate()
        => playerControls.Ground.Rotate.ReadValue<float>() * 0.15f;

    public bool PlayerJumpedThisFrame()
        => playerControls.Ground.Jump.IsPressed();

    public bool PlayerIsSneaking()
        => playerControls.Ground.Crouch.IsPressed();

    public bool PlayerIsRunning()
        => playerControls.Ground.Run.IsPressed();
}