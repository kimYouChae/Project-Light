using System;
using TMPro;
using UnityEngine;

/// <summary>피격 숫자를 띄우고 시간이 지나면 풀에 반환합니다.</summary>
[DisallowMultipleComponent]
public sealed class PlayerHitNumber : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private RectTransform rectTransform;
    private RectTransform parent;
    private Camera worldCamera;
    private Camera canvasCamera;
    private Vector3 startPosition;
    private Color startColor;
    private float riseDistance;
    private float duration;
    private float elapsed;
    private Action<PlayerHitNumber> returnToPool;

    /// <summary>활성화 전에 숫자와 위치, 반환 동작을 준비합니다.</summary>
    public void Prepare(
        int damage,
        Vector3 worldPosition,
        Camera sourceCamera,
        Canvas targetCanvas,
        RectTransform targetParent,
        Color color,
        float size,
        float distance,
        float displayDuration,
        Action<PlayerHitNumber> returnCallback)
    {
        if (rectTransform == null)
            rectTransform = (RectTransform)transform;

        parent = targetParent;
        worldCamera = sourceCamera;
        canvasCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;
        startPosition = worldPosition;
        startColor = color;
        riseDistance = distance;
        duration = displayDuration;
        elapsed = 0f;
        returnToPool = returnCallback;

        label.text = "-" + damage;
        label.fontSize = size;
        label.color = color;
        transform.localScale = Vector3.one;
        UpdatePosition();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        UpdatePosition();

        Color color = startColor;
        color.a *= 1f - progress;
        label.color = color;

        if (progress >= 1f)
        {
            Action<PlayerHitNumber> callback = returnToPool;
            returnToPool = null;
            callback?.Invoke(this);
        }
    }

    private void UpdatePosition()
    {
        float progress = Mathf.Clamp01(elapsed / duration);
        Vector3 position = startPosition + Vector3.up * (riseDistance * progress);
        Vector2 screenPosition = worldCamera.WorldToScreenPoint(position);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, screenPosition, canvasCamera, out Vector2 localPosition))
        {
            rectTransform.anchoredPosition = localPosition;
        }
    }
}
