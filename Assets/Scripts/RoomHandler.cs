using UnityEngine;

public class RoomHandler : MonoBehaviour
{
    [SerializeField] private Camera cam;
    public void ChangeTo(Transform pos)
    {
        cam.transform.position = new Vector3(pos.localPosition.x, pos.localPosition.y, -10.0f);
    }
}
