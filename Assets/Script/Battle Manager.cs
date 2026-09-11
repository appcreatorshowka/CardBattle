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
        leftHP.text = "30"; leftATK.text = "40"; leftSPD.text = "30"; // 左のカードの初期値
        rightHP.text = "30"; rightATK.text = "40"; rightSPD.text = "30"; // 右のカードの初期値

        leftStartPos = leftCard.anchoredPosition; // 左カードの初期位置を保存
        rightStartPos = rightCard.anchoredPosition; // 右カードの初期位置を保存

        battleButton.onClick.AddListener(() => StartCoroutine(BattleSequence())); // バトル開始ボタンにイベント登録

        randomButton.onClick.AddListener(SetRandomStats); // ランダムステータス生成ボタンにイベント登録
    }

    bool IsValidInput() // 入力が正しいかどうかをチェックするメソッド
    {
        if (leftHP.text == "" || leftATK.text == "" || leftSPD.text == "" ||
            rightHP.text == "" || rightATK.text == "" || rightSPD.text == "") // いずれかの入力が空の場合
        {
            resultText.text = "Input is missing."; // 結果表示にエラーメッセージを表示
            return false; // 入力が不正なのでfalseを返す
        }

        int lHP = int.Parse(leftHP.text); // 左側のHPを整数に変換 InputFieldのtextはstring型なのでintに変換する必要がある
        int lATK = int.Parse(leftATK.text); // 左側のATKを整数に変換
        int lSPD = int.Parse(leftSPD.text); // 左側のSPDを整数に変換

        int rHP = int.Parse(rightHP.text); // 右側のHPを整数に変換
        int rATK = int.Parse(rightATK.text); // 右側のATKを整数に変換
        int rSPD = int.Parse(rightSPD.text); // 右側のSPDを整数に変換

        if (lHP + lATK + lSPD != 100 || rHP + rATK + rSPD != 100) // 左右のステータスの合計が100でない場合
        {
            resultText.text = "The total status must be 100."; // 結果表示にエラーメッセージを表示
            return false; // 入力が不正なのでfalseを返す
        }

        return true; // 入力が正しいのでtrueを返す
    }

    IEnumerator BattleSequence() // バトルの一連の流れをコルーチンで実行（Voidにしていないのがポイント）
    {
        resultText.text = ""; // 結果表示をクリア（前回の分をクリア）
        battleDescription.text = ""; // バトル詳細ログをクリア（前回の分をクリア）

        if (!IsValidInput()) yield break; // 入力が不正な場合は処理を中断

        yield return StartCoroutine(MoveCardsFancy()); // カードの移動アニメーションを実行
        PlayEffect(); // エフェクトを再生
        StartCoroutine(ShakeUI(shakeArea)); // UIを揺らすコルーチンを開始
        yield return new WaitForSeconds(1f); // エフェクトと揺れが終わるまで待機

        yield return StartCoroutine(RunBattleStepByStep()); // バトルをステップごとに実行

        ResetCards(); // カードの位置をリセット
    }

    IEnumerator RunBattleStepByStep() // バトルをステップごとに実行するコルーチン
    {
        int lHP = int.Parse(leftHP.text) * 2; // 左側のHPを整数に変換し、2倍する（バトル中のHPとして使用）
        int lATK = int.Parse(leftATK.text); // 左側のATKを整数に変換
        int lSPD = int.Parse(leftSPD.text); // 左側のSPDを整数に変換

        int rHP = int.Parse(rightHP.text) * 2; // 右側のHPを整数に変換し、2倍する（バトル中のHPとして使用）
        int rATK = int.Parse(rightATK.text); // 右側のATKを整数に変換
        int rSPD = int.Parse(rightSPD.text); // 右側のSPDを整数に変換

        AddLog("[Battle Start]"); // バトル開始のログを追加
        AddLog($"Left HP:{lHP}  Right HP:{rHP}"); // 左右のHPをログに追加

        yield return new WaitForSeconds(0.8f); // 少し待機してからバトル開始

        // SPDが高い方が先攻
        bool leftTurn; // 左側のターンかどうかを示すフラグ

        if (lSPD > rSPD) leftTurn = true; // 左側が先攻
        else if (rSPD > lSPD) leftTurn = false; // 右側が先攻
        else leftTurn = Random.value < 0.5f; // SPDが同じ場合はランダムで先攻を決定

        // バトル開始
        while (true) // 無限ループでバトルを続ける
        {
            if (leftTurn) // 左側のターンの場合
            {
                // 左が右を攻撃
                rHP = DoAttack("Left", "Right", rHP, lATK, lSPD, rSPD);

                if (rHP <= 0) // 右側のHPが0以下になった場合
                {
                    AddLog("[Result] Left Win"); // 左側の勝利ログを追加
                    resultText.text = "Left Win"; // 結果表示に左側の勝利を表示
                    yield break; // バトル終了
                }
            }
            else
            {
                // 右が左を攻撃
                lHP = DoAttack("Right", "Left", lHP, rATK, rSPD, lSPD); // 右側が左側を攻撃

                if (lHP <= 0) // 左側のHPが0以下になった場合
                {
                    AddLog("[Result] Right Win"); // 右側の勝利ログを追加
                    resultText.text = "Right Win"; // 結果表示に右側の勝利を表示
                    yield break; // バトル終了
                }
            }

            // 攻撃後の待ち時間
            yield return new WaitForSeconds(1.2f);

            // 攻撃する側を交代
            leftTurn = !leftTurn;

            AddLog("---- Next Turn ----"); // 次のターンのログを追加
            yield return new WaitForSeconds(0.8f); // 次のターンまで少し待機
        }
    }

    int DoAttack(string attacker, string defender, int defHP, int atk, int spdA, int spdD) // 攻撃処理を行うメソッド
    {
        float dmg = atk; // 基本ダメージは攻撃力と同じ

        bool crit = Random.value < 0.25f; // 25%の確率でクリティカルヒット
        if (crit)
        {
            dmg *= 1.2f; // クリティカルヒットの場合はダメージを1.2倍にする
        }
     
        float evadeChance = GetEvadeChance(spdA - spdD); // SPD差に応じて回避率を計算
        bool evaded = Random.value < evadeChance; // 回避判定を行う

        bool defended = Random.value < 0.25f; // 25%の確率で防御成功
        if (defended) dmg *= 0.5f; // 防御成功の場合はダメージを半減

        int finalDamage = evaded ? 0 : Mathf.RoundToInt(dmg); // 回避された場合はダメージを0にする

        defHP -= finalDamage; // 防御側のHPからダメージを引く
        if (defHP < 0) defHP = 0; // HPが0未満にならないようにする

        AddLog($"{attacker} attacks {defender} → {finalDamage} dmg" 
            + (evaded ? " (Evaded!)" : "")
            + (crit ? " (Critical!)" : "")
            + (defended ? " (Defended!)" : "")); // 攻撃ログを追加

        AddLog($"{defender} HP after: {defHP}"); // 防御側のHPログを追加

        return defHP; // 攻撃後の防御側のHPを返す
    }

    float GetEvadeChance(int spdDiff) // SPD差に応じて回避率を計算するメソッド
    {
        if (spdDiff >= 20) return 0.50f; // SPD差が20以上の場合は50%の回避率
        if (spdDiff >= 10) return 0.25f; // SPD差が10以上の場合は25%の回避率
        return 0f; // SPD差が10未満の場合は回避率0%
    }

    IEnumerator MoveCardsFancy() // カードの移動アニメーションを行うコルーチン
    {
        float centerX = 0f; // 中央のX座標（0に設定）

        // 引く距離（外側へ）
        Vector2 leftFar = leftStartPos + new Vector2(-pullDistance, 0f); // 左カードの外側への位置
        Vector2 rightFar = rightStartPos + new Vector2(pullDistance, 0f); // 右カードの外側への位置

        // ぶつかる距離（中央付近）
        Vector2 leftCenter = new Vector2(centerX - collideDistance, leftStartPos.y); // 左カードのぶつかる位置
        Vector2 rightCenter = new Vector2(centerX + collideDistance, rightStartPos.y); // 右カードのぶつかる位置

        float t = 0f; // 補間用のパラメータ

        // 一旦外側へ引く
        while (t < 1f) // tが1未満の間ループ
        {
            t += Time.deltaTime * 2f;
            leftCard.anchoredPosition = Vector2.Lerp(leftCard.anchoredPosition, leftFar, t); // 左カードを外側へ移動
            rightCard.anchoredPosition = Vector2.Lerp(rightCard.anchoredPosition, rightFar, t); // 右カードを外側へ移動
            yield return null; // 1フレーム待機
        }

        t = 0f; // 補間用のパラメータをリセット

        // 中央へ戻る（ぶつかる位置）
        while (t < 1f) // tが1未満の間ループ
        {
            t += Time.deltaTime * 3f; // ぶつかる位置への移動は少し速くする
            leftCard.anchoredPosition = Vector2.Lerp(leftCard.anchoredPosition, leftCenter, t); // 左カードを中央へ移動
            rightCard.anchoredPosition = Vector2.Lerp(rightCard.anchoredPosition, rightCenter, t); // 右カードを中央へ移動
            yield return null; // 1フレーム待機
        }
    }

    void PlayEffect() // エフェクトを再生するメソッド
    {
        if (clashEffectPrefab == null) return; // エフェクトのプレハブが設定されていない場合は処理を中断

        GameObject fx = Instantiate(clashEffectPrefab, effectParent, true); // エフェクトのプレハブを生成し、effectParentの子として配置
        fx.transform.localPosition = Vector3.zero; // エフェクトの位置を親の中心に設定

        var psRenderer = fx.GetComponent<ParticleSystemRenderer>(); // ParticleSystemRendererを取得
        if (psRenderer != null) // ParticleSystemRendererが存在する場合
        {
            psRenderer.sortingLayerName = "UI"; // UIレイヤーに設定
            psRenderer.sortingOrder = 200; // UIの上に表示されるようにソート順を設定
        }

        Destroy(fx, 2f); // エフェクトを2秒後に破棄してメモリを解放
    }

    IEnumerator ShakeUI(RectTransform target) // UIを揺らすコルーチン
    {
        Vector3 origin = target.anchoredPosition; // 元の位置を保存

        for (int i = 0; i < 10; i++) // 10回揺らす
        {
            target.anchoredPosition = origin + (Vector3)Random.insideUnitCircle * 10f; // ランダムな方向に10ピクセル以内で揺らす
            yield return new WaitForSeconds(0.02f); // 20ミリ秒待機
        }

        target.anchoredPosition = origin; // 元の位置に戻す
    }

    void ResetCards() // カードの位置をリセットするメソッド
    {
        leftCard.anchoredPosition = leftStartPos; // 左カードの位置を初期位置に戻す
        rightCard.anchoredPosition = rightStartPos; // 右カードの位置を初期位置に戻す
    }

    void AddLog(string log) // バトルログを追加するメソッド
    {
        battleDescription.text += "\n" + log; // バトル詳細ログに新しいログを追加

        Canvas.ForceUpdateCanvases(); // UIの更新を強制的に行う（スクロールビューの内容が更新されるようにする）
        scrollRect.verticalNormalizedPosition = 0f; // スクロールビューを一番下にスクロールして最新のログが見えるようにする
    }

    void SetRandomStats() // ランダムステータスを設定するメソッド
    {
        // 左右のステータスを即時更新
        SetRandomSide(leftHP, leftATK, leftSPD); // 左側のステータスをランダムに設定
        SetRandomSide(rightHP, rightATK, rightSPD); // 右側のステータスをランダムに設定

        // UIを即時リフレッシュ
        leftHP.ForceLabelUpdate(); // 入力フィールドのラベルを強制的に更新
        leftATK.ForceLabelUpdate(); 
        leftSPD.ForceLabelUpdate();
        rightHP.ForceLabelUpdate();
        rightATK.ForceLabelUpdate();
        rightSPD.ForceLabelUpdate();
    }

    void SetRandomSide(TMP_InputField hpField, TMP_InputField atkField, TMP_InputField spdField) // ランダムステータスを設定するメソッド（1サイド分）
    {
        int hp = Random.Range(20, 60); // HPは20～60の範囲でランダム
        int atk = Random.Range(10, 100 - hp); // ATKは残りの範囲でランダム
        int spd = 100 - hp - atk; // SPDは残りの値で決定（合計が100になるように）

        hpField.text = hp.ToString(); // 入力フィールドにHPを設定
        atkField.text = atk.ToString(); // 入力フィールドにATKを設定
        spdField.text = spd.ToString(); // 入力フィールドにSPDを設定
    }
}
