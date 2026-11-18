using System.Collections.Generic;
using UnityEngine;

/// <summary>피격 시 몸과 대여한 숫자를 표시합니다.</summary>
[DisallowMultipleComponent]
public sealed class PlayerHitView : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform numberParent;
    [SerializeField] private PlayerHitNumber numberPrefab;

    // ##TEMP 피격 표시 수치는 플레이 후 조정합니다.
    [SerializeField] private Color flashColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField, Min(0.01f)] private float flashDuration = 0.12f;
    // ##TEMP 깜빡임 주기와 최저 알파는 플레이 후 조정합니다.
    [SerializeField, Min(0.01f)] private float blinkPeriod = 0.2f;
    [SerializeField, Range(0f, 1f)] private float blinkMinimumAlpha = 0.3f;
    // ##TEMP 퍼펙트 무적 색은 플레이 후 조정합니다.
    [SerializeField] private Color perfectColor = new Color(1f, 0.55f, 0.05f, 1f);
    [SerializeField] private Color numberColor = Color.white;
    [SerializeField, Min(1f)] private float numberSize = 48f;
    [SerializeField, Min(0f)] private float numberRiseDistance = 0.5f;
    [SerializeField, Min(0.01f)] private float numberDuration = 0.3f;
    [SerializeField, Min(0)] private int initialPoolCount = 4;
    [SerializeField] private Vector3 bodyOffset = new Vector3(0f, 0.6f, 0f);

    private ComponentPool<PlayerHitNumber> numberPool;
    private readonly List<PlayerHitNumber> activeNumbers = new List<PlayerHitNumber>();
    private Color originalBodyColor;
    private float flashRemaining;

    private void Awake()
    {
        if (health == null || bodyRenderer == null || worldCamera == null
            || canvas == null || numberParent == null || numberPrefab == null
            || numberPrefab.gameObject.activeSelf || initialPoolCount < 1)
        {
            PlayLog.Write("HitView", "피격 표시의 참조·비활성 프리팹·초기 수량을 확인해주세요.", LogType.Warning);
            enabled = false;
            return;
        }

        originalBodyColor = bodyRenderer.color;
        numberPool = new ComponentPool<PlayerHitNumber>(
            new PlayerHitNumberFactory(numberPrefab, numberParent), initialPoolCount, numberParent);
    }

    private void OnEnable()
    {
        if (numberPool != null)
            health.RegisterHit(OnHit);
    }

    private void OnDisable()
    {
        if (numberPool == null)
            return;
        health.UnregisterHit(OnHit);
        flashRemaining = 0f;
        bodyRenderer.color = originalBodyColor;
        ReleaseAllNumbers();
    }

    private void OnDestroy()
    {
        ReleaseAllNumbers();
        numberPool?.Clear();
        numberPool = null;
    }

    private void ReleaseAllNumbers()
    {
        if (numberPool != null)
        {
            foreach (PlayerHitNumber number in activeNumbers)
                if (number != null)
                    numberPool.Return(number);
        }
        activeNumbers.Clear();
    }

    private void Update()
    {
        if (flashRemaining > 0f)
            flashRemaining -= Time.deltaTime;

        UpdateBodyColor();
    }

    /// <summary>Update와 OnHit가 호출하며 번쩍임 → 금색 → 깜빡임 → 기본 색 순으로 몸 표시를 결정합니다.</summary>
    private void UpdateBodyColor()
    {
        if (flashRemaining > 0f)
        {
            bodyRenderer.color = flashColor;
            return;
        }

        if (!health.IsDead && Time.time < health.PerfectInvulnerableUntil)
        {
            Color perfectBodyColor = perfectColor;
            perfectBodyColor.a = originalBodyColor.a;
            bodyRenderer.color = perfectBodyColor;
            return;
        }

        Color bodyColor = originalBodyColor;
        if (!health.IsDead && Time.time < health.BlinkInvulnerableUntil)
        {
            float period = Mathf.Max(0.01f, blinkPeriod);
            if (Mathf.Repeat(Time.time, period) >= period * 0.5f)
                bodyColor.a *= Mathf.Clamp01(blinkMinimumAlpha);
        }

        bodyRenderer.color = bodyColor;
    }

    private void OnHit(int damage)
    {
        flashRemaining = flashDuration;
        UpdateBodyColor();

        if (damage <= 0)
            return;

        Vector3 startPosition = bodyRenderer.transform.position + bodyOffset;
        PlayerHitNumber rented = numberPool.Rent(number => number.Prepare(
            damage,
            startPosition,
            worldCamera,
            canvas,
            numberParent,
            numberColor,
            numberSize,
            numberRiseDistance,
            numberDuration,
            ReturnNumber));
        activeNumbers.Add(rented);
    }

    private void ReturnNumber(PlayerHitNumber number)
    {
        activeNumbers.Remove(number);
        numberPool.Return(number);
    }
}
