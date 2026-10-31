namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Enumerates the states of the Stage 2b handshake interaction across Batches 1, 2, and 3.
    /// </summary>
    public enum HandshakeState
    {
        None = 0,
        Reaching = 1,
        Waiting = 2,
        Grip = 3,
        Shaking = 4,
        Withdrawing = 5,
        Laugh = 6,
        Angry = 7,
        ClapLaugh = 8,
        Holding = 9,
        Catching = 10,
        Caught = 11,
        ContestTriumph = 12,
        ContestPain = 13,
        ContestTie = 14,
        EscapedStumble = 15,
        GraspEmpty = 16,
        Leap = 17
    }

    /// <summary>
    /// Enumerates the arbitrated outcome of a handshake session across Batches 1, 2, and 3.
    /// </summary>
    public enum HandshakeOutcome
    {
        None = 0,
        Cancelled = 1,
        Success = 2,
        Prank = 3,
        BothWithdraw = 4,
        StrengthContestWin = 5,
        StrengthContestTie = 6,
        StrengthContestZeroZero = 7,
        Caught = 8,
        Escaped = 9
    }
}
