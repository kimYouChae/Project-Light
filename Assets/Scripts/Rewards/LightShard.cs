using System;
using UnityEngine;

/// <summary>접촉하면 체력을 회복하고 보유량을 늘린 뒤 풀로 돌아가는 빛 조각.</summary>
public sealed class LightShard : MonoBehaviour
{
    // ##TEMP 사용자 승인 빛 조각 회복량과 접촉 반경.
    [SerializeField, Min(1)] private int healAmount = 2;
    [SerializeField, Min(0.01f)] private float pickupRadius = 0.15f;
    private PlayerHealth playerHealth;
    private LightShardWallet wallet;
    private Action<LightShard> release;
    private bool prepared;

    /// <summary>활성화 전에 위치와 획득 대상을 설정하고 이전 대여 상태를 초기화한다.</summary>
    public void Prepare(Vector3 position, PlayerHealth target,
        LightShardWallet targetWallet, Action<LightShard> onRelease)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * (pickupRadius * 2f);
        playerHealth = target;
        wallet = targetWallet;
        release = onRelease;
        prepared = true;
    }

    private void Update()
    {
        if (!prepared || playerHealth == null || playerHealth.IsDead
            || !playerHealth.isActiveAndEnabled)
            return;
        float radius = pickupRadius + playerHealth.BodyRadius;
        Vector2 offset = transform.position - playerHealth.transform.position;
        if (offset.sqrMagnitude <= radius * radius)
            TryCollect();
    }

    /// <summary>한 번만 회복·보유량 증가를 적용한다. 체력이 가득 차도 획득한다.</summary>
    public bool TryCollect()
    {
        if (!prepared || !isActiveAndEnabled || playerHealth == null || wallet == null
            || !playerHealth.isActiveAndEnabled || playerHealth.IsDead)
            return false;
        prepared = false;
        playerHealth.TryHeal(healAmount);
        wallet.Add(1);
        Release();
        return true;
    }

    /// <summary>획득 효과 없이 대여 중인 조각을 한 번만 반환한다.</summary>
    public void ReturnToPool()
    {
        if (!prepared)
            return;
        prepared = false;
        Release();
    }

    private void Release()
    {
        Action<LightShard> callback = release;
        release = null;
        playerHealth = null;
        wallet = null;
        callback?.Invoke(this);
    }

    private void OnValidate()
    {
        healAmount = Mathf.Max(1, healAmount);
        pickupRadius = Mathf.Max(0.01f, pickupRadius);
    }
}
