using System.Collections.Generic;
using UnityEngine;

/// <summary>씬의 활성 탄 목록을 소유한다. 발사기의 소유 목록과는 별도로 관리한다.</summary>
[DisallowMultipleComponent]
public sealed class BulletRegistry : MonoBehaviour
{
    private readonly HashSet<TestBullet> activeBullets = new HashSet<TestBullet>();

    /// <summary>퍼펙트 입력 시 검사할 전체 활성 탄 목록.</summary>
    public IReadOnlyCollection<TestBullet> ActiveBullets => activeBullets;

    /// <summary>활성화된 탄을 한 번만 등록한다.</summary>
    public void Register(TestBullet bullet)
    {
        if (bullet != null && bullet.isActiveAndEnabled)
            activeBullets.Add(bullet);
    }

    /// <summary>비활성화 또는 회수되는 탄의 등록을 해제한다.</summary>
    public void Unregister(TestBullet bullet)
    {
        if (bullet != null)
            activeBullets.Remove(bullet);
    }

    private void OnDestroy()
    {
        activeBullets.Clear();
    }
}
