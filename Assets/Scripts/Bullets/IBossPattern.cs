/// <summary>보스 패턴의 시작과 중단을 정의합니다.</summary>
public interface IBossPattern
{
    /// <summary>보스의 공격 패턴을 시작합니다.</summary>
    void StartPattern();

    /// <summary>새 공격을 중단하고 기존 공격의 회수를 시작합니다.</summary>
    void StopPattern();
}
