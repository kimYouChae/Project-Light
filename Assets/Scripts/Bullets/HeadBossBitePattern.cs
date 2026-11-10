using System;
using UnityEngine;

/// <summary>물린 플레이어를 들어 올리고 흔들기 회차를 처리합니다.</summary>
public sealed class HeadBossBitePattern : MonoBehaviour
{
    private const int SHAKE_COUNT = 3;

    [SerializeField] private Transform biteHoldPoint;
    [SerializeField] private LineRenderer braceRing;

    // ##TEMP 물림 연출의 프로토타입 시작값.
    [SerializeField, Min(0f)] private float liftHeight = 0.5f;
    [SerializeField, Min(0.01f)] private float liftDuration = 0.25f;
    // ##TEMP 흔들기 프로토타입 시작값.
    [SerializeField, Min(0.02f)] private float shakeInterval = 0.8f;
    [SerializeField, Min(0.01f)] private float braceWindow = 0.1f;
    [SerializeField, Min(0f)] private float shakeDistance = 0.2f;
    [SerializeField, Min(0.01f)] private float ringStartOffset = 0.6f;
    // ##TEMP 내던지기 프로토타입 시작값.
    [SerializeField, Min(0f)] private float throwDistance = 1.5f;
    [SerializeField, Min(0.01f)] private float throwDuration = 0.35f;
    [SerializeField, Min(0f)] private float throwArcHeight = 0.35f;
    [SerializeField, Min(0f)] private float landingInvulnerability = 1f;

    private PlayerHealth heldPlayer;
    private PlayerMovement heldMovement;
    private PlayerLight playerLight;
    private PlayerPerfectDodge perfectDodge;
    private Vector3 liftStartPosition;
    private float liftElapsed;
    private double roundStartedAt = -1d;
    private int completedShakes;
    private bool triedBrace;
    private bool braced;
    private Vector2 throwDirection;
    private Vector3 throwStart;
    private Vector3 throwEnd;
    private float throwElapsed;
    private bool isThrowing;
    private Action onLanded;

    /// <summary>플레이어를 입에 고정하고 정상 착지 시 호출할 동작을 저장합니다.</summary>
    public bool BeginHold(PlayerHealth player, Vector2 direction, Action landed)
    {
        if (!isActiveAndEnabled || heldMovement != null
            || player == null || player.IsDead || biteHoldPoint == null || braceRing == null)
        {
            return false;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        PlayerLight light = player.GetComponent<PlayerLight>();
        PlayerPerfectDodge dodge = player.GetComponent<PlayerPerfectDodge>();
        if (movement == null || movement.IsHeldByBite || light == null || dodge == null)
        {
            return false;
        }

        heldPlayer = player;
        heldMovement = movement;
        playerLight = light;
        perfectDodge = dodge;
        liftStartPosition = transform.position;
        liftElapsed = 0f;
        roundStartedAt = -1d;
        completedShakes = 0;
        triedBrace = false;
        braced = false;
        throwDirection = direction;
        onLanded = landed;

        heldMovement.SetBiteHeld(true);
        PlayLog.Write("HeadBoss", "Bite Start");
        perfectDodge.RegisterBiteBraceInput(TryBrace);
        PlaceAtMouth();
        return true;
    }

    private void LateUpdate()
    {
        if (heldPlayer == null || heldPlayer.IsDead
            || heldMovement == null || biteHoldPoint == null || braceRing == null
            || playerLight == null || perfectDodge == null)
        {
            ReleaseHold();
            return;
        }

        if (Time.deltaTime <= 0f)
        {
            return;
        }

        if (isThrowing)
        {
            UpdateThrow();
            return;
        }

        if (liftElapsed < liftDuration)
        {
            liftElapsed = Mathf.Min(liftElapsed + Time.deltaTime, liftDuration);
            float progress = liftElapsed / liftDuration;
            transform.position = liftStartPosition + Vector3.up * (liftHeight * progress);
        }
        else
        {
            UpdateShakeRound();
        }

        if (heldPlayer != null && !isThrowing)
        {
            PlaceAtMouth();
        }
    }

    private void UpdateShakeRound()
    {
        if (completedShakes >= SHAKE_COUNT)
        {
            return;
        }

        double now = Time.timeAsDouble;
        if (roundStartedAt < 0d)
        {
            roundStartedAt = now;
        }

        float elapsed = (float)(now - roundStartedAt);
        float strikeTime = shakeInterval * 0.5f;
        Vector3 raisedPosition = liftStartPosition + Vector3.up * liftHeight;

        if (elapsed >= shakeInterval)
        {
            transform.position = raisedPosition;
            PlayLog.Write("HeadBoss",
                $"Bite Shake {completedShakes + 1}/{SHAKE_COUNT} {(braced ? "Success" : "Failed")}");

            if (!braced)
            {
                heldPlayer.ApplyBiteShakeDamage();
            }

            completedShakes++;
            triedBrace = false;
            braced = false;
            roundStartedAt = -1d;
            braceRing.enabled = false;

            if (heldPlayer.IsDead)
            {
                ReleaseHold();
            }
            else if (completedShakes == SHAKE_COUNT)
            {
                BeginThrow();
            }

            return;
        }

        // 회차마다 반대 방향으로 흔들고 원래 위치로 돌아온다.
        float swayProgress = Mathf.InverseLerp(strikeTime, shakeInterval, elapsed);
        float direction = completedShakes % 2 == 0 ? -1f : 1f;
        float sway = Mathf.Sin(swayProgress * Mathf.PI) * shakeDistance * direction;
        transform.position = raisedPosition + Vector3.right * sway;

        float radius = Mathf.Max(0.01f,
            playerLight.CoreRadius + ringStartOffset * (1f - elapsed / strikeTime));
        DrawBraceRing(radius);
    }

    private void TryBrace()
    {
        if (roundStartedAt < 0d || triedBrace || completedShakes >= SHAKE_COUNT)
        {
            return;
        }

        double elapsed = Time.timeAsDouble - roundStartedAt;
        if (elapsed >= shakeInterval)
        {
            return;
        }

        triedBrace = true;
        float strikeTime = shakeInterval * 0.5f;
        braced = System.Math.Abs(elapsed - strikeTime) <= braceWindow;
    }

    private void DrawBraceRing(float radius)
    {
        // 플레이어를 배치할 입의 XY 좌표를 중심으로 원을 그린다.
        Vector3 center = biteHoldPoint.position;
        center.z = heldPlayer.transform.position.z;
        int pointCount = braceRing.positionCount;

        for (int index = 0; index < pointCount; index++)
        {
            float angle = 2f * Mathf.PI * index / pointCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            braceRing.SetPosition(index, center + offset);
        }

        braceRing.enabled = true;
    }

    private void PlaceAtMouth()
    {
        // 플레이어의 표시 깊이를 유지하고 입의 XY 위치를 따라간다.
        Vector3 position = biteHoldPoint.position;
        position.z = heldPlayer.transform.position.z;
        heldPlayer.transform.position = position;
    }

    private void BeginThrow()
    {
        PlaceAtMouth();
        throwStart = heldPlayer.transform.position;
        throwEnd = heldMovement.ClampToPlayfield(
            throwStart + (Vector3)(throwDirection * throwDistance));

        throwElapsed = 0f;
        isThrowing = true;
        PlayLog.Write("HeadBoss", "Bite Throw");
        perfectDodge.UnregisterBiteBraceInput(TryBrace);
    }

    private void UpdateThrow()
    {
        throwElapsed = Mathf.Min(throwElapsed + Time.deltaTime, throwDuration);
        float progress = throwElapsed / throwDuration;

        // 시작과 착지 사이에 화면 위쪽으로 포물선을 그린다.
        float arc = 4f * throwArcHeight * progress * (1f - progress);
        heldPlayer.transform.position =
            Vector3.Lerp(throwStart, throwEnd, progress) + Vector3.up * arc;

        if (throwElapsed >= throwDuration)
        {
            heldPlayer.transform.position = throwEnd;
            heldPlayer.GrantInvulnerability(landingInvulnerability);
            PlayLog.Write("HeadBoss", $"Bite Landing (invulnerability={landingInvulnerability:F3}s)");
            isThrowing = false;

            Action landed = onLanded;
            ReleaseHold();
            landed?.Invoke();
        }
    }

    /// <summary>입 고정을 풀고 플레이어의 물림 상태를 해제합니다.</summary>
    public void ReleaseHold()
    {
        if (isThrowing && heldPlayer != null && heldMovement != null)
        {
            heldPlayer.transform.position =
                heldMovement.ClampToPlayfield(heldPlayer.transform.position);
        }

        isThrowing = false;
        onLanded = null;

        if (perfectDodge != null)
        {
            perfectDodge.UnregisterBiteBraceInput(TryBrace);
        }

        if (braceRing != null && braceRing.enabled)
        {
            braceRing.enabled = false;
        }

        if (heldMovement != null)
        {
            heldMovement.SetBiteHeld(false);
        }

        heldPlayer = null;
        heldMovement = null;
        playerLight = null;
        perfectDodge = null;
    }

    private void OnDisable()
    {
        ReleaseHold();
    }

    private void OnValidate()
    {
        braceWindow = Mathf.Clamp(braceWindow, 0.01f, shakeInterval * 0.5f);
    }
}
