using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace IEDLabs.EditorUtilities
{
    public class MclView : VisualElement
    {
        private MultiColumnListView mcList;
        private List<SelectionEntry> listSource;
        private Action<SelectionEntry> onButtonClick;

        public MclView(List<SelectionEntry> itemsSource, string title, string buttonText, Action<SelectionEntry> buttonAction)
        {
            var container = LoadUxml();
            container.CloneTree(this);
            listSource = itemsSource;
            onButtonClick = buttonAction;
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

            mcList.columns["name"].makeCell = () => new Label();
            mcList.columns["name"].bindCell = (element, index) =>
            {
                var entry = listSource[index];
                var label = element as Label;
                label.text = entry.objectName;
            };

            mcList.columns["asset"].makeCell = () => new Button();
            mcList.columns["asset"].bindCell = (element, index) =>
            {
                var entry = listSource[index];
                var button = element as Button;
                button.text = entry.objectName;
                //var asset = AssetDatabase.GetMainAssetTypeFromGUID(new (entry.guid));
                button.iconImage = SelectionTrackerUtils.GetBgForAsset(entry.guid);
                button.style.alignItems = new StyleEnum<Align>(Align.FlexStart);
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

        private static VisualTreeAsset LoadUxml()
        {
            var assets = AssetDatabase
                .FindAssets($"MclView");

            var asset = assets
                .Select(a => AssetDatabase.GUIDToAssetPath(a))
                .FirstOrDefault(p => p.EndsWith("uxml"));

            if (string.IsNullOrEmpty(asset))
            {
                Debug.LogError($"#SELECTION_TRACKER# couldn't find uxml file for {nameof(MclView)}");
                return null;
            }

            return
                AssetDatabase
                    .LoadAssetAtPath<VisualTreeAsset>(asset);
        }
    }
}
