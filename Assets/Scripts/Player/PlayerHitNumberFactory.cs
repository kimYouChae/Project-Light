using System;
using UnityEngine;

/// <summary>피격 숫자 풀에 비활성 객체를 생성합니다.</summary>
public sealed class PlayerHitNumberFactory : IObjectFactory<PlayerHitNumber>
{
    private readonly PlayerHitNumber prefab;
    private readonly Transform parent;

    /// <summary>비활성 숫자 프리팹과 풀 부모를 지정합니다.</summary>
    public PlayerHitNumberFactory(PlayerHitNumber prefab, Transform parent)
    {
        if (prefab == null || prefab.gameObject.activeSelf)
            throw new ArgumentException("비활성 숫자 프리팹이 필요합니다.", nameof(prefab));

        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>숫자를 비활성 상태로 생성합니다.</summary>
    public PlayerHitNumber CreateInstance()
    {
        return UnityEngine.Object.Instantiate(prefab, parent);
    }
}
