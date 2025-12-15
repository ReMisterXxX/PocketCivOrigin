using System.Collections;
using UnityEngine;
using TMPro;

public class ResourceTopPanelUI : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI goldValueText;
    public TextMeshProUGUI goldIncomeText;
    public TextMeshProUGUI coalValueText;
    public TextMeshProUGUI coalIncomeText;
    public TextMeshProUGUI turnText;

    [Header("Optional: pulse whole groups (recommended)")]
    public Transform goldGroup;   // GoldGroup из иерархии
    public Transform coalGroup;   // CoalGroup из иерархии
    public Transform turnLabel;   // TurnLable из иерархии (объект)

    [Header("Value animation")]
    public float countDuration = 0.25f;
    public float pulseScale = 1.2f;
    public float pulseDuration = 0.15f;

    [Header("Income/Turn animation")]
    public bool pulseIncomeOnChange = true;
    public float incomePulseScale = 1.15f;
    public float incomePulseDuration = 0.12f;

    public bool pulseTurnOnChange = true;
    public float turnPulseScale = 1.12f;
    public float turnPulseDuration = 0.12f;

    private int lastGold = int.MinValue;
    private int lastCoal = int.MinValue;
    private int lastGoldIncome = int.MinValue;
    private int lastCoalIncome = int.MinValue;
    private int lastTurn = int.MinValue;

    private Coroutine goldAnimCoroutine;
    private Coroutine coalAnimCoroutine;

    private Coroutine goldIncomePulseCoroutine;
    private Coroutine coalIncomePulseCoroutine;
    private Coroutine turnPulseCoroutine;

    public void UpdateAll(PlayerResources pr)
    {
        if (pr == null) return;

        // === GOLD value ===
        if (goldValueText != null)
        {
            if (lastGold == int.MinValue)
            {
                lastGold = pr.Gold;
                goldValueText.text = pr.Gold.ToString();
            }
            else if (pr.Gold != lastGold)
            {
                if (goldAnimCoroutine != null) StopCoroutine(goldAnimCoroutine);
                goldAnimCoroutine = StartCoroutine(
                    AnimateValueAndPulse(
                        goldValueText,
                        lastGold,
                        pr.Gold,
                        countDuration,
                        pulseScale,
                        pulseDuration,
                        // пульсим всю группу (если задана), иначе сам текст
                        goldGroup != null ? goldGroup : goldValueText.transform
                    )
                );
                lastGold = pr.Gold;
            }
            else
            {
                goldValueText.text = pr.Gold.ToString();
            }
        }

        // === COAL value ===
        if (coalValueText != null)
        {
            if (lastCoal == int.MinValue)
            {
                lastCoal = pr.Coal;
                coalValueText.text = pr.Coal.ToString();
            }
            else if (pr.Coal != lastCoal)
            {
                if (coalAnimCoroutine != null) StopCoroutine(coalAnimCoroutine);
                coalAnimCoroutine = StartCoroutine(
                    AnimateValueAndPulse(
                        coalValueText,
                        lastCoal,
                        pr.Coal,
                        countDuration,
                        pulseScale,
                        pulseDuration,
                        coalGroup != null ? coalGroup : coalValueText.transform
                    )
                );
                lastCoal = pr.Coal;
            }
            else
            {
                coalValueText.text = pr.Coal.ToString();
            }
        }

        // === income texts + pulse ===
        if (goldIncomeText != null)
        {
            int inc = pr.GoldIncome;
            goldIncomeText.text = $"+{inc}";

            if (pulseIncomeOnChange && lastGoldIncome != int.MinValue && inc != lastGoldIncome)
            {
                if (goldIncomePulseCoroutine != null) StopCoroutine(goldIncomePulseCoroutine);

                Transform target = goldGroup != null ? goldGroup : goldIncomeText.transform;
                goldIncomePulseCoroutine = StartCoroutine(PulseOnly(target, incomePulseScale, incomePulseDuration));
            }

            lastGoldIncome = inc;
        }

        if (coalIncomeText != null)
        {
            int inc = pr.CoalIncome;
            coalIncomeText.text = $"+{inc}";

            if (pulseIncomeOnChange && lastCoalIncome != int.MinValue && inc != lastCoalIncome)
            {
                if (coalIncomePulseCoroutine != null) StopCoroutine(coalIncomePulseCoroutine);

                Transform target = coalGroup != null ? coalGroup : coalIncomeText.transform;
                coalIncomePulseCoroutine = StartCoroutine(PulseOnly(target, incomePulseScale, incomePulseDuration));
            }

            lastCoalIncome = inc;
        }
    }

    public void UpdateTurn(int turn)
    {
        if (turnText != null)
            turnText.text = $"Turn {turn}";

        if (!pulseTurnOnChange) { lastTurn = turn; return; }

        if (lastTurn == int.MinValue)
        {
            lastTurn = turn;
            return;
        }

        if (turn != lastTurn)
        {
            if (turnPulseCoroutine != null) StopCoroutine(turnPulseCoroutine);

            Transform target = turnLabel != null ? turnLabel : (turnText != null ? turnText.transform : null);
            if (target != null)
                turnPulseCoroutine = StartCoroutine(PulseOnly(target, turnPulseScale, turnPulseDuration));

            lastTurn = turn;
        }
    }

    // ===== Anim helpers =====

    private IEnumerator AnimateValueAndPulse(
        TextMeshProUGUI valueText,
        int from,
        int to,
        float valueDur,
        float scaleTo,
        float scaleDur,
        Transform pulseTarget
    )
    {
        if (valueText == null) yield break;
        if (pulseTarget == null) pulseTarget = valueText.transform;

        Vector3 baseScale = pulseTarget.localScale;

        float tValue = 0f;
        float tScale = 0f;

        while (tValue < valueDur || tScale < scaleDur)
        {
            // count-up
            if (valueDur > 0f && tValue < valueDur)
            {
                tValue += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(tValue / valueDur);
                float ease = Mathf.SmoothStep(0f, 1f, k);
                int v = Mathf.RoundToInt(Mathf.Lerp(from, to, ease));
                valueText.text = v.ToString();
            }
            else
            {
                valueText.text = to.ToString();
            }

            // pulse
            if (scaleDur > 0f && tScale < scaleDur)
            {
                tScale += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(tScale / scaleDur);

                float s = (k < 0.5f)
                    ? Mathf.Lerp(1f, scaleTo, k / 0.5f)
                    : Mathf.Lerp(scaleTo, 1f, (k - 0.5f) / 0.5f);

                pulseTarget.localScale = baseScale * s;
            }
            else
            {
                pulseTarget.localScale = baseScale;
            }

            yield return null;
        }

        valueText.text = to.ToString();
        pulseTarget.localScale = baseScale;
    }

    private IEnumerator PulseOnly(Transform target, float scaleTo, float dur)
    {
        if (target == null) yield break;

        Vector3 baseScale = target.localScale;
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);

            float s = (k < 0.5f)
                ? Mathf.Lerp(1f, scaleTo, k / 0.5f)
                : Mathf.Lerp(scaleTo, 1f, (k - 0.5f) / 0.5f);

            target.localScale = baseScale * s;
            yield return null;
        }

        target.localScale = baseScale;
    }
}
