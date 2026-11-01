using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] Sprite OpenIMG;
    [SerializeField] Sprite CloseIMG;
    [SerializeField] SpriteRenderer DoorSprite;

    [SerializeField] AudioClip OpenSound;
    [SerializeField] AudioClip CloseSound;
    [SerializeField] private AudioSource SoundPlayer;

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
        }
        get
        {
            return closed;
        }
    }
    public void ToggleDoor()
    {
        Closed = !Closed;
        if (closed)
        {
            DoorSprite.sprite = CloseIMG;
            SoundPlayer.PlayOneShot(CloseSound);
        }
        else
        {
            DoorSprite.sprite = OpenIMG;
            SoundPlayer.PlayOneShot(OpenSound);
        }
    }
}