using System;
using UnityEngine;

/// <summary>카드 선택 화면 표시와 버튼 입력을 전달한다.</summary>
public sealed class RewardSelectionView : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private UnityEngine.UI.Button[] cardButtons;
    [SerializeField] private UnityEngine.UI.Image[] cardBorders;
    [SerializeField] private TMPro.TMP_Text[] cardTitles;
    [SerializeField] private TMPro.TMP_Text[] cardCategories;
    [SerializeField] private TMPro.TMP_Text[] cardDescriptions;
    [SerializeField] private TMPro.TMP_Text walletText;
    [SerializeField] private TMPro.TMP_Text investmentText;
    [SerializeField] private UnityEngine.UI.Button decreaseButton;
    [SerializeField] private UnityEngine.UI.Button increaseButton;
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    // ##TEMP 선택 테두리 표시 색상.
    [SerializeField] private Color normalBorder = new Color(0.25f, 0.3f, 0.36f, 1f);
    [SerializeField] private Color selectedBorder = new Color(1f, 0.78f, 0.3f, 1f);
    private UnityEngine.Events.UnityAction[] cardActions;

    /// <summary>카드 클릭 알림. 구독자는 비활성화 시 해제한다.</summary>
    public event Action<int> CardSelected;
    /// <summary>투입 감소 요청. 구독자는 비활성화 시 해제한다.</summary>
    public event Action DecreaseRequested;
    /// <summary>투입 증가 요청. 구독자는 비활성화 시 해제한다.</summary>
    public event Action IncreaseRequested;
    /// <summary>선택 확정 요청. 구독자는 비활성화 시 해제한다.</summary>
    public event Action ConfirmRequested;

    private void OnEnable()
    {
        if (cardButtons == null)
            return;
        if (cardActions == null)
        {
            cardActions = new UnityEngine.Events.UnityAction[cardButtons.Length];
            for (int i = 0; i < cardActions.Length; i++)
            {
                int index = i;
                cardActions[i] = () => CardSelected?.Invoke(index);
            }
        }
        for (int i = 0; i < cardButtons.Length; i++)
            cardButtons[i].onClick.AddListener(cardActions[i]);
        decreaseButton.onClick.AddListener(RequestDecrease);
        increaseButton.onClick.AddListener(RequestIncrease);
        confirmButton.onClick.AddListener(RequestConfirm);
    }

    private void RequestDecrease() => DecreaseRequested?.Invoke();
    private void RequestIncrease() => IncreaseRequested?.Invoke();
    private void RequestConfirm() => ConfirmRequested?.Invoke();

    /// <summary>선택 화면을 표시한다.</summary>
    public void Show() => panelRoot.SetActive(true);

    /// <summary>선택 화면을 숨긴다.</summary>
    public void Hide() => panelRoot.SetActive(false);

    /// <summary>카드 표시·선택 테두리·보유량·투입 조작을 갱신한다.</summary>
    public void Refresh(RewardCardData[] cards, int selectedIndex, int investment, int owned)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cardTitles[i].text = cards[i].DisplayName;
            cardCategories[i].text = cards[i].Category;
            cardDescriptions[i].text = cards[i].Description;
            cardBorders[i].color = i == selectedIndex ? selectedBorder : normalBorder;
        }
        walletText.text = $"빛 조각 보유: {owned}개";
        investmentText.text = investment.ToString();
        decreaseButton.interactable = investment > 0;
        increaseButton.interactable = investment < owned;
        confirmButton.interactable = selectedIndex >= 0;
    }

    private void OnDisable()
    {
        if (cardActions == null)
            return;
        for (int i = 0; i < cardButtons.Length; i++)
            if (cardButtons[i] != null)
                cardButtons[i].onClick.RemoveListener(cardActions[i]);
        if (decreaseButton != null)
            decreaseButton.onClick.RemoveListener(RequestDecrease);
        if (increaseButton != null)
            increaseButton.onClick.RemoveListener(RequestIncrease);
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(RequestConfirm);
    }
}
