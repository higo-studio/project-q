// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Physics.GraphicsIntegration;
// // using Unity.Physics.Stateful;
// using Unity.Transforms;
// using Unity.NetCode;
// // override the behavior of BufferInterpolatedRigidBodiesMotion
// [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
// // [UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(ExportPhysicsWorld))]
// [/* UpdateAfter(typeof(BufferInterpolatedRigidBodiesMotion)),  */UpdateBefore(typeof(CharacterControllerStepSystem))]
// public class BufferInterpolatedCharacterControllerMotion : SystemBase
// {
//     CharacterControllerStepSystem m_CharacterControllerSystem;

//     protected override void OnCreate()
//     {
//         base.OnCreate();
//         m_CharacterControllerSystem = World.GetOrCreateSystem<CharacterControllerStepSystem>();
//     }

//     protected override void OnUpdate()
//     {
//         Dependency = Entities
//             .WithName("UpdateCCInterpolationBuffers")
//             .WithNone<PhysicsExclude>()
//             .WithBurst()
//             .ForEach((ref PhysicsGraphicalInterpolationBuffer interpolationBuffer, in CharacterControllerInternalData ccInternalData, in Translation position, in Rotation orientation) =>
//             {
//                 interpolationBuffer = new PhysicsGraphicalInterpolationBuffer
//                 {
//                     PreviousTransform = new RigidTransform(orientation.Value, position.Value),
//                     PreviousVelocity = ccInternalData.Velocity,
//                 };
//             }).ScheduleParallel(Dependency);

//         m_CharacterControllerSystem.AddInputDependency(Dependency);
//     }
// }
