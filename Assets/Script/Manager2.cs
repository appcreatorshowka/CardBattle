using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class BattleCharacter // バトルキャラクターのステータスを保持するクラス
{ 
    public string name; // キャラクター名
    public int hp; // ヒットポイント
    public int atk; // 攻撃力
    public int spd; // 速度

    public BattleCharacter(string name, int hp, int atk, int spd) // コンストラクタ
    {
        this.name = name; // キャラクター名を設定
        this.hp = hp; // ヒットポイントを設定
        this.atk = atk; // 攻撃力を設定
        this.spd = spd; // 速度を設定
    }
}

public class AttackResult // 攻撃結果を保持するクラス
{
    public int damage; // ダメージ量
    public bool isCritical; // クリティカルヒットかどうか
    public bool isEvaded; // 回避されたかどうか
    public bool isDefended; // 防御されたかどうか
}

public class Manager2 : MonoBehaviour 
    {
    [Header("Input Fields")]
    public TMP_InputField leftHP; // 左側のHP入力欄
    public TMP_InputField leftATK; // 左側の攻撃力入力欄
    public TMP_InputField leftSPD; // 左側の速度入力欄

    public TMP_InputField rightHP; // 右側のHP入力欄
    public TMP_InputField rightATK; // 右側の攻撃力入力欄
    public TMP_InputField rightSPD; // 右側の速度入力欄

    [Header("Cards")]
    public RectTransform leftCard; // 左側のカードのRectTransform
    public RectTransform rightCard; // 右側のカードのRectTransform

    [Header("Character Images")]
    [SerializeField] private Image leftCharacterImage;
    [SerializeField] private Image rightCharacterImage;

    [Header("Effect")]
    public GameObject clashEffectPrefab; // 衝突エフェクト
    public GameObject smokeEffectPrefab; // 回避エフェクト
    public GameObject slashEffectPrefab; // クリティカルエフェクト
    public GameObject defendEffectPrefab; // 防御エフェクト

    public RectTransform effectParent; // 中央エフェクトの親

    [Header("Character Effect Positions")]
    public RectTransform leftEffectPosition; // 左側キャラのエフェクト位置
    public RectTransform rightEffectPosition; // 右側キャラのエフェクト位置

    [Header("Shake Target")]
    public RectTransform shakeArea; // 揺らす対象のRectTransform

    [Header("UI")]
    public TMP_Text resultText; // バトル結果表示用のテキスト
    public TMP_Text battleDescription; // バトルログ表示用のテキスト
    public Button battleButton; // バトル開始ボタン
    public Button restartButton; // バトルリスタートボタン

    public Slider leftHPBar; // 左側のHPバー
    public Slider rightHPBar; // 右側のHPバー

    [Header("Scroll")]
    public ScrollRect scrollRect; // バトルログのスクロールビュー

    [Header("Random")]
    public Button randomButton; // ランダムステータス設定ボタン

    [Header("Card Movement")]
    public float pullDistance = 100f; // カードを外側に引く距離
    public float collideDistance = 250f; // カードが中央で衝突する距離

    [Header("Battle")]
    public float logWaitTime = 0.8f; // バトルログの表示間隔

    private Vector2 leftStartPos; // 左側のカードの初期位置
    private Vector2 rightStartPos; // 右側のカードの初期位置

    private bool isStart; // バトルが開始されたかどうかのフラグ
    [SerializeField] private CanvasGroup cg; // UIの操作を制御するCanvasGroup

    private void Start() 
    {
        SetInitialStats(); // 初期ステータスを設定する

        leftStartPos = leftCard.anchoredPosition; // 左側のカードの初期位置を保存
        rightStartPos = rightCard.anchoredPosition; // 右側のカードの初期位置を保存

        battleButton.onClick.AddListener(StartBattle); // バトル開始ボタンにStartBattleメソッドを登録
        randomButton.onClick.AddListener(SetRandomStats); // ランダムステータス設定ボタンにSetRandomStatsメソッドを登録

        restartButton.gameObject.SetActive(false); // 初期は非表示
        restartButton.onClick.AddListener(RestartBattle); // Restart処理登録

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

        leftHPBar.maxValue = leftCharacter.hp; // 左側のHPバーの最大値を設定
        leftHPBar.value = leftCharacter.hp; // 左側のHPバーの現在値を設定

        rightHPBar.maxValue = rightCharacter.hp; // 右側のHPバーの最大値を設定
        rightHPBar.value = rightCharacter.hp; // 右側のHPバーの現在値を設定

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
                cg.blocksRaycasts = true; // UIの操作を再び可能にする
                cg.interactable = true; // UIの操作を再び可能にする
                restartButton.gameObject.SetActive(true); // Restartボタンを表示
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

    /// 攻撃を実行する
    private IEnumerator DoAttack(BattleCharacter attacker, BattleCharacter defender) {
        AttackResult attackResult = CalculateAttack(attacker, defender);

        // 攻撃開始
        AddLog($"{attacker.name} attacks {defender.name}!");

        yield return new WaitForSeconds(logWaitTime);

        // 回避
        if (attackResult.isEvaded) {
            AddLog($"{defender.name} evaded!");

            // 防御側にスモークエフェクト
            PlayCharacterEffect(defender, smokeEffectPrefab);

            yield return new WaitForSeconds(logWaitTime);
            yield break;
        }

        // クリティカル
        if (attackResult.isCritical) {
            AddLog("Critical hit!");

            Image attackerImage =

            // 攻撃側のキャラクター画像を取得
            attacker.name == "Left"
            ? leftCharacterImage
            : rightCharacterImage;
            StartCoroutine(CriticalGlow(attackerImage));

            // 防御側にスラッシュエフェクト
            PlayCharacterEffect(defender, slashEffectPrefab);

            yield return new WaitForSeconds(logWaitTime);
        }

        // 防御
        if (attackResult.isDefended) {
            AddLog($"{defender.name} defended!");

            // 防御側に防御エフェクト
            PlayCharacterEffect(defender, defendEffectPrefab);

            yield return new WaitForSeconds(logWaitTime);
        }

        // HPを減らす
        defender.hp -= attackResult.damage;
        defender.hp = Mathf.Max(defender.hp, 0);

        if (defender.name == "Left") // 左側のキャラクターの場合
        {
            leftHPBar.value = defender.hp; // 左側のHPバーの値を更新
        }
        else
        {
            rightHPBar.value = defender.hp; // 右側のHPバーの値を更新
        }

        // 画面を揺らす
        // StartCoroutine(ShakeUI(shakeArea));
        // PlayEffect();

        // ダメージ
        AddLog($"{defender.name} took {attackResult.damage} damage!");

        yield return new WaitForSeconds(logWaitTime);

        // 残りHP
        AddLog($"{defender.name} HP: {defender.hp}");

        yield return new WaitForSeconds(logWaitTime);
    }

    private IEnumerator CriticalGlow(Image target) // クリティカル時の光るエフェクト
    {   
    Color original = target.color;
    Color gold = new Color(1f, 0.85f, 0f);
    for (int i = 0; i< 3; i++)
        {
        target.color = gold;
        yield return new WaitForSeconds(0.08f);
        target.color = original;
        yield return new WaitForSeconds(0.08f);
        }
    }

    /// 攻撃結果を計算する
    private AttackResult CalculateAttack(BattleCharacter attacker, BattleCharacter defender) {
        AttackResult result = new();

        float damage = attacker.atk;

        // クリティカル判定
        result.isCritical = Random.value < 1.0f;

        if (result.isCritical) {
            damage *= 1.2f;
        }

        // 回避判定
        float evadeChance = GetEvadeChance(defender.spd - attacker.spd);

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
            return 0.40f;
        }

        if (spdDiff >= 10) {
            return 0.20f;
        }

        return 0.1f;
    }

    /// カードを外側に引いてから中央へ移動する
    private IEnumerator MoveCardsFancy() {
        float centerX = 0f;

        Vector2 leftFar = leftStartPos + new Vector2(-pullDistance, 0f);
        Vector2 rightFar = rightStartPos + new Vector2(pullDistance, 0f);

        Vector2 leftCenter = new Vector2(centerX - collideDistance, leftStartPos.y);
        Vector2 rightCenter = new Vector2(centerX + collideDistance, rightStartPos.y);

        yield return StartCoroutine(MoveCards(leftFar, rightFar, 2f));
        yield return StartCoroutine(MoveCards(leftCenter, rightCenter, 3f));
    }

    /// 左右のカードを指定位置へ移動する
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

    /// 衝突エフェクトを再生する
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

    /// UIを揺らす
    private IEnumerator ShakeUI(
        RectTransform target) {
        Vector3 origin = target.anchoredPosition;

        for (int i = 0; i < 10; i++) {
            target.anchoredPosition = origin + (Vector3)Random.insideUnitCircle * 10f;
            yield return new WaitForSeconds(0.02f);
        }

        target.anchoredPosition = origin;
    }

    /// キャラクター側のエフェクトを再生する
    private void PlayCharacterEffect(
        BattleCharacter defender,
        GameObject effectPrefab)
    {
        if (effectPrefab == null)
        {
            return;
        }

        RectTransform effectPosition;

        if (defender.name == "Left")
        {
            effectPosition = leftEffectPosition;
        }
        else
        {
            effectPosition = rightEffectPosition;
        }

        // 衝突エフェクトと同じ方法で生成
        GameObject fx = Instantiate(
            effectPrefab,
            effectPosition,
            true
        );

        // エフェクトのワールド座標を位置指定オブジェクトに合わせる
        fx.transform.position = effectPosition.position;

        // 子オブジェクトも含めてRendererを探す
        ParticleSystemRenderer psRenderer =
            fx.GetComponentInChildren<ParticleSystemRenderer>();

        if (psRenderer != null)
        {
            psRenderer.sortingLayerName = "UI";
            psRenderer.sortingOrder = 200;
        }

        // 1.5秒後に削除
        Destroy(fx, 1.5f);
    }

    /// <summary>
    /// カードを初期位置に戻す
    /// </summary>
    private void ResetCards() // カードを初期位置に戻す
    {
        leftCard.anchoredPosition = leftStartPos; // 左側のカードを初期位置に戻す
        rightCard.anchoredPosition = rightStartPos; // 右側のカードを初期位置に戻す
    }

    /// <summary>
    /// バトルログを1行追加する
    /// </summary>
    private void AddLog(string log) // バトルログを1行追加する 
    {
        battleDescription.text += "\n" + log; // バトルログに1行追加する

        Canvas.ForceUpdateCanvases(); // レイアウトを強制的に更新する

        scrollRect.verticalNormalizedPosition = 0f; // スクロールを一番下に移動する
    }

    /// <summary>
    /// 左右のステータスをランダムに設定する
    /// </summary>
    private void SetRandomStats() // HP + ATK + SPD = 100になるようにランダムに設定
    {
        if (isStart) // バトル中はランダム設定を無効化
        {
            return; // バトル中はランダム設定を無効化
        }

        SetRandomSide(leftHP, leftATK, leftSPD); // 左側のステータスをランダムに設定
        SetRandomSide(rightHP, rightATK, rightSPD); // 右側のステータスをランダムに設定

        RefreshInputFields(); // InputFieldの表示を強制的に更新
    }

    /// <summary>
    /// 1キャラクター分のランダムステータスを設定する
    /// </summary>
    private void SetRandomSide(TMP_InputField hpField, TMP_InputField atkField, TMP_InputField spdField) // HP + ATK + SPD = 100になるようにランダムに設定
    {
        int hp = Random.Range(20, 60); // HPは20～60の範囲でランダムに設定
        int atk = Random.Range(10, 100 - hp - 10); // ATKは10～(100 - HP - 10)の範囲でランダムに設定
        int spd = 100 - hp - atk; // SPDは残りの値で設定

        SetStats(hpField, atkField, spdField, hp, atk, spd); // InputFieldに設定
    }

    /// <summary>
    /// InputFieldの表示を更新する
    /// </summary>
    private void RefreshInputFields() // InputFieldの表示を強制的に更新する
    {
        leftHP.ForceLabelUpdate(); // InputFieldの表示を強制的に更新
        leftATK.ForceLabelUpdate(); // InputFieldの表示を強制的に更新
        leftSPD.ForceLabelUpdate(); // InputFieldの表示を強制的に更新

        rightHP.ForceLabelUpdate(); // InputFieldの表示を強制的に更新
        rightATK.ForceLabelUpdate(); // InputFieldの表示を強制的に更新
        rightSPD.ForceLabelUpdate(); // InputFieldの表示を強制的に更新
    }

    private void RestartBattle() // バトルをリスタートする
    {
        // UI操作を再び可能にする
        cg.blocksRaycasts = true;
        cg.interactable = true;

        // 結果表示をクリア
        resultText.text = "";
        battleDescription.text = "";

        // カード位置を初期化
        ResetCards();

        // ★ 初期ステータスに戻す
        SetInitialStats();

        leftHPBar.value = 0; // 左側のHPバーを初期化
        rightHPBar.value = 0; // 右側のHPバーを初期化

        // ★ Battleボタンを再び押せるようにする
        battleButton.gameObject.SetActive(true);

        // ★ Restartボタンを消す
        restartButton.gameObject.SetActive(false);
    }

}