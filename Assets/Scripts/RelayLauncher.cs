using UnityEngine;
using System;
using System.Threading.Tasks;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using TMPro;
using UnityEngine.UI;

public class RelayLauncher : MonoBehaviour
{
    // ============================================
    [Header("Relay Settings")]
    [SerializeField] int maxPlayers = 4;
    private const string ConnectionType = "dtls";
    private bool isBusy = false;

    public string JoinCode { get; private set; }

    [Header("UI")]
    [SerializeField] TMP_InputField nameInputField;
    [SerializeField] TMP_InputField codeInputField;
    [SerializeField] GameObject loginMenu;
    [SerializeField] TMP_Text joinCodeText;
    // ============================================

    private void Start()
    {
        if (joinCodeText != null)
            joinCodeText.gameObject.SetActive(false);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }
    }

    private void SaveLocalName()
    {
        LocalPlayerData.PlayerName = nameInputField != null ? nameInputField.text.Trim() : "";
    }

    private async Task InitializeServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions options = new InitializationOptions();
            options.SetProfile("P" + Guid.NewGuid().ToString("N").Substring(0, 8));

            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
        }
    }

    public async void StartHost()
    {
        if (isBusy || NetworkManager.Singleton.IsListening)
            return;

        isBusy = true;
        SaveLocalName();

        try
        {
            await InitializeServices();

            int maxConnections = maxPlayers - 1;

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));

            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host started successfully.");
                Debug.Log("Relay Join Code: " + JoinCode);

                if (joinCodeText != null)
                {
                    joinCodeText.text = "Join Code: " + JoinCode;
                    joinCodeText.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("Failed to start host.");
            }
        }

        catch (Exception e)
        {
            Debug.LogError("Relay Host Error: " + e.Message);
        }
        finally
        {
            isBusy = false;
        }
    }

    public async void JoinGame()
    {
        if (isBusy || NetworkManager.Singleton.IsListening)
            return;

        if (string.IsNullOrEmpty(codeInputField.text))
        {
            Debug.Log("Please enter a join code.");
            return;
        }

        isBusy = true;
        SaveLocalName();

        try
        {
            await InitializeServices();

            string joinCode = codeInputField.text.Trim().ToUpperInvariant();

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client started, connecting...");
            }
            else
            {
                Debug.LogError("Failed to start client.");
            }
        }

        catch (Exception e)
        {
            Debug.LogError("Relay Join Error: " + e.Message);
        }
        finally
        {
            isBusy = false;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        Debug.Log("Connected to game as Client " + clientId);

        if (loginMenu != null)
            loginMenu.SetActive(false);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client " + clientId + " left the game");
            return;
        }

        string reason = NetworkManager.Singleton.DisconnectReason;
        Debug.LogWarning("Disconnected from game. Reason: " + (string.IsNullOrEmpty(reason) ? "None given" : reason));
    }

    private void OnClientStopped(bool wasHost)
    {
        Debug.Log(wasHost ? "Stopped hosting." : "Left the game.");

        if (loginMenu != null)
            loginMenu.SetActive(true);

        if (joinCodeText != null)
            joinCodeText.gameObject.SetActive(false);

        if (codeInputField != null)
            codeInputField.text = "";
    }
}