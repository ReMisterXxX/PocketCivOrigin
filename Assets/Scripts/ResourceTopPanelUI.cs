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

    [Header("Number jump animation")]
    public float jumpDuration = 0.15f;
    public float jumpScale = 1.2f;

    private int lastGold = int.MinValue;
    private int lastCoal = int.MinValue;

    private Coroutine goldJumpCoroutine;
    private Coroutine coalJumpCoroutine;

    public void UpdateAll(PlayerResources pr)
    {
        if (pr == null) return;

        // GOLD
        if (goldValueText != null)
        {
            if (pr.Gold != lastGold)
            {
                if (goldJumpCoroutine != null)
                    StopCoroutine(goldJumpCoroutine);

                goldJumpCoroutine = StartCoroutine(
                    AnimateNumberJump(goldValueText.rectTransform)
                );
            }

            goldValueText.text = pr.Gold.ToString();
            lastGold = pr.Gold;
        }

        // COAL
        if (coalValueText != null)
        {
            if (pr.Coal != lastCoal)
            {
                if (coalJumpCoroutine != null)
                    StopCoroutine(coalJumpCoroutine);

                coalJumpCoroutine = StartCoroutine(
                    AnimateNumberJump(coalValueText.rectTransform)
                );
            }

            coalValueText.text = pr.Coal.ToString();
            lastCoal = pr.Coal;
        }

        if (goldIncomeText != null)
            goldIncomeText.text = $"+{pr.GoldIncome}";

        if (coalIncomeText != null)
            coalIncomeText.text = $"+{pr.CoalIncome}";
    }

    public void UpdateTurn(int turn)
    {
        if (turnText != null)
            turnText.text = $"Turn {turn}";
    }

    private IEnumerator AnimateNumberJump(RectTransform target)
    {
        if (target == null) yield break;

        Vector3 baseScale = target.localScale;
        Vector3 maxScale = baseScale * jumpScale;

        float t = 0f;
        while (t < jumpDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / jumpDuration);

            // плавный «подскок»: вверх и обратно
            float curve = Mathf.Sin(k * Mathf.PI); // 0 → 1 → 0
            target.localScale = Vector3.Lerp(baseScale, maxScale, curve);

            yield return null;
        }

        target.localScale = baseScale;
    }
}
