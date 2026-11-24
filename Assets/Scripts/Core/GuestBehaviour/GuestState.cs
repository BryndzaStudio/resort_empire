
using System;

[Serializable]
public class GuestState
{
    public int id;
    public GuestNeed need;

    // BRYN-81: kedy hosť došiel na ostrov (koniec móla), Time.time. -1 = ešte nedošiel.
    public float arrivedAt = -1f;

    public GuestState(int id, GuestNeed need)
    {
        this.id = id;
        this.need = need;
    }
}
