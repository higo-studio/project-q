﻿using Unity.Entities;
using UnityEngine;

namespace VertexFragment
{
    /// <summary>
    /// Main control system for player input.
    /// </summary>
    public class PlayerControllerSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            Application.targetFrameRate = 60;    
        }

        protected override void OnUpdate()
        {
            Entities.WithAll<PlayerControllerComponent>().ForEach((
                Entity entity,
                ref CameraFollowComponent camera,
                ref CharacterControllerComponent controller) =>
            {
                ProcessMovement(ref controller, ref camera);
            });
        }

        /// <summary>
        /// Processes the horizontal movement input from the player to move the entity along the xz plane.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="camera"></param>
        private void ProcessMovement(ref CharacterControllerComponent controller, ref CameraFollowComponent camera)
        {
            float movementX = Input.GetAxisRaw("Horizontal");
            float movementZ = Input.GetAxisRaw("Vertical");

            Vector3 forward = new Vector3(camera.Forward.x, 0.0f, camera.Forward.z).normalized;
            Vector3 right = new Vector3(camera.Right.x, 0.0f, camera.Right.z).normalized;

            if (!MathUtils.IsZero(movementX) || !MathUtils.IsZero(movementZ))
            {
                controller.CurrentDirection = ((forward * movementZ) + (right * movementX)).normalized;
                controller.CurrentMagnitude = Input.GetKey(KeyCode.LeftShift) ? 1.5f : 1.0f;
            }
            else
            {
                controller.CurrentMagnitude = 0.0f;
            }

            controller.Jump = Input.GetAxis("Jump") > 0.0f;
        }
    }
}
