using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카탈로그에서 서로 다른 보상 후보 3장을 선택합니다.
/// </summary>
public sealed class RewardCardSelector : MonoBehaviour
{
    private const int CANDIDATE_COUNT = 3;

    /// <summary>
    /// 서로 다른 후보 3장을 선택합니다.
    /// 카탈로그가 없거나 카드가 부족하면 false를 반환합니다.
    /// </summary>
    public bool TrySelectCards(out RewardCardData[] selectedCards)
    {
        selectedCards = new RewardCardData[0];

        RewardCardCatalog catalog = RewardCardCatalog.Instance;

        if (catalog == null || catalog.Cards.Count < CANDIDATE_COUNT)
        {
            return false;
        }

        // 원본 목록을 유지하기 위해 추출용 목록을 만든다.
        List<RewardCardData> candidates =
            new List<RewardCardData>(catalog.Cards);

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
