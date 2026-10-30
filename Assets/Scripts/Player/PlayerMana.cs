using UnityEngine;
using UnityEngine.UI;

public class PlayerMana : MonoBehaviour
{
    public const int StartingMana = 30;
    public int maxMana = StartingMana;
    public int currentMana;
    public Image manaBarFill;

    private void Start()
    {
        // Older scenes used a five-point pool. Keep earned capacity bonuses.
        if (maxMana < StartingMana) maxMana += StartingMana - 5;
        currentMana = maxMana;
        UpdateManaBar();
    }

    public bool UseMana(int amount)
    {
        if (currentMana < amount) return false;
        currentMana -= amount;
        UpdateManaBar();
        return true;
    }

    public void UpdateManaBar()
    {
        if (manaBarFill != null)
            manaBarFill.fillAmount = maxMana > 0 ? Mathf.Clamp01((float)currentMana / maxMana) : 0f;
    }

    public void LevelUpBoost(int extraMana)
    {
        maxMana += extraMana;
        RestoreFull();
    }

    public void RestoreFull()
    {
        currentMana = maxMana;
        UpdateManaBar();
    }

    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Clamp(currentMana + Mathf.Max(0, amount), 0, maxMana);
        UpdateManaBar();
    }

    public void RestoreState(int savedMaxMana, int savedCurrentMana)
    {
        int oldMaximum = Mathf.Max(1, savedMaxMana);
        maxMana = oldMaximum < StartingMana ? oldMaximum + StartingMana - 5 : oldMaximum;
        currentMana = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(savedCurrentMana / (float)oldMaximum) * maxMana), 0, maxMana);
        UpdateManaBar();
    }
}
