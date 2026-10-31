using UnityEngine;

public class FitSpriteToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private SpriteRenderer sr;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    private void LateUpdate() => Fit();

    public void Fit()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (sr == null || sr.sprite == null || cam == null || !cam.orthographic) return;

        Vector2 spriteSize = sr.sprite.bounds.size;
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        Vector3 camPos = cam.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
    }
}