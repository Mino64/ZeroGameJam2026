using UnityEngine;

public class OfficeSideView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private Sprite doorOpenSprite;
    [SerializeField] private Sprite doorClosedSprite;
    [SerializeField] private Door[] doors;

    private void OnEnable()
    {
        if (doors != null)
            foreach (Door d in doors)
                if (d != null) d.ClosedChanged += OnClosedChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (doors == null) return;

        foreach (Door d in doors)
            if (d != null) d.ClosedChanged -= OnClosedChanged;
    }

    private void OnClosedChanged(bool closed) => Refresh();

    private void Refresh()
    {
        if (background == null) return;

        bool anyClosed = false;
        if (doors != null)
            foreach (Door d in doors)
                if (d != null && d.Closed) { anyClosed = true; break; }

        Sprite sprite = anyClosed ? doorClosedSprite : doorOpenSprite;
        if (sprite != null) background.sprite = sprite;
    }
}