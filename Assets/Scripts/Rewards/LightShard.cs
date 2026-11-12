using System;
using UnityEngine;

/// <summary>접촉하면 체력을 회복하고 보유량을 늘린 뒤 풀로 돌아가는 빛 조각.</summary>
public sealed class LightShard : MonoBehaviour
{
    // ##TEMP 사용자 승인 빛 조각 회복량과 접촉 반경.
    [SerializeField, Min(1)] private int healAmount = 2;
    [SerializeField, Min(0.01f)] private float pickupRadius = 0.15f;

    // ##TEMP 드롭 후 대기 시간과 흡수 이동 속도.
    [SerializeField, Min(0f)] private float pickupDelay = 1f;
    [SerializeField, Min(0.01f)] private float attractionSpeed = 8f;

    private double attractionStartsAt;
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
        attractionStartsAt = Time.timeAsDouble + pickupDelay;
        prepared = true;
    }

    private void Update()
    {
        // 필드 시간이 정지된 동안 이동·공격·판정·획득을 처리하지 않는다.
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (!prepared || playerHealth == null || playerHealth.IsDead
            || !playerHealth.isActiveAndEnabled)
            return;
        if (Time.timeAsDouble < attractionStartsAt)
        {
            return;
        }

        Vector3 position = transform.position;
        Vector2 destination = playerHealth.transform.position;
        Vector2 nextPosition = Vector2.MoveTowards(
            position, destination, attractionSpeed * Time.deltaTime);
        transform.position = new Vector3(nextPosition.x, nextPosition.y, position.z);

        float radius = pickupRadius + playerHealth.BodyRadius;
        if ((nextPosition - destination).sqrMagnitude <= radius * radius)
        {
            TryCollect();
        }
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
