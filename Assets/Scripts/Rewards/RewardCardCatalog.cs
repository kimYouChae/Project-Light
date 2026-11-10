using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 데이터를 한곳에 등록하고 ID로 조회합니다.
/// </summary>
[DefaultExecutionOrder(-400)]
[DisallowMultipleComponent]
public sealed class RewardCardCatalog
    : Singleton<RewardCardCatalog>
{
    // 전체 카드 에셋을 등록하는 유일한 목록
    [SerializeField] private List<RewardCardData> cards =
        new List<RewardCardData>();

    // 등록 목록으로부터 구성하는 ID 조회 테이블
    private Dictionary<string, RewardCardData> cardsById;

    /// <summary>
    /// 초기화가 끝난 카드 목록을 후보 추출기에 제공합니다.
    /// </summary>
    public IReadOnlyList<RewardCardData> Cards =>
        cardsById != null
            ? cards
            : Array.Empty<RewardCardData>();

    /// <summary>
    /// 카드 목록을 검사하고 ID 조회 테이블을 구성합니다.
    /// Singleton의 Awake가 호출합니다.
    /// </summary>
    protected override void OnSingletonAwake()
    {
        var table = new Dictionary<string, RewardCardData>(
            StringComparer.Ordinal);

        foreach (RewardCardData card in cards)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.Id))
            {
                PlayLog.Write("Reward",
                    "카탈로그에 비어 있는 카드 또는 ID가 있습니다.",
                    LogType.Error, this);
                enabled = false;
                return;
            }

            if (table.ContainsKey(card.Id))
            {
                PlayLog.Write("Reward",
                    $"카탈로그에 중복된 카드 ID가 있습니다: {card.Id}",
                    LogType.Error, this);
                enabled = false;
                return;
            }

            table.Add(card.Id, card);
        }

        cardsById = table;
    }

    /// <summary>
    /// ID에 해당하는 카드 데이터를 조회합니다.
    /// PlayerStats와 선택 확정 코드가 호출합니다.
    /// </summary>
    public bool TryGetCard(string id, out RewardCardData card)
    {
        card = null;

        return cardsById != null
            && !string.IsNullOrEmpty(id)
            && cardsById.TryGetValue(id, out card);
    }
}
