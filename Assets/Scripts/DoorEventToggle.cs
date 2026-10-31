using UnityEngine;

public class DoorEventToggle : Event
{
    [SerializeField] Door door;
    public override void DoSomething()
    {
        door.Closed = !door.Closed;
    }
}