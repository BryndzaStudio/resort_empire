
using TMPro;
using UnityEngine;

public class GuestStatisticsPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text servedGuestsText;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text queueText;

    private int lastServed = -1;
    private int lastCapacity = -1;
    private int lastQueue = -1;

    private void Update()
    {
        GuestStatistics statistics = GuestStatistics.Instance;

        int served = statistics != null ? statistics.ServedGuests : 0;
        int queue = statistics != null ? statistics.GuestsInQueue : 0;
        int capacity = GetTotalCapacity();

        if (served != lastServed)
        {
            lastServed = served;
            if (servedGuestsText != null)
                servedGuestsText.text = served.ToString();
        }

        if (capacity != lastCapacity)
        {
            lastCapacity = capacity;
            if (capacityText != null)
                capacityText.text = capacity.ToString();
        }

        if (queue != lastQueue)
        {
            lastQueue = queue;
            if (queueText != null)
                queueText.text = queue.ToString();
        }
    }

    private int GetTotalCapacity()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState == null ||
            GameManager.Instance.CurrentState.buildings == null)
        {
            return 0;
        }

        int total = 0;

        foreach (BuildingState building in
                 GameManager.Instance.CurrentState.buildings)
        {
            if (building != null && building.isActive)
            {
                total += building.capacity;
            }
        }

        return total;
    }
}
