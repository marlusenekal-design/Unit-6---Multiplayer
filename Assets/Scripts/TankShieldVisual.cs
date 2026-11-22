using UnityEngine;

public class TankShieldVisual : MonoBehaviour
{
    [SerializeField] NetworkStats stats;
    [SerializeField] GameObject shieldBubble;
    [SerializeField] float pulseSpeed = 4f;
    [SerializeField] float pulseAmount = 0.05f;

    private Vector3 baseScale;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<NetworkStats>();

        if (shieldBubble != null)
        {
            baseScale = shieldBubble.transform.localScale;
            shieldBubble.SetActive(false);
        }
    }

    private void Start()
    {
        if (stats == null || shieldBubble == null)
            return;

        stats.IsShielded.OnValueChanged += OnShieldChanged;

        shieldBubble.SetActive(stats.IsShielded.Value);
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.IsShielded.OnValueChanged -= OnShieldChanged;
    }

    private void OnShieldChanged(bool oldValue, bool newValue)
    {
        shieldBubble.SetActive(newValue);
    }

    private void Update()
    {
        if (shieldBubble == null || !shieldBubble.activeSelf)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        shieldBubble.transform.localScale = baseScale * pulse;
    }
}