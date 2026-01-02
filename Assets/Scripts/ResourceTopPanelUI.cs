using System.Collections;
using System.Collections.Generic;
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
    public Transform goldGroup;   // GoldGroup из иерархии (если есть)
    public Transform coalGroup;   // CoalGroup из иерархии (если есть)
    public Transform turnGroup;   // TurnLabel / TurnGroup (если есть)

    [Header("Count animation")]
    [SerializeField] private bool animateCount = true;
    [SerializeField] private float countDuration = 0.18f;

    [Header("Pulse animation")]
    [SerializeField] private bool pulseOnChange = true;
    [SerializeField] private float pulseScale = 1.08f;
    [SerializeField] private float pulseDuration = 0.12f;

    // last values (for change detection)
    private int lastGold = int.MinValue;
    private int lastCoal = int.MinValue;
    private int lastGoldIncome = int.MinValue;
    private int lastCoalIncome = int.MinValue;
    private int lastTurn = int.MinValue;

    // coroutines
    private Coroutine goldAnimCoroutine;
    private Coroutine coalAnimCoroutine;

    private Coroutine goldIncomePulseCoroutine;
    private Coroutine coalIncomePulseCoroutine;
    private Coroutine turnPulseCoroutine;

    // ===== SCALE SAFETY (fixes "growing" when spamming) =====
    private readonly Dictionary<Transform, Vector3> initialScale = new Dictionary<Transform, Vector3>();

    private Vector3 GetInitialScale(Transform t)
    {
        if (t == null) return Vector3.one;
        if (!initialScale.TryGetValue(t, out var s))
        {
            s = t.localScale;
            initialScale[t] = s;
        }
        return s;
    }

    private void ResetToInitialScale(Transform t)
    {
        if (t == null) return;
        t.localScale = GetInitialScale(t);
    }

    // === PUBLIC API ===
    // Эти методы вызываются из TurnManager и CityRecruitPanelUI.

    public void UpdateAll(PlayerResources pr)
    {
        if (pr == null) return;

        // GOLD value
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

                Transform pulseTarget = goldGroup != null ? goldGroup : goldValueText.transform;
                ResetToInitialScale(pulseTarget);

                if (animateCount)
                {
                    goldAnimCoroutine = StartCoroutine(
                        AnimateValueAndPulse(
                            goldValueText,
                            lastGold,
                            pr.Gold,
                            countDuration,
                            pulseScale,
                            pulseDuration,
                            pulseTarget
                        )
                    );
                }
                else
                {
                    goldValueText.text = pr.Gold.ToString();
                    if (pulseOnChange) Pulse(pulseTarget);
                }

                lastGold = pr.Gold;
            }
        }

        // COAL value
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

                Transform pulseTarget = coalGroup != null ? coalGroup : coalValueText.transform;
                ResetToInitialScale(pulseTarget);

                if (animateCount)
                {
                    coalAnimCoroutine = StartCoroutine(
                        AnimateValueAndPulse(
                            coalValueText,
                            lastCoal,
                            pr.Coal,
                            countDuration,
                            pulseScale,
                            pulseDuration,
                            pulseTarget
                        )
                    );
                }
                else
                {
                    coalValueText.text = pr.Coal.ToString();
                    if (pulseOnChange) Pulse(pulseTarget);
                }

                lastCoal = pr.Coal;
            }
        }

        // GOLD income
        if (goldIncomeText != null)
        {
            int gInc = pr.GoldIncome;
            goldIncomeText.text = $"+{gInc}";

            if (pulseOnChange && lastGoldIncome != int.MinValue && gInc != lastGoldIncome)
            {
                Transform t = goldGroup != null ? goldGroup : goldIncomeText.transform;
                ResetToInitialScale(t);

                if (goldIncomePulseCoroutine != null) StopCoroutine(goldIncomePulseCoroutine);
                goldIncomePulseCoroutine = StartCoroutine(PulseRoutine(t, pulseScale, pulseDuration));
            }

            lastGoldIncome = gInc;
        }

        // COAL income
        if (coalIncomeText != null)
        {
            int cInc = pr.CoalIncome;
            coalIncomeText.text = $"+{cInc}";

            if (pulseOnChange && lastCoalIncome != int.MinValue && cInc != lastCoalIncome)
            {
                Transform t = coalGroup != null ? coalGroup : coalIncomeText.transform;
                ResetToInitialScale(t);

                if (coalIncomePulseCoroutine != null) StopCoroutine(coalIncomePulseCoroutine);
                coalIncomePulseCoroutine = StartCoroutine(PulseRoutine(t, pulseScale, pulseDuration));
            }

            lastCoalIncome = cInc;
        }
    }

    public void UpdateTurn(int turn)
    {
        if (turnText != null)
            turnText.text = $"Turn {turn}";

        if (!pulseOnChange) { lastTurn = turn; return; }

        if (lastTurn == int.MinValue)
        {
            lastTurn = turn;
            return;
        }

        if (turn != lastTurn)
        {
            Transform t = turnGroup != null ? turnGroup : (turnText != null ? turnText.transform : null);
            if (t != null)
            {
                // ВАЖНО: перед новым пульсом — сбросить scale, иначе будет "накапливаться"
                ResetToInitialScale(t);

                if (turnPulseCoroutine != null) StopCoroutine(turnPulseCoroutine);
                turnPulseCoroutine = StartCoroutine(PulseRoutine(t, pulseScale, pulseDuration));
            }
        }

        lastTurn = turn;
    }

    // === Helpers ===

    private IEnumerator AnimateValueAndPulse(
        TextMeshProUGUI text,
        int from,
        int to,
        float dur,
        float scaleTo,
        float pulseDur,
        Transform pulseTarget)
    {
        if (text == null) yield break;

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, dur));
            int v = Mathf.RoundToInt(Mathf.Lerp(from, to, k));
            text.text = v.ToString();
            yield return null;
        }
        text.text = to.ToString();

        if (pulseOnChange && pulseTarget != null)
            yield return PulseRoutine(pulseTarget, scaleTo, pulseDur);
    }

    private void Pulse(Transform target)
    {
        if (target == null) return;
        ResetToInitialScale(target);
        StartCoroutine(PulseRoutine(target, pulseScale, pulseDuration));
    }

    private IEnumerator PulseRoutine(Transform target, float scaleTo, float dur)
    {
        if (target == null) yield break;

        // ВСЕГДА используем оригинальный scale, а не текущий (иначе растёт)
        Vector3 baseScale = GetInitialScale(target);

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, dur));

            float s = (k < 0.5f)
                ? Mathf.Lerp(1f, scaleTo, k / 0.5f)
                : Mathf.Lerp(scaleTo, 1f, (k - 0.5f) / 0.5f);

            target.localScale = baseScale * s;
            yield return null;
        }

        target.localScale = baseScale;
    }
}
