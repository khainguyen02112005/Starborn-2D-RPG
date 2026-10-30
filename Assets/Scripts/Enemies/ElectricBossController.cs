using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricBossController : MonoBehaviour
{
    public Sprite telegraphSprite;
    public Sprite[] lightningFrames;
    public float attackInterval = 3.4f;
    public float warningTime = 1.15f;
    public float strikeRadius = 1.25f;
    public int strikeDamage = 4;
    [Header("Phép bổ sung")]
    public Sprite fireSprite;
    public Sprite waterSprite;
    public int fireDamage = 7;
    public int waterDamage = 5;
    public float projectileTime = 0.8f;
    private Transform player;
    private float nextAttack;
    private bool attacking;
    private bool encounterActive;
    private bool phaseTwo;
    private EnemySPUMAnimation animationDriver;
    private PlayerMovement slowedMovement;
    private float speedBeforeSlow;
    private readonly EnemyAI.PathFollower navigation = new EnemyAI.PathFollower();

    public bool EncounterActive => encounterActive;

    private void Start()
    {
        if (GetComponent<BossEncounterController>() == null)
            gameObject.AddComponent<BossEncounterController>();
        animationDriver = GetComponent<EnemySPUMAnimation>();
        if (animationDriver == null) animationDriver = gameObject.AddComponent<EnemySPUMAnimation>();
        FindPlayer();
        nextAttack = Time.time + 2f;
    }

    private void Update()
    {
        if (!encounterActive) return;
        if (player == null) FindPlayer();
        if (player != null && !attacking && Time.time >= nextAttack) StartCoroutine(CastRandomPattern());
    }

    private void FixedUpdate()
    {
        if (!encounterActive || attacking || player == null || CinematicPresentationController.IsCinematicActive) return;
        var body = GetComponent<Rigidbody2D>();
        if (body == null || !body.simulated) return;
        float distance = Vector2.Distance(body.position, player.position);
        Vector2 next = distance > 3f ? navigation.NextPosition(body.position, player.position, 1.3f * Time.fixedDeltaTime, .42f) : body.position;
        body.MovePosition(next);
        animationDriver?.SetMoving(Vector2.Distance(body.position, next) > .001f);
    }

    public void ActivateEncounter()
    {
        encounterActive = true;
        nextAttack = Time.time + 1.2f;
    }

    public void DeactivateEncounter()
    {
        encounterActive = false;
        attacking = false;
        StopAllCoroutines();
        OwnedAttackLifetime.Clear(this);
        RestoreMovementSpeed();
    }

    private void OnDisable() => DeactivateEncounter();
    private void OnDestroy() => DeactivateEncounter();
    private void RestoreMovementSpeed()
    {
        if (slowedMovement != null) slowedMovement.moveSpeed = speedBeforeSlow;
        slowedMovement = null;
    }
    public bool TryGetUnslowedSpeed(PlayerMovement movement, out float speed)
    {
        speed = speedBeforeSlow;
        return slowedMovement != null && slowedMovement == movement;
    }
    private GameObject Own(GameObject attack, float life = 8f)
    {
        attack.AddComponent<OwnedAttackLifetime>().Initialize(this, life);
        return attack;
    }

    public void EnterPhaseTwo()
    {
        phaseTwo = true;
        attackInterval = Mathf.Max(1.8f, attackInterval * 0.72f);
        GameAudioManager.Play(GameSound.BossRoar, transform.position, 0.9f, 0.7f);
    }

    private IEnumerator CastRandomPattern()
    {
        attacking = true;
        animationDriver?.PlayMagicAttack();
        int choices = phaseTwo ? 4 : 3;
        int pattern = Random.Range(0, choices);
        if (pattern == 0) yield return ElectricPattern();
        else if (pattern == 1) yield return FireVolley();
        else if (pattern == 2) yield return WaterNova();
        else yield return ArcLightningSweep();
        nextAttack = Time.time + attackInterval;
        attacking = false;
    }

    private void FindPlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null) player = found.transform;
    }

    private IEnumerator ElectricPattern()
    {
        GameAudioManager.Play(GameSound.BossCast, transform.position, 0.72f, 1.05f);
        List<Vector3> positions = new List<Vector3>();
        if (Random.value > 0.45f)
        {
            positions.Add(player.position);
            positions.Add(player.position + (Vector3)Random.insideUnitCircle * 1.8f);
        }
        else
        {
            for (int i = 0; i < 4; i++) positions.Add(transform.position + (Vector3)Random.insideUnitCircle * 5f);
        }

        List<GameObject> warnings = new List<GameObject>();
        foreach (Vector3 position in positions)
        {
            GameObject warning = Own(new GameObject("ElectricWarning"));
            warning.transform.position = position;
            warning.transform.localScale = Vector3.one * strikeRadius * 2f;
            SpriteRenderer renderer = warning.AddComponent<SpriteRenderer>();
            renderer.sprite = telegraphSprite;
            renderer.color = new Color(0.15f, 0.9f, 1f, 0.38f);
            renderer.sortingOrder = 50;
            warnings.Add(warning);
        }
        yield return new WaitForSeconds(warningTime);
        foreach (GameObject warning in warnings)
        {
            if (warning == null) continue;
            Vector3 position = warning.transform.position;
            Destroy(warning);
            StartCoroutine(StrikeVisual(position));
            GameAudioManager.Play(GameSound.BossLightning, position, 0.9f, Random.Range(0.92f, 1.12f));
            if (player != null && Vector2.Distance(player.position, position) <= strikeRadius)
                player.GetComponent<PlayerHealth>()?.TakeDamage(strikeDamage, true);
        }
    }

    private IEnumerator FireVolley()
    {
        if (player == null) yield break;
        GameAudioManager.Play(GameSound.BossFire, transform.position, 0.88f, 0.88f);
        Vector3 target = player.position;
        int count = phaseTwo ? 5 : 3;
        List<GameObject> orbs = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject orb = Own(new GameObject("BossFireOrb"));
            orb.transform.position = transform.position + Vector3.up * 0.7f;
            SpriteRenderer renderer = orb.AddComponent<SpriteRenderer>();
            renderer.sprite = fireSprite != null ? fireSprite : telegraphSprite;
            renderer.color = new Color(1f, 0.2f + i * 0.06f, 0.06f, 1f);
            renderer.sortingOrder = 70;
            orb.transform.localScale = Vector3.one * 0.7f;
            CombatVFX.AttachProjectileAura(orb.transform, ElementalProjectile.Element.Fire);
            orbs.Add(orb);
        }
        float elapsed = 0f;
        while (elapsed < projectileTime)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < orbs.Count; i++)
            {
                if (orbs[i] == null) continue;
                Vector3 spread = Vector3.up * ((i - (count - 1) * 0.5f) * 0.55f);
                orbs[i].transform.position = Vector3.Lerp(transform.position, target + spread, elapsed / projectileTime);
            }
            yield return null;
        }
        foreach (GameObject orb in orbs)
        {
            if (orb == null) continue;
            CombatVFX.SpawnFireImpact(orb.transform.position, 0.85f);
            Destroy(orb);
        }
        if (player != null && Vector2.Distance(player.position, target) < 1.7f)
            player.GetComponent<PlayerHealth>()?.TakeDamage(fireDamage, true);
        CameraShakeManager.instance?.ShakeLight();
    }

    private IEnumerator WaterNova()
    {
        GameAudioManager.Play(GameSound.BossWater, transform.position, 0.92f, 0.82f);
        GameObject wave = Own(new GameObject("BossWaterNova"));
        wave.transform.position = transform.position;
        CombatVFX.AttachProjectileAura(wave.transform, ElementalProjectile.Element.Water);
        SpriteRenderer renderer = wave.AddComponent<SpriteRenderer>();
        renderer.sprite = waterSprite != null ? waterSprite : telegraphSprite;
        renderer.color = new Color(0.1f, 0.65f, 1f, 0.55f);
        renderer.sortingOrder = 65;
        bool damaged = false;
        float elapsed = 0f;
        const float duration = 0.9f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float radius = Mathf.Lerp(0.5f, phaseTwo ? 7f : 5.5f, elapsed / duration);
            wave.transform.localScale = Vector3.one * radius;
            Color color = renderer.color;
            color.a = Mathf.Lerp(0.6f, 0f, elapsed / duration);
            renderer.color = color;
            if (!damaged && player != null && Vector2.Distance(player.position, transform.position) <= radius * 0.5f)
            {
                damaged = true;
                player.GetComponent<PlayerHealth>()?.TakeDamage(waterDamage, true);
                StartCoroutine(SlowPlayer());
            }
            yield return null;
        }
        Destroy(wave);
    }

    private IEnumerator ArcLightningSweep()
    {
        if (player == null) yield break;
        GameAudioManager.Play(GameSound.BossLightning, transform.position, 0.9f, 0.82f);
        Vector3 center = player.position;
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 2.4f;
            StartCoroutine(StrikeVisual(position));
            if (Vector2.Distance(player.position, position) <= strikeRadius)
                player.GetComponent<PlayerHealth>()?.TakeDamage(strikeDamage, true);
            yield return new WaitForSeconds(0.13f);
        }
    }

    private IEnumerator SlowPlayer()
    {
        if (player == null) yield break;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null) yield break;
        if (slowedMovement == null) { slowedMovement = movement; speedBeforeSlow = movement.moveSpeed; }
        movement.moveSpeed = Mathf.Max(1.5f, speedBeforeSlow * 0.55f);
        yield return new WaitForSeconds(1.8f);
        RestoreMovementSpeed();
    }

    private IEnumerator StrikeVisual(Vector3 position)
    {
        GameObject visual = new GameObject("BossLightningStrike");
        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        visual.AddComponent<OwnedAttackLifetime>().Initialize(this, 2f, line.sharedMaterial);
        line.startColor = Color.white;
        line.endColor = new Color(0.2f, 0.85f, 1f, 1f);
        line.startWidth = 0.16f;
        line.endWidth = 0.05f;
        line.sortingOrder = 72;
        line.positionCount = 8;
        CombatVFX.SpawnLightningImpact(position, 1.3f);
        for (int frame = 0; frame < 5; frame++)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector3 point = Vector3.Lerp(position + Vector3.up * 6f, position, t);
                if (i > 0 && i < line.positionCount - 1)
                    point += (Vector3)Random.insideUnitCircle * 0.25f;
                line.SetPosition(i, point);
            }
            yield return new WaitForSeconds(0.045f);
        }
        CameraShakeManager.instance?.ShakeHeavy();
        Destroy(visual);
    }
}
