using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerMovingComponent))]
public class Player : Character
{
    protected override void Awake()
    {
        base.Awake();

        PlayerInput input = GetComponent<PlayerInput>();
        InputActionMap actionMap = input.actions.FindActionMap("Player");

        actionMap.FindAction("Fist").started += context =>
        {
            weapon.SetFistMode();
        };

        actionMap.FindAction("Sword").started += context =>
        {
            weapon.SetSwordMode();
        };

        actionMap.FindAction("Hammer").started += context =>
        {
            weapon.SetHammerMode();
        };

        actionMap.FindAction("FireBall").started += context =>
        {
            weapon.SetFireBallMode();
        };


        actionMap.FindAction("Action").started += context =>
        {
            weapon.DoAction();
        };
    }

    private void OnGUI()
    {
        
    }
}