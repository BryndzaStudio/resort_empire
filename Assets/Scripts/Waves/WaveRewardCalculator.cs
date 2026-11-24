
using UnityEngine;

// BRYN-69: Calculates wave ratings and coin rewards.
// Does not modify the player's coin balance.
public static class WaveRewardCalculator
{
    private const int CoinsPerGuest = 10;

    private static readonly float[] RatingMultipliers =
    {
        0f,   // 0 stars
        1.0f, // 1 star
        1.2f, // 2 stars
        1.5f, // 3 stars
        1.8f, // 4 stars
        2.0f  // 5 stars
    };

    public static int CalculateRating(int servedGuests, int totalGuests)
    {
        if (servedGuests <= 0 || totalGuests <= 0)
            return 0;

        float servedRatio = Mathf.Clamp01(
            (float)servedGuests / totalGuests
        );

        return Mathf.Clamp(
            Mathf.CeilToInt(servedRatio * 5f),
            1,
            5
        );
    }

    public static int CalculateCoins(int servedGuests, int rating)
    {
        if (servedGuests <= 0 || rating <= 0)
            return 0;

        rating = Mathf.Clamp(rating, 1, 5);

        float multiplier = RatingMultipliers[rating];

        return Mathf.RoundToInt(
            servedGuests * CoinsPerGuest * multiplier
        );
    }
}
