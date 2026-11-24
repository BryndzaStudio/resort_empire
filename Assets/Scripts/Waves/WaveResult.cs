using System;
using System.Collections.Generic;

// BRYN-52 / BRYN-54: Výsledok jednej vlny. WaveManager ho vytvorí pri skončení vlny
// a pošle ho v udalosti WaveEvents.WaveFinished (napr. obrazovke s výsledkom).
// BRYN-80/81: spokojnosť hostí a hodnotenie v hviezdičkách. BRYN-83/84: výsledok jednotlivých budov.
[Serializable]
public class WaveResult
{
    public int guestsArrived;       // koľko hostí vlna priviezla na ostrov
    public int coinsEarned;         // o koľko mincí má hráč viac ako na začiatku vlny
    public float durationSeconds;   // ako dlho vlna trvala (od kliknutia na Start Wave)
    public float finishedAtRealtime; // Time.realtimeSinceStartup v momente skončenia (meranie BRYN-93)

    public int guestsRated;         // BRYN-81: koľko hostí sa započítalo do spokojnosti (začala sa ich obsluha)
    public float satisfaction;      // BRYN-81: priemerná spokojnosť hostí v % (0 – 100)
    public int stars;               // BRYN-80: hodnotenie vlny 1 – 5 hviezdičiek

    // BRYN-83/84: aktívne budovy a ako sa im vo vlne darilo.
    public List<BuildingWaveResult> buildings = new List<BuildingWaveResult>();
}

// BRYN-83/84: výsledok jednej budovy vo vlne.
[Serializable]
public class BuildingWaveResult
{
    public BuildingType type;
    public int guests;              // koľko hostí v nej začalo obsluhu
    public float averageWaitSeconds; // priemerné čakanie jej hostí (príchod -> začiatok obsluhy)
    public bool isBottleneck;       // hostia v nej čakali priemerne dlhšie ako GameConfig.bottleneckWaitSeconds
}
