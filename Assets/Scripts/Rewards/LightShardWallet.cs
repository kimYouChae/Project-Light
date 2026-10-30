using UnityEngine;

/// <summary>이번 플레이에서 모은 빛 조각 보유량을 관리한다.</summary>
public sealed class LightShardWallet : MonoBehaviour
{
    [SerializeField, Min(0)] private int count;

    /// <summary>현재 보유한 빛 조각 수.</summary>
    public int Count => count;

    /// <summary>빛 조각 보유량을 늘린다.</summary>
    public void Add(int amount)
    {
        if (amount > 0)
            count += amount;
    }

    /// <summary>보유량이 충분하면 지정 수량을 소비한다.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || amount > count)
            return false;
        count -= amount;
        return true;
    }

    private void OnValidate() => count = Mathf.Max(0, count);
}
