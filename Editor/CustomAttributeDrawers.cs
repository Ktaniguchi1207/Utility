#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class DesignFitterHierarchyExtension {
    static Texture _consoleIcon;

    [InitializeOnLoadMethod]
    static void Initialize() {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceId, Rect selectionRect) {
        var obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        if (obj == null) return;

        bool found = false;
        foreach (var component in obj.GetComponents<Component>()) {
            if (component is MonoBehaviour == false) continue;
            foreach (var field in component.GetType().GetFields(
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.Instance |
                         BindingFlags.FlattenHierarchy |
                         BindingFlags.Static
                     )) {
                if (field.GetCustomAttributes(typeof(DesignFitterAttribute), true).Length > 0) {
                    found = true;
                    break;
                }
            }
            if (found) break;
        }

        if (found == false) return;

        Rect bgRect = selectionRect;
        bgRect.xMin = 0;
        bgRect.xMax = selectionRect.xMax + 16;
        EditorGUI.DrawRect(bgRect, new Color(0.5f, 1f, 0.5f, 0.08f));

        if (_consoleIcon == null) {
            var content = EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow");
            if (content != null) _consoleIcon = content.image;
        }

        if (_consoleIcon != null) {
            Rect iconRect = selectionRect;
            iconRect.x = iconRect.xMax - 16;
            iconRect.width = 16;
            iconRect.height = 16;
            GUI.DrawTexture(iconRect, _consoleIcon);
        }
    }
}

[CustomPropertyDrawer(typeof(DesignFitterAttribute))]
public class DesignFitterAttributeDrawer : PropertyDrawer {
    readonly Color _textColor = new Color(0.5f, 1f, 0.5f);

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        var attr = (DesignFitterAttribute)attribute;
        return EditorGUI.GetPropertyHeight(property, new GUIContent(attr.Label), true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var attr = attribute as DesignFitterAttribute;
        if (attr == null) {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        var customLabel = new GUIContent(attr.Label);

        if (!property.hasVisibleChildren) {
            var oldColor = GUI.color;
            GUI.color = _textColor;
            EditorGUI.PropertyField(position, property, customLabel, false);
            GUI.color = oldColor;
            return;
        }

        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var prevColor = GUI.color;
        GUI.color = _textColor;
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, customLabel, true);
        GUI.color = prevColor;

        if (!property.isExpanded) return;

        EditorGUI.indentLevel++;
        float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        var child = property.Copy();
        var end = child.GetEndProperty();
        bool enterChildren = true;
        while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end)) {
            enterChildren = false;
            float childHeight = EditorGUI.GetPropertyHeight(child, null, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, childHeight), child, true);
            y += childHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        EditorGUI.indentLevel--;
    }
}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyAttributeDrawer : PropertyDrawer {
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var prev = GUI.enabled;
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = prev;
    }
}

[CustomPropertyDrawer(typeof(InlineButtonAttribute))]
public class InlineButtonAttributeDrawer : PropertyDrawer {
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var attr = attribute as InlineButtonAttribute;
        if (attr == null) {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        float buttonWidth = 70f;
        Rect fieldRect = new Rect(
            position.x,
            position.y,
            Math.Max(0, position.width - (buttonWidth + 4f)),
            position.height
        );
        Rect buttonRect = new Rect(
            position.xMax - buttonWidth,
            position.y,
            buttonWidth,
            EditorGUIUtility.singleLineHeight
        );

        EditorGUI.PropertyField(fieldRect, property, label, true);

        if (GUI.Button(buttonRect, attr.ButtonLabel)) {
            foreach (var t in property.serializedObject.targetObjects) {
                if (t == null) continue;
                var method = t.GetType().GetMethod(
                    attr.MethodName,
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static
                );
                if (method != null) {
                    method.Invoke(t, null);
                    EditorUtility.SetDirty(t);
                }
                else {
                    Debug.LogWarning($"InlineButton: Method '{attr.MethodName}' not found on {t.GetType().Name}");
                }
            }
        }
    }
}

[CustomPropertyDrawer(typeof(ButtonAttribute))]
public class ButtonAttributeDrawer : PropertyDrawer {
    const float ButtonHeight = 26f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return ButtonHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var attr = attribute as ButtonAttribute;
        if (attr == null) return;

        Rect buttonRect = new Rect(position.x, position.y, position.width, ButtonHeight);

        if (GUI.Button(buttonRect, attr.ButtonLabel)) {
            foreach (var t in property.serializedObject.targetObjects) {
                if (t == null) continue;
                var method = t.GetType().GetMethod(
                    attr.MethodName,
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static
                );
                if (method != null) {
                    method.Invoke(t, null);
                    EditorUtility.SetDirty(t);
                }
                else {
                    Debug.LogWarning($"Button: Method '{attr.MethodName}' not found on {t.GetType().Name}");
                }
            }
        }
    }
}

[CustomPropertyDrawer(typeof(RequiredAttribute))]
public class RequiredFieldAttributeDrawer : PropertyDrawer {
    readonly Color _warningColor = new Color(1f, 0.3f, 0.3f, 0.3f);
    const float WarningBoxHeight = 20f;
    const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);
        return IsMissing(property) ? baseHeight + WarningBoxHeight + Spacing : baseHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        bool missing = IsMissing(property);

        Rect fieldRect = position;
        fieldRect.height = EditorGUI.GetPropertyHeight(property, label, true);

        if (missing) {
            EditorGUI.DrawRect(fieldRect, _warningColor);
        }

        EditorGUI.PropertyField(fieldRect, property, label, true);

        if (missing) {
            var attr = (RequiredAttribute)attribute;
            Rect warningRect = position;
            warningRect.y = fieldRect.yMax + Spacing;
            warningRect.height = WarningBoxHeight;

            GUIStyle style = new GUIStyle(EditorStyles.helpBox) {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };
            EditorGUI.LabelField(warningRect, "⚠ " + attr.Message, style);
        }
    }

    bool IsMissing(SerializedProperty property) {
        switch (property.propertyType) {
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue == null;
            case SerializedPropertyType.String:
                return string.IsNullOrEmpty(property.stringValue);
            case SerializedPropertyType.ExposedReference:
                return property.exposedReferenceValue == null;
            default:
                return false;
        }
    }
}

[CustomPropertyDrawer(typeof(SceneNameAttribute))]
public class SceneNameAttributeDrawer : PropertyDrawer {
    readonly Color _warningColor = new Color(1f, 0.3f, 0.3f, 0.3f);

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (property.propertyType != SerializedPropertyType.String) {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string[] sceneNames = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => Path.GetFileNameWithoutExtension(s.path))
            .ToArray();

        if (sceneNames.Length == 0) {
            EditorGUI.DrawRect(position, _warningColor);
            EditorGUI.LabelField(position, label.text, "Build Settingsにシーンがありません");
            return;
        }

        int currentIndex = Array.IndexOf(sceneNames, property.stringValue);

        if (currentIndex < 0) {
            if (string.IsNullOrEmpty(property.stringValue) == false) {
                EditorGUI.DrawRect(position, _warningColor);
            }
            currentIndex = 0;
        }

        int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, sceneNames);
        property.stringValue = sceneNames[selectedIndex];
    }
}

[CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
public class ConditionalHideAttributeDrawer : PropertyDrawer {
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        if (ShouldShow(property)) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        return -EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (ShouldShow(property) == false) return;
        EditorGUI.PropertyField(position, property, label, true);
    }

    bool ShouldShow(SerializedProperty property) {
        var attr = (ConditionalHideAttribute)attribute;
        string conditionPath = property.propertyPath.Contains(".")
            ? property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.') + 1) + attr.ConditionFieldName
            : attr.ConditionFieldName;

        var conditionProperty = property.serializedObject.FindProperty(conditionPath);
        if (conditionProperty == null) return true;

        bool value;
        switch (conditionProperty.propertyType) {
            case SerializedPropertyType.Boolean:
                value = conditionProperty.boolValue;
                break;
            case SerializedPropertyType.ObjectReference:
                value = conditionProperty.objectReferenceValue != null;
                break;
            case SerializedPropertyType.Integer:
                value = conditionProperty.intValue != 0;
                break;
            case SerializedPropertyType.Float:
                value = conditionProperty.floatValue != 0f;
                break;
            case SerializedPropertyType.String:
                value = string.IsNullOrEmpty(conditionProperty.stringValue) == false;
                break;
            case SerializedPropertyType.Enum:
                value = conditionProperty.enumValueIndex != 0;
                break;
            default:
                return true;
        }

        return attr.Invert ? !value : value;
    }
}

[CustomPropertyDrawer(typeof(CustomLabelAttribute))]
public class CustomLabelAttributeDrawer : PropertyDrawer {
    //エディタ上でカスタムプロパティを描画
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        //カスタムアトリビュートをCustomLabelAttributeとして取得
        CustomLabelAttribute newLabel = attribute as CustomLabelAttribute;
        //カスタムアトリビュートのラベルをプロパティのラベルに設定
        if (newLabel != null) label = newLabel.Label;
        //エディタ上にプロパティを描画
        EditorGUI.PropertyField(position, property, label, true);
    }

    //エディタ上でプロパティの高さを取得
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        //プロパティの高さを取得
        return EditorGUI.GetPropertyHeight(property, true);
    }
}


[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeAttributeDrawer : PropertyDrawer {
    const float NumberFieldWidth = 50f;
    const float Gap = 4f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var attr = (MinMaxRangeAttribute)attribute;

        var minProp = property.FindPropertyRelative("min");
        var maxProp = property.FindPropertyRelative("max");

        if (minProp != null && maxProp != null) {
            bool isInt = minProp.propertyType == SerializedPropertyType.Integer;
            DrawMinMaxSlider(position, minProp, maxProp, label, attr, isInt);
            return;
        }

        if (property.propertyType == SerializedPropertyType.Float) {
            DrawSingleSlider(position, property, label, attr, false);
        }
        else if (property.propertyType == SerializedPropertyType.Integer) {
            DrawSingleSlider(position, property, label, attr, true);
        }
        else {
            EditorGUI.PropertyField(position, property, label);
        }
    }

    void DrawMinMaxSlider(Rect position, SerializedProperty minProp, SerializedProperty maxProp, GUIContent label, MinMaxRangeAttribute attr, bool isInt) {
        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        EditorGUI.LabelField(labelRect, label);

        float fieldX = position.x + EditorGUIUtility.labelWidth + 2f;
        float fieldWidth = position.width - EditorGUIUtility.labelWidth - 2f;

        Rect minRect = new Rect(fieldX, position.y, NumberFieldWidth, position.height);
        Rect sliderRect = new Rect(
            fieldX + NumberFieldWidth + Gap,
            position.y,
            fieldWidth - (NumberFieldWidth * 2 + Gap * 2),
            position.height
        );
        Rect maxRect = new Rect(sliderRect.xMax + Gap, position.y, NumberFieldWidth, position.height);

        float minVal = isInt ? minProp.intValue : minProp.floatValue;
        float maxVal = isInt ? maxProp.intValue : maxProp.floatValue;

        EditorGUI.BeginChangeCheck();

        if (isInt) {
            minVal = EditorGUI.IntField(minRect, (int)minVal);
            maxVal = EditorGUI.IntField(maxRect, (int)maxVal);
        }
        else {
            minVal = EditorGUI.FloatField(minRect, minVal);
            maxVal = EditorGUI.FloatField(maxRect, maxVal);
        }

        EditorGUI.MinMaxSlider(sliderRect, ref minVal, ref maxVal, attr.Min, attr.Max);

        if (EditorGUI.EndChangeCheck()) {
            minVal = Mathf.Clamp(minVal, attr.Min, maxVal);
            maxVal = Mathf.Clamp(maxVal, minVal, attr.Max);

            if (isInt) {
                minProp.intValue = Mathf.RoundToInt(minVal);
                maxProp.intValue = Mathf.RoundToInt(maxVal);
            }
            else {
                minProp.floatValue = (float)Math.Round(minVal, 2);
                maxProp.floatValue = (float)Math.Round(maxVal, 2);
            }
        }
    }

    void DrawSingleSlider(Rect position, SerializedProperty property, GUIContent label, MinMaxRangeAttribute attr, bool isInt) {
        EditorGUI.BeginChangeCheck();

        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        EditorGUI.LabelField(labelRect, label);

        float fieldX = position.x + EditorGUIUtility.labelWidth + 2f;
        float totalWidth = position.width - EditorGUIUtility.labelWidth - 2f;
        float sliderWidth = totalWidth - 54f;

        Rect sliderRect = new Rect(fieldX, position.y, sliderWidth, position.height);
        Rect valueRect = new Rect(sliderRect.xMax + 4f, position.y, 50f, position.height);

        float current = isInt ? property.intValue : property.floatValue;
        float newValue = GUI.HorizontalSlider(sliderRect, current, attr.Min, attr.Max);

        if (isInt) {
            int intVal = EditorGUI.IntField(valueRect, Mathf.RoundToInt(newValue));
            if (EditorGUI.EndChangeCheck()) {
                property.intValue = Mathf.Clamp(intVal, Mathf.RoundToInt(attr.Min), Mathf.RoundToInt(attr.Max));
            }
        }
        else {
            float floatVal = EditorGUI.FloatField(valueRect, (float)Math.Round(newValue, 2));
            if (EditorGUI.EndChangeCheck()) {
                property.floatValue = Mathf.Clamp(floatVal, attr.Min, attr.Max);
            }
        }
    }
}
#endif