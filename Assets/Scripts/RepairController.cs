using UnityEngine;

public class RepairController : MonoBehaviour
{
    [SerializeField] private CameraController cams;
    [SerializeField] private PentagramQTE qte;
    [SerializeField] private NoiseSystem noise;
    [SerializeField] private MonitorController monitor;
    [SerializeField] private GameObject repairButton;

    private AlterableRoom target;

    private void Update()
    {
        AlterableRoom room = cams.CurrentRoom as AlterableRoom;

        bool canRepair = monitor.IsOpen && room != null && room.IsAltered
                         && !room.MonsterPresent && !qte.IsActive;
        repairButton.SetActive(canRepair);

        if (qte.IsActive && cams.CurrentRoom != target)
            qte.Cancel();
    }

    public void OnRepairClick()
    {
        target = cams.CurrentRoom as AlterableRoom;
        if (target == null) return;

        qte.Begin(OnSuccess, OnFail);
    }

    private void OnSuccess() => target.Restore();
    private void OnFail() { }
}
