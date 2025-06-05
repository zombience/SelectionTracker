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

        public MclView(List<SelectionEntry> itemsSource, string buttonText, Action<SelectionEntry> buttonAction)
        {
            var container = LoadUXML();
            container.CloneTree(this);
            listSource = itemsSource;
            onButtonClick = buttonAction;
            BuildMclView(buttonText);
        }

        public void RefreshView()
        {
            mcList.RefreshItems();
        }

        private void BuildMclView(string buttonText)
        {
            mcList = this.Q<MultiColumnListView>("mclView");

            mcList.itemsSource = listSource;

            mcList.columns["name"].makeCell = () => new Label();
            mcList.columns["name"].bindCell = (element, index) =>
            {
                var entry = listSource[index];
                var label = element as Label;
                label.text = entry.objectName;
            };

            mcList.columns["asset"].makeCell = () => new ObjectField();
            mcList.columns["asset"].bindCell = (element, index) =>
            {
                var entry = listSource[index];
                var objectField = element as ObjectField;
                objectField.value = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(entry.guid));
                objectField.allowSceneObjects = false;
                objectField.objectType = typeof(Object);
                objectField.name = "objectField_" + entry.guid;
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
        }

        private VisualTreeAsset LoadUXML()
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
