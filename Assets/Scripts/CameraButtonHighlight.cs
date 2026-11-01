using UnityEngine;
using UnityEngine.UI;

public class CameraButtonHighlight : MonoBehaviour
{
    [SerializeField] private CameraController cams;
    [SerializeField] private Room room;
    [SerializeField] private Image image;
    [SerializeField] private Color normalColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.75f, 0.25f, 1f);
    [SerializeField] private Color wincolor = Color.purple;

    private void OnEnable()
    {
        if (cams == null) return;
        cams.RoomViewed += OnRoomViewed;
        Apply(cams.CurrentRoom);
    }

    private void OnDisable()
    {
        if (cams != null) cams.RoomViewed -= OnRoomViewed;
    }

    private void OnRoomViewed(Room viewed) => Apply(viewed);

    private void Apply(Room viewed)
    {
        if (image != null) image.color = viewed == room ? selectedColor : normalColor;
    }
}
