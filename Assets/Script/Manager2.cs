using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleCharacter {
    public string name;
    public int hp;
    public int atk;
    public int spd;

    public BattleCharacter(string name, int hp, int atk, int spd) {
        this.name = name;
        this.hp = hp;
        this.atk = atk;
        this.spd = spd;
    }
}


public class AttackResult {
    public int damage;
    public bool isCritical;
    public bool isEvaded;
    public bool isDefended;
}


public class Manager2 : MonoBehaviour {
    [Header("Input Fields")]
    public TMP_InputField leftHP;
    public TMP_InputField leftATK;
    public TMP_InputField leftSPD;

    public TMP_InputField rightHP;
    public TMP_InputField rightATK;
    public TMP_InputField rightSPD;

    [Header("Cards")]
    public RectTransform leftCard;
    public RectTransform rightCard;

    [Header("Effect")]
    public GameObject clashEffectPrefab;
    public RectTransform effectParent;

    [Header("Shake Target")]
    public RectTransform shakeArea;

    [Header("UI")]
    public TMP_Text resultText;
    public TMP_Text battleDescription;
    public Button battleButton;

    [Header("Scroll")]
    public ScrollRect scrollRect;

    [Header("Random")]
    public Button randomButton;

    [Header("Card Movement")]
    public float pullDistance = 100f;
    public float collideDistance = 250f;

    [Header("Battle")]
    public float logWaitTime = 0.8f;

    private Vector2 leftStartPos;
    private Vector2 rightStartPos;

    private bool isStart;
    [SerializeField] private CanvasGroup cg;

    private void Start() {
        SetInitialStats();

        leftStartPos = leftCard.anchoredPosition;
        rightStartPos = rightCard.anchoredPosition;

        battleButton.onClick.AddListener(StartBattle);
        randomButton.onClick.AddListener(SetRandomStats);
    }

    /// <summary>
    /// 初期ステータスを設定する
    /// </summary>
    private void SetInitialStats() {
        SetStats(leftHP, leftATK, leftSPD, 30, 40, 30);
        SetStats(rightHP, rightATK, rightSPD, 30, 40, 30);
    }

    /// <summary>
    /// InputFieldにステータスを設定する
    /// </summary>
    private void SetStats(TMP_InputField hpField, TMP_InputField atkField, TMP_InputField spdField, int hp, int atk, int spd) {
        hpField.text = hp.ToString();
        atkField.text = atk.ToString();
        spdField.text = spd.ToString();
    }

    /// <summary>
    /// バトルを開始する
    /// </summary>
    private void StartBattle() {
        if (isStart) {
            return;
        }
        //isStart = true;

        if (!IsValidInput()) {
            return;
        }

        cg.blocksRaycasts = false;
        cg.interactable = false;

        StartCoroutine(BattleSequence());
    }

    /// <summary>
    /// バトル全体の流れ
    /// </summary>
    private IEnumerator BattleSequence() {
        ClearBattleResult();

        //if (!IsValidInput()) {
        //    yield break;
        //}

        yield return StartCoroutine(MoveCardsFancy());

        PlayEffect();

        StartCoroutine(ShakeUI(shakeArea));

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(RunBattleStepByStep());

        ResetCards();
    }

    /// <summary>
    /// バトル結果とログをクリアする
    /// </summary>
    private void ClearBattleResult() {
        resultText.text = "";
        battleDescription.text = "";
    }

    /// <summary>
    /// 入力値が正しいか確認する
    /// </summary>
    private bool IsValidInput() {
        if (IsAnyInputEmpty()) {
            resultText.text = "Input is missing.";
            return false;
        }

        if (!TryGetStats(leftHP, leftATK, leftSPD, out int leftHPValue, out int leftATKValue, out int leftSPDValue)) {
            resultText.text = "Please enter numbers.";
            return false;
        }

        if (!TryGetStats(rightHP, rightATK, rightSPD, out int rightHPValue, out int rightATKValue, out int rightSPDValue)) {
            resultText.text = "Please enter numbers.";
            return false;
        }

        if (leftHPValue + leftATKValue + leftSPDValue != 100 || rightHPValue + rightATKValue + rightSPDValue != 100) {
            resultText.text = "The total status must be 100.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 入力欄に空欄があるか確認する
    /// </summary>
    private bool IsAnyInputEmpty() {
        return string.IsNullOrEmpty(leftHP.text) ||
               string.IsNullOrEmpty(leftATK.text) ||
               string.IsNullOrEmpty(leftSPD.text) ||
               string.IsNullOrEmpty(rightHP.text) ||
               string.IsNullOrEmpty(rightATK.text) ||
               string.IsNullOrEmpty(rightSPD.text);
    }

    /// <summary>
    /// InputFieldからステータスを取得する
    /// </summary>
    private bool TryGetStats(TMP_InputField hpField, TMP_InputField atkField, TMP_InputField spdField, out int hp, out int atk, out int spd) {
        bool hpResult = int.TryParse(hpField.text, out hp);
        bool atkResult = int.TryParse(atkField.text, out atk);
        bool spdResult = int.TryParse(spdField.text, out spd);

        return hpResult && atkResult && spdResult;
    }

    /// <summary>
    /// InputFieldからバトルキャラクターを作成する
    /// </summary>
    private BattleCharacter CreateCharacter(string characterName, TMP_InputField hpField, TMP_InputField atkField, TMP_InputField spdField) {
        int hp = int.Parse(hpField.text) * 2;
        int atk = int.Parse(atkField.text);
        int spd = int.Parse(spdField.text);

        return new BattleCharacter(characterName, hp, atk, spd);
    }

    /// <summary>
    /// バトルをステップごとに実行する
    /// </summary>
    private IEnumerator RunBattleStepByStep() {
        // 双方のキャラ作成
        BattleCharacter leftCharacter = CreateCharacter("Left", leftHP, leftATK, leftSPD);
        BattleCharacter rightCharacter = CreateCharacter("Right", rightHP, rightATK, rightSPD);

        AddLog($"[Battle Start]\n");

        yield return new WaitForSeconds(logWaitTime);

        AddLog($"Left HP:{leftCharacter.hp} / Right HP:{rightCharacter.hp}");

        yield return new WaitForSeconds(logWaitTime);

        // 先行を決める
        bool leftTurn = DecideFirstTurn(leftCharacter, rightCharacter);

        AddLog(leftTurn ? "Left goes first!" : "Right goes first!");

        yield return new WaitForSeconds(logWaitTime);

        // バトル開始
        while (leftCharacter.hp > 0 && rightCharacter.hp > 0) {
            BattleCharacter attacker;
            BattleCharacter defender;

            if (leftTurn) {
                attacker = leftCharacter;
                defender = rightCharacter;
            } else {
                attacker = rightCharacter;
                defender = leftCharacter;
            }

            yield return StartCoroutine(DoAttack(attacker, defender));

            // 攻撃を受けた側のHPが0以下ならバトル終了
            if (defender.hp <= 0) {
                AddLog($"\n[Result] {attacker.name} Win\n");
                resultText.text = $"{attacker.name} Win";
                yield break;
            }

            yield return new WaitForSeconds(0.5f);

            AddLog($"\n---- Next Turn ----");

            yield return new WaitForSeconds(logWaitTime);

            leftTurn = !leftTurn;
        }
    }

    /// <summary>
    /// 先攻するキャラクターを決定する
    /// </summary>
    private bool DecideFirstTurn(BattleCharacter leftCharacter, BattleCharacter rightCharacter) {
        if (leftCharacter.spd > rightCharacter.spd) {
            return true;
        }

        if (rightCharacter.spd > leftCharacter.spd) {
            return false;
        }

        return Random.value < 0.5f;
    }

    /// <summary>
    /// 攻撃を実行する
    /// </summary>
    private IEnumerator DoAttack(BattleCharacter attacker, BattleCharacter defender) {
        AttackResult attackResult = CalculateAttack(attacker, defender);

        // 攻撃開始
        AddLog($"{attacker.name} attacks {defender.name}!");

        yield return new WaitForSeconds(logWaitTime);

        // 回避
        if (attackResult.isEvaded) {
            AddLog($"{defender.name} evaded!");
            yield return new WaitForSeconds(logWaitTime);
            yield break;
        }

        // クリティカル
        if (attackResult.isCritical) {
            AddLog("Critical hit!");
            yield return new WaitForSeconds(logWaitTime);
        }

        // 防御
        if (attackResult.isDefended) {
            AddLog($"{defender.name} defended!");
            yield return new WaitForSeconds(logWaitTime);
        }

        // HPを減らす
        defender.hp -= attackResult.damage;
        defender.hp = Mathf.Max(defender.hp, 0);

        // 画面を揺らす
        StartCoroutine(ShakeUI(shakeArea));
        PlayEffect();

        // ダメージ
        AddLog($"{defender.name} took {attackResult.damage} damage!");

        yield return new WaitForSeconds(logWaitTime);

        // 残りHP
        AddLog($"{defender.name} HP: {defender.hp}");

        yield return new WaitForSeconds(logWaitTime);
    }

    /// <summary>
    /// 攻撃結果を計算する
    /// </summary>
    private AttackResult CalculateAttack(BattleCharacter attacker, BattleCharacter defender) {
        AttackResult result = new();

        float damage = attacker.atk;

        // クリティカル判定
        result.isCritical = Random.value < 0.25f;

        if (result.isCritical) {
            damage *= 1.2f;
        }

        // 回避判定
        float evadeChance = GetEvadeChance(attacker.spd - defender.spd);

        result.isEvaded = Random.value < evadeChance;

        // 防御判定
        result.isDefended = Random.value < 0.25f;

        if (result.isDefended) {
            damage *= 0.5f;
        }

        // 最終ダメージ
        if (result.isEvaded) {
            result.damage = 0;
        } else {
            result.damage = Mathf.RoundToInt(damage);
        }

        return result;
    }

    /// <summary>
    /// SPD差から回避率を取得する
    /// </summary>
    private float GetEvadeChance(int spdDiff) {
        if (spdDiff >= 20) {
            return 0.50f;
        }

        if (spdDiff >= 10) {
            return 0.25f;
        }

        return 0f;
    }

    /// <summary>
    /// カードを外側に引いてから中央へ移動する
    /// </summary>
    private IEnumerator MoveCardsFancy() {
        float centerX = 0f;

        Vector2 leftFar = leftStartPos + new Vector2(-pullDistance, 0f);
        Vector2 rightFar = rightStartPos + new Vector2(pullDistance, 0f);

        Vector2 leftCenter = new Vector2(centerX - collideDistance, leftStartPos.y);
        Vector2 rightCenter = new Vector2(centerX + collideDistance, rightStartPos.y);

        yield return StartCoroutine(MoveCards(leftFar, rightFar, 2f));
        yield return StartCoroutine(MoveCards(leftCenter, rightCenter, 3f));
    }

    /// <summary>
    /// 左右のカードを指定位置へ移動する
    /// </summary>
    private IEnumerator MoveCards(Vector2 leftTarget, Vector2 rightTarget, float speed) {
        float t = 0f;

        while (t < 1f) {
            t += Time.deltaTime * speed;

            leftCard.anchoredPosition = Vector2.Lerp(leftCard.anchoredPosition, leftTarget, t);
            rightCard.anchoredPosition = Vector2.Lerp(rightCard.anchoredPosition, rightTarget, t);

            yield return null;
        }

        leftCard.anchoredPosition = leftTarget;
        rightCard.anchoredPosition = rightTarget;
    }

    /// <summary>
    /// 衝突エフェクトを再生する
    /// </summary>
    private void PlayEffect() {
        if (clashEffectPrefab == null) {
            return;
        }

        GameObject fx = Instantiate(clashEffectPrefab, effectParent, true);

        fx.transform.localPosition = Vector3.zero;
        
        if (fx.TryGetComponent<ParticleSystemRenderer>(out var psRenderer)) {
            psRenderer.sortingLayerName = "UI";
            psRenderer.sortingOrder = 200;
        }

        Destroy(fx, 2f);
    }

    /// <summary>
    /// UIを揺らす
    /// </summary>
    private IEnumerator ShakeUI(
        RectTransform target) {
        Vector3 origin = target.anchoredPosition;

        for (int i = 0; i < 10; i++) {
            target.anchoredPosition = origin + (Vector3)Random.insideUnitCircle * 10f;
            yield return new WaitForSeconds(0.02f);
        }

        target.anchoredPosition = origin;
    }

    /// <summary>
    /// カードを初期位置に戻す
    /// </summary>
    private void ResetCards() {
        leftCard.anchoredPosition = leftStartPos;
        rightCard.anchoredPosition = rightStartPos;
    }

    /// <summary>
    /// バトルログを1行追加する
    /// </summary>
    private void AddLog(string log) {
        battleDescription.text += "\n" + log;

        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// 左右のステータスをランダムに設定する
    /// </summary>
    private void SetRandomStats() {
        if (isStart) {
            return;
        }

        SetRandomSide(leftHP, leftATK, leftSPD);
        SetRandomSide(rightHP, rightATK, rightSPD);

        RefreshInputFields();
    }

    /// <summary>
    /// 1キャラクター分のランダムステータスを設定する
    /// </summary>
    private void SetRandomSide(TMP_InputField hpField, TMP_InputField atkField, TMP_InputField spdField) {
        int hp = Random.Range(20, 60);
        int atk = Random.Range(10, 100 - hp);
        int spd = 100 - hp - atk;

        SetStats(hpField, atkField, spdField, hp, atk, spd);
    }

    /// <summary>
    /// InputFieldの表示を更新する
    /// </summary>
    private void RefreshInputFields() {
        leftHP.ForceLabelUpdate();
        leftATK.ForceLabelUpdate();
        leftSPD.ForceLabelUpdate();

        rightHP.ForceLabelUpdate();
        rightATK.ForceLabelUpdate();
        rightSPD.ForceLabelUpdate();
    }
}