using UnityEngine;

/// <summary>퍼펙트 게이지를 읽어 화면의 채움 이미지에 표시한다.</summary>
public sealed class PerfectGaugeView : MonoBehaviour
{
    [SerializeField] private PlayerPerfectDodge perfectDodge;
    [SerializeField] private UnityEngine.UI.Image gaugeFill;

    private void LateUpdate()
    {
        if (perfectDodge == null || gaugeFill == null)
            return;

        float ratio = Mathf.Clamp01(perfectDodge.CurrentGauge / perfectDodge.MaxGauge);
        if (!Mathf.Approximately(gaugeFill.fillAmount, ratio))
            gaugeFill.fillAmount = ratio;
    }
}
