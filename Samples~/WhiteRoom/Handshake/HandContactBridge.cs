using System;
using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Trigger collider bridge attached to a participant's right hand to report exact hand-to-hand physical contact.
    /// Hand sphere radius respects natural anatomical size configured in scene/prefab.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HandContactBridge : MonoBehaviour
    {
        public HandshakeParticipant Owner { get; private set; }
        public Collider HandCollider { get; private set; }

        public event Action<HandContactBridge, HandContactBridge> OnTriggerContact;

        private void Awake()
        {
            if (HandCollider == null)
            {
                HandCollider = GetComponent<Collider>();
            }
            if (Owner == null)
            {
                Owner = GetComponentInParent<HandshakeParticipant>();
            }
        }

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

        private void Update()
        {
            CheckContactOverlaps();
        }

        private void FixedUpdate()
        {
            CheckContactOverlaps();
        }

        private void CheckContactOverlaps()
        {
            if (HandCollider == null)
            {
                HandCollider = GetComponent<Collider>();
            }
            if (HandCollider is SphereCollider sphere)
            {
                float effectiveRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
                Collider[] hits = Physics.OverlapSphere(transform.position, effectiveRadius, Physics.AllLayers, QueryTriggerInteraction.Collide);
                foreach (var hit in hits)
                {
                    if (hit == HandCollider) continue;
                    var otherBridge = hit.GetComponent<HandContactBridge>();
                    if (otherBridge != null && otherBridge.Owner != null && Owner != null && otherBridge.Owner != Owner)
                    {
                        OnTriggerContact?.Invoke(this, otherBridge);
                    }
                }
            }
        }
    }
}
