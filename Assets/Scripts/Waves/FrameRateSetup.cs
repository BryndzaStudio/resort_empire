using UnityEngine;

// BRYN-94: Android may default to 30 FPS, which would make the 100 ms limit
// for the wave start only 3 frames. Runs before the first scene loads.
public static class FrameRateSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        Application.targetFrameRate = 60;
    }
}
