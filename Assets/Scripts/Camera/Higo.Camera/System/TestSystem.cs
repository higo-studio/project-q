using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class TestSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        //只有当Query寻找到符合的原型才执行Update
        RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<MyOwnComponent>()));
    }

    protected override void OnUpdate()
    {
        Debug.Log("This is OnUpdate");
    }
}

public struct MyOwnComponent : IComponentData
{
    int num;
}
