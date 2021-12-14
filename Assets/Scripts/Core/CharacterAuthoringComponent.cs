using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct CharacterComponent : IComponentData
{
    [GhostField]
    public int SpeedPlus;
    [GhostField]
    public int Blood;
    [GhostField]
    public int MaxBlood;
}

[DisallowMultipleComponent]
internal class CharacterAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public int SpeedPlus = 0;
    public int MaxBlood = 0;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CharacterComponent()
        {
            SpeedPlus = SpeedPlus,
            Blood = MaxBlood,
            MaxBlood = MaxBlood
        });
    }
}