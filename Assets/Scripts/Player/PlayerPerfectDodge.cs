using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>스페이스 입력 순간에 코어 경계의 탄을 검사하고 무적·게이지를 부여한다.</summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth), typeof(PlayerLight))]
public sealed class PlayerPerfectDodge : MonoBehaviour
{
    [Header("탄 관리자")]
    [SerializeField] private BulletRegistry bulletRegistry;

    [Header("퍼펙트 회피")]
    // ##TEMP 승인한 프로토타입 시작값. 인스펙터에서 조정한다.
    [SerializeField, Min(0f)] private float successInvulnerability = 0.25f;
    [SerializeField, Min(0f)] private float failureLockDuration = 0.5f;
    [SerializeField, Min(1)] private int simultaneousBonusLimit = 3;

    [Header("게이지")]
    // ##TEMP 승인한 프로토타입 시작값. 자동 폭주는 다음 단계에서 연결한다.
    [SerializeField, Min(1f)] private float maxGauge = 100f;
    [SerializeField, Min(0f)] private float gaugePerBullet = 10f;
    [SerializeField] private float currentGauge;

    private PlayerHealth playerHealth;
    private PlayerLight playerLight;
    private float nextInputTime;

    /// <summary>현재 충전된 퍼펙트 게이지.</summary>
    public float CurrentGauge => currentGauge;

    /// <summary>게이지 최대 충전량.</summary>
    public float MaxGauge => maxGauge;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerLight = GetComponent<PlayerLight>();
        currentGauge = 0f;
        nextInputTime = float.NegativeInfinity;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            TryPerfectDodge();
    }

    private void TryPerfectDodge()
    {
        if (!isActiveAndEnabled || playerHealth.IsDead || bulletRegistry == null || Time.time < nextInputTime)
            return;

        int count = 0;
        // 입력 프레임에만 전체 목록을 순회한다. 충전 상한을 넘어도 모두 처리 표시한다.
        foreach (TestBullet bullet in bulletRegistry.ActiveBullets)
        {
            if (bullet != null && !bullet.PerfectProcessed
                && playerLight.IsOnCoreBoundary(bullet.transform.position, bullet.Radius)
                && bullet.TryMarkPerfect())
            {
                count++;
            }
        }

        if (count == 0)
        {
            nextInputTime = Time.time + failureLockDuration;
            return;
        }

        playerHealth.GrantInvulnerability(successInvulnerability);
        int bonusCount = Mathf.Min(count, simultaneousBonusLimit);
        currentGauge = Mathf.Min(maxGauge, currentGauge + bonusCount * gaugePerBullet);
    }

    private void OnValidate()
    {
        successInvulnerability = Mathf.Max(0f, successInvulnerability);
        failureLockDuration = Mathf.Max(0f, failureLockDuration);
        simultaneousBonusLimit = Mathf.Max(1, simultaneousBonusLimit);
        maxGauge = Mathf.Max(1f, maxGauge);
        gaugePerBullet = Mathf.Max(0f, gaugePerBullet);
        currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
    }
}
