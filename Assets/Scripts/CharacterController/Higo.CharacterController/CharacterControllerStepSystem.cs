using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
// using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.NetCode;
using static CharacterControllerUtilities;

[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
// [UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
// [UpdateInGroup(typeof(Unity.NetCode.GhostPredictionSystemGroup))]
public class CharacterControllerStepSystem : SystemBase
{
    const float k_DefaultTau = 0.4f;
    const float k_DefaultDamping = 0.9f;

    // JobHandle m_InputDependency;
    // public JobHandle OutDependency => Dependency;

    // public void AddInputDependency(JobHandle inputDep) =>
    //     m_InputDependency = JobHandle.CombineDependencies(m_InputDependency, inputDep);

    [BurstCompile]
    struct CharacterControllerJob : IJobEntityBatch
    {
        public float DeltaTime;
        public uint Tick;

        [ReadOnly]
        public PhysicsWorld PhysicsWorld;

        public ComponentTypeHandle<CharacterControllerInternalData> CharacterControllerInternalType;
        public ComponentTypeHandle<CharacterControllerTransform> CharacterControllerTransformType;
        public ComponentTypeHandle<Translation> TranslationType;
        public ComponentTypeHandle<Rotation> RotationType;
        [ReadOnly]
        public ComponentTypeHandle<PredictedGhostComponent> PredictComponentType;
        // public BufferTypeHandle<StatefulCollisionEvent> CollisionEventBufferType;
        // public BufferTypeHandle<StatefulTriggerEvent> TriggerEventBufferType;
        [ReadOnly] public ComponentTypeHandle<CharacterControllerComponentData> CharacterControllerComponentType;
        [ReadOnly] public ComponentTypeHandle<PhysicsCollider> PhysicsColliderType;

        // Stores impulses we wish to apply to dynamic bodies the character is interacting with.
        // This is needed to avoid race conditions when 2 characters are interacting with the
        // same body at the same time.
        [NativeDisableParallelForRestriction] public NativeStream.Writer DeferredImpulseWriter;

        public unsafe void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkCCData = batchInChunk.GetNativeArray(CharacterControllerComponentType);
            var chunkCCInternalData = batchInChunk.GetNativeArray(CharacterControllerInternalType);
            var chunkPhysicsColliderData = batchInChunk.GetNativeArray(PhysicsColliderType);
            var chunkCCTrsData = batchInChunk.GetNativeArray(CharacterControllerTransformType);
            var chunkTranslationData = batchInChunk.GetNativeArray(TranslationType);
            var chunkRotationData = batchInChunk.GetNativeArray(RotationType);
            var chunkPredictData = batchInChunk.GetNativeArray(PredictComponentType);

            // var hasChunkCollisionEventBufferType = batchInChunk.Has(CollisionEventBufferType);
            // var hasChunkTriggerEventBufferType = batchInChunk.Has(TriggerEventBufferType);

            // BufferAccessor<StatefulCollisionEvent> collisionEventBuffers = default;
            // BufferAccessor<StatefulTriggerEvent> triggerEventBuffers = default;
            // if (hasChunkCollisionEventBufferType)
            // {
            //     collisionEventBuffers = batchInChunk.GetBufferAccessor(CollisionEventBufferType);
            // }
            // if (hasChunkTriggerEventBufferType)
            // {
            //     triggerEventBuffers = batchInChunk.GetBufferAccessor(TriggerEventBufferType);
            // }

            DeferredImpulseWriter.BeginForEachIndex(batchIndex);

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var prediction = chunkPredictData[i];
                if (!GhostPredictionSystemGroup.ShouldPredict(Tick, prediction))
                    return;
                var ccComponentData = chunkCCData[i];
                var ccInternalData = chunkCCInternalData[i];
                var collider = chunkPhysicsColliderData[i];
                var position = chunkTranslationData[i];
                var rotation = chunkRotationData[i];
                var transform = chunkCCTrsData[i];

                // DynamicBuffer<StatefulCollisionEvent> collisionEventBuffer = default;
                // DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer = default;

                // if (hasChunkCollisionEventBufferType)
                // {
                //     collisionEventBuffer = collisionEventBuffers[i];
                // }

                // if (hasChunkTriggerEventBufferType)
                // {
                //     triggerEventBuffer = triggerEventBuffers[i];
                // }

                // Collision filter must be valid
                if (!collider.IsValid || collider.Value.Value.Filter.IsEmpty)
                    continue;

                var up = math.select(math.up(), -math.normalize(ccComponentData.Gravity),
                    math.lengthsq(ccComponentData.Gravity) > 0f);

                // Character step input
                CharacterControllerStepInput stepInput = new CharacterControllerStepInput
                {
                    World = PhysicsWorld,
                    DeltaTime = DeltaTime,
                    Up = up,
                    Gravity = ccComponentData.Gravity,
                    MaxIterations = ccComponentData.MaxIterations,
                    Tau = k_DefaultTau,
                    Damping = k_DefaultDamping,
                    SkinWidth = ccComponentData.SkinWidth,
                    ContactTolerance = ccComponentData.ContactTolerance,
                    MaxSlope = ccComponentData.MaxSlope,
                    RigidBodyIndex = PhysicsWorld.GetRigidBodyIndex(ccInternalData.Entity),
                    MaxMovementSpeed = ccComponentData.MaxMovementSpeed
                };

                // Character transform

                // NativeList<StatefulCollisionEvent> currentFrameCollisionEvents = default;
                // NativeList<StatefulTriggerEvent> currentFrameTriggerEvents = default;

                // if (ccComponentData.RaiseCollisionEvents != 0)
                // {
                //     currentFrameCollisionEvents = new NativeList<StatefulCollisionEvent>(Allocator.Temp);
                // }

                // if (ccComponentData.RaiseTriggerEvents != 0)
                // {
                //     currentFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Temp);
                // }


                // Check support
                CheckSupport(ref PhysicsWorld, ref collider, stepInput, transform.Value,
                    out ccInternalData.SupportedState, out float3 surfaceNormal, out float3 surfaceVelocity/* ,
                    currentFrameCollisionEvents */);

                // User input
                float3 desiredVelocity = ccInternalData.Velocity;
                HandleUserInput(ccComponentData, stepInput.Up, surfaceVelocity, ref ccInternalData, ref desiredVelocity, ref transform.Value);

                // Calculate actual velocity with respect to surface
                if (ccInternalData.SupportedState == CharacterSupportState.Supported)
                {
                    CalculateMovement(math.forward(transform.Value.rot), stepInput.Up, ccInternalData.IsJumping,
                        ccInternalData.Velocity, desiredVelocity, surfaceNormal, surfaceVelocity, out ccInternalData.Velocity);
                }
                else
                {
                    ccInternalData.Velocity = desiredVelocity;
                }

                // World collision + integrate
                CollideAndIntegrate(stepInput, ccComponentData.CharacterMass, ccComponentData.AffectsPhysicsBodies != 0,
                    collider.ColliderPtr, ref transform.Value, ref ccInternalData.Velocity, ref DeferredImpulseWriter/* ,
                    currentFrameCollisionEvents, currentFrameTriggerEvents */);

                // Update collision event status
                // if (currentFrameCollisionEvents.IsCreated)
                // {
                //     UpdateCollisionEvents(currentFrameCollisionEvents, collisionEventBuffer);
                // }

                // if (currentFrameTriggerEvents.IsCreated)
                // {
                //     UpdateTriggerEvents(currentFrameTriggerEvents, triggerEventBuffer);
                // }

                // Write back and orientation integration
                position.Value = transform.Value.pos;
                rotation.Value = transform.Value.rot;

                // Write back to chunk data
                {
                    chunkCCInternalData[i] = ccInternalData;
                    chunkTranslationData[i] = position;
                    chunkRotationData[i] = rotation;
                    chunkCCTrsData[i] = transform;
                }
            }

            DeferredImpulseWriter.EndForEachIndex();
        }

        private void HandleUserInput(CharacterControllerComponentData ccComponentData, float3 up, float3 surfaceVelocity,
            ref CharacterControllerInternalData ccInternalData, ref float3 linearVelocity, ref RigidTransform trs)
        {
            // Reset jumping state and unsupported velocity
            if (ccInternalData.SupportedState == CharacterSupportState.Supported)
            {
                ccInternalData.IsJumping = false;
                ccInternalData.UnsupportedVelocity = float3.zero;
            }

            // Movement and jumping
            bool shouldJump = false;
            float3 requestedMovementDirection = float3.zero;
            var requestedMovementMag = 1f;
            {
                float3 forward = math.forward(quaternion.identity);
                float3 right = math.cross(up, forward);

                float horizontal = ccComponentData.Movement.x;
                float vertical = ccComponentData.Movement.y;
                bool jumpRequested = ccComponentData.Jumped != 0;
                bool haveInput = (math.abs(horizontal) > float.Epsilon) || (math.abs(vertical) > float.Epsilon);
                if (haveInput)
                {
                    float3 localSpaceMovement = forward * vertical + right * horizontal;
                    float3 worldSpaceMovement = math.rotate(trs.rot, localSpaceMovement);
                    requestedMovementDirection = math.normalize(worldSpaceMovement);
                    requestedMovementMag = math.length(localSpaceMovement);
                }
                shouldJump = jumpRequested && ccInternalData.SupportedState != CharacterSupportState.Unsupported;
            }

            // Turning
            {
                float horizontal = ccComponentData.Looking.x / 1000_000f;
                bool haveInput = (math.abs(horizontal) > float.Epsilon);
                if (haveInput)
                {
                    var userRotationSpeed = horizontal * ccComponentData.RotationSpeed;
                    trs.rot = math.mul(quaternion.RotateY(userRotationSpeed * DeltaTime), trs.rot);
                }
            }

            // Apply input velocities
            {
                if (shouldJump)
                {
                    // Add jump speed to surface velocity and make character unsupported
                    ccInternalData.IsJumping = true;
                    ccInternalData.SupportedState = CharacterSupportState.Unsupported;
                    ccInternalData.UnsupportedVelocity = surfaceVelocity + ccComponentData.JumpUpwardsSpeed * up;
                }
                else if (ccInternalData.SupportedState != CharacterSupportState.Supported)
                {
                    // Apply gravity
                    ccInternalData.UnsupportedVelocity += ccComponentData.Gravity * DeltaTime;
                }
                // If unsupported then keep jump and surface momentum
                linearVelocity = requestedMovementDirection * ccComponentData.MovementSpeed * requestedMovementMag +
                    (ccInternalData.SupportedState != CharacterSupportState.Supported ? ccInternalData.UnsupportedVelocity : float3.zero);
            }
        }

        private void CalculateMovement(float3 forward, float3 up, bool isJumping,
            float3 currentVelocity, float3 desiredVelocity, float3 surfaceNormal, float3 surfaceVelocity, out float3 linearVelocity)
        {

            Rotation surfaceFrame;
            float3 binorm;
            {
                binorm = math.cross(forward, up);
                binorm = math.normalize(binorm);

                float3 tangent = math.cross(binorm, surfaceNormal);
                tangent = math.normalize(tangent);

                binorm = math.cross(tangent, surfaceNormal);
                binorm = math.normalize(binorm);

                surfaceFrame.Value = new quaternion(new float3x3(binorm, tangent, surfaceNormal));
            }

            float3 relative = currentVelocity - surfaceVelocity;
            relative = math.rotate(math.inverse(surfaceFrame.Value), relative);

            float3 diff;
            {
                float3 sideVec = math.cross(forward, up);
                float fwd = math.dot(desiredVelocity, forward);
                float side = math.dot(desiredVelocity, sideVec);
                float len = math.length(desiredVelocity);
                float3 desiredVelocitySF = new float3(-side, -fwd, 0.0f);
                desiredVelocitySF = math.normalizesafe(desiredVelocitySF, float3.zero);
                desiredVelocitySF *= len;
                diff = desiredVelocitySF - relative;
            }

            relative += diff;

            linearVelocity = math.rotate(surfaceFrame.Value, relative) + surfaceVelocity +
                (isJumping ? math.dot(desiredVelocity, up) * up : float3.zero);
        }

        // private void UpdateTriggerEvents(NativeList<StatefulTriggerEvent> triggerEvents,
        //     DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer)
        // {
        //     triggerEvents.Sort();

        //     var previousFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(triggerEventBuffer.Length, Allocator.Temp);

        //     for (int i = 0; i < triggerEventBuffer.Length; i++)
        //     {
        //         var triggerEvent = triggerEventBuffer[i];
        //         if (triggerEvent.State != EventOverlapState.Exit)
        //         {
        //             previousFrameTriggerEvents.Add(triggerEvent);
        //         }
        //     }

        //     var eventsWithState = new NativeList<StatefulTriggerEvent>(triggerEvents.Length, Allocator.Temp);

        //     TriggerEventConversionSystem.UpdateTriggerEventState(previousFrameTriggerEvents, triggerEvents, eventsWithState);

        //     triggerEventBuffer.Clear();

        //     for (int i = 0; i < eventsWithState.Length; i++)
        //     {
        //         triggerEventBuffer.Add(eventsWithState[i]);
        //     }
        // }

        // private void UpdateCollisionEvents(NativeList<StatefulCollisionEvent> collisionEvents,
        //     DynamicBuffer<StatefulCollisionEvent> collisionEventBuffer)
        // {
        //     collisionEvents.Sort();

        //     var previousFrameCollisionEvents = new NativeList<StatefulCollisionEvent>(collisionEventBuffer.Length, Allocator.Temp);

        //     for (int i = 0; i < collisionEventBuffer.Length; i++)
        //     {
        //         var collisionEvent = collisionEventBuffer[i];
        //         if (collisionEvent.CollidingState != EventCollidingState.Exit)
        //         {
        //             previousFrameCollisionEvents.Add(collisionEvent);
        //         }
        //     }

        //     var eventsWithState = new NativeList<StatefulCollisionEvent>(collisionEvents.Length, Allocator.Temp);

        //     CollisionEventConversionSystem.UpdateCollisionEventState(previousFrameCollisionEvents, collisionEvents, eventsWithState);

        //     collisionEventBuffer.Clear();

        //     for (int i = 0; i < eventsWithState.Length; i++)
        //     {
        //         collisionEventBuffer.Add(eventsWithState[i]);
        //     }
        // }
    }

    [BurstCompile]
    struct ApplyDefferedPhysicsUpdatesJob : IJob
    {
        // Chunks can be deallocated at this point
        [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;

        public NativeStream.Reader DeferredImpulseReader;

        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityData;
        public ComponentDataFromEntity<PhysicsMass> PhysicsMassData;
        public ComponentDataFromEntity<Translation> TranslationData;
        public ComponentDataFromEntity<Rotation> RotationData;

        public void Execute()
        {
            int index = 0;
            int maxIndex = DeferredImpulseReader.ForEachCount;
            DeferredImpulseReader.BeginForEachIndex(index++);
            while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
            {
                DeferredImpulseReader.BeginForEachIndex(index++);
            }

            while (DeferredImpulseReader.RemainingItemCount > 0)
            {
                // Read the data
                var impulse = DeferredImpulseReader.Read<DeferredCharacterControllerImpulse>();
                while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
                {
                    DeferredImpulseReader.BeginForEachIndex(index++);
                }

                PhysicsVelocity pv = PhysicsVelocityData[impulse.Entity];
                PhysicsMass pm = PhysicsMassData[impulse.Entity];
                Translation t = TranslationData[impulse.Entity];
                Rotation r = RotationData[impulse.Entity];

                // Don't apply on kinematic bodies
                if (pm.InverseMass > 0.0f)
                {
                    // Apply impulse
                    pv.ApplyImpulse(pm, t, r, impulse.Impulse, impulse.Point);

                    // Write back
                    PhysicsVelocityData[impulse.Entity] = pv;
                }
            }
        }
    }

    // override the behavior of CopyPhysicsVelocityToSmoothing
    // [BurstCompile]
    // struct CopyVelocityToGraphicalSmoothingJob : IJobEntityBatch
    // {
    //     [ReadOnly] public ComponentTypeHandle<CharacterControllerInternalData> CharacterControllerInternalType;
    //     public ComponentTypeHandle<PhysicsGraphicalSmoothing> PhysicsGraphicalSmoothingType;

    //     public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
    //     {
    //         NativeArray<CharacterControllerInternalData> ccInternalDatas = batchInChunk.GetNativeArray(CharacterControllerInternalType);
    //         NativeArray<PhysicsGraphicalSmoothing> physicsGraphicalSmoothings = batchInChunk.GetNativeArray(PhysicsGraphicalSmoothingType);

    //         for (int i = 0, count = batchInChunk.Count; i < count; ++i)
    //         {
    //             var smoothing = physicsGraphicalSmoothings[i];
    //             smoothing.CurrentVelocity = ccInternalDatas[i].Velocity;
    //             physicsGraphicalSmoothings[i] = smoothing;
    //         }
    //     }
    // }

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    ExportPhysicsWorld m_ExportPhysicsWorldSystem;
    EndFramePhysicsSystem m_EndFramePhysicsSystem;

    EntityQuery m_CharacterControllersGroup;
    EntityQuery m_SmoothedCharacterControllersGroup;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_ExportPhysicsWorldSystem = World.GetOrCreateSystem<ExportPhysicsWorld>();
        m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();

        EntityQueryDesc query = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CharacterControllerComponentData),
                typeof(CharacterControllerInternalData),
                typeof(PhysicsCollider),
                typeof(Translation),
                typeof(Rotation),
                typeof(PredictedGhostComponent),
                typeof(CharacterControllerTransform)
            }
        };
        m_CharacterControllersGroup = GetEntityQuery(query);
        m_SmoothedCharacterControllersGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CharacterControllerInternalData),
                typeof(PhysicsGraphicalSmoothing),
            }
        });
    }

    protected override void OnUpdate()
    {
        if (m_CharacterControllersGroup.CalculateEntityCount() == 0)
            return;

        var group = World.GetExistingSystem<GhostPredictionSystemGroup>();
        var tick = group.PredictingTick;


        // Combine implicit input dependency with the user one
        // Dependency = JobHandle.CombineDependencies(Dependency, m_InputDependency);

        var chunks = m_CharacterControllersGroup.CreateArchetypeChunkArray(Allocator.TempJob);

        var ccComponentType = GetComponentTypeHandle<CharacterControllerComponentData>();
        var ccInternalType = GetComponentTypeHandle<CharacterControllerInternalData>();
        var physicsColliderType = GetComponentTypeHandle<PhysicsCollider>();
        var translationType = GetComponentTypeHandle<Translation>();
        var rotationType = GetComponentTypeHandle<Rotation>();
        var predictComponentType = GetComponentTypeHandle<PredictedGhostComponent>();
        var ccTransformType = GetComponentTypeHandle<CharacterControllerTransform>();
        // var collisionEventBufferType = GetBufferTypeHandle<StatefulCollisionEvent>();
        // var triggerEventBufferType = GetBufferTypeHandle<StatefulTriggerEvent>();

        var deferredImpulses = new NativeStream(chunks.Length, Allocator.TempJob);

        var ccJob = new CharacterControllerJob
        {
            Tick = tick,
            // Archetypes
            CharacterControllerComponentType = ccComponentType,
            CharacterControllerInternalType = ccInternalType,
            PredictComponentType = predictComponentType,
            PhysicsColliderType = physicsColliderType,
            TranslationType = translationType,
            RotationType = rotationType,
            CharacterControllerTransformType = ccTransformType,
            // CollisionEventBufferType = collisionEventBufferType,
            // TriggerEventBufferType = triggerEventBufferType,

            // Input
            DeltaTime = Time.DeltaTime,
            PhysicsWorld = m_BuildPhysicsWorldSystem.PhysicsWorld,
            DeferredImpulseWriter = deferredImpulses.AsWriter()
        };

        // Dependency = JobHandle.CombineDependencies(Dependency, m_ExportPhysicsWorldSystem.GetOutputDependency());
        Dependency = ccJob.ScheduleParallel(m_CharacterControllersGroup, 1, Dependency);

        // var copyVelocitiesHandle = new CopyVelocityToGraphicalSmoothingJob
        // {
        //     CharacterControllerInternalType = GetComponentTypeHandle<CharacterControllerInternalData>(true),
        //     PhysicsGraphicalSmoothingType = GetComponentTypeHandle<PhysicsGraphicalSmoothing>()
        // }.ScheduleParallel(m_SmoothedCharacterControllersGroup, 1, Dependency);

        var applyJob = new ApplyDefferedPhysicsUpdatesJob()
        {
            Chunks = chunks,
            DeferredImpulseReader = deferredImpulses.AsReader(),
            PhysicsVelocityData = GetComponentDataFromEntity<PhysicsVelocity>(),
            PhysicsMassData = GetComponentDataFromEntity<PhysicsMass>(),
            TranslationData = GetComponentDataFromEntity<Translation>(),
            RotationData = GetComponentDataFromEntity<Rotation>()
        };

        Dependency = applyJob.Schedule(Dependency);
        Dependency = deferredImpulses.Dispose(Dependency);
        CompleteDependency();
    }
}
