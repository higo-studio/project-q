using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Higo.Animation.Controller;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class CharacterAnimatorSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Assign values to local variables captured in your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
        //     float deltaTime = Time.DeltaTime;

        // This declares a new kind of job, which is a unit of work to do.
        // The job is declared as an Entities.ForEach with the target components as parameters,
        // meaning it will process all entities in the world that have both
        // Translation and Rotation components. Change it to process the component
        // types you want.


        var bufferFromEntity = GetBufferFromEntity<AnimationControllerStateParamBuffer>();
        Entities.ForEach((in CharacterRenderStateComponent render, in CharacterControllerComponentData cc) =>
        {
            if (!bufferFromEntity.HasComponent(render.Value)) return;
            var stateParamBuffer = bufferFromEntity[render.Value];
            var factory = cc.Movement.y * (cc.MovementSpeed / cc.MaxMovementSpeed);
            UnityEngine.Debug.Log(factory);
            for (var i = 0; i < stateParamBuffer.Length; i++)
            {
                ref var paramData = ref stateParamBuffer.ElementAt(i);
                paramData.ParamX = factory;
            }
        }).Schedule();
    }
}
