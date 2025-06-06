using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace IEDLabs.EditorUtilities
{
    public class MclView : VisualElement
    {
        private MultiColumnListView mcList;
        private List<SelectionEntry> listSource;
        private Action<SelectionEntry> onButtonClick;

        public MclView(List<SelectionEntry> itemsSource, string title, string buttonText, Action<SelectionEntry> onClick)
        {
            var container = SelectionTrackerUtils.LoadMatchingUxml(nameof(MclView));
            container.CloneTree(this);
            listSource = itemsSource;
            onButtonClick = onClick;
            BuildMclView(title, buttonText);
        }

        public void RefreshView()
        {
            mcList.RefreshItems();
        }

        private void BuildMclView(string title, string buttonText)
        {
            mcList = this.Q<MultiColumnListView>("mclView");

            mcList.itemsSource = listSource;
            mcList.headerTitle = title;

            mcList.columns["asset"].makeCell = () => new CellButton();
            mcList.columns["asset"].bindCell = (element, index) =>
            {
                var entry = listSource[index];
                var button = element as CellButton;
                var icon = SelectionTrackerUtils.GetBgForAsset(entry.guid);
                button.SetupCellButton(entry.objectName, icon.texture, () => PingAsset(entry.guid));
            };

            mcList.columns["action"].makeCell = () => new Button();
            mcList.columns["action"].bindCell = (element, index) =>
            {
                var entry = listSource[index];
                var button = element as Button;
                button.text = buttonText;

                button.clicked -= () => onButtonClick?.Invoke(entry); // Remove previous lambda
                button.clicked += () => onButtonClick?.Invoke(entry);
            };

            mcList.columns["time"].makeCell = () => new Label();
            mcList.columns["time"].bindCell = (element, index) =>
            {
                var entry = listSource[index];
                var label = element as Label;
                var dt = DateTimeOffset.FromUnixTimeSeconds(entry.lastSelected);
                label.text = dt.ToString("hh:mm yy-MM-dd");
            };
        }

        private void PingAsset(string guid)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            EditorGUIUtility.PingObject(obj);
        }


    }
}
