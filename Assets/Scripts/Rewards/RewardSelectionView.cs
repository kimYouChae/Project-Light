using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// 보상 후보 카드 3장의 이름, 설명, 선택 상태를 표시합니다.
/// </summary>
public sealed class RewardSelectionView : MonoBehaviour
{
    private const int CARD_COUNT = 3;

    // 왼쪽부터 순서대로 연결할 카드 이름 텍스트
    [SerializeField] private TMP_Text[] nameTexts = new TMP_Text[CARD_COUNT];

    // 왼쪽부터 순서대로 연결할 카드 설명 텍스트
    [SerializeField] private TMP_Text[] descriptionTexts = new TMP_Text[CARD_COUNT];

    // 왼쪽부터 순서대로 연결할 선택 테두리
    [SerializeField] private GameObject[] selectionBorders =
        new GameObject[CARD_COUNT];

    // 현재 표시 중인 카드
    private readonly RewardCardData[] displayedCards =
        new RewardCardData[CARD_COUNT];

    // 현재 선택한 카드
    private RewardCardData selectedCard;

    // 보유량과 투입량을 표시할 텍스트
    [SerializeField] private TMP_Text walletText;
    [SerializeField] private TMP_Text investmentText;

    // 선택한 카드의 투입량 반영 예상 효과
    [SerializeField] private TMP_Text effectPreviewText;

    // 투입량 조절 버튼
    [SerializeField] private UnityEngine.UI.Button decreaseButton;
    [SerializeField] private UnityEngine.UI.Button increaseButton;

    // 선택을 확정하는 버튼
    [SerializeField] private UnityEngine.UI.Button confirmButton;

    // 외부에서 전달받는 이번 플레이의 빛 조각 지갑
    private LightShardWallet wallet;

    // 선택한 카드에 투입할 예정인 조각 수
    private int investmentAmount;

    /// <summary>
    /// 현재 투입 예정인 빛 조각 수를 반환합니다.
    /// </summary>
    public int InvestmentAmount => investmentAmount;

    /// <summary>
    /// 현재 선택한 카드를 반환합니다. 선택 전에는 null입니다.
    /// </summary>
    public RewardCardData SelectedCard => selectedCard;

    /// <summary>
    /// 카드 3장의 이름과 설명을 표시하고 이전 선택을 초기화합니다.
    /// 카드 또는 표시 참조가 올바르지 않으면 false를 반환합니다.
    /// </summary>
    public bool TryDisplayCards(RewardCardData[] cards)
    {
        // 표시 전에 카드 수와 모든 참조를 확인한다.
        if (cards == null || cards.Length != CARD_COUNT
            || nameTexts == null || nameTexts.Length != CARD_COUNT
            || descriptionTexts == null
            || descriptionTexts.Length != CARD_COUNT
            || selectionBorders == null
            || selectionBorders.Length != CARD_COUNT)
        {
            return false;
        }

        for (int i = 0; i < CARD_COUNT; i++)
        {
            if (cards[i] == null
                || nameTexts[i] == null
                || descriptionTexts[i] == null
                || selectionBorders[i] == null)
            {
                return false;
            }
        }

        // 새 후보를 표시하면서 이전 선택과 테두리를 초기화한다.
        selectedCard = null;
        investmentAmount = 0;

        for (int i = 0; i < CARD_COUNT; i++)
        {
            displayedCards[i] = cards[i];
            nameTexts[i].text = cards[i].DisplayName;
            descriptionTexts[i].text = cards[i].Description;
            selectionBorders[i].SetActive(false);
        }

        RefreshInvestment();
        return true;
    }

    /// <summary>
    /// 지정한 위치의 카드를 선택합니다. 카드 버튼이 호출합니다.
    /// </summary>
    public void SelectCard(int index)
    {
        if (index < 0 || index >= CARD_COUNT
            || displayedCards[index] == null)
        {
            return;
        }

        if (selectedCard != displayedCards[index])
        {
            selectedCard = displayedCards[index];
            investmentAmount = 0;
            PlayLog.Write("Reward", $"Card Selected (card={selectedCard.Id})");
        }

        // 선택한 카드의 테두리만 표시한다.
        for (int i = 0; i < CARD_COUNT; i++)
        {
            selectionBorders[i].SetActive(i == index);
        }

        RefreshInvestment();
    }

    /// <summary>
    /// 보유량을 표시할 지갑을 연결합니다.
    /// 씬 연결을 담당하는 코드가 호출합니다.
    /// </summary>
    public void BindWallet(LightShardWallet lightShardWallet)
    {
        wallet = lightShardWallet;
        investmentAmount = 0;
        RefreshInvestment();
    }

    /// <summary>
    /// 투입 예정 조각 수를 1개 줄입니다. 감소 버튼이 호출합니다.
    /// </summary>
    public void DecreaseInvestment()
    {
        if (wallet == null || selectedCard == null)
        {
            return;
        }

        investmentAmount = Mathf.Clamp(
            investmentAmount, 0, wallet.Count);

        if (investmentAmount > 0)
        {
            investmentAmount--;
        }

        RefreshInvestment();
    }

    /// <summary>
    /// 투입 예정 조각 수를 1개 늘립니다. 증가 버튼이 호출합니다.
    /// </summary>
    public void IncreaseInvestment()
    {
        if (wallet == null || selectedCard == null)
        {
            return;
        }

        investmentAmount = Mathf.Clamp(
            investmentAmount, 0, wallet.Count);

        if (investmentAmount < wallet.Count)
        {
            investmentAmount++;
        }

        RefreshInvestment();
    }

    private void LateUpdate()
    {
        // 지갑 보유량이 바뀌면 표시와 투입 가능 범위를 갱신한다.
        RefreshInvestment();
    }

    private void RefreshInvestment()
    {
        int availableCount = wallet != null ? wallet.Count : 0;

        investmentAmount = Mathf.Clamp(
            investmentAmount, 0, availableCount);

        if (walletText != null)
        {
            walletText.text = wallet != null
                ? $"보유 빛 조각: {availableCount}"
                : "보유 빛 조각: 미연결";
        }

        if (investmentText != null)
        {
            investmentText.text = $"투입 빛 조각: {investmentAmount}";
        }

        bool canAdjust = wallet != null && selectedCard != null;

        if (decreaseButton != null)
        {
            decreaseButton.interactable =
                canAdjust && investmentAmount > 0;
        }

        if (increaseButton != null)
        {
            increaseButton.interactable =
                canAdjust && investmentAmount < availableCount;
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = canAdjust;
        }

        RefreshEffectPreview();
    }

    private void RefreshEffectPreview()
    {
        if (effectPreviewText == null)
        {
            return;
        }

        if (selectedCard == null)
        {
            effectPreviewText.text = "카드를 선택하세요";
            return;
        }

        // 수치 또는 표시 서식이 준비되지 않았다면 계산하지 않는다.
        if (selectedCard.ShardsPerIncrease < 1
            || string.IsNullOrEmpty(selectedCard.EffectDisplayFormat))
        {
            effectPreviewText.text = "효과 수치 미정";
            return;
        }

        // 필요한 조각 수를 채운 횟수만큼 추가 수치를 반영한다.
        int increaseCount =
            investmentAmount / selectedCard.ShardsPerIncrease;

        float effectAmount = selectedCard.BaseEffectAmount
            + increaseCount * selectedCard.AdditionalEffectAmount;

        effectPreviewText.text = string.Format(
            CultureInfo.InvariantCulture,
            selectedCard.EffectDisplayFormat,
            effectAmount);
    }
}
