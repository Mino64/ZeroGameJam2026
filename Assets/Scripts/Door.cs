using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] Color OpenColor = Color.green;
    [SerializeField] Color CloseColor = Color.red;
    [SerializeField] SpriteRenderer DoorSprite;
    private bool closed = false;

    private void Start()
    {
        if (DoorSprite != null) Closed = closed;
    }

    public bool Closed
    {
        set
        {
            closed = value;
            if (closed)
            {
                DoorSprite.color = CloseColor;
            }
            else
            {
                DoorSprite.color = OpenColor;
            }
        }
        get
        {
            return closed;
        }
    }
    public void ToggleDoor()
    {
        Closed = !Closed;
    }
}