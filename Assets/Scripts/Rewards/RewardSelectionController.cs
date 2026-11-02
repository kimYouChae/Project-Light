using System.Collections.Generic;
using UnityEngine;

/// <summary>중복 없는 보상 제시·투입량·소비·선택 기록을 관리한다.</summary>
public sealed class RewardSelectionController : MonoBehaviour
{
    [SerializeField] private MiniBossBattleFlow battleFlow;
    [SerializeField] private LightShardWallet wallet;
    [SerializeField] private RewardSelectionHistory selectionHistory;
    [SerializeField] private RewardSelectionView view;
    // ##TEMP 기획서의 짤보스 13종 표시 데이터. 실제 카드 효과는 아직 적용하지 않는다.
    [SerializeField] private RewardCardData[] cardPool =
    {
        new RewardCardData("swift_feet", "날렵한 발", "스탯", "이동속도 +10%\n빛 조각 1개당 +2%"),
        new RewardCardData("large_lantern", "큰 등불", "스탯", "투사체 크기 +15%\n빛 조각 1개당 +3%"),
        new RewardCardData("fast_light", "빠른 빛", "스탯", "투사체 속도 +15%\n빛 조각 1개당 +3%"),
        new RewardCardData("wide_halo", "넓은 헤일로", "스탯", "헤일로 반경 +10%\n시야와 최대 체력\n빛 조각 1개당 +2%"),
        new RewardCardData("pulse", "고동", "스탯", "폭주 게이지 충전 +15%\n빛 조각 1개당 +3%"),
        new RewardCardData("long_breath", "긴 숨", "스탯", "폭주 지속 +0.5초\n빛 조각 1개당 +0.1초"),
        new RewardCardData("instant", "찰나", "스탯", "퍼펙트 무적 +0.05초\n빛 조각 1개당 +0.01초"),
        new RewardCardData("poison_light", "독 묻은 빛", "독", "평타에 독 1중첩\n중첩당 지속 피해"),
        new RewardCardData("venom_strike", "맹독 강타", "독", "강화 평타 독 2중첩\n빛 조각 2개당 +1중첩"),
        new RewardCardData("frost_light", "서리 빛", "서리", "평타에 서리 1\n쌓이면 보스 발사 간격이\n잠깐 느려짐"),
        new RewardCardData("cold_strike", "한파 강타", "서리", "강화 평타 서리 수치 +2\n빛 조각 2개당 +1"),
        new RewardCardData("ember_light", "불씨 빛", "불씨", "평타 맞은 자리에 불씨\n잠깐 주변을 밝히고 화상"),
        new RewardCardData("large_ember", "큰 불씨", "불씨", "강화 평타 불씨 범위 +20%\n빛 조각 1개당 +5%")
    };

    private const int OFFERED_CARD_COUNT = 3;
    private readonly RewardCardData[] offeredCards = new RewardCardData[OFFERED_CARD_COUNT];
    [SerializeField] private int selectedIndex = -1;
    [SerializeField] private int investment;
    private int lastWalletCount;
    private bool isOpen;

    private void OnEnable()
    {
        if (battleFlow != null)
            battleFlow.RewardRequested += OpenSelection;
        if (view == null)
            return;
        view.CardSelected += SelectCard;
        view.DecreaseRequested += DecreaseInvestment;
        view.IncreaseRequested += IncreaseInvestment;
        view.ConfirmRequested += ConfirmSelection;
    }

    private void Start()
    {
        if (battleFlow == null || wallet == null || selectionHistory == null || view == null)
        {
            Debug.LogError("보상 선택의 전투 흐름·보유량·기록·UI 참조를 확인해주세요.", this);
            enabled = false;
            return;
        }
        view.Hide();
        if (battleFlow.IsRewardPending)
            OpenSelection();
    }

    private void Update()
    {
        if (isOpen && wallet.Count != lastWalletCount)
            RefreshView();
    }

    /// <summary>과거 선택 카드를 제외하지 않고 서로 다른 세 카드를 제시한다.</summary>
    public void OpenSelection()
    {
        if (isOpen)
            return;
        List<RewardCardData> candidates = new List<RewardCardData>();
        HashSet<string> ids = new HashSet<string>();
        if (cardPool != null)
            foreach (RewardCardData card in cardPool)
                if (card != null && !string.IsNullOrEmpty(card.Id) && ids.Add(card.Id))
                    candidates.Add(card);
        if (candidates.Count < OFFERED_CARD_COUNT)
        {
            Debug.LogError("서로 다른 보상 카드가 최소 3장 필요합니다.", this);
            return;
        }
        for (int i = 0; i < offeredCards.Length; i++)
        {
            int index = Random.Range(0, candidates.Count);
            offeredCards[i] = candidates[index];
            candidates.RemoveAt(index);
        }
        selectedIndex = -1;
        investment = 0;
        isOpen = true;
        RefreshView();
        view.Show();
    }

    private void SelectCard(int index)
    {
        if (!isOpen || index < 0 || index >= offeredCards.Length)
            return;
        selectedIndex = index;
        RefreshView();
    }

    /// <summary>보유량 안에서 빛 조각 투입량을 한 개 늘린다.</summary>
    public void IncreaseInvestment()
    {
        if (!isOpen)
            return;
        investment = Mathf.Min(wallet.Count, investment + 1);
        RefreshView();
    }

    /// <summary>빛 조각 투입량을 0까지 한 개 줄인다.</summary>
    public void DecreaseInvestment()
    {
        if (!isOpen)
            return;
        investment = Mathf.Max(0, investment - 1);
        RefreshView();
    }

    /// <summary>한 번만 빛 조각을 소비하고 결과를 기록한 뒤 화면을 닫는다.</summary>
    public void ConfirmSelection()
    {
        if (!isOpen || selectedIndex < 0)
            return;
        if (!wallet.TrySpend(investment))
        {
            RefreshView();
            return;
        }
        isOpen = false;
        selectionHistory.Record(offeredCards[selectedIndex].Id, investment);
        view.Hide();
    }

    private void RefreshView()
    {
        investment = Mathf.Clamp(investment, 0, wallet.Count);
        lastWalletCount = wallet.Count;
        view.Refresh(offeredCards, selectedIndex, investment, wallet.Count);
    }

    private void OnDisable()
    {
        if (battleFlow != null)
            battleFlow.RewardRequested -= OpenSelection;
        if (view != null)
        {
            view.CardSelected -= SelectCard;
            view.DecreaseRequested -= DecreaseInvestment;
            view.IncreaseRequested -= IncreaseInvestment;
            view.ConfirmRequested -= ConfirmSelection;
            view.Hide();
        }
        isOpen = false;
    }
}
