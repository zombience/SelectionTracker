using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace IEDLabs.EditorUtilities
{
    public class MclView : VisualElement
    {
        private MultiColumnListView mcList;

        public MclView(List<SelectionEntry> itemsSource)
        {
            var container = LoadUXML();
            container.CloneTree(this);
            mcList = this.Q<MultiColumnListView>("mclView");

            mcList.itemsSource = itemsSource;

            mcList.columns["name"].makeCell = () => new Label();
            mcList.columns["name"].bindCell = (element, index) =>
            {
                var entry = itemsSource[index];
                var label = element as Label;
                label.text = entry.objectName;
            };


            mcList.columns["asset"].makeCell = () => new ObjectField();
            mcList.columns["asset"].bindCell = (element, index) =>
            {
                var entry = itemsSource[index];
                var objectField = element as ObjectField;
                objectField.value = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(entry.guid));
                objectField.allowSceneObjects = false;
                objectField.objectType = typeof(Object);
                objectField.name = "objectField_" + entry.guid;
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
