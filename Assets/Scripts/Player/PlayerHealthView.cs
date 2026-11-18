using UnityEngine;

/// <summary>플레이어 체력 등불을 표시합니다.</summary>
[DisallowMultipleComponent]
public sealed class PlayerHealthView : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private HealthLanternGraphic lanternGraphic;

    private bool hasStarted;

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.RegisterHealthChanged(Refresh);
        }

        if (hasStarted)
        {
            Refresh();
        }
    }

    private void Start()
    {
        hasStarted = true;
        Refresh();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.UnregisterHealthChanged(Refresh);
        }
    }

    private void Refresh()
    {
        if (playerHealth == null || lanternGraphic == null)
            return;

        lanternGraphic.SetSlots(playerHealth.MaxHealth, playerHealth.CurrentHealth);
    }
}
