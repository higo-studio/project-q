using System;
using Unity.Entities;
using Unity.Entities.Editor;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct VirtualCameraComponent : IComponentData {
    public int Priority;
}