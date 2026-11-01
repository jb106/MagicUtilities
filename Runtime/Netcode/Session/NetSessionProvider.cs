using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace MagicUtilities.Netcode
{
    /// <summary>
    /// How a session is created and joined (direct IP, Multiplayer Services, Steam...).
    /// Create one as an asset and assign it on the NetSession. Inherit from it to add your own backend.
    /// </summary>
    public abstract class NetSessionProvider : ScriptableObject
    {
        /// <summary>Starts the NetworkManager as host. Returns the code other players use to join (can be empty).</summary>
        public abstract Task<string> HostAsync(NetworkManager networkManager, int maxPlayers);

        /// <summary>Starts the NetworkManager as client, using the code given by the host.</summary>
        public abstract Task JoinAsync(NetworkManager networkManager, string code);

        /// <summary>Leaves the session. The default only shuts the NetworkManager down.</summary>
        public virtual Task LeaveAsync(NetworkManager networkManager)
        {
            if (networkManager.IsListening) networkManager.Shutdown();
            return Task.CompletedTask;
        }

        /// <summary>Called when the NetworkManager stopped for any reason (left, host gone, kicked).</summary>
        public virtual void OnStopped() { }
    }
}
