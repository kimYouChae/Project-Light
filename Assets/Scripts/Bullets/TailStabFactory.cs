using UnityEngine;

/// <summary>꼬리 꽂기 표시물을 풀에 생성합니다.</summary>
public sealed class TailStabFactory : IObjectFactory<SpriteRenderer>
{
    private readonly SpriteRenderer prefab;
    private readonly Transform parent;

    /// <summary>꼬리 표시 프리팹과 풀 부모를 저장합니다.</summary>
    public TailStabFactory(SpriteRenderer prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>풀에서 사용할 꼬리 표시를 생성합니다.</summary>
    public SpriteRenderer CreateInstance()
    {
        return Object.Instantiate(prefab, parent);
    }
}
