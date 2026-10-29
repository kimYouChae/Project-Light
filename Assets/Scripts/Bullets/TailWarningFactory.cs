using System;
using UnityEngine;

/// <summary>피해 판정이 없는 비활성 위험존 표시물을 생성한다.</summary>
public sealed class TailWarningFactory : IObjectFactory<SpriteRenderer>
{
    private readonly SpriteRenderer prefab;
    private readonly Transform parent;

    /// <summary>풀에서 사용할 비활성 표시 프리팹을 받는다.</summary>
    public TailWarningFactory(SpriteRenderer prefab, Transform parent)
    {
        if (prefab == null || prefab.gameObject.activeSelf)
            throw new ArgumentException("비활성 위험존 프리팹을 지정해주세요.", nameof(prefab));
        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>위험존 표시물을 비활성 상태로 생성한다.</summary>
    public SpriteRenderer CreateInstance()
    {
        SpriteRenderer instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.transform.InitTransform(parent);
        return instance;
    }
}
