using System.Collections.Generic;
using UnityEngine;

/// <summary>이번 플레이의 카드 선택 결과를 누적 보관한다.</summary>
public sealed class RewardSelectionHistory : MonoBehaviour
{
    [SerializeField] private List<RewardSelectionRecord> records = new List<RewardSelectionRecord>();

    /// <summary>앞선 전투를 포함한 카드 선택 목록.</summary>
    public IReadOnlyList<RewardSelectionRecord> Records => records;

    /// <summary>선택 기록 변경 알림. 구독자는 비활성화 시 해제한다.</summary>
    public event System.Action Changed;

    /// <summary>동일 카드도 별도 항목으로 기록하고 스탯 재계산을 알린다.</summary>
    public void Record(string cardId, int investedShards)
    {
        records.Add(new RewardSelectionRecord(cardId, investedShards));
        Changed?.Invoke();
    }
}
