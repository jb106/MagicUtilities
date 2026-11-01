using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace MagicUtilities.Netcode
{
    /// <summary>
    /// Enables things only on the machine that owns this object (input, camera, audio listener, HUD),
    /// or only on the other machines (nameplate, full body model...).
    /// Everything listed is disabled until the object is spawned, so nothing runs before ownership is known.
    /// </summary>
    public class NetOwnerOnly : NetworkBehaviour
    {
        [Header("Only for the owner")]
        [SerializeField] private List<Behaviour> ownerComponents = new();
        [SerializeField] private List<GameObject> ownerObjects = new();

        [Header("Only for the other players")]
        [SerializeField] private List<Behaviour> remoteComponents = new();
        [SerializeField] private List<GameObject> remoteObjects = new();

        [Header("Events")]
        public UnityEvent onBecameOwner;
        public UnityEvent onBecameRemote;

        protected virtual void Awake() => SetAll(false, false);

        public override void OnNetworkSpawn() => Apply(IsOwner);
        public override void OnGainedOwnership() => Apply(true);
        public override void OnLostOwnership() => Apply(false);

        /// <summary>Called on spawn and when ownership changes. Override to add your own setup.</summary>
        protected virtual void Apply(bool isOwner)
        {
            SetAll(isOwner, isOwner == false);

            if (isOwner) onBecameOwner?.Invoke();
            else onBecameRemote?.Invoke();
        }

        private void SetAll(bool owner, bool remote)
        {
            foreach (Behaviour component in ownerComponents) if (component != null) component.enabled = owner;
            foreach (GameObject go in ownerObjects) if (go != null) go.SetActive(owner);
            foreach (Behaviour component in remoteComponents) if (component != null) component.enabled = remote;
            foreach (GameObject go in remoteObjects) if (go != null) go.SetActive(remote);
        }
    }
}
