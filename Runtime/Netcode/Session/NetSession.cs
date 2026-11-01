using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace MagicUtilities.Netcode
{
    public enum NetSessionState
    {
        Offline,
        Connecting,
        Connected,
        Disconnecting
    }

    /// <summary>
    /// Single entry point to host, join and leave a session. Knows nothing about the UI:
    /// call Host / Join / Leave from your buttons and listen to the events.
    /// The provider asset decides how the session is created (direct IP, Multiplayer Services, Steam...).
    /// </summary>
    public class NetSession : MonoBehaviour
    {
        public static NetSession Instance { get; private set; }

        [Tooltip("Use NetworkManager.Singleton. Uncheck to assign one below.")]
        [SerializeField] private bool autoGetNetworkManager = true;
        [Tooltip("Only used when Auto Get Network Manager is unchecked.")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private NetSessionProvider provider;
        [SerializeField, Min(1)] private int maxPlayers = 4;

        [Header("Events")]
        public UnityEvent<NetSessionState> onStateChanged;
        public UnityEvent onConnected;
        public UnityEvent<string> onDisconnected;
        public UnityEvent<string> onFailed;

        public event Action<NetSessionState> StateChanged;
        public event Action Connected;
        /// <summary>The session stopped after being connected. Parameter: reason (empty when we left ourselves).</summary>
        public event Action<string> Disconnected;
        /// <summary>Hosting or joining did not work. Parameter: reason.</summary>
        public event Action<string> Failed;

        public NetSessionState State { get; private set; }
        /// <summary>Code to share with other players. Empty until the session is hosted.</summary>
        public string JoinCode { get; private set; } = string.Empty;
        /// <summary>Last failure or disconnect reason.</summary>
        public string LastReason { get; private set; } = string.Empty;
        public bool IsHost => NetworkManager != null && NetworkManager.IsHost;

        public NetworkManager NetworkManager => autoGetNetworkManager ? NetworkManager.Singleton : networkManager;
        public NetSessionProvider Provider { get => provider; set => provider = value; }
        public int MaxPlayers { get => maxPlayers; set => maxPlayers = Mathf.Max(1, value); }

        private NetworkManager _subscribed;
        private bool _providerDone;
        private bool _leaving;

        protected virtual void Awake()
        {
            if (Instance == null) Instance = this;
        }

        protected virtual void Start() => Subscribe();

        protected virtual void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------------------------------------

        public async void Host() => await HostAsync();
        public async void Join(string code) => await JoinAsync(code);
        public async void Leave() => await LeaveAsync();

        public async Task HostAsync()
        {
            if (CanStart() == false) return;

            BeginConnecting();
            try
            {
                JoinCode = await provider.HostAsync(NetworkManager, maxPlayers) ?? string.Empty;
                EndProvider();
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
        }

        public async Task JoinAsync(string code)
        {
            if (CanStart() == false) return;

            BeginConnecting();
            try
            {
                await provider.JoinAsync(NetworkManager, code);
                JoinCode = code ?? string.Empty;
                EndProvider();
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
        }

        public async Task LeaveAsync()
        {
            if (State == NetSessionState.Offline || State == NetSessionState.Disconnecting) return;

            _leaving = true;
            SetState(NetSessionState.Disconnecting);
            try
            {
                if (provider != null) await provider.LeaveAsync(NetworkManager);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetSession] Error while leaving: {e.Message}");
            }

            // Whatever the provider did, the session must end
            if (NetworkManager != null && NetworkManager.IsListening) NetworkManager.Shutdown();
            else if (State != NetSessionState.Offline) Stop(string.Empty);
        }

        // ------------------------------------------------------------------------------------------------

        private bool CanStart()
        {
            if (State != NetSessionState.Offline) return false;

            if (provider == null)
            {
                Debug.LogError("[NetSession] No provider assigned.");
                return false;
            }

            Subscribe();
            if (NetworkManager == null)
            {
                Debug.LogError("[NetSession] No NetworkManager found.");
                return false;
            }

            return true;
        }

        private void BeginConnecting()
        {
            _providerDone = false;
            _leaving = false;
            JoinCode = string.Empty;
            LastReason = string.Empty;
            SetState(NetSessionState.Connecting);
        }

        // Connected once the provider is done (join code known) AND the local client is connected
        private void EndProvider()
        {
            if (State != NetSessionState.Connecting) return;

            _providerDone = true;
            if (NetworkManager.IsConnectedClient) SetConnected();
        }

        private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
        {
            if (data.EventType != ConnectionEvent.ClientConnected) return;
            if (data.ClientId != manager.LocalClientId) return;

            if (State == NetSessionState.Connecting && _providerDone) SetConnected();
        }

        private void OnClientStopped(bool wasHost)
        {
            string reason = _leaving ? string.Empty : NetworkManager.DisconnectReason;
            Stop(reason);
        }

        private void SetConnected()
        {
            SetState(NetSessionState.Connected);
            Connected?.Invoke();
            onConnected?.Invoke();
        }

        private void Fail(string reason)
        {
            if (State == NetSessionState.Offline) return;

            LastReason = string.IsNullOrEmpty(reason) ? "Connection failed." : reason;
            JoinCode = string.Empty;
            SetState(NetSessionState.Offline);   // before Shutdown, so OnClientStopped does nothing

            if (NetworkManager != null && NetworkManager.IsListening)
            {
                _leaving = true;
                NetworkManager.Shutdown();
            }

            if (provider != null) provider.OnStopped();
            Failed?.Invoke(LastReason);
            onFailed?.Invoke(LastReason);
        }

        private void Stop(string reason)
        {
            if (State == NetSessionState.Offline) return;

            // Stopped before being connected: it's a failure, not a disconnection
            if (State == NetSessionState.Connecting)
            {
                Fail(reason);
                return;
            }

            if (provider != null) provider.OnStopped();
            JoinCode = string.Empty;
            LastReason = reason ?? string.Empty;
            SetState(NetSessionState.Offline);
            Disconnected?.Invoke(LastReason);
            onDisconnected?.Invoke(LastReason);
        }

        private void SetState(NetSessionState state)
        {
            if (State == state) return;

            State = state;
            StateChanged?.Invoke(state);
            onStateChanged?.Invoke(state);
        }

        private void Subscribe()
        {
            NetworkManager manager = NetworkManager;
            if (manager == null || manager == _subscribed) return;

            Unsubscribe();
            _subscribed = manager;
            manager.OnConnectionEvent += OnConnectionEvent;
            manager.OnClientStopped += OnClientStopped;
        }

        private void Unsubscribe()
        {
            if (_subscribed == null) return;

            _subscribed.OnConnectionEvent -= OnConnectionEvent;
            _subscribed.OnClientStopped -= OnClientStopped;
            _subscribed = null;
        }
    }
}
