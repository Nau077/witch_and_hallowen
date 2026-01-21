using UnityEngine;

public abstract class EnemyMoveBrainBase : MonoBehaviour
{
    protected EnemyWalker brain;
    protected EnemyHealth selfHP;

    public virtual void Init(EnemyWalker walker)
    {
        brain = walker;
        if (walker) selfHP = walker.GetComponent<EnemyHealth>();
    }

    /// <summary>
    /// ¬озвращает "желательное" направление движени€ (обычно нормализованное).
    /// ћожно вернуть Vector2.zero чтобы сто€ть.
    /// </summary>
    public abstract Vector2 GetDesiredMoveDir();

    /// <summary>
    /// ¬ызываетс€ раз в decideEvery (или когда мозг решит), если тебе надо "перекинуть" состо€ние.
    /// ѕо умолчанию ничего не делает.
    /// </summary>
    public virtual void OnDecideTick() { }
}
