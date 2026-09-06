using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField leftHP, leftATK, leftSPD;
    public TMP_InputField rightHP, rightATK, rightSPD;

    [Header("Cards")]
    public RectTransform leftCard, rightCard;

    [Header("Effect")]
    public GameObject clashEffectPrefab;
    public RectTransform effectParent;

    [Header("Shake Target")]
    public RectTransform shakeArea;

    [Header("UI")]
    public TMP_Text resultText;
    public TMP_Text battleDescription;
    public Button battleButton;

    private Vector2 leftStartPos, rightStartPos;

    void Start()
    {
        leftHP.text = "30"; leftATK.text = "40"; leftSPD.text = "30";
        rightHP.text = "30"; rightATK.text = "40"; rightSPD.text = "30";

        leftStartPos = leftCard.anchoredPosition;
        rightStartPos = rightCard.anchoredPosition;

        battleButton.onClick.AddListener(() => StartCoroutine(BattleSequence()));
    }

    IEnumerator BattleSequence()
    {
        resultText.text = "";
        battleDescription.text = "";

        if (!IsValidInput()) yield break;

        yield return StartCoroutine(MoveCardsFancy());
        PlayEffect();
        StartCoroutine(ShakeUI(shakeArea));
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(RunBattleStepByStep());

        ResetCards();
    }

    bool IsValidInput()
    {
        if (leftHP.text == "" || leftATK.text == "" || leftSPD.text == "" ||
            rightHP.text == "" || rightATK.text == "" || rightSPD.text == "")
        {
            resultText.text = "Input is missing.";
            return false;
        }

        int lHP = int.Parse(leftHP.text);
        int lATK = int.Parse(leftATK.text);
        int lSPD = int.Parse(leftSPD.text);

        int rHP = int.Parse(rightHP.text);
        int rATK = int.Parse(rightATK.text);
        int rSPD = int.Parse(rightSPD.text);

        if (lHP + lATK + lSPD != 100 || rHP + rATK + rSPD != 100)
        {
            resultText.text = "The total status must be 100.";
            return false;
        }

        return true;
    }

    IEnumerator RunBattleStepByStep()
    {
        int lHP = int.Parse(leftHP.text)*4;
        int lATK = int.Parse(leftATK.text);
        int lSPD = int.Parse(leftSPD.text);

        int rHP = int.Parse(rightHP.text)*4;
        int rATK = int.Parse(rightATK.text);
        int rSPD = int.Parse(rightSPD.text);

        battleDescription.text = "[Battle Start]\n";
        battleDescription.text += $"Left HP:{lHP}  Right HP:{rHP}\n\n";
        yield return new WaitForSeconds(0.8f);

        // 攻撃順決定
        bool leftFirst;
        if (lSPD > rSPD) leftFirst = true;
        else if (rSPD > lSPD) leftFirst = false;
        else leftFirst = (Random.value < 0.5f); // SPD同じ → 50%

        // ターン制ループ
        while (true)
        {
            if (leftFirst)
            {
                rHP = DoAttack("Left", "Right", rHP, lATK, lSPD, rSPD);
                yield return new WaitForSeconds(1.2f);
                if (rHP <= 0)
                {
                    battleDescription.text += "[Result] Left Win";
                    resultText.text = "Left Win";
                    yield break;
                }

                lHP = DoAttack("Right", "Left", lHP, rATK, rSPD, lSPD);
                yield return new WaitForSeconds(1.2f);
                if (lHP <= 0)
                {
                    battleDescription.text += "[Result] Right Win";
                    resultText.text = "Right Win";
                    yield break;
                }
            }
            else
            {
                lHP = DoAttack("Right", "Left", lHP, rATK, rSPD, lSPD);
                yield return new WaitForSeconds(1.2f);
                if (lHP <= 0)
                {
                    battleDescription.text += "[Result] Right Win";
                    resultText.text = "Right Win";
                    yield break;
                }

                rHP = DoAttack("Left", "Right", rHP, lATK, lSPD, rSPD);
                yield return new WaitForSeconds(1.2f);
                if (rHP <= 0)
                {
                    battleDescription.text += "[Result] Left Win";
                    resultText.text = "Left Win";
                    yield break;
                }
            }

            battleDescription.text += "---- Next Turn ----\n";
            yield return new WaitForSeconds(0.8f);
        }
    }

    // 攻撃処理（クリティカル・回避含む）
    int DoAttack(string attacker, string defender, int defHP, int atk, int spdA, int spdD)
    {
        // 攻撃力は入力値そのまま（int）
        float dmg = atk;

        // クリティカル（10%で1.2倍）
        bool crit = Random.value < 0.10f;
        if (crit) dmg *= 1.2f;

        // 回避（SPD差）
        float evadeChance = GetEvadeChance(spdA - spdD);
        bool evaded = Random.value < evadeChance;

        // 防御（50%でダメージ半減）
        bool defended = Random.value < 0.50f;
        if (defended) dmg *= 0.5f;

        // 最終ダメージは int に丸める
        int finalDamage = evaded ? 0 : Mathf.RoundToInt(dmg);

        defHP -= finalDamage;
        if (defHP < 0) defHP = 0;

        battleDescription.text += $"{attacker} attacks {defender} → {finalDamage} dmg";
        if (evaded) battleDescription.text += " (Evaded!)";
        if (crit) battleDescription.text += " (Critical!)";
        if (defended) battleDescription.text += " (Defended!)";
        battleDescription.text += $"\n{defender} HP after: {defHP}\n\n";

        return defHP;
    }



    float GetEvadeChance(int spdDiff)
    {
        if (spdDiff >= 20) return 0.50f;
        if (spdDiff >= 10) return 0.25f;
        return 0f;
    }

    IEnumerator MoveCardsFancy()
    {
        float centerX = 0f;
        Vector2 leftFar = leftStartPos + new Vector2(-120f, 0f);
        Vector2 leftCenter = new Vector2(centerX - 120f, leftStartPos.y);
        Vector2 rightFar = rightStartPos + new Vector2(120f, 0f);
        Vector2 rightCenter = new Vector2(centerX + 120f, rightStartPos.y);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            leftCard.anchoredPosition = Vector2.Lerp(leftCard.anchoredPosition, leftFar, t);
            rightCard.anchoredPosition = Vector2.Lerp(rightCard.anchoredPosition, rightFar, t);
            yield return null;
        }

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            leftCard.anchoredPosition = Vector2.Lerp(leftCard.anchoredPosition, leftCenter, t);
            rightCard.anchoredPosition = Vector2.Lerp(rightCard.anchoredPosition, rightCenter, t);
            yield return null;
        }
    }

    void PlayEffect()
    {
        if (clashEffectPrefab == null) return;

        GameObject fx = Instantiate(clashEffectPrefab, effectParent);
        fx.transform.localPosition = Vector3.zero;

        var psRenderer = fx.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.sortingLayerName = "UI";
            psRenderer.sortingOrder = 200;
        }

        Destroy(fx, 2f);
    }

    IEnumerator ShakeUI(RectTransform target)
    {
        Vector3 origin = target.anchoredPosition;

        for (int i = 0; i < 10; i++)
        {
            target.anchoredPosition = origin + (Vector3)Random.insideUnitCircle * 10f;
            yield return new WaitForSeconds(0.02f);
        }

        target.anchoredPosition = origin;
    }

    void ResetCards()
    {
        leftCard.anchoredPosition = leftStartPos;
        rightCard.anchoredPosition = rightStartPos;
    }
}
