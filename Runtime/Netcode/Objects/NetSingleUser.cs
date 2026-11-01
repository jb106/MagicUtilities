using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace MagicUtilities.Netcode
{
    /// <summary>
    /// Something only one player can use at a time: chest, terminal, workbench, seat...
    /// TryUse asks the server, which grants it to one player only. The user is released when they
    /// call Release or disconnect. Works offline too.
    /// </summary>
    public class NetSingleUser : NetworkBehaviour
    {
        public const ulong NoUser = ulong.MaxValue;

        [Tooltip("Raise OnInUse / OnFree with the current state when the object spawns (late join, initial visuals).")]
        [SerializeField] private bool notifyOnSpawn = true;

        [Header("For the player who asked")]
        public UnityEvent onUseGranted;
        public UnityEvent onUseDenied;
        [Tooltip("This player stopped using it (released, or released by the server).")]
        public UnityEvent onUseEnded;

        [Header("For every player")]
        public UnityEvent onInUse;
        public UnityEvent onFree;

        public event Action UseGranted;
        public event Action UseDenied;
        public event Action UseEnded;
        /// <summary>The user changed. Parameter: new user, or NoUser.</summary>
        public event Action<ulong> UserChanged;

        public ulong CurrentUser => IsSpawned ? _user.Value : _localUser;
        public bool IsInUse => CurrentUser != NoUser;
        public bool IsUsedByMe => IsInUse && CurrentUser == LocalId;

        private readonly NetworkVariable<ulong> _user = new(NoUser);
        private ulong _localUser = NoUser;

        private ulong LocalId => IsSpawned ? NetworkManager.LocalClientId : 0;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _user.Value = NoUser;
                NetworkManager.OnConnectionEvent += OnConnectionEvent;
            }

            _user.OnValueChanged += OnUserChanged;
            if (notifyOnSpawn) NotifyState(_user.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) NetworkManager.OnConnectionEvent -= OnConnectionEvent;
            _user.OnValueChanged -= OnUserChanged;

            if (IsUsedByMe) NotifyEnded();
            _localUser = NoUser;
        }

        // ------------------------------------------------------------------------------------------------

        /// <summary>Asks to use it. OnUseGranted or OnUseDenied is raised on this player with the answer.</summary>
        public void TryUse()
        {
            if (IsSpawned == false)
            {
                bool granted = (_localUser == NoUser || _localUser == 0) && CanUse(0);
                if (granted && _localUser == NoUser) SetLocalUser(0);
                NotifyAnswer(granted);
                return;
            }

            if (IsServer) NotifyAnswer(ServerTryUse(NetworkManager.LocalClientId));
            else UseServerRpc();
        }

        /// <summary>Stops using it. Does nothing if this player is not the user.</summary>
        public void Release()
        {
            if (IsUsedByMe == false) return;

            if (IsSpawned == false) SetLocalUser(NoUser);
            else if (IsServer) ServerRelease(NetworkManager.LocalClientId);
            else ReleaseServerRpc();
        }

        /// <summary>Server only: frees it whoever is using it (end of round, object destroyed...).</summary>
        public void ForceRelease()
        {
            if (IsSpawned == false) SetLocalUser(NoUser);
            else if (IsServer) _user.Value = NoUser;
        }

        /// <summary>
        /// Checked on the server when a player asks to use it, after checking nobody else is using it.
        /// Override for your own rule (distance, key, fainted player...).
        /// </summary>
        protected virtual bool CanUse(ulong clientId) => true;

        // ------------------------------------------------------------------------------------------------

        private bool ServerTryUse(ulong clientId)
        {
            ulong user = _user.Value;
            if (user == clientId) return true;   // already the user
            if (user != NoUser) return false;
            if (CanUse(clientId) == false) return false;

            _user.Value = clientId;
            return true;
        }

        private void ServerRelease(ulong clientId)
        {
            if (_user.Value == clientId) _user.Value = NoUser;
        }

        private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
        {
            if (data.EventType == ConnectionEvent.ClientDisconnected) ServerRelease(data.ClientId);
        }

        [Rpc(SendTo.Server)]
        private void UseServerRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            bool granted = ServerTryUse(sender);
            AnswerRpc(granted, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        private void ReleaseServerRpc(RpcParams rpcParams = default) =>
            ServerRelease(rpcParams.Receive.SenderClientId);

        [Rpc(SendTo.SpecifiedInParams)]
        private void AnswerRpc(bool granted, RpcParams rpcParams) => NotifyAnswer(granted);

        // ------------------------------------------------------------------------------------------------

        private void SetLocalUser(ulong user)
        {
            ulong previous = _localUser;
            if (previous == user) return;

            _localUser = user;
            OnUserChanged(previous, user);
        }

        private void OnUserChanged(ulong previous, ulong current)
        {
            if (previous == current) return;

            if (previous == LocalId && previous != NoUser) NotifyEnded();
            UserChanged?.Invoke(current);
            NotifyState(current);
        }

        /// <summary>Raises OnInUse / OnFree. Override to react in code.</summary>
        protected virtual void NotifyState(ulong user)
        {
            if (user != NoUser) onInUse?.Invoke();
            else onFree?.Invoke();
        }

        /// <summary>Raises OnUseGranted / OnUseDenied on the player who asked. Override to react in code.</summary>
        protected virtual void NotifyAnswer(bool granted)
        {
            if (granted)
            {
                UseGranted?.Invoke();
                onUseGranted?.Invoke();
            }
            else
            {
                UseDenied?.Invoke();
                onUseDenied?.Invoke();
            }
        }

        protected virtual void NotifyEnded()
        {
            UseEnded?.Invoke();
            onUseEnded?.Invoke();
        }
    }
}
