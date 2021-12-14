using UnityEngine;
using Unity.Entities;

namespace Higo.Camera
{
    public class CameraBrainAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new CameraControllerComponent());
            dstManager.AddComponentData(entity, new CameraBrainComponent());
        }
    }
}