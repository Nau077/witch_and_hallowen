using UnityEngine;

[DisallowMultipleComponent]
public class TutorialScarecrowDummyTarget : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private EnemyWalker enemyWalker;
    [SerializeField] private Rigidbody2D rb2d;
    [SerializeField] private EnemySkillBase[] enemySkills;

    [Header("Behavior")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool disableEnemyWalker = true;
    [SerializeField] private bool disableEnemySkills = true;

    private void Reset()
    {
        enemyWalker = GetComponent<EnemyWalker>();
        rb2d = GetComponent<Rigidbody2D>();
        enemySkills = GetComponents<EnemySkillBase>();
    }

    private void Awake()
    {
        if (enemyWalker == null)
            enemyWalker = GetComponent<EnemyWalker>();
        if (rb2d == null)
            rb2d = GetComponent<Rigidbody2D>();
        if (enemySkills == null || enemySkills.Length == 0)
            enemySkills = GetComponents<EnemySkillBase>();
    }

    private void OnEnable()
    {
        if (applyOnEnable)
            ApplyDummyPreset();
    }

    public void ApplyDummyPreset()
    {
        if (disableEnemyWalker && enemyWalker != null)
            enemyWalker.enabled = false;

        if (disableEnemySkills && enemySkills != null)
        {
            for (int i = 0; i < enemySkills.Length; i++)
            {
                if (enemySkills[i] != null)
                    enemySkills[i].enabled = false;
            }
        }

        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }
    }
}
