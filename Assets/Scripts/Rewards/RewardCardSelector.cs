using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지정된 카드 목록에서 서로 다른 보상 후보 3장을 선택합니다.
/// </summary>
public sealed class RewardCardSelector : MonoBehaviour
{
    private const int CANDIDATE_COUNT = 3;

    // 보상 후보로 사용할 카드 목록
    [SerializeField] private List<RewardCardData> cards =
        new List<RewardCardData>();

    /// <summary>
    /// 서로 다른 카드 3장을 선택합니다.
    /// 유효한 카드가 3장 미만이면 false와 빈 배열을 반환합니다.
    /// </summary>
    public bool TrySelectCards(out RewardCardData[] selectedCards)
    {
        // 비어 있는 참조와 중복 등록된 카드는 제외한다.
        List<RewardCardData> candidates = new List<RewardCardData>();

        foreach (RewardCardData card in cards)
        {
            if (card == null || candidates.Contains(card))
            {
                continue;
            }

            candidates.Add(card);
        }

        if (candidates.Count < CANDIDATE_COUNT)
        {
            selectedCards = new RewardCardData[0];
            return false;
        }

        selectedCards = new RewardCardData[CANDIDATE_COUNT];

        // 임시 목록에서만 제거하여 원본 카드 목록을 유지한다.
        for (int i = 0; i < CANDIDATE_COUNT; i++)
        {
            int index = Random.Range(0, candidates.Count);

            selectedCards[i] = candidates[index];
            candidates.RemoveAt(index);
        }

        return true;
    }
}
