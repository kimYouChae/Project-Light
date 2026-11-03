using UnityEngine;
using UnityEngine.InputSystem;

// ##TEMP F8로 보상 선택창을 여는 테스트 전용 코드
/// <summary>
/// Play 중 F8 입력으로 보상 선택창을 엽니다.
/// </summary>
public sealed class RewardSelectionPreview_TEMP : MonoBehaviour
{
    // ##TEMP 정식 보상 화면 열기 흐름
    [SerializeField] private RewardSelectionFlow flow;

    private void Update()
    {
        // ##TEMP 패널이 이미 열렸다면 Flow에서 재열기를 막는다.
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.f8Key.wasPressedThisFrame
            && flow != null)
        {
            flow.TryOpenSelection();
        }
    }
}
