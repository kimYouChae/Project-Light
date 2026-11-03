using UnityEngine;

/// <summary>
/// 짤보스 전투 종료 요청을 받아 보상 선택창을 엽니다.
/// </summary>
public sealed class RewardSelectionFlow : MonoBehaviour
{
    // 보상 요청을 전달할 전투 흐름
    [SerializeField] private MiniBossBattleFlow battleFlow;

    // 후보 추출 및 화면 처리
    [SerializeField] private RewardCardSelector selector;
    [SerializeField] private RewardSelectionView view;
    [SerializeField] private RewardSelectionController controller;

    // 이번 플레이의 지갑과 획득 기록
    [SerializeField] private LightShardWallet wallet;
    [SerializeField] private RewardSelectionHistory history;

    private void OnEnable()
    {
        if (battleFlow != null)
        {
            battleFlow.RegisterRewardRequested(OnRewardRequested);
        }
    }

    private void OnDisable()
    {
        if (battleFlow != null)
        {
            battleFlow.UnregisterRewardRequested(OnRewardRequested);
        }
    }

    private void OnRewardRequested()
    {
        TryOpenSelection();
    }

    /// <summary>
    /// 후보를 추출하여 보상 선택창을 엽니다.
    /// 전투 종료 알림 또는 임시 F8 입력 코드가 호출합니다.
    /// </summary>
    public bool TryOpenSelection()
    {
        if (!Application.isPlaying
            || selector == null || view == null
            || controller == null || wallet == null || history == null
            || controller.IsSelectionOpen)
        {
            return false;
        }

        if (!selector.TrySelectCards(out RewardCardData[] cards)
            || !view.TryDisplayCards(cards))
        {
            return false;
        }

        controller.BeginSelection(wallet, history);
        view.gameObject.SetActive(true);
        return true;
    }
}
