using System;
using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Trigger collider bridge attached to a participant's right hand to report exact hand-to-hand physical contact.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HandContactBridge : MonoBehaviour
    {
        public HandshakeParticipant Owner { get; private set; }
        public Collider HandCollider { get; private set; }

        public event Action<HandContactBridge, HandContactBridge> OnTriggerContact;

        /// <summary>
        /// Configures this contact bridge with its owning participant and ensures a kinematic Rigidbody is present.
        /// </summary>
        public void Initialize(HandshakeParticipant owner)
        {
            Owner = owner;
            HandCollider = GetComponent<Collider>();
            if (HandCollider != null)
            {
                HandCollider.isTrigger = true;
            }

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var otherBridge = other.GetComponent<HandContactBridge>();
            if (otherBridge != null && otherBridge.Owner != null && Owner != null && otherBridge.Owner != Owner)
            {
                OnTriggerContact?.Invoke(this, otherBridge);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var otherBridge = other.GetComponent<HandContactBridge>();
            if (otherBridge != null && otherBridge.Owner != null && Owner != null && otherBridge.Owner != Owner)
            {
                OnTriggerContact?.Invoke(this, otherBridge);
            }
        }

        private void FixedUpdate()
        {
            if (HandCollider is SphereCollider sphere)
            {
                Collider[] overlaps = Physics.OverlapSphere(transform.position, sphere.radius);
                for (int i = 0; i < overlaps.Length; i++)
                {
                    Collider col = overlaps[i];
                    if (col == HandCollider) continue;

                    var otherBridge = col.GetComponent<HandContactBridge>();
                    if (otherBridge != null && otherBridge.Owner != null && Owner != null && otherBridge.Owner != Owner)
                    {
                        OnTriggerContact?.Invoke(this, otherBridge);
                        break;
                    }
                }
            }
        }
    }
}
