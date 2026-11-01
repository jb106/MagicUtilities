using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace MagicUtilities.Netcode
{
    /// <summary>
    /// Spawns one player prefab per connected client, on spawn points. Runs on the server only.
    /// Leave the NetworkManager's own Player Prefab empty, otherwise players are spawned twice.
    /// </summary>
    public class NetPlayerSpawner : MonoBehaviour
    {
        public enum SpawnMode
        {
            /// <summary>First spawn point nobody is using.</summary>
            FirstFree,
            /// <summary>Each new player takes the next spawn point.</summary>
            RoundRobin,
            Random
        }

        [Tooltip("Use NetworkManager.Singleton. Uncheck to assign one below.")]
        [SerializeField] private bool autoGetNetworkManager = true;
        [Tooltip("Only used when Auto Get Network Manager is unchecked.")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private List<Transform> spawnPoints = new();
        [SerializeField] private SpawnMode mode = SpawnMode.FirstFree;

        [Header("Events (server only)")]
        public UnityEvent<ulong, NetworkObject> onPlayerSpawned;

        public event Action<ulong, NetworkObject> PlayerSpawned;

        public NetworkManager NetworkManager => autoGetNetworkManager ? NetworkManager.Singleton : networkManager;
        public IReadOnlyList<Transform> SpawnPoints => spawnPoints;

        /// <summary>Spawn point index used by each client.</summary>
        protected readonly Dictionary<ulong, int> UsedSpawns = new();

        private NetworkManager _subscribed;
        private int _nextIndex;

        protected virtual void Start()
        {
            NetworkManager manager = NetworkManager;
            if (manager == null)
            {
                Debug.LogError("[NetPlayerSpawner] No NetworkManager found.");
                return;
            }

            if (manager.NetworkConfig.PlayerPrefab != null)
                Debug.LogWarning("[NetPlayerSpawner] The NetworkManager also has a Player Prefab: players will be spawned twice.");

            _subscribed = manager;
            manager.OnConnectionEvent += OnConnectionEvent;
            manager.OnServerStopped += OnServerStopped;
        }

        protected virtual void OnDestroy()
        {
            if (_subscribed == null) return;

            _subscribed.OnConnectionEvent -= OnConnectionEvent;
            _subscribed.OnServerStopped -= OnServerStopped;
        }

        private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
        {
            if (manager.IsServer == false) return;

            if (data.EventType == ConnectionEvent.ClientConnected) SpawnPlayer(data.ClientId);
            else if (data.EventType == ConnectionEvent.ClientDisconnected) UsedSpawns.Remove(data.ClientId);
        }

        private void OnServerStopped(bool wasHost)
        {
            UsedSpawns.Clear();
            _nextIndex = 0;
        }

        // ------------------------------------------------------------------------------------------------

        /// <summary>Spawns the player of a client. Called automatically when a client connects.</summary>
        public virtual NetworkObject SpawnPlayer(ulong clientId)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[NetPlayerSpawner] No player prefab assigned.");
                return null;
            }

            int index = ChooseSpawnIndex(clientId);
            Transform point = index >= 0 && index < spawnPoints.Count ? spawnPoints[index] : null;
            if (point != null) UsedSpawns[clientId] = index;

            Vector3 position = point != null ? point.position : Vector3.zero;
            Quaternion rotation = point != null ? point.rotation : Quaternion.identity;

            NetworkObject player = Instantiate(playerPrefab, position, rotation);
            player.SpawnAsPlayerObject(clientId);

            PlayerSpawned?.Invoke(clientId, player);
            onPlayerSpawned?.Invoke(clientId, player);
            return player;
        }

        /// <summary>Index of the spawn point for this client, or -1 for none. Override for your own rule.</summary>
        protected virtual int ChooseSpawnIndex(ulong clientId)
        {
            if (spawnPoints.Count == 0) return -1;

            switch (mode)
            {
                case SpawnMode.RoundRobin:
                    return _nextIndex++ % spawnPoints.Count;

                case SpawnMode.Random:
                    return UnityEngine.Random.Range(0, spawnPoints.Count);

                default:
                    for (int i = 0; i < spawnPoints.Count; i++)
                        if (UsedSpawns.ContainsValue(i) == false) return i;
                    return _nextIndex++ % spawnPoints.Count;   // all used: share them
            }
        }
    }
}
