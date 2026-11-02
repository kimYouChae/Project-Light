using System;
using UnityEngine;

/// <summary>보상 선택 화면에 표시할 카드 정보. 실제 효과 적용과 분리한다.</summary>
[Serializable]
public sealed class RewardCardData
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private string category;
    [SerializeField, TextArea] private string description;

    /// <summary>선택 기록에 저장하는 고유 식별자.</summary>
    public string Id => id;
    /// <summary>화면에 표시할 카드 이름.</summary>
    public string DisplayName => displayName;
    /// <summary>카드 분류.</summary>
    public string Category => category;
    /// <summary>기획서 기준 기본 효과와 빛 조각 추가량 설명.</summary>
    public string Description => description;

    /// <summary>표시용 카드 정보를 생성한다.</summary>
    public RewardCardData(string id, string displayName, string category, string description)
    {
        this.id = id;
        this.displayName = displayName;
        this.category = category;
        this.description = description;
    }
}
