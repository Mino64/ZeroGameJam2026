using UnityEngine;

public class EventStarter : MonoBehaviour
{
    [SerializeField] Event EventToHappen;
    void Start()
    {
        EventToHappen.DoSomething();
    }
}
