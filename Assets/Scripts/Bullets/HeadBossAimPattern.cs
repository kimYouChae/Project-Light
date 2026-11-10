using UnityEngine;

/// <summary>뱀 머리의 눈을 표시하고 조준선으로 플레이어를 추적합니다.</summary>
public sealed class HeadBossAimPattern : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer leftEye;
    [SerializeField] private SpriteRenderer rightEye;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private HeadBossDashPattern dashPattern;

    [SerializeField, Min(0.01f)] private float trackingDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float lockDuration = 0.5f;

    // ##TEMP 고정 예고의 표시 시작값.
    [SerializeField, Min(1f)] private float lockedWidthMultiplier = 1.6f;
    [SerializeField, Range(0f, 1f)] private float lockedOpacity = 1f;

    [SerializeField] private AudioSource lockAudioSource;
    [SerializeField] private AudioClip lockClip;

    private Transform target;
    private double phaseStartedAt;
    private bool isAiming;
    private bool isLocked;
    private Vector2 lockedDirection;
    private bool wasBodyVisible;
    private float originalWidthMultiplier;
    private Color originalStartColor;
    private Color originalEndColor;

    /// <summary>지정한 플레이어를 향해 눈과 추적 조준선을 표시합니다.</summary>
    public void BeginTracking(Transform player)
    {
        if (!isActiveAndEnabled || isAiming || player == null
            || body == null || leftEye == null || rightEye == null || aimLine == null)
        {
            return;
        }

        target = player;
        phaseStartedAt = Time.timeAsDouble;
        wasBodyVisible = body.enabled;
        originalWidthMultiplier = aimLine.widthMultiplier;
        originalStartColor = aimLine.startColor;
        originalEndColor = aimLine.endColor;
        isLocked = false;
        isAiming = true;

        body.enabled = false;
        leftEye.enabled = true;
        rightEye.enabled = true;
        aimLine.positionCount = 2;
        DrawAimLine();
        aimLine.enabled = true;
    }

    private void Update()
    {
        if (!isAiming)
        {
            return;
        }

        if (target == null)
        {
            StopTracking();
            return;
        }

        if (Time.deltaTime <= 0f)
        {
            return;
        }

        double elapsed = Time.timeAsDouble - phaseStartedAt;

        if (isLocked)
        {
            if (elapsed >= lockDuration)
            {
                PlayerHealth dashTarget = target.GetComponent<PlayerHealth>();
                StopTracking();
                if (dashPattern != null)
                {
                    dashPattern.BeginDash(dashTarget, lockedDirection);
                }
            }

            return;
        }

        if (elapsed >= trackingDuration)
        {
            LockAim();
            return;
        }

        DrawAimLine();
    }

    private void LockAim()
    {
        // 고정 시작 순간의 플레이어 위치를 마지막으로 그린다.
        DrawAimLine();
        Vector2 aimDirection = aimLine.GetPosition(1) - aimLine.GetPosition(0);
        lockedDirection = aimDirection.normalized;
        isLocked = true;
        phaseStartedAt = Time.timeAsDouble;
        PlayLog.Write("HeadBoss", $"Aim Lock Warning Start ({lockDuration:F3}s)");

        aimLine.widthMultiplier = originalWidthMultiplier * lockedWidthMultiplier;
        Color startColor = originalStartColor;
        Color endColor = originalEndColor;
        startColor.a = lockedOpacity;
        endColor.a = lockedOpacity;
        aimLine.startColor = startColor;
        aimLine.endColor = endColor;

        if (lockAudioSource != null && lockClip != null)
        {
            lockAudioSource.PlayOneShot(lockClip);
        }
    }

    private void DrawAimLine()
    {
        // 두 눈의 중점에서 플레이어 중심까지 월드 좌표로 연결한다.
        Vector3 eyeCenter =
            (leftEye.transform.position + rightEye.transform.position) * 0.5f;

        aimLine.SetPosition(0, eyeCenter);
        aimLine.SetPosition(1, target.position);
    }

    /// <summary>조준 표시와 소리를 끄고 조준 시작 전의 외형을 복원합니다.</summary>
    public void StopTracking()
    {
        if (!isAiming)
        {
            return;
        }

        isAiming = false;
        isLocked = false;
        target = null;
        leftEye.enabled = false;
        rightEye.enabled = false;
        aimLine.enabled = false;
        aimLine.widthMultiplier = originalWidthMultiplier;
        aimLine.startColor = originalStartColor;
        aimLine.endColor = originalEndColor;
        body.enabled = wasBodyVisible;

        if (lockAudioSource != null)
        {
            lockAudioSource.Stop();
        }
    }

    private void OnDisable()
    {
        StopTracking();
    }
}
