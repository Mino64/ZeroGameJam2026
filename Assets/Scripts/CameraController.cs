using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Image feed;
    [SerializeField] private Room startRoom;

    [Header("Som")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchClip;

    public Room CurrentRoom { get; private set; }

    public event System.Action<Room> RoomViewed;

    private void Start()
    {
        if (startRoom != null) ViewRoom(startRoom);
    }

    public void ViewRoom(Room room)
    {
        bool switched = CurrentRoom != null && room != CurrentRoom;

        if (CurrentRoom != null) CurrentRoom.Changed -= Refresh;

        CurrentRoom = room;
        CurrentRoom.Changed += Refresh;
        Refresh();
        RoomViewed?.Invoke(room);

        if (switched && audioSource != null && switchClip != null)
            audioSource.PlayOneShot(switchClip);
    }

    private void Refresh() => feed.sprite = CurrentRoom.GetCurrentSprite();

    private void OnDestroy()
    {
        if (CurrentRoom != null) CurrentRoom.Changed -= Refresh;
    }
}