using System;
using UnityEngine;

/// <summary>수풀 통과 탄의 비활성 방향 경고를 로컬 생성합니다.</summary>
public sealed class BushBulletWarningFactory : IObjectFactory<BushBulletWarning>
{
    private readonly BushBulletWarning prefab;
    private readonly Transform parent;

    /// <summary>풀에서 사용할 비활성 경고 프리팹과 부모를 지정합니다.</summary>
    public BushBulletWarningFactory(BushBulletWarning prefab, Transform parent)
    {
        if (prefab == null || prefab.gameObject.activeSelf)
        {
            throw new ArgumentException(
                "비활성 수풀 탄 경고 프리팹이 필요합니다.",
                nameof(prefab));
        }

        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>경고를 비활성 상태로 생성합니다.</summary>
    public BushBulletWarning CreateInstance()
    {
        BushBulletWarning instance =
            UnityEngine.Object.Instantiate(prefab, parent);
        instance.transform.InitTransform(parent);
        return instance;
    }
}
