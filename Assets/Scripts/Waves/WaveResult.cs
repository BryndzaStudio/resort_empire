
using System;
using System.Collections.Generic;

// BRYN-52/54: Results of a completed wave.
// BRYN-80/81: Guest satisfaction and star rating.
// BRYN-83/84: Individual building performance.
// BRYN-69: Calculated coin rewards.
[Serializable]
public class WaveResult
{
    // Basic wave information
    public int guestsArrived;
    public int coinsEarned;
    public float durationSeconds;
    public float finishedAtRealtime;

    // BRYN-80/81: Satisfaction and rating
    public int guestsRated;
    public float satisfaction;
    public int stars;

    // BRYN-83/84: Building performance
    public List<BuildingWaveResult> buildings =
        new List<BuildingWaveResult>();

    // BRYN-69: Coin reward calculation
    public int guestsServed;
    public int calculatedCoinReward;
    // BRYN-71: Energy actually awarded after the wave.
    public int energyEarned;
}

// BRYN-83/84: Results for an individual building.
[Serializable]
public class BuildingWaveResult
{
    public BuildingType type;
    public int guests;
    public float averageWaitSeconds;
    public bool isBottleneck;
}
