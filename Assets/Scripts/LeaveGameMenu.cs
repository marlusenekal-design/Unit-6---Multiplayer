using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class LeaveGameMenu : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] InputActionReference pauseAction;
    [SerializeField] InputActionReference[] actionsToBlock;

    [Header("UI")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] TMP_Text leaveButtonText;

    private void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.action.Enable();
    }

    private void Start()
    {
        ClosePanel();
    }

    private void Update()
    {
        bool inGame = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (!inGame)
        {
            if (pausePanel != null && pausePanel.activeSelf)
                ClosePanel();

            return;
        }

        if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
        {
            if (pausePanel.activeSelf)
                ClosePanel();
            else
                OpenPanel();
        }
    }

    private void OpenPanel()
    {
        if (pausePanel == null)
            return;

        pausePanel.SetActive(true);

        if (leaveButtonText != null)
            leaveButtonText.text = NetworkManager.Singleton.IsHost ? "End Game (Host)" : "Leave Game";

        SetActionsBlocked(true);
    }

    private void ClosePanel()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        SetActionsBlocked(false);
    }

    private void SetActionsBlocked(bool blocked)
    {
        if (actionsToBlock == null)
            return;

        foreach (InputActionReference actionRef in actionsToBlock)
        {
            if (actionRef == null)
                continue;

            if (blocked)
                actionRef.action.Disable();
            else
                actionRef.action.Enable();
        }
    }

    public void Resume()
    {
        ClosePanel();
    }

    public void LeaveGame()
    {
        ClosePanel();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }
}