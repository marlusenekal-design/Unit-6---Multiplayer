using TMPro;
using UnityEngine;

public class PowerUpHUD : MonoBehaviour
{
    public static PowerUpHUD Instance { get; private set; }

    [SerializeField] TMP_Text powerUpText;

    private void Awake()
    {
        Instance = this;

        if (powerUpText == null)
            powerUpText = GetComponent<TMP_Text>();

        Hide();
    }

    public void Show(string label, Color color, float secondsLeft)
    {
        if (powerUpText == null)
            return;

        powerUpText.gameObject.SetActive(true);
        powerUpText.text = $"{label}  {Mathf.CeilToInt(secondsLeft)}s";
        powerUpText.color = color;
    }

    public void Hide()
    {
        if (powerUpText != null)
            powerUpText.gameObject.SetActive(false);
    }
}