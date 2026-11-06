using UnityEngine;

/// <summary>얼어붙기 패턴의 혀 예고와 선택적 소리를 관리합니다.</summary>
public sealed class HeadBossFreezeWarning : MonoBehaviour
{
    [SerializeField] private SpriteRenderer tongueVisual;
    [SerializeField] private AudioSource audioSource;

    // 소리 에셋이 준비되면 인스펙터에서 연결한다.
    [SerializeField] private AudioClip hissClip;

    // ##TEMP 승인한 얼어붙기 예고 시간.
    [SerializeField, Min(0.01f)] private float duration = 0.5f;

    private float elapsed;
    private bool isShowing;
    private bool isComplete;
    private bool audioPaused;

    /// <summary>예고 표시가 정상적으로 끝났는지 반환합니다.</summary>
    public bool IsComplete => isComplete;

    private void Awake()
    {
        StopWarning();
    }

    /// <summary>
    /// 혀 표시와 선택적 소리 예고를 시작합니다.
    /// 이후 벽 패턴 제어부가 벽 접근 완료 후 호출합니다.
    /// </summary>
    public void BeginWarning()
    {
        if (!isActiveAndEnabled || isShowing)
        {
            return;
        }

        if (tongueVisual == null)
        {
            Debug.LogError("얼어붙기 혀 표시를 연결해주세요.", this);
            return;
        }

        elapsed = 0f;
        isComplete = false;
        isShowing = true;
        audioPaused = false;

        tongueVisual.gameObject.SetActive(true);

        if (audioSource != null && hissClip != null)
        {
            audioSource.clip = hissClip;
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        // 필드 시간 정지에 맞춰 예고 소리도 일시 정지한다.
        if (Time.timeScale <= 0f)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
                audioPaused = true;
            }

            return;
        }

        if (audioPaused)
        {
            if (audioSource != null)
            {
                audioSource.UnPause();
            }

            audioPaused = false;
        }

        elapsed += Time.deltaTime;

        if (elapsed >= duration)
        {
            CompleteWarning();
        }
    }

    private void CompleteWarning()
    {
        HideAndStopAudio();
        isShowing = false;
        isComplete = true;
    }

    /// <summary>
    /// 예고를 취소하고 표시·소리를 정리합니다.
    /// HeadBossPattern.StopPattern과 비활성 처리에서 호출합니다.
    /// </summary>
    public void StopWarning()
    {
        HideAndStopAudio();
        elapsed = 0f;
        isShowing = false;
        isComplete = false;
    }

    private void HideAndStopAudio()
    {
        if (tongueVisual != null)
        {
            tongueVisual.gameObject.SetActive(false);
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        audioPaused = false;
    }

    private void OnDisable()
    {
        StopWarning();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
    }
}
