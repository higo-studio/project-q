using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Animation.Hybrid;
using UnityEditor.Animations;
public class AnimationBlendTree : MonoBehaviour, IConvertGameObjectToEntity
{
    public BlendTree blendTree;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (blendTree == null) return;
        var rigComponent = GetComponent<RigComponent>();
        if (blendTree.blendType == BlendTreeType.Simple1D)
        {
            BlendTree1DGraph.AddGraphSetupComponent(blendTree, entity, dstManager, conversionSystem);
        }
        else if (blendTree.blendType == BlendTreeType.SimpleDirectional2D)
        {
            BlendTree2DGraph.AddGraphSetupComponent(blendTree, entity, dstManager, conversionSystem);
        }
    }
}