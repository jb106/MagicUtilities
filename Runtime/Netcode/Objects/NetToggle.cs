using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace MagicUtilities.Netcode
{
    /// <summary>
    /// An on/off state synced for every player: door, lever, light...
    /// Call Toggle / SetOn / SetOff from anywhere, plug what happens in the events.
    /// Works offline too (the state is just local).
    /// </summary>
    public class NetToggle : NetworkBehaviour
    {
        public enum WritePermission
        {
            Everyone,
            /// <summary>Only the owner of this object.</summary>
            Owner,
            /// <summary>Only the server/host. Clients' calls are ignored.</summary>
            Server
        }

        [SerializeField] private bool startOn;
        [SerializeField] private WritePermission whoCanChange = WritePermission.Everyone;
        [Tooltip("Raise the events with the current state when the object spawns (late join, initial visuals).")]
        [SerializeField] private bool notifyOnSpawn = true;

        [Header("Events")]
        public UnityEvent onOn;
        public UnityEvent onOff;
        public UnityEvent<bool> onChanged;

        public event Action<bool> Changed;

        public bool IsOn => IsSpawned ? _isOn.Value : _localValue;

        private readonly NetworkVariable<bool> _isOn = new();
        private bool _localValue;

        protected virtual void Awake() => _localValue = startOn;

        public override void OnNetworkSpawn()
        {
            if (IsServer) _isOn.Value = _localValue;

            _isOn.OnValueChanged += OnValueChanged;
            if (notifyOnSpawn) Notify(_isOn.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isOn.OnValueChanged -= OnValueChanged;
            _localValue = _isOn.Value;
        }

        // ------------------------------------------------------------------------------------------------

        public void Toggle() => Set(!IsOn);
        public void SetOn() => Set(true);
        public void SetOff() => Set(false);

        public void Set(bool value)
        {
            if (IsSpawned == false)
            {
                if (_localValue == value) return;
                _localValue = value;
                Notify(value);
                return;
            }

            if (IsServer) TryApply(value, NetworkManager.LocalClientId);
            else SetServerRpc(value);
        }

        /// <summary>Checked on the server before applying a change. Override for your own rule (key, distance...).</summary>
        protected virtual bool CanChange(ulong clientId, bool value)
        {
            switch (whoCanChange)
            {
                case WritePermission.Owner: return clientId == OwnerClientId;
                case WritePermission.Server: return clientId == NetworkManager.ServerClientId;
                default: return true;
            }
        }

        /// <summary>Raises the events. Override to react in code instead of (or in addition to) the inspector.</summary>
        protected virtual void Notify(bool value)
        {
            Changed?.Invoke(value);
            onChanged?.Invoke(value);

            if (value) onOn?.Invoke();
            else onOff?.Invoke();
        }

        // ------------------------------------------------------------------------------------------------

        [Rpc(SendTo.Server)]
        private void SetServerRpc(bool value, RpcParams rpcParams = default) =>
            TryApply(value, rpcParams.Receive.SenderClientId);

        private void TryApply(bool value, ulong clientId)
        {
            if (CanChange(clientId, value) == false) return;
            _isOn.Value = value;
        }

        private void OnValueChanged(bool previous, bool current)
        {
            if (previous != current) Notify(current);
        }
    }
}
