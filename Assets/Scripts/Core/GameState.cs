
using System;

[Serializable]
public class GameState
{
    public int coins;
    public int energy;
    public int resortValue;

    public GameState(int startingCoins, int startingEnergy, int startingResortValue)
    {
        coins = startingCoins;
        energy = startingEnergy;
        resortValue = startingResortValue;
    }
}
