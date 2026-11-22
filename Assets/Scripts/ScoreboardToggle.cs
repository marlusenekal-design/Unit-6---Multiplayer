using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class ScoreboardToggle : MonoBehaviour
{
    [SerializeField] InputActionReference showScoreboardAction;
    [SerializeField] CanvasGroup overlayGroup;

    private void Awake()
    {
        if (overlayGroup == null)
            overlayGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (showScoreboardAction != null)
            showScoreboardAction.action.Enable();
    }

    private void Start()
    {
        SetVisible(false);
    }

    private void Update()
    {
        bool inGame = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        bool isHeld = showScoreboardAction != null && showScoreboardAction.action.IsPressed();

        bool matchEnded = MatchManager.Instance != null && MatchManager.Instance.IsSpawned &&
                          MatchManager.Instance.State.Value == MatchState.Ended;

        SetVisible(inGame && (isHeld || matchEnded));
    }

    private void SetVisible(bool visible)
    {
        if (overlayGroup == null)
            return;

        overlayGroup.alpha = visible ? 1f : 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;
    }
}