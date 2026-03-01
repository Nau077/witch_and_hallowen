using UnityEngine;

[DisallowMultipleComponent]
public class TutorialScarecrowBeamGuard : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private EnemyWalker enemyWalker;
    [SerializeField] private EnemySkillEnergyBeam beamSkill;
    [SerializeField] private EnemySkillBase[] skillsToDisable;

    [Header("Tutorial Beam Settings")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool keepWalkerEnabled = true;
    [SerializeField] private bool forceNoMovement = true;
    [SerializeField] private float forcedMoveSpeed = 0f;
    [SerializeField] private int beamDamagePerTick = 999;
    [SerializeField] private float beamAttackInterval = 0.05f;
    [SerializeField] private float beamPreAttackHold = 0.05f;
    [SerializeField] private float beamPostAttackHold = 0.05f;
    [SerializeField] private float beamChaseDuration = 6f;
    [SerializeField] private float beamTickInterval = 0.06f;
    [SerializeField] private bool disableNonBeamSkills = true;
    [SerializeField] private bool logWarnings = true;
    [SerializeField] private bool keepTryingAssignPlayer = true;

    private void Reset()
    {
        enemyWalker = GetComponent<EnemyWalker>();
        beamSkill = GetComponent<EnemySkillEnergyBeam>();
        skillsToDisable = GetComponents<EnemySkillBase>();
    }

    private void Awake()
    {
        ResolveRefs();
        if (skillsToDisable == null || skillsToDisable.Length == 0)
            skillsToDisable = GetComponents<EnemySkillBase>();
    }

    private void OnEnable()
    {
        if (applyOnEnable)
            ApplyBeamGuardPreset();
    }

    private void Update()
    {
        if (!keepTryingAssignPlayer)
            return;

        if (enemyWalker == null || enemyWalker.player != null)
            return;

        TryAssignPlayer();
    }

    public void ApplyBeamGuardPreset()
    {
        ResolveRefs();

        if (enemyWalker != null)
        {
            enemyWalker.enabled = keepWalkerEnabled;
            if (keepWalkerEnabled)
            {
                if (forceNoMovement)
                    enemyWalker.moveSpeed = Mathf.Max(0f, forcedMoveSpeed);

                enemyWalker.attackInterval = Mathf.Max(0.01f, beamAttackInterval);
                enemyWalker.preAttackHold = Mathf.Max(0f, beamPreAttackHold);
                enemyWalker.postAttackHold = Mathf.Max(0f, beamPostAttackHold);

                TryAssignPlayer();
            }
        }

        if (beamSkill != null)
        {
            beamSkill.enabled = true;
            beamSkill.useChance = 1f;
            beamSkill.everyNthAttack = 0;
            beamSkill.damagePerTick = Mathf.Max(1, beamDamagePerTick);
            beamSkill.beamChaseDuration = Mathf.Max(0.2f, beamChaseDuration);
            beamSkill.tickInterval = Mathf.Max(0.01f, beamTickInterval);
        }

        if (disableNonBeamSkills && skillsToDisable != null)
        {
            for (int i = 0; i < skillsToDisable.Length; i++)
            {
                var skill = skillsToDisable[i];
                if (skill == null)
                    continue;
                if (beamSkill != null && skill == beamSkill)
                    continue;
                skill.enabled = false;
            }
        }

        if (logWarnings)
        {
            if (enemyWalker == null)
                Debug.LogWarning("[TutorialScarecrowBeamGuard] EnemyWalker not found on object/parent/children: " + gameObject.name);
            else if (enemyWalker.player == null)
                Debug.LogWarning("[TutorialScarecrowBeamGuard] EnemyWalker.player is not assigned: " + gameObject.name);

            if (beamSkill == null)
                Debug.LogWarning("[TutorialScarecrowBeamGuard] EnemySkillEnergyBeam not found on object/parent/children: " + gameObject.name);
            else if (beamSkill.beamPrefab == null)
                Debug.LogWarning("[TutorialScarecrowBeamGuard] beamPrefab is not assigned on EnemySkillEnergyBeam: " + gameObject.name);
        }
    }

    private void ResolveRefs()
    {
        if (enemyWalker == null)
            enemyWalker = GetComponent<EnemyWalker>();
        if (enemyWalker == null)
            enemyWalker = GetComponentInParent<EnemyWalker>();
        if (enemyWalker == null)
            enemyWalker = GetComponentInChildren<EnemyWalker>(true);

        if (beamSkill == null)
            beamSkill = GetComponent<EnemySkillEnergyBeam>();
        if (beamSkill == null)
            beamSkill = GetComponentInParent<EnemySkillEnergyBeam>();
        if (beamSkill == null)
            beamSkill = GetComponentInChildren<EnemySkillEnergyBeam>(true);
    }

    private void TryAssignPlayer()
    {
        if (enemyWalker == null || enemyWalker.player != null)
            return;

        if (RunLevelManager.Instance != null && RunLevelManager.Instance.playerTransform != null)
        {
            enemyWalker.player = RunLevelManager.Instance.playerTransform;
            return;
        }

        var playerHealth = FindObjectOfType<PlayerHealth>(true);
        if (playerHealth != null)
        {
            enemyWalker.player = playerHealth.transform;
            return;
        }

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            enemyWalker.player = playerGo.transform;
    }
}
