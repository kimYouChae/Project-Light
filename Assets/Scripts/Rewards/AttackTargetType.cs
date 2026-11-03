using System;

/// <summary>
/// 카드 효과가 적용되는 공격 구분입니다.
/// </summary>
[Flags]
public enum AttackTargetType
{
    None = 0,
    Normal = 1,
    Enhanced = 2,
    Frenzy = 4
}
