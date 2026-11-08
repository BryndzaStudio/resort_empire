using System;

// BRYN-48: static hub for wave events. The Start Wave button raises
// StartWaveRequested; the wave logic (BRYN-49) subscribes to it.
public static class WaveEvents
{
    public static event Action StartWaveRequested;

    public static void RaiseStartWaveRequested()
    {
        StartWaveRequested?.Invoke();
    }
}
