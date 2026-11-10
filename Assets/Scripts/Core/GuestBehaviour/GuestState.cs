
using System;

[Serializable]
public class GuestState
{
    public int id;
    public GuestNeed need;

    public GuestState(int id, GuestNeed need)
    {
        this.id = id;
        this.need = need;
    }
}
