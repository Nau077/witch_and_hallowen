// Assets/Scripts/Enemy/skills/EnemySkillBase.cs
using UnityEngine;

public abstract class EnemySkillBase : MonoBehaviour
{
    [Header("Common Skill Settings")]
    [Tooltip("Более высокий приоритет получает первый шанс обработать атаку.")]
    public int priority = 0;

    [Range(0f, 1f)]
    [Tooltip("Вероятность срабатывания при подходящем тике.")]
    public float useChance = 1f;

    [Tooltip("Если > 0, скилл проверяет только каждый N-й глобальный тик атаки.")]
    public int everyNthAttack = 0;

    protected EnemyWalker brain;
    protected EnemyHealth selfHP;
    protected SpriteRenderer spriteRenderer;

    public int Priority => priority;

    /// <summary>Вызывается мозгом из Start.</summary>
    public virtual void Init(EnemyWalker brain)
    {
        this.brain = brain;
        if (brain != null)
        {
            selfHP = brain.GetComponent<EnemyHealth>();
            spriteRenderer = brain.GetComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// Вызывается мозгом в момент атаки.
    /// attackIndex – глобальный счётчик атак.
    /// attackConsumed – если предыдущий скилл уже «занял» тик, он ставит true.
    /// </summary>
    public virtual void OnBrainAttackTick(int attackIndex, ref bool attackConsumed) { }

    /// <summary>Вызывается каждый кадр Update, если враг жив и не заморожен/не в стаггере.</summary>
    public virtual void OnBrainUpdateSkill(float deltaTime) { }

    /// <summary>
    /// ВАЖНО: вызывается, когда мозг ПРЕРЫВАЕТ текущую атаку (freeze/stagger/внешний interrupt).
    /// Наследники должны тут останавливать свои корутины/Invoke и сбрасывать состояние.
    /// </summary>
    public virtual void OnBrainAttackInterrupted() { }

    protected bool CanUse(int attackIndex, bool alreadyConsumed)
    {
        if (!enabled) return false;
        if (!isActiveAndEnabled) return false;
        if (alreadyConsumed) return false;

        if (brain == null) return false;
        if (selfHP != null && selfHP.IsDead) return false;
        if (brain.PlayerIsDead) return false;

        if (everyNthAttack > 0 && attackIndex % everyNthAttack != 0)
            return false;

        if (useChance < 1f && Random.value > useChance)
            return false;

        return true;
    }
}
