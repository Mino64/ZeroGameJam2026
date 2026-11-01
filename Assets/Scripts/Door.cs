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

    public event System.Action<bool> ClosedChanged;


    private void Start()
    {
        if (DoorSprite != null) Closed = closed;

    }

    public bool Closed
    {
        set
        {
            if (closed == value) return;
            closed = value;
            ClosedChanged?.Invoke(closed);
        }
        get
        {
            return closed;
        }
    }
    public void ToggleDoor()
    {
        Closed = !Closed;

        if (DoorSprite != null)
        {
            Sprite sprite = closed ? CloseIMG : OpenIMG;
            if (sprite != null)
            {
                DoorSprite.sprite = sprite;
                DoorSprite.color = Color.white;
            }
            else
            {
                DoorSprite.color = closed ? Color.red : Color.green;
            }
        }

        AudioClip clip = closed ? CloseSound : OpenSound;
        if (SoundPlayer != null && clip != null) SoundPlayer.PlayOneShot(clip);
    }
}