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
        playerAnimation.UpdateAnimations(playerMovement.IsGrounded(), MovementInput);
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
        {
            playerMovement.SetJumpPressed(true);
            playerMovement.Jump();
            playerAnimation.PlayJumpAnimation();
        }
        if (value.canceled)
        {
            playerMovement.SetJumpPressed(false);
        }
    }

    public void OnDash(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            playerMovement.Dash();
            playerAnimation.PlayDashAnimation();
        }
    }

    public void OnRoll(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            playerMovement.Roll();
            playerAnimation.PlayRollAnimation();
        }
    }
    public void OnAttack(InputAction.CallbackContext value)
    {
        if (value.started)
            playerCombat.Attack();
    }

    public void OnSpecialAttack(InputAction.CallbackContext value)
    {
        if (value.started)
            playerCombat.SpecialAttack();
    }

    public void OnAbility1(InputAction.CallbackContext value)
    {
        if (value.started)
            playerCombat.UseAbility(0);
    }

    // #endregion




}