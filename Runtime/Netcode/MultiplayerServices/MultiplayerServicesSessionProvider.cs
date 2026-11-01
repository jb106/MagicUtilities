using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace MagicUtilities.Netcode
{
    /// <summary>
    /// Online session through Unity Multiplayer Services (Relay): no port to open, players join with a short code.
    /// Requires the com.unity.services.multiplayer package and the project linked to Unity Cloud.
    /// Players are signed in anonymously.
    /// </summary>
    [CreateAssetMenu(menuName = "MagicUtilities/Netcode/Multiplayer Services Session Provider")]
    public class MultiplayerServicesSessionProvider : NetSessionProvider
    {
        [Tooltip("Sessions are private: players join with the code only.")]
        public bool isPrivate = true;

        [Tooltip("In the editor, sign in with a new anonymous player each run. Needed to test with several " +
                 "editor instances (Multiplayer Play Mode): otherwise they all share the same player.")]
        public bool uniquePlayerInEditor = true;

        [NonSerialized] private ISession _session;

        public ISession Session => _session;

        public override async Task<string> HostAsync(NetworkManager networkManager, int maxPlayers)
        {
            await SignInAsync();

            SessionOptions options = new SessionOptions
            {
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate
            }.WithRelayNetwork();

            _session = await MultiplayerService.Instance.CreateSessionAsync(options);
            return _session.Code;
        }

        public override async Task JoinAsync(NetworkManager networkManager, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new Exception("Enter a join code.");

            await SignInAsync();
            _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim());
        }

        public override async Task LeaveAsync(NetworkManager networkManager)
        {
            ISession session = _session;
            _session = null;

            if (session != null) await session.LeaveAsync();
            await base.LeaveAsync(networkManager);
        }

        public override void OnStopped()
        {
            // Stopped without LeaveAsync (host gone, kicked): leave the service session too
            ISession session = _session;
            _session = null;
            if (session != null) _ = LeaveQuietly(session);
        }

        protected virtual async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                InitializationOptions options = new InitializationOptions();
#if UNITY_EDITOR
                if (uniquePlayerInEditor) options.SetProfile("editor_" + Guid.NewGuid().ToString("N").Substring(0, 8));
#endif
                await UnityServices.InitializeAsync(options);
            }

            if (AuthenticationService.Instance.IsSignedIn == false)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private static async Task LeaveQuietly(ISession session)
        {
            try
            {
                await session.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MultiplayerServicesSessionProvider] Could not leave the session: {e.Message}");
            }
        }
    }
}
