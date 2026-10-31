using UnityEngine;

public class AlterableRoom : Room
{
    [Header("Versão alterada")]
    [SerializeField] private Sprite alteredEmptySprite;
    [SerializeField] private Sprite alteredMonsterSprite;

    public bool IsAltered { get; private set; }

    public override Sprite GetCurrentSprite()
    {
        if (!IsAltered) return base.GetCurrentSprite();
        return MonsterPresent ? alteredMonsterSprite : alteredEmptySprite;
    }

    public void Restore()
    {
        if (!IsAltered) return;
        IsAltered = false;
        NotifyChanged();
    }


    protected override void OnMonsterExited()
    {
        if (IsAltered) return;
        IsAltered = true;
    }
}
