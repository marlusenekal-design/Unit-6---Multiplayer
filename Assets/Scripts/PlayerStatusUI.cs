using UnityEngine;

using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using Unity.Netcode;

public class PlayerStatusUI : MonoBehaviour
{
    public bool isLocalHUD = false;

    [SerializeField] Image healthbar;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text livesText;
    [SerializeField] TMP_Text scoreText;

    private NetworkStats stats;

    public void Bind(NetworkStats statsScript)
    {
        Unsubscribe();

        stats = statsScript;
        if (stats != null)
        {
            stats.CurrentHealth.OnValueChanged += OnHealthChanged;
            stats.Lives.OnValueChanged += OnLivesChanged;
            stats.Score.OnValueChanged += OnScoreChanged;
            stats.PlayerName.OnValueChanged += OnNameChanged;

            if (isLocalHUD)
                gameObject.SetActive(true);
        }

        Refresh();
    }

    public void Unbind()
    {
        Unsubscribe();

        if (isLocalHUD)
            gameObject.SetActive(false);
    }

    private void Unsubscribe()
    {
        if (stats != null)
        {
            stats.CurrentHealth.OnValueChanged -= OnHealthChanged;
            stats.Lives.OnValueChanged -= OnLivesChanged;
            stats.Score.OnValueChanged -= OnScoreChanged;
            stats.PlayerName.OnValueChanged -= OnNameChanged;
        }

        stats = null;
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Refresh();
    }

    private void OnLivesChanged(int oldValue, int newValue)
    {
        Refresh();
    }

    private void OnScoreChanged(int oldValue, int newValue)
    {
        Refresh();
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (stats != null)
        {
            nameText.text = stats.DisplayName;
            healthText.text = $"{stats.CurrentHealth.Value} / {stats.MaxHealth}";
            healthbar.fillAmount = stats.CurrentHealth.Value / stats.MaxHealth;

            livesText.text = $"Lives: {stats.Lives.Value}";
            scoreText.text = $"Score: {stats.Score.Value}";
        }
    }

    public void UpdateContent(string displayName, int health, int lives, int score)
    {
        nameText.text = displayName;
        healthText.text = health + " / 100";
        healthbar.fillAmount = (float)health / 100f;
        livesText.text = "Lives: " + lives.ToString();
        scoreText.text = "Score: " + score.ToString();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}