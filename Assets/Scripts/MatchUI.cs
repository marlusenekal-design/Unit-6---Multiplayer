using TMPro;
using UnityEngine;

public class MatchUI : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] float warningTime = 10f;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color warningColor = new Color(1f, 0.3f, 0.3f);

    [Header("Results")]
    [SerializeField] GameObject resultsPanel;
    [SerializeField] TMP_Text resultText;
    [SerializeField] TMP_Text nextRoundText;

    private void Update()
    {
        MatchManager match = MatchManager.Instance;

        bool inGame = match != null && match.IsSpawned;
        bool isPlaying = inGame && match.State.Value == MatchState.Playing;
        bool isEnded = inGame && match.State.Value == MatchState.Ended;

        if (timerText != null)
            timerText.gameObject.SetActive(isPlaying);

        if (resultsPanel != null)
            resultsPanel.SetActive(isEnded);

        if (isPlaying)
            UpdateTimer(match.TimeRemaining);

        if (isEnded)
            UpdateResults(match);
    }

    private void UpdateTimer(float secondsLeft)
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(secondsLeft);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes}:{seconds:00}";

        timerText.color = secondsLeft <= warningTime ? warningColor : normalColor;
    }

    private void UpdateResults(MatchManager match)
    {
        if (resultText != null)
            resultText.text = match.ResultText.Value.ToString();

        if (nextRoundText != null)
            nextRoundText.text = $"Next round in {Mathf.CeilToInt(match.TimeUntilNextRound)}";
    }
}