using System;
using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Represents a unified action intent snapshot decoupled from input hardware, AI logic, or network transport.
    /// </summary>
    [Serializable]
    public struct CombatActionIntent
    {
        public Vector2 MoveInput;
        public bool Jump;
        public bool CrouchHold;
        public bool CrouchToggle;
        public bool Attack;
        public bool GuardHold;
        public bool ParryTap;
        public bool InteractHold;
        public bool InteractDown;
        public bool InteractUp;
        public bool GlobalReset;
        public float Timestamp;

        public static CombatActionIntent Empty => new CombatActionIntent
        {
            MoveInput = Vector2.zero,
            Jump = false,
            CrouchHold = false,
            CrouchToggle = false,
            Attack = false,
            GuardHold = false,
            ParryTap = false,
            InteractHold = false,
            InteractDown = false,
            InteractUp = false,
            GlobalReset = false,
            Timestamp = 0f
        };
    }

    /// <summary>
    /// Defines a provider of per-frame action intents for a <see cref="CombatEntity"/>.
    /// </summary>
    public interface ICombatInputSource
    {
        CombatActionIntent PollIntent(CombatEntity entity);
    }
}
