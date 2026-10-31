using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Image feed;
    [SerializeField] private Room startRoom;

    public Room CurrentRoom { get; private set; }

    private void Start()
    {
        if (startRoom != null) ViewRoom(startRoom);
    }

    public void ViewRoom(Room room)
    {
        if (CurrentRoom != null) CurrentRoom.Changed -= Refresh;

        CurrentRoom = room;
        CurrentRoom.Changed += Refresh;
        Refresh();
    }

    private void Refresh() => feed.sprite = CurrentRoom.GetCurrentSprite();

    private void OnDestroy()
    {
        if (CurrentRoom != null) CurrentRoom.Changed -= Refresh;
    }
}
