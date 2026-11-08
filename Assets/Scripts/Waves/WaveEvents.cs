using System;
using UnityEngine;

// BRYN-48: static hub for wave events. The Start Wave button raises
// StartWaveRequested; the wave logic (BRYN-49) subscribes to it.
// BRYN-52: WaveManager raises WaveFinished when the wave ends.
public static class WaveEvents
{
    public static event Action StartWaveRequested;

    // BRYN-52: vlna skončila (hostia už nepribúdajú a všetky rady sú prázdne).
    public static event Action<WaveResult> WaveFinished;

    // Static events survive between Play sessions when Reload Domain is off.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        StartWaveRequested = null;
        WaveFinished = null;
    }

    public static void RaiseStartWaveRequested()
    {
        StartWaveRequested?.Invoke();
    }

    public static void RaiseWaveFinished(WaveResult result)
    {
        WaveFinished?.Invoke(result);
    }
}
