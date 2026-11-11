using UnityEngine;

/// <summary>서치라이트에 사용하는 비활성 돌 표시를 생성합니다.</summary>
public sealed class SearchlightRockFactory : IObjectFactory<SpriteRenderer>
{
    private readonly SpriteRenderer prefab;
    private readonly Transform parent;

    /// <summary>돌 표시 프리팹과 풀 부모를 저장합니다.</summary>
    public SearchlightRockFactory(SpriteRenderer prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>풀에서 사용할 돌 표시를 생성합니다.</summary>
    public SpriteRenderer CreateInstance()
    {
        return Object.Instantiate(prefab, parent);
    }
}
