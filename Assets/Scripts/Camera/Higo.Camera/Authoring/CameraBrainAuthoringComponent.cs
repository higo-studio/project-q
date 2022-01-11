using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Higo.Camera.Editor")]
namespace Higo.Camera
{
    public class CameraBrainAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new CameraControllerComponent());
            dstManager.AddComponentData(entity, new CameraBrainComponent());

            // CopyTransformToGameObject
            // ThinClientComponent

            // active virtual camera -> camera brain -> Camera
        }
    }
}
