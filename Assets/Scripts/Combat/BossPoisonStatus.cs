using System;
using UnityEngine;

/// <summary>
/// 보스의 독 중첩, 주기 피해, 만료를 관리합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BossHealth))]
public sealed class BossPoisonStatus : MonoBehaviour
{
    // 보스들이 공유하는 독 규칙
    [SerializeField] private PoisonSettings settings;

    // 현재 보스에게 적용된 독 중첩
    [SerializeField] private int currentStacks;

    private BossHealth bossHealth;
    private double expiresAt;
    private double nextDamageAt;

    /// <summary>현재 독 중첩 수를 반환합니다.</summary>
    public int CurrentStacks => currentStacks;

    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
        ClearPoison();

        if (settings == null)
        {
            Debug.LogError("독 규칙 에셋을 연결해주세요.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.Defeated += ClearPoison;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.Defeated -= ClearPoison;
        }

        ClearPoison();
    }

    /// <summary>
    /// 독 중첩을 추가하고 전체 지속 시간을 갱신합니다.
    /// 이후 연결할 독 부여 처리기가 호출합니다.
    /// </summary>
    public void AddStacks(int amount)
    {
        if (amount <= 0 || !isActiveAndEnabled
            || settings == null
            || bossHealth == null || bossHealth.IsDefeated
            || Time.timeScale <= 0f)
        {
            return;
        }

        double now = Time.timeAsDouble;

        // 새 중첩을 더하기 전에 기존 독의 피해와 만료를 처리한다.
        AdvancePoison(now);

        if (bossHealth.IsDefeated)
        {
            return;
        }

        // 독이 없을 때만 첫 피해 시점을 정한다.
        if (currentStacks == 0)
        {
            nextDamageAt = now + settings.DamageInterval;
        }

        int availableStacks = Mathf.Max(
            0, settings.MaxStacks - currentStacks);

        currentStacks += Mathf.Min(amount, availableStacks);

        // 공유 설정은 읽기만 하고 보스별 만료 시각을 갱신한다.
        expiresAt = now + settings.Duration;
    }

    private void Update()
    {
        if (settings == null || Time.timeScale <= 0f)
        {
            return;
        }

        AdvancePoison(Time.timeAsDouble);
    }

    private void AdvancePoison(double now)
    {
        if (currentStacks <= 0)
        {
            return;
        }

        if (bossHealth == null || bossHealth.IsDefeated)
        {
            ClearPoison();
            return;
        }

        // 만료 시점 이후의 피해는 적용하지 않는다.
        double damageUntil = Math.Min(now, expiresAt);

        // 프레임이 늦어져도 지나간 피해 주기를 순서대로 처리한다.
        while (currentStacks > 0 && nextDamageAt <= damageUntil)
        {
            int damage = currentStacks * settings.DamagePerStack;
            nextDamageAt += settings.DamageInterval;

            // 직접 적중 API를 사용하지 않아 빛 조각 드롭을 발생시키지 않는다.
            bossHealth.TryTakeDamage(damage);

            if (bossHealth.IsDefeated)
            {
                ClearPoison();
                return;
            }
        }

        // 만료와 겹친 마지막 주기 피해까지 처리한 뒤 제거한다.
        if (now >= expiresAt)
        {
            ClearPoison();
        }
    }

    private void ClearPoison()
    {
        currentStacks = 0;
        expiresAt = 0d;
        nextDamageAt = 0d;
    }

}
