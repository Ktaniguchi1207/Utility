using System;
using UnityEngine;

/// <summary> デザイン調整が必要なパラメータであることを示す属性。Inspectorで緑色にハイライトされる </summary>
[AttributeUsage(AttributeTargets.Field)]
public class DesignFitterAttribute : PropertyAttribute {
    public string Label;
    /// <param name="label">Inspectorに表示するラベル名</param>
    public DesignFitterAttribute(string label) { Label = label; }
}

/// <summary> Inspectorでの編集を禁止し、値の確認のみ可能にする属性 </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ReadOnlyAttribute : PropertyAttribute { }

/// <summary> フィールドの横にボタンを配置し、指定したメソッドを実行できるようにする属性 </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InlineButtonAttribute : PropertyAttribute {
    public string ButtonLabel;
    public string MethodName;
    /// <param name="buttonLabel">ボタンに表示するテキスト</param>
    /// <param name="methodName">ボタン押下時に実行する同クラス内のメソッド名</param>
    public InlineButtonAttribute(string buttonLabel, string methodName) {
        ButtonLabel = buttonLabel;
        MethodName = methodName;
    }
}

/// <summary> Inspectorにメソッド実行用のボタンを独立した1行として表示する属性。メソッドに付与する </summary>
/// <summary> フィールドを非表示にし、1行まるごとボタンとして表示する属性。押下時に指定メソッドを実行する </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ButtonAttribute : PropertyAttribute {
    public string ButtonLabel;
    public string MethodName;
    /// <param name="buttonLabel">ボタンに表示するテキスト</param>
    /// <param name="methodName">ボタン押下時に実行する同クラス内のメソッド名</param>
    public ButtonAttribute(string buttonLabel, string methodName) {
        ButtonLabel = buttonLabel;
        MethodName = methodName;
    }
}

/// <summary> 参照や文字列が未設定の場合に警告を表示する属性 </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RequiredAttribute : PropertyAttribute {
    public string Message;
    /// <param name="message">未設定時にInspectorへ表示する警告メッセージ</param>
    public RequiredAttribute(string message = "この参照は必須です") { Message = message; }
}

/// <summary> Build Settingsに登録済みのシーン名をドロップダウンで選択できるようにする属性。string型フィールド専用 </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SceneNameAttribute : PropertyAttribute { }

/// <summary> 指定したbool等のフィールドの値に応じて表示・非表示を切り替える属性 </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ConditionalHideAttribute : PropertyAttribute {
    public string ConditionFieldName;
    public bool Invert;
    /// <param name="conditionFieldName">表示条件として参照する同クラス内のフィールド名</param>
    /// <param name="invert">trueにすると条件を反転し、条件が偽のときに表示する</param>
    public ConditionalHideAttribute(string conditionFieldName, bool invert = false) {
        ConditionFieldName = conditionFieldName;
        Invert = invert;
    }
}

/// <summary> floatまたはintフィールドにMinMaxスライダーを表示し、指定範囲内で値を制限する属性 </summary>
[AttributeUsage(AttributeTargets.Field)]
public class MinMaxRangeAttribute : PropertyAttribute {
    public float Min;
    public float Max;
    /// <param name="min">スライダーの最小値</param>
    /// <param name="max">スライダーの最大値</param>
    public MinMaxRangeAttribute(float min, float max) {
        Min = min;
        Max = max;
    }
}

/// <summary> float型の最小値・最大値ペア。MinMaxRangeAttributeと組み合わせて使用する。暗黙的にfloatへ変換するとminを返す </summary>
[Serializable]
public struct RangeFloat {
    public float min;
    public float max;

    public RangeFloat(float min, float max) {
        this.min = min;
        this.max = max;
    }
    /// <summary> 設定したminからmaxまでの値をランダムに返す </summary>
    public float Random() => UnityEngine.Random.Range(min, max);
    public static implicit operator float(RangeFloat r) => r.min;
}

/// <summary> int型の最小値・最大値ペア。MinMaxRangeAttributeと組み合わせて使用する。暗黙的にintへ変換するとminを返す </summary>
[Serializable]
public struct RangeInt {
    public int min;
    public int max;

    public RangeInt(int min, int max) {
        this.min = min;
        this.max = max;
    }

    public int Random() => UnityEngine.Random.Range(min, max + 1);
    public static implicit operator int(RangeInt r) => r.min;
}