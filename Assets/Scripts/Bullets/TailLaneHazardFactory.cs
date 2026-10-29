using System;
using UnityEngine;

/// <summary>비활성 레인 꼬리 프리팹을 로컬 생성한다.</summary>
public sealed class TailLaneHazardFactory : IObjectFactory<TailLaneHazard>
{
    private readonly TailLaneHazard prefab;
    private readonly Transform parent;

    /// <summary>풀의 비활성 프리팹과 부모를 지정한다.</summary>
    public TailLaneHazardFactory(TailLaneHazard prefab, Transform parent)
    {
        if (prefab == null || prefab.gameObject.activeSelf)
            throw new ArgumentException("비활성 레인 꼬리 프리팹이 필요합니다.", nameof(prefab));
        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>레인 꼬리 인스턴스를 생성한다.</summary>
    public TailLaneHazard CreateInstance()
    {
        TailLaneHazard instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.transform.InitTransform(parent);
        return instance;
    }
}
