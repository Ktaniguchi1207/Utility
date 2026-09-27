using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtanLab.ProjectTimeTracker
{
    /// <summary>
    /// Scene ビュー上に作業時間を表示するオーバーレイ。
    /// ボタンを押すと「今回 / 累計」の詳細表示が開閉する。
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "Project Time", true)]
    public class ProjectTimeOverlay : Overlay
    {
        public const string OverlayId = "atanlab-project-time";

        Button compactButton;
        VisualElement detailContainer;
        Label sessionLabel;
        Label totalLabel;
        bool isExpanded;

        public override VisualElement CreatePanelContent()
        {
            VisualElement root = new VisualElement();
            root.style.minWidth = 96f;

            compactButton = new Button(OnCompactButtonClicked);
            compactButton.tooltip = "クリックで内訳を開閉します";
            compactButton.style.marginLeft = 0f;
            compactButton.style.marginRight = 0f;
            compactButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(compactButton);

            detailContainer = new VisualElement();
            detailContainer.style.display = DisplayStyle.None;
            detailContainer.style.paddingTop = 2f;
            detailContainer.style.paddingLeft = 2f;

            sessionLabel = new Label();
            totalLabel = new Label();
            detailContainer.Add(sessionLabel);
            detailContainer.Add(totalLabel);
            root.Add(detailContainer);

            root.schedule.Execute(RefreshLabels).Every(1000);
            RefreshLabels();

            return root;
        }

        void OnCompactButtonClicked()
        {
            isExpanded = !isExpanded;
            detailContainer.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RefreshLabels()
        {
            double totalSeconds = ProjectTimeTracker.GetAccumulatedSeconds();
            double sessionSeconds = ProjectTimeTracker.GetSessionAccumulatedSeconds();
            bool idle = ProjectTimeTracker.IsIdle();

            compactButton.text = (idle ? "💤 " : "") + ProjectTimeFormat.Short(totalSeconds);
            sessionLabel.text = "今回: " + ProjectTimeFormat.Detailed(sessionSeconds);
            totalLabel.text = "累計: " + ProjectTimeFormat.Detailed(totalSeconds);
        }
    }
}
