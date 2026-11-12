using System.Collections.Generic;
using UnityEngine;

/// <summary>필드 축소 경고, 경계 적용과 플레이어 위치 보정을 관리합니다.</summary>
public sealed class FieldManager : Singleton<FieldManager>
{
    [SerializeField] private SpriteRenderer playfield;

    // 위, 아래, 왼쪽, 오른쪽 경고 표시를 순서대로 연결한다.
    [SerializeField] private SpriteRenderer[] warningStrips;
    [SerializeField, Min(0.01f)] private float warningDuration = 3f;
    [SerializeField, Min(0.01f)] private float shrinkDuration = 1f;

    private readonly Queue<(int Stage, float Ratio)> pendingShrinks =
        new Queue<(int Stage, float Ratio)>();

    private PlayerMovement playerMovement;
    private Vector3 originalScale;
    private bool hasOriginalScale;
    private Vector2 originalSize;
    private bool isActive;
    private bool hasShrunk;
    private System.Action<Bounds> onBoundsChanged;
    private bool isShrinking;
    private Vector3 shrinkStartScale;
    private double shrinkStartedAt;
    private int warningStage;
    private float warningRatio;
    private double warningEndsAt;

    /// <summary>현재 적용된 필드 경계를 반환합니다.</summary>
    public Bounds CurrentBounds => playfield.bounds;

    /// <summary>실제 축소가 끝난 마지막 단계입니다.</summary>
    public int AppliedShrinkStage { get; private set; }

    /// <summary>전투 시작 시점 대비 현재 필드 가로 비율입니다.</summary>
    public float CurrentWidthRatio => originalSize.x > 0f
        ? CurrentBounds.size.x / originalSize.x
        : 1f;

    protected override void OnSingletonAwake()
    {
        HideWarning();
    }

    /// <summary>전투 시작 시 필드의 크기와 스케일을 저장합니다.</summary>
    public void BeginShrinkSequence(
        PlayerMovement player, System.Action<Bounds> onBoundsChanged = null)
    {
        EndShrinkSequence();
        playerMovement = player;
        this.onBoundsChanged = onBoundsChanged;
        originalScale = playfield.transform.localScale;
        hasOriginalScale = true;
        originalSize = playfield.bounds.size;
        AppliedShrinkStage = 0;
        isActive = true;
    }

    /// <summary>축소를 예약하고 원래 필드 기준의 목표 크기를 반환합니다.</summary>
    public Vector2 RequestShrink(int stage, float ratio)
    {
        pendingShrinks.Enqueue((stage, ratio));
        return originalSize * ratio;
    }

    private void LateUpdate()
    {
        if (!isActive || Time.deltaTime <= 0f)
        {
            return;
        }

        if (warningStage != 0 && !isShrinking
            && Time.timeAsDouble >= warningEndsAt)
        {
            isShrinking = true;
            shrinkStartedAt = Time.timeAsDouble;
            shrinkStartScale = playfield.transform.localScale;
            PlayLog.Write("Field", $"Shrink Move Start (stage={warningStage})");
        }

        if (isShrinking)
        {
            AdvanceShrink();
        }

        if (warningStage == 0 && pendingShrinks.Count > 0)
        {
            BeginWarning();
        }

        // 물림 중에도 축소 후에는 플레이어 몸체를 새 경계 안으로 보정한다.
        if (hasShrunk && playerMovement != null)
        {
            Transform playerTransform = playerMovement.transform;
            playerTransform.position =
                playerMovement.ClampToPlayfield(playerTransform.position);
        }
    }

    private void BeginWarning()
    {
        (warningStage, warningRatio) = pendingShrinks.Dequeue();
        warningEndsAt = Time.timeAsDouble + warningDuration;
        UpdateWarningStrips();
        PlayLog.Write("Field", $"Shrink Warning Start (stage={warningStage})");
    }

    private void UpdateWarningStrips()
    {
        Bounds current = playfield.bounds;
        Vector2 targetSize = originalSize * warningRatio;
        Vector2 inset = ((Vector2)current.size - targetSize) * 0.5f;
        Vector3 center = current.center;

        ShowStrip(0, center + Vector3.up * (current.extents.y - inset.y * 0.5f),
            new Vector2(current.size.x, inset.y));
        ShowStrip(1, center + Vector3.down * (current.extents.y - inset.y * 0.5f),
            new Vector2(current.size.x, inset.y));
        ShowStrip(2, center + Vector3.left * (current.extents.x - inset.x * 0.5f),
            new Vector2(inset.x, targetSize.y));
        ShowStrip(3, center + Vector3.right * (current.extents.x - inset.x * 0.5f),
            new Vector2(inset.x, targetSize.y));

    }

    private void ShowStrip(int index, Vector3 center, Vector2 size)
    {
        SpriteRenderer strip = warningStrips[index];
        if (size.x <= 0f || size.y <= 0f)
        {
            strip.enabled = false;
            return;
        }

        Vector3 spriteSize = strip.sprite.bounds.size;
        strip.transform.position = center;
        strip.transform.localScale = new Vector3(
            size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        strip.enabled = true;
    }

    private void AdvanceShrink()
    {
        float progress = Mathf.Clamp01(
            (float)((Time.timeAsDouble - shrinkStartedAt) / shrinkDuration));
        Vector3 targetScale = new Vector3(
            originalScale.x * warningRatio,
            originalScale.y * warningRatio,
            originalScale.z);

        playfield.transform.localScale =
            Vector3.Lerp(shrinkStartScale, targetScale, progress);
        hasShrunk = true;
        onBoundsChanged?.Invoke(CurrentBounds);

        if (progress < 1f)
        {
            UpdateWarningStrips();
            return;
        }

        HideWarning();
        AppliedShrinkStage = warningStage;
        PlayLog.Write("Field", $"Shrink Applied (stage={warningStage})");
        isShrinking = false;
        warningStage = 0;
    }

    /// <summary>현재 경고와 대기 중인 축소 요청을 정리합니다.</summary>
    public void EndShrinkSequence()
    {
        isActive = false;
        hasShrunk = false;
        isShrinking = false;
        onBoundsChanged = null;
        warningStage = 0;
        playerMovement = null;
        pendingShrinks.Clear();
        HideWarning();
    }

    /// <summary>전투 시작 시점의 필드 크기로 복구합니다.</summary>
    public void RestoreOriginalSize()
    {
        if (!hasOriginalScale)
        {
            return;
        }

        playfield.transform.localScale = originalScale;
        hasOriginalScale = false;
        PlayLog.Write("Field", "Original Size Restored");
    }

    private void HideWarning()
    {
        foreach (SpriteRenderer strip in warningStrips)
        {
            strip.enabled = false;
        }
    }

    private void OnDisable()
    {
        EndShrinkSequence();
    }
}
