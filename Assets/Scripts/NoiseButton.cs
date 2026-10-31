using UnityEngine;

public class NoiseButton : MonoBehaviour
{
    [SerializeField] private CameraController cams;
    [SerializeField] private NoiseSystem noise;

    public void OnClick() => noise.MakeNoise(cams.CurrentRoom);
}
