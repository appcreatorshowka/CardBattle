using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Input Fields")] 
    public TMP_InputField leftHP, leftATK, leftSPD; // 左側のステータス入力フィールド
    public TMP_InputField rightHP, rightATK, rightSPD; // 右側のステータス入力フィールド

    [Header("Cards")]
    public RectTransform leftCard, rightCard; // 左右のカードのRectTransform

    [Header("Effect")]
    public GameObject clashEffectPrefab; // エフェクトのプレハブ
    public RectTransform effectParent; // エフェクトの親Transform

    [Header("Shake Target")]
    public RectTransform shakeArea; // ぶつかったときに揺れるUIのRectTransform

    [Header("UI")]
    public TMP_Text resultText; // 結果表示用のテキスト
    public TMP_Text battleDescription; // バトルの詳細ログ表示用のテキスト
    public Button battleButton; // バトル開始ボタン

    private Vector2 leftStartPos, rightStartPos; // 左右のカードの初期位置
    public ScrollRect scrollRect; // スクロールビューの参照
    public Button randomButton; // ランダムステータス生成ボタン
    public float pullDistance = 100f;      // 引きの距離（外側へ）
    public float collideDistance = 250f;    // ぶつかる距離（中央付近)



    void Start()
    {
        leftHP.text = "30"; leftATK.text = "40"; leftSPD.text = "30";
        rightHP.text = "30"; rightATK.text = "40"; rightSPD.text = "30";

        leftStartPos = leftCard.anchoredPosition;
        rightStartPos = rightCard.anchoredPosition;

        battleButton.onClick.AddListener(() => StartCoroutine(BattleSequence()));

        randomButton.onClick.AddListener(SetRandomStats); // ← ここでイベント登録
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
        int lHP = int.Parse(leftHP.text) * 2;
        int lATK = int.Parse(leftATK.text);
        int lSPD = int.Parse(leftSPD.text);

        int rHP = int.Parse(rightHP.text) * 2;
        int rATK = int.Parse(rightATK.text);
        int rSPD = int.Parse(rightSPD.text);

        AddLog("[Battle Start]");
        AddLog($"Left HP:{lHP}  Right HP:{rHP}");

        yield return new WaitForSeconds(0.8f);

        bool leftFirst;
        if (lSPD > rSPD) leftFirst = true;
        else if (rSPD > lSPD) leftFirst = false;
        else leftFirst = (Random.value < 0.5f);

        while (true)
        {
            if (leftFirst)
            {
                rHP = DoAttack("Left", "Right", rHP, lATK, lSPD, rSPD);
                yield return new WaitForSeconds(1.2f);
                if (rHP <= 0)
                {
                    AddLog("[Result] Left Win");
                    resultText.text = "Left Win";
                    yield break;
                }

                lHP = DoAttack("Right", "Left", lHP, rATK, rSPD, lSPD);
                yield return new WaitForSeconds(1.2f);
                if (lHP <= 0)
                {
                    AddLog("[Result] Right Win");
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
                    AddLog("[Result] Right Win");
                    resultText.text = "Right Win";
                    yield break;
                }

                rHP = DoAttack("Left", "Right", rHP, lATK, lSPD, rSPD);
                yield return new WaitForSeconds(1.2f);
                if (rHP <= 0)
                {
                    AddLog("[Result] Left Win");
                    resultText.text = "Left Win";
                    yield break;
                }
            }

            AddLog("---- Next Turn ----");
            yield return new WaitForSeconds(0.8f);
        }
    }

    int DoAttack(string attacker, string defender, int defHP, int atk, int spdA, int spdD)
    {
        float dmg = atk;

        bool crit = Random.value < 0.25f;
        if (crit)
        {
            dmg *= 1.2f;
        }
     

        float evadeChance = GetEvadeChance(spdA - spdD);
        bool evaded = Random.value < evadeChance;

        bool defended = Random.value < 0.25f;
        if (defended) dmg *= 0.5f;

        int finalDamage = evaded ? 0 : Mathf.RoundToInt(dmg);

        defHP -= finalDamage;
        if (defHP < 0) defHP = 0;

        AddLog($"{attacker} attacks {defender} → {finalDamage} dmg"
            + (evaded ? " (Evaded!)" : "")
            + (crit ? " (Critical!)" : "")
            + (defended ? " (Defended!)" : ""));

        AddLog($"{defender} HP after: {defHP}");

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

        // 引く距離（外側へ）
        Vector2 leftFar = leftStartPos + new Vector2(-pullDistance, 0f);
        Vector2 rightFar = rightStartPos + new Vector2(pullDistance, 0f);

        // ぶつかる距離（中央付近）
        Vector2 leftCenter = new Vector2(centerX - collideDistance, leftStartPos.y);
        Vector2 rightCenter = new Vector2(centerX + collideDistance, rightStartPos.y);

        float t = 0f;

        // 一旦外側へ引く
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            leftCard.anchoredPosition = Vector2.Lerp(leftCard.anchoredPosition, leftFar, t);
            rightCard.anchoredPosition = Vector2.Lerp(rightCard.anchoredPosition, rightFar, t);
            yield return null;
        }

        t = 0f;

        // 中央へ戻る（ぶつかる位置）
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

        GameObject fx = Instantiate(clashEffectPrefab, effectParent, true);
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

    void AddLog(string log)
    {
        battleDescription.text += "\n" + log;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    void SetRandomStats()
    {
        // 左右のステータスを即時更新
        SetRandomSide(leftHP, leftATK, leftSPD);
        SetRandomSide(rightHP, rightATK, rightSPD);

        // UIを即時リフレッシュ
        leftHP.ForceLabelUpdate();
        leftATK.ForceLabelUpdate();
        leftSPD.ForceLabelUpdate();
        rightHP.ForceLabelUpdate();
        rightATK.ForceLabelUpdate();
        rightSPD.ForceLabelUpdate();
    }

    void SetRandomSide(TMP_InputField hpField, TMP_InputField atkField, TMP_InputField spdField)
    {
        int hp = Random.Range(20, 60); // HPは安定範囲
        int atk = Random.Range(10, 100 - hp); // Attackは残り範囲
        int spd = 100 - hp - atk; // Speedは残り

        hpField.text = hp.ToString();
        atkField.text = atk.ToString();
        spdField.text = spd.ToString();
    }
}
