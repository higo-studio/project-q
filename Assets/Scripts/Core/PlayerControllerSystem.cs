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

                cc.Movement = math.normalizesafe(input.Movement);
                cc.MovementSpeed = 10f;
                cc.Jumped = input.Jump ? 1 : 0;
                // cc.Looking.x = (int)(input.Looking.x * 1000_000);
                // cc.Looking.y = (int)(input.Looking.y * 1000_000);
                if (math.distancesq(cc.Movement.x, cc.Movement.y) > 0)
                {
                    ccTrs.Value.rot = quaternion.LookRotation(mathEx.ProjectOnPlane(input.Forward, math.up()), math.up());
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
