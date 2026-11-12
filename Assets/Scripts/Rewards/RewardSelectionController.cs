using UnityEngine;

/// <summary>
/// 보상 선택 확정, 빛 조각 소비, 획득 기록을 처리합니다.
/// </summary>
public sealed class RewardSelectionController : MonoBehaviour
{
    // 확정할 카드와 투입량을 제공하는 화면
    [SerializeField] private RewardSelectionView view;

    // 이번 선택에서 사용할 지갑과 획득 기록
    private LightShardWallet wallet;
    private RewardSelectionHistory history;

    // 현재 선택을 확정할 수 있는 상태
    private bool isSelectionOpen;

    // 선택창을 열기 전 시간 배율
    private float previousTimeScale;

    // 이 컨트롤러가 시간을 정지했는지 여부
    private bool ownsPause;
    private System.Action selectionConfirmed;

    /// <summary>선택창에서 확정을 기다리고 있는지 반환합니다.</summary>
    public bool IsSelectionOpen => isSelectionOpen;

    /// <summary>보상 확정 알림을 등록합니다.</summary>
    public void RegisterSelectionConfirmed(System.Action callback)
    {
        selectionConfirmed += callback;
    }

    /// <summary>보상 확정 알림을 해제합니다.</summary>
    public void UnregisterSelectionConfirmed(System.Action callback)
    {
        selectionConfirmed -= callback;
    }

    /// <summary>
    /// 지갑과 기록을 연결하고 필드 시간을 정지합니다.
    /// RewardSelectionFlow가 호출합니다.
    /// </summary>
    public void BeginSelection(
        LightShardWallet wallet, RewardSelectionHistory history)
    {
        if (isSelectionOpen)
        {
            return;
        }

        this.wallet = wallet;
        this.history = history;

        if (view == null || wallet == null || history == null)
        {
            return;
        }

        view.BindWallet(wallet);

        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0f;
        isSelectionOpen = true;
        PlayLog.Write("Reward", "Selection Open");
    }

    /// <summary>
    /// 선택을 확정하고 조각 소비 후 획득을 기록합니다.
    /// 확정 버튼이 호출합니다.
    /// </summary>
    public void ConfirmSelection()
    {
        if (!isSelectionOpen || view == null
            || wallet == null || history == null)
        {
            return;
        }

        RewardCardData card = view.SelectedCard;
        int investedShards = view.InvestmentAmount;

        RewardCardCatalog catalog = RewardCardCatalog.Instance;

        if (card == null || catalog == null
            || !catalog.TryGetCard(card.Id, out _))
        {
            return;
        }

        // 보유량이 부족하면 소비와 기록을 모두 진행하지 않는다.
        if (!wallet.TrySpend(investedShards))
        {
            return;
        }

        // 이번 선택을 닫아 중복 확정을 막는다.
        isSelectionOpen = false;
        history.AddRecord(card.Id, investedShards);
        PlayLog.Write("Reward", $"Confirmed (card={card.Id}, shards={investedShards})");
        RestoreTimeScale();
        view.gameObject.SetActive(false);
        selectionConfirmed?.Invoke();
    }

    /// <summary>선택을 확정하지 않고 보상창을 닫습니다.</summary>
    public void CancelSelection()
    {
        if (!isSelectionOpen)
        {
            return;
        }

        isSelectionOpen = false;
        RestoreTimeScale();
        if (view != null)
        {
            view.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // 화면이 다른 경로로 닫혀도 시간 정지가 남지 않게 한다.
        isSelectionOpen = false;
        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!ownsPause)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        ownsPause = false;
    }
}
