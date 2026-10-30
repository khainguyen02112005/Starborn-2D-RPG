using System.Collections;
using TMPro;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public Transform checkpoint;
    public float respawnDelay = 1.2f;
    public Vector2 nearbyOffset = new Vector2(-1.5f, 0.5f);
    public GameObject respawnOverlay;
    public TextMeshProUGUI respawnText;

    private Vector3 lastSafePosition;
    private Transform trackedPlayer;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            trackedPlayer = player.transform;
            lastSafePosition = checkpoint != null ? checkpoint.position : trackedPlayer.position;
        }
        if (respawnOverlay != null) respawnOverlay.SetActive(false);
        StartCoroutine(TrackSafePosition());
    }

    public void Respawn(PlayerHealth health)
    {
        StartCoroutine(RespawnRoutine(health));
    }

    public void SetCheckpoint(Vector3 position)
    {
        lastSafePosition = position;
    }

    private IEnumerator TrackSafePosition()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.5f);
            if (trackedPlayer == null) continue;
            PlayerHealth health = trackedPlayer.GetComponent<PlayerHealth>();
            if (health != null && !health.IsDead) lastSafePosition = trackedPlayer.position;
        }
    }

    private IEnumerator RespawnRoutine(PlayerHealth health)
    {
        if (respawnOverlay != null) respawnOverlay.SetActive(true);
        if (respawnText != null) respawnText.text = "Linh hồn đang trở lại...";
        yield return new WaitForSecondsRealtime(respawnDelay);

        Vector3 target = checkpoint != null ? checkpoint.position : lastSafePosition + (Vector3)nearbyOffset;
        MapSceneLayout layout = FindAnyObjectByType<MapSceneLayout>();
        if (layout != null) target = layout.ResolveSafePosition(target, lastSafePosition);
        health.ReviveAt(target);
        PlayerMana mana = health.GetComponent<PlayerMana>();
        if (mana != null) mana.RestoreFull();
        PlayerStamina stamina = health.GetComponent<PlayerStamina>();
        if (stamina != null) stamina.currentStamina = stamina.maxStamina;

        RespawnWhisperBubble whisper = health.GetComponent<RespawnWhisperBubble>();
        if (whisper == null) whisper = health.gameObject.AddComponent<RespawnWhisperBubble>();
        int deathCount = GameProgressManager.Instance != null
            ? GameProgressManager.Instance.DeathCount
            : PlayerPrefs.GetInt("RPG.DeathCount", 1);
        whisper.ShowAfterRespawn(deathCount);

        if (respawnText != null) respawnText.text = "Đã hồi sinh — Máu và Mana được phục hồi";
        yield return new WaitForSecondsRealtime(0.7f);
        if (respawnOverlay != null) respawnOverlay.SetActive(false);
        GameProgressManager.Instance?.SaveActiveSlot();
    }
}
