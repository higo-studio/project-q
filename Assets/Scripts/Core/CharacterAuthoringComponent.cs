using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Entities.Conversion;

public struct CharacterComponent : IComponentData
{

    [GhostField]
    public int Blood;
    [GhostField]
    public int MaxBlood;
    public Entity RenderPrefab;
}

[DisallowMultipleComponent]
internal class CharacterAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public int MaxBlood = 0;
    public GameObject RenderPrefab;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CharacterComponent()
        {
            Blood = MaxBlood,
            MaxBlood = MaxBlood,
            RenderPrefab = conversionSystem.GetPrimaryEntity(RenderPrefab)
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        if (RenderPrefab != null && RenderPrefab)
        {
            referencedPrefabs.Add(RenderPrefab);
        }
    }
}