using Unity.Entities;
using UnityEngine;
using System;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Higo.Camera;
using Unity.Transforms;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[AlwaysSynchronizeSystem]
public class InputSystem : ComponentSystem, InputActions.IPlayerActions
{
    InputActions m_actions;

    float2 m_movement;
    float2 m_looking;
    float m_firing;
    bool m_jumped;

    bool m_cursorHide;
    bool CursorHide
    {
        set
        {
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
            m_cursorHide = value;

            var p = m_actions.Player;
            if (value)
            {
                p.Look.Enable();
                p.Move.Enable();
                p.Fire.Enable();
                p.Jump.Enable();
            }
            else
            {
                p.Look.Disable();
                p.Move.Disable();
                p.Fire.Disable();
                p.Jump.Disable();
            }
        }

        get => m_cursorHide;
    }

    float2 currentMovement;
    float2 movementCurrentVelocity;


    protected override void OnCreate()
    {
        RequireSingletonForUpdate<NetworkIdComponent>();

        m_actions = new InputActions();
        m_actions.Player.SetCallbacks(this);
        CursorHide = false;
    }

    protected override void OnDestroy()
    {
        m_actions.Dispose();
        Cursor.lockState = CursorLockMode.None;
        CursorHide = false;
    }

    protected override void OnStartRunning() => m_actions.Enable();

    protected override void OnStopRunning() => m_actions.Disable();

    protected override void OnUpdate()
    {
        var localInput = GetSingleton<CommandTargetComponent>().targetEntity;
        if (Entity.Null == localInput)
        {
            var localPlayerId = GetSingleton<NetworkIdComponent>().Value;
            Entities.WithAll<CharacterComponent>().WithNone<PlayerInputComponent>().ForEach((Entity ent, ref GhostOwnerComponent ghostOwner) =>
            {
                if (ghostOwner.NetworkId == localPlayerId)
                {
                    PostUpdateCommands.AddBuffer<PlayerInputComponent>(ent);
                    PostUpdateCommands.SetComponent(GetSingletonEntity<CommandTargetComponent>(), new CommandTargetComponent { targetEntity = ent });
                }
            });
            return;
        }

        if (HasSingleton<CameraControllerComponent>())
        {
            var controller = GetSingleton<CameraControllerComponent>();
            controller.Axis = !CursorHide ? float2.zero : m_looking;
            SetSingleton(controller);
        }

        var input = default(PlayerInputComponent);
        currentMovement = mathEx.SmoothDamp(currentMovement, m_movement, ref movementCurrentVelocity, 0.5f, Time.DeltaTime);
        if (math.EPSILON > math.abs(currentMovement.x)) currentMovement.x = 0;
        if (math.EPSILON > math.abs(currentMovement.y)) currentMovement.y = 0;
        input.Movement = currentMovement;
        input.MovementRaw = m_movement;
        input.Jump = m_jumped;
        input.Tick = World.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick;

        if (HasSingleton<CameraBrainComponent>())
        {
            var brain = GetSingleton<CameraBrainComponent>();
            if (Entity.Null != brain.CurrentCamera)
            {
                var l2w = EntityManager.GetComponentData<LocalToWorld>(brain.CurrentCamera);
                input.CameraForward = l2w.Forward;
            }
        }
        var inputBuffer = EntityManager.GetBuffer<PlayerInputComponent>(localInput);
        inputBuffer.AddCommandData(input);
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        m_firing = context.ReadValue<float>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        var src = context.ReadValue<Vector2>();
        m_looking.x = math.radians(src.x);
        m_looking.y = math.radians(src.y);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        m_movement = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        m_jumped = context.ReadValueAsButton();
    }

    public void OnCursor(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CursorHide = !CursorHide;
        }
    }
}
