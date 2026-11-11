
using System;
using UnityEngine;

public class GuestStatistics : MonoBehaviour
{
    public static GuestStatistics Instance { get; private set; }

    public int ServedGuests { get; private set; }
    public int GuestsInQueue { get; private set; }

    public event Action OnStatisticsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void GuestServed()
    {
        ServedGuests++;
        OnStatisticsChanged?.Invoke();
    }

    public void GuestJoinedQueue()
    {
        GuestsInQueue++;
        OnStatisticsChanged?.Invoke();
    }

    public void GuestLeftQueue()
    {
        GuestsInQueue = Mathf.Max(0, GuestsInQueue - 1);
        OnStatisticsChanged?.Invoke();
    }

    public void SetQueueCount(int count)
    {
        count = Mathf.Max(0, count);
        if (GuestsInQueue == count) return;
        GuestsInQueue = count;
        OnStatisticsChanged?.Invoke();
    }

    public void ResetStatistics()
    {
        ServedGuests = 0;
        GuestsInQueue = 0;
        OnStatisticsChanged?.Invoke();
    }
}
