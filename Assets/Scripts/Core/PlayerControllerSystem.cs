using Unity.Entities;
using UnityEngine;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

namespace VertexFragment
{
    /// <summary>
    /// Main control system for player input.
    /// </summary>
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    // [UpdateBefore(typeof(CharacterControllerStepSystem))]
    public class PlayerControllerSystem : SystemBase
    {
        protected override void OnCreate()
        {
        }

        protected override void OnUpdate()
        {
            var group = World.GetExistingSystem<GhostPredictionSystemGroup>();
            var tick = group.PredictingTick;
            var deltaTime = Time.DeltaTime;
            var worldIdx = World.Name == "ServerWorld" ? 1 : 0;
            Entities.ForEach((
                DynamicBuffer<PlayerInputComponent> inputBuffer,
                ref CharacterControllerComponentData cc,
                ref CharacterControllerTransform ccTrs,
                in PredictedGhostComponent prediction,
                in CharacterComponent character) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(tick, prediction))
                    return;
                inputBuffer.GetDataAtTick(tick, out var input);

                cc.Movement = float2.zero.Equals(input.Movement) ? float2.zero : new float2(0, 1);
                cc.MovementSpeed = 10f;
                cc.Jumped = input.Jump ? 1 : 0;
                // cc.Looking.x = (int)(input.Looking.x * 1000_000);
                // cc.Looking.y = (int)(input.Looking.y * 1000_000);
                if (math.length(cc.Movement) > 0)
                {
                    // Follow Movement
                    ccTrs.Value.rot =
                        math.mul(
                            quaternion.LookRotation(mathEx.ProjectOnPlane(input.CameraForward, math.up()), math.up())
                            , quaternion.LookRotation(math.normalizesafe(new float3(input.Movement.x, 0, input.Movement.y)), math.up())
                        );
                    // Follow Camera Forward
                    // ccTrs.Value.rot = quaternion.LookRotation(mathEx.ProjectOnPlane(input.CameraForward, math.up()), math.up());
                }
            }).ScheduleParallel();
        }
    }

    // [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    // [UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
    // public class DebugClientDeltaTime : SystemBase
    // {
    //     protected override void OnUpdate()
    //     {
    //        Debug.Log("Client: " + Time.DeltaTime);
    //     }
    // }

    // [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    // [UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
    // public class DebugServerDeltaTime : SystemBase
    // {
    //     protected override void OnUpdate()
    //     {
    //        Debug.Log("Server: " + Time.DeltaTime);
    //     }
    // }
}
