
using System;

// BRYN-52 / BRYN-54: Results of a completed wave.
// BRYN-69: Includes calculated rating and coin rewards.
[Serializable]
public class WaveResult
{
    public int guestsArrived;
    public int guestsServed;

    public int ratingStars;
    public int calculatedCoinReward;

    // Existing field: change in balance during the wave.
    public int coinsEarned;

    public float durationSeconds;
    public float finishedAtRealtime;
}
