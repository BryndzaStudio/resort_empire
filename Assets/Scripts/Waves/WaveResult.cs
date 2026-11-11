using System;

// BRYN-52 / BRYN-54: Výsledok jednej vlny. WaveManager ho vytvorí pri skončení vlny
// a pošle ho v udalosti WaveEvents.WaveFinished (napr. obrazovke s výsledkom).
[Serializable]
public class WaveResult
{
    public int guestsArrived;       // koľko hostí vlna priviezla na ostrov
    public int coinsEarned;         // o koľko mincí má hráč viac ako na začiatku vlny
    public float durationSeconds;   // ako dlho vlna trvala (od kliknutia na Start Wave)
    public float finishedAtRealtime; // Time.realtimeSinceStartup v momente skončenia (meranie BRYN-93)
}
