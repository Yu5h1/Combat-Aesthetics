namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Enumerates the states of the Stage 2b handshake interaction.
    /// </summary>
    public enum HandshakeState
    {
        None = 0,
        Reaching = 1,
        Waiting = 2,
        Grip = 3,
        Shaking = 4,
        Withdrawing = 5
    }

    /// <summary>
    /// Enumerates the arbitrated outcome of a handshake session.
    /// </summary>
    public enum HandshakeOutcome
    {
        None = 0,
        Cancelled = 1,
        Success = 2,
        EarlyReleaseDuringGrip = 3
    }
}
