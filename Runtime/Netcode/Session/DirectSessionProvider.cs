using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MagicUtilities.Netcode
{
    /// <summary>
    /// Direct connection with an IP address and a port, without any online service.
    /// For local tests (Multiplayer Play Mode, LAN). The join code is "address:port".
    /// </summary>
    [CreateAssetMenu(menuName = "MagicUtilities/Netcode/Direct Session Provider")]
    public class DirectSessionProvider : NetSessionProvider
    {
        public string address = "127.0.0.1";
        public ushort port = 7777;

        public override Task<string> HostAsync(NetworkManager networkManager, int maxPlayers)
        {
            GetTransport(networkManager).SetConnectionData(address, port, "0.0.0.0");

            if (networkManager.StartHost() == false) throw new Exception("Could not start the host.");
            return Task.FromResult($"{address}:{port}");
        }

        public override Task JoinAsync(NetworkManager networkManager, string code)
        {
            string joinAddress = address;
            ushort joinPort = port;

            // An empty code uses the address and port of this asset
            if (string.IsNullOrWhiteSpace(code) == false)
            {
                string[] parts = code.Trim().Split(':');
                joinAddress = parts[0];
                if (parts.Length > 1 && ushort.TryParse(parts[1], out ushort parsedPort)) joinPort = parsedPort;
            }

            GetTransport(networkManager).SetConnectionData(joinAddress, joinPort);

            if (networkManager.StartClient() == false) throw new Exception("Could not start the client.");
            return Task.CompletedTask;
        }

        private static UnityTransport GetTransport(NetworkManager networkManager)
        {
            if (networkManager.NetworkConfig.NetworkTransport is UnityTransport transport) return transport;
            throw new Exception("The NetworkManager does not use a UnityTransport.");
        }
    }
}
