using System;
using UnityEngine;

/// <summary>
/// 플레이어 공격의 직접 적중 알림을 전달합니다.
/// </summary>
public sealed class PlayerAttackEvents : MonoBehaviour
{
    // 직접 적중한 보스와 발사 당시 공격 구분
    private Action<BossHealth, AttackTargetType> hit;

    /// <summary>
    /// 직접 적중 알림을 등록합니다.
    /// 적중 효과를 처리하는 쪽에서 호출합니다.
    /// </summary>
    public void RegisterHit(Action<BossHealth, AttackTargetType> callback)
    {
        hit += callback;
    }

    /// <summary>
    /// 직접 적중 알림을 해제합니다.
    /// 등록한 쪽에서 비활성화 시 호출합니다.
    /// </summary>
    public void UnregisterHit(Action<BossHealth, AttackTargetType> callback)
    {
        hit -= callback;
    }

    /// <summary>
    /// 직접 적중을 알립니다. PlayerBullet이 호출합니다.
    /// </summary>
    public void RaiseHit(BossHealth target, AttackTargetType attackType)
    {
        hit?.Invoke(target, attackType);
    }
}
