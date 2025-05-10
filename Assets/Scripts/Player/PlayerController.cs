using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [Header("Behaviours")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerAnimation playerAnimation;

    [Header("Input Settings")]
    public PlayerInput playerInput;
    private Vector2 MovementInput;



    private void Update()
    {
        // Update timers

        // Update movement
        playerMovement.UpdateMovement(MovementInput);
        // Update animations
        // playerAnimation.UpdateAnimations();
    }

    void FixedUpdate() { }

    public void Awake()
    {
        // gameObject.layer = Layers.PlayerLayer;
        // if (hitboxTransform != null)
        //     hitboxTransform.gameObject.layer = Layers.PlayerLayer;
    }
    // #region Input Callbacks

    public void OnMove(InputAction.CallbackContext value)
    {
        MovementInput = value.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext value)
    {
        if (value.started)
            playerMovement.Jump();
        // isJumpPressed = true;
        // isJumpReleased = false;
        // lastJumpPressedTime = jumpBufferTime;

        // // Handle double jump
        // if (lastGroundedTime <= 0 && jumpCount < maxJumps && !isDashing && !isRolling)
        // {
        //     Jump();
        // }
    }

    // private void OnJumpReleased(InputAction.CallbackContext context)
    // {
    //     isJumpPressed = false;
    //     isJumpReleased = true;
    // }

    public void OnDash(InputAction.CallbackContext value)
    {
        if (value.started)
            playerMovement.Dash();
    }

    public void OnRoll(InputAction.CallbackContext value)
    {
        if (value.started)
            playerMovement.Roll();
    }

    // #endregion




}