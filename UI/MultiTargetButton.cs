using System;
using UnityEngine;
using UnityEngine.UI;

public class MultiTargetButton : Button {
    [Serializable]
    public class TargetGraphicEntry {
        public Graphic graphic;
        public Color normalColor = new Color(1f, 1f, 1f, 1f);
        public Color highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        public Color pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        public Color selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        public Color disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        [Range(1f, 5f)] public float colorMultiplier = 1f;
        public float fadeDuration = 0.1f;
    }

    [SerializeField] TargetGraphicEntry[] targetGraphics;

    void ApplyColor(TargetGraphicEntry entry, SelectionState state, bool instant) {
        if (entry.graphic == null) return;

        Color target = state switch {
            SelectionState.Normal => entry.normalColor,
            SelectionState.Highlighted => entry.highlightedColor,
            SelectionState.Pressed => entry.pressedColor,
            SelectionState.Selected => entry.selectedColor,
            SelectionState.Disabled => entry.disabledColor,
            _ => Color.white
        };

        entry.graphic.CrossFadeColor(target * entry.colorMultiplier, instant ? 0f : entry.fadeDuration, true, true);
    }

    protected override void DoStateTransition(SelectionState state, bool instant) {
        base.DoStateTransition(state, instant);

        if (targetGraphics == null) return;

        foreach (var entry in targetGraphics)
            ApplyColor(entry, state, instant);
    }

#if UNITY_EDITOR
    protected override void OnValidate() {
        base.OnValidate();

        if (targetGraphics == null) return;

        foreach (var entry in targetGraphics)
            ApplyColor(entry, currentSelectionState, true);
    }
#endif
}
