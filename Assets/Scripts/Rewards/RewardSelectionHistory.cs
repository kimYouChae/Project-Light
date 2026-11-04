using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// 이번 플레이에서 확정한 카드 획득 기록을 보관합니다.
/// </summary>
public sealed class RewardSelectionHistory : MonoBehaviour
{
    // 획득 순서대로 보관할 기록
    [SerializeField] private List<RewardSelectionRecord> records =
        new List<RewardSelectionRecord>();

    // 획득 기록이 추가됐을 때 전달할 알림
    private Action changed;

    /// <summary>획득 기록을 읽기 전용으로 제공합니다.</summary>
    public IReadOnlyList<RewardSelectionRecord> Records => records;

    /// <summary>
    /// 획득 기록 변경 알림을 등록합니다.
    /// PlayerStats가 호출합니다.
    /// </summary>
    public void RegisterChanged(Action callback)
    {
        changed += callback;
    }

    /// <summary>
    /// 획득 기록 변경 알림을 해제합니다.
    /// PlayerStats가 호출합니다.
    /// </summary>
    public void UnregisterChanged(Action callback)
    {
        changed -= callback;
    }

    /// <summary>
    /// 확정된 카드 ID와 투입량을 기록합니다.
    /// RewardSelectionController가 호출합니다.
    /// </summary>
    public void AddRecord(string cardId, int investedShards)
    {
        records.Add(new RewardSelectionRecord(cardId, investedShards));
        changed?.Invoke();
    }
}
