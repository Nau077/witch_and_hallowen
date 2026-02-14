using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMana : MonoBehaviour
{
    [Header("Mana")]
    [Min(1)] public int maxMana = 50;
    [Min(0)] public int currentMana = 50;
    [Min(0f)] public float regenPerSecond = 2f;

    private float _manaExact;
    [SerializeField] private int baseMaxMana;
    [SerializeField] private int permanentMaxManaBonus;

    private void Awake()
    {
        if (baseMaxMana <= 0)
            baseMaxMana = Mathf.Max(1, maxMana);

        _manaExact = Mathf.Clamp(currentMana, 0, maxMana);
        currentMana = Mathf.FloorToInt(_manaExact);
    }

    private void Update()
    {
        if (regenPerSecond > 0f && _manaExact < maxMana)
        {
            _manaExact = Mathf.Min(maxMana, _manaExact + regenPerSecond * Time.deltaTime);
            currentMana = Mathf.FloorToInt(_manaExact);
        }
    }

    public bool CanSpend(int amount) => _manaExact >= amount;

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (_manaExact < amount) return false;

        _manaExact -= amount;
        if (_manaExact < 0f) _manaExact = 0f;
        currentMana = Mathf.FloorToInt(_manaExact);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        _manaExact = Mathf.Min(maxMana, _manaExact + amount);
        currentMana = Mathf.FloorToInt(_manaExact);
    }

    /// <summary>Поднять ману до 100% (для перехода на новый уровень).</summary>
    public void FillToMax()
    {
        _manaExact = maxMana;
        currentMana = maxMana;
    }

    public void ApplyPermanentMaxManaBonus(int bonus)
    {
        permanentMaxManaBonus = Mathf.Max(0, bonus);
        maxMana = Mathf.Max(1, baseMaxMana + permanentMaxManaBonus);
        FillToMax();
    }

    // для UI
    public float Normalized => maxMana > 0 ? currentMana / (float)maxMana : 0f;
    public float NormalizedExact => maxMana > 0 ? _manaExact / maxMana : 0f;
}
