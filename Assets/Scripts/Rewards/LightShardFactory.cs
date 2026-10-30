using System;
using UnityEngine;

/// <summary>비활성 빛 조각 프리팹을 로컬 생성한다.</summary>
public sealed class LightShardFactory : IObjectFactory<LightShard>
{
    private readonly LightShard prefab;
    private readonly Transform parent;

    /// <summary>풀에서 사용할 비활성 프리팹과 부모를 지정한다.</summary>
    public LightShardFactory(LightShard prefab, Transform parent)
    {
        if (prefab == null || prefab.gameObject.activeSelf)
            throw new ArgumentException("비활성 빛 조각 프리팹이 필요합니다.", nameof(prefab));
        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>빛 조각 인스턴스를 생성한다.</summary>
    public LightShard CreateInstance()
    {
        LightShard instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.transform.InitTransform(parent);
        return instance;
    }
}
