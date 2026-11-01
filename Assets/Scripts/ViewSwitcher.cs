using UnityEngine;

public class ViewSwitcher : MonoBehaviour
{
    [SerializeField] private RoomHandler viewSwitcher;
    [SerializeField] private Transform TargetPos;

    public void MoveTo()
    {
        viewSwitcher.ChangeTo(TargetPos);
    }
}