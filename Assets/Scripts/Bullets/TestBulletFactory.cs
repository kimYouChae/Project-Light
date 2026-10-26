using System;
using UnityEngine;

/// <summary>비활성 테스트 탄 프리팹을 로컬로 생성한다.</summary>
public sealed class TestBulletFactory : IObjectFactory<TestBullet>
{
    private readonly TestBullet prefab;
    private readonly Transform parent;

    /// <summary>준비 전에 OnEnable이 실행되지 않도록 비활성 프리팹을 받는다.</summary>
    public TestBulletFactory(TestBullet prefab, Transform parent)
    {
        if (prefab == null || prefab.gameObject.activeSelf)
            throw new ArgumentException("비활성 탄 프리팹을 지정해주세요.", nameof(prefab));
        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>비활성 탄을 네트워크 없이 생성한다.</summary>
    public TestBullet CreateInstance()
    {
        TestBullet instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.transform.InitTransform(parent);
        return instance;
    }
}
