using System;
using UnityEngine;

/// <summary>비활성 플레이어 탄 프리팹을 생성한다.</summary>
public sealed class PlayerBulletFactory : IObjectFactory<PlayerBullet>
{
    private readonly PlayerBullet prefab;
    private readonly Transform parent;

    /// <summary>준비 후 활성화할 비활성 프리팹을 받는다.</summary>
    public PlayerBulletFactory(PlayerBullet prefab, Transform parent)
    {
        if (prefab == null || prefab.gameObject.activeSelf)
            throw new ArgumentException("비활성 탄 프리팹을 지정해주세요.", nameof(prefab));
        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>풀에서 사용할 탄을 생성한다.</summary>
    public PlayerBullet CreateInstance()
    {
        PlayerBullet instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.transform.InitTransform(parent);
        return instance;
    }
}
