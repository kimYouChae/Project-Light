using System;
using UnityEngine;

/// <summary>판정 없는 비활성 벽 표시물을 생성합니다.</summary>
public sealed class HeadBossWallVisualFactory : IObjectFactory<SpriteRenderer>
{
    private readonly SpriteRenderer prefab;
    private readonly Transform parent;

    /// <summary>벽 표시용 비활성 프리팹과 풀 부모를 받습니다.</summary>
    public HeadBossWallVisualFactory(
        SpriteRenderer prefab,
        Transform parent)
    {
        if (prefab == null || prefab.gameObject.activeSelf)
        {
            throw new ArgumentException(
                "비활성 벽 표시 프리팹을 지정해주세요.",
                nameof(prefab));
        }

        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>판정 없는 벽 표시물을 비활성 상태로 생성합니다.</summary>
    public SpriteRenderer CreateInstance()
    {
        SpriteRenderer instance =
            UnityEngine.Object.Instantiate(prefab, parent);

        instance.transform.InitTransform(parent);
        return instance;
    }
}
