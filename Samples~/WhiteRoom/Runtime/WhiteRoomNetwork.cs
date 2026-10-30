using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Represents a sequenced action intent packet in the local network simulation queue.
    /// </summary>
    [Serializable]
    public struct CombatNetworkMessage
    {
        public uint SequenceId;
        public string SenderId;
        public float Timestamp;
        public CombatActionIntent Intent;

        public override string ToString()
        {
            return $"Seq#{SequenceId} [{SenderId}] At {Timestamp:F2}s (Atk:{Intent.Attack}, Guard:{Intent.GuardHold}, Move:{Intent.MoveInput})";
        }
    }

    /// <summary>
    /// In-process memory queue simulating latency, jitter, and packet loss for diagnostic experiments.
    /// Does not provide cross-process transport or remote host arbitration.
    /// </summary>
    public class SimulatedNetworkChannel
    {
        private class QueuedPacket
        {
            public CombatNetworkMessage Message;
            public float DeliverTime;
        }

        public float LatencyMs = 50f;
        public float JitterMs = 10f;
        [Range(0f, 1f)]
        public float PacketLossRate = 0f;

        private readonly List<QueuedPacket> _packetQueue = new List<QueuedPacket>();
        private uint _nextSeq = 1;
        private uint _droppedCount = 0;
        private uint _sentCount = 0;

        public uint TotalSent => _sentCount;
        public uint TotalDropped => _droppedCount;

        public event Action<CombatNetworkMessage> OnPacketDelivered;
        public event Action<CombatNetworkMessage> OnPacketDropped;

        /// <summary>
        /// Enqueues a packet with simulated latency/jitter or drops it according to the configured loss rate.
        /// </summary>
        public void SendPacket(string senderId, CombatActionIntent intent)
        {
            _sentCount++;

            if (PacketLossRate > 0.001f && UnityEngine.Random.value < PacketLossRate)
            {
                _droppedCount++;
                var droppedMsg = new CombatNetworkMessage
                {
                    SequenceId = _nextSeq++,
                    SenderId = senderId,
                    Timestamp = Time.time,
                    Intent = intent
                };
                OnPacketDropped?.Invoke(droppedMsg);
                return;
            }

            float jitterOffset = UnityEngine.Random.Range(-JitterMs, JitterMs);
            float totalDelay = Mathf.Max(0f, (LatencyMs + jitterOffset) / 1000f);

            var packet = new QueuedPacket
            {
                Message = new CombatNetworkMessage
                {
                    SequenceId = _nextSeq++,
                    SenderId = senderId,
                    Timestamp = Time.time,
                    Intent = intent
                },
                DeliverTime = Time.time + totalDelay
            };

            _packetQueue.Add(packet);
        }

        /// <summary>
        /// Delivers any queued packets whose simulated delivery timestamp has been reached.
        /// </summary>
        public void UpdateQueue()
        {
            float now = Time.time;
            for (int i = _packetQueue.Count - 1; i >= 0; i--)
            {
                if (now >= _packetQueue[i].DeliverTime)
                {
                    var msg = _packetQueue[i].Message;
                    _packetQueue.RemoveAt(i);
                    OnPacketDelivered?.Invoke(msg);
                }
            }
        }

        /// <summary>
        /// Clears all pending packets and resets sequence and drop counters.
        /// </summary>
        public void ResetStats()
        {
            _packetQueue.Clear();
            _nextSeq = 1;
            _sentCount = 0;
            _droppedCount = 0;
        }
    }
}
