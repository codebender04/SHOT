using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : Singleton<GameInput>
{
    private InputSystem_Actions actions;

    public Vector2 AimScreenPosition => actions.Gameplay.Aim.ReadValue<Vector2>();

    public event Action ShootPressed;
    public event Action PausePressed;
    private bool shootBlocked;

    private void Awake()
    {
        actions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        actions.Enable();

        actions.Gameplay.Shoot.performed += OnShootPerformed;
    }

    private void OnDisable()
    {
        actions.Gameplay.Shoot.performed -= OnShootPerformed;

        actions.Disable();
    }
    private void Update()
    {
        if (shootBlocked && !actions.Gameplay.Shoot.IsPressed())
            shootBlocked = false;
    }
    public void BlockShootUntilReleased()
    {
        shootBlocked = true;
    }
    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        if (shootBlocked)
            return;

        ShootPressed?.Invoke();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        PausePressed?.Invoke();
    }
}