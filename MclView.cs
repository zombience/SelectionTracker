using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

using Utils = IEDLabs.EditorUtilities.SelectionTrackerUtils;

namespace IEDLabs.EditorUtilities
{
    public class MclView : VisualElement
    {
        private MultiColumnListView mcList;
        private SelectionCollection listSource;
        private Action<SelectionEntry> onButtonClick;
        private Action<SelectionEntry> onRemoveMissingClick;

        private bool
            shouldReverseNameSort,
            shouldReverseMissingSort,
            shouldReverseTimeSort;

        public MclView(
            SelectionCollection itemsSource,
            string title,
            string buttonText,
            Action<SelectionEntry> onClick,
            Action<SelectionEntry> onMissingClickToRemove)
        {
            VisualTreeAsset container = Utils.LoadMatchingUxml(nameof(MclView));
            container.CloneTree(this);
            listSource = itemsSource;
            onButtonClick = onClick;
            onRemoveMissingClick = onMissingClickToRemove;
            BuildMclView(title, buttonText);
        }

        public void RefreshView()
        {
            mcList.RefreshItems();
        }

        private void BuildMclView(string title, string buttonText)
        {
            mcList = this.Q<MultiColumnListView>("mclView");

            mcList.itemsSource = listSource.entries;
            mcList.headerTitle = title;

            mcList.columns["asset"].makeHeader = () => new CellButton(false);
            mcList.columns["asset"].bindHeader = (element) =>
            {
                var header = element as CellButton;
                header.tooltip = "click to sort by name";
                header.SetText("Asset");
                header.SetButtonAction(SortByName);
                header.Button.style.backgroundColor = Color.black;
            };

            mcList.columns["asset"].makeCell = () => new CellButton();
            mcList.columns["asset"].bindCell = (element, index) =>
            {
                SelectionEntry entry = listSource.entries[index];
                var cellButton = element as CellButton;
                var icon = Utils.GetBgForAsset(entry.guid);

                entry.isNull = !icon.texture;
                float opacity = entry.isNull ? 0.5f : 1.0f;

                cellButton.SetupCellButton(entry.objectName, icon.texture, () => PingAsset(entry.guid));
                cellButton.style.opacity = opacity;
            };

            mcList.columns["action"].makeHeader = () => new CellButton(false);
            mcList.columns["action"].bindHeader = (element) =>
            {
                var header = element as CellButton;
                header.tooltip = "click to sort by missing";
                header.SetText("Action");
                header.SetButtonAction(SortByMissing);
                header.Button.style.backgroundColor = Color.black;
            };

            mcList.columns["action"].makeCell = () => new CellButton(false);
            mcList.columns["action"].bindCell = (element, index) =>
            {
                SelectionEntry entry = listSource.entries[index];
                var cellButton = element as CellButton;
                if (entry.isNull)
                {
                    cellButton.SetText("(missing - remove)");
                    cellButton.Button.style.backgroundColor = Color.red * .8f;
                    cellButton.SetButtonAction(() => onRemoveMissingClick?.Invoke(entry));
                }
                else
                {
                    cellButton.SetText(buttonText);
                    cellButton.Button.style.backgroundColor = StyleKeyword.Initial;
                    cellButton.SetButtonAction(() => onButtonClick?.Invoke(entry));
                }
            };

            mcList.columns["time"].makeHeader = () => new CellButton(false);
            mcList.columns["time"].bindHeader = (element) =>
            {
                var header = element as CellButton;
                header.tooltip = "click to sort by selected time";
                header.SetText("Last Selected");
                header.SetButtonAction(SortByTime);
                header.Button.style.backgroundColor = Color.black;
            };

            mcList.columns["time"].makeCell = () => new Label();
            mcList.columns["time"].bindCell = (element, index) =>
            {
                SelectionEntry entry = listSource.entries[index];
                var label = element as Label;
                var dt = DateTimeOffset.FromUnixTimeSeconds(entry.lastSelected).ToLocalTime();
                float opacity = entry.isNull ? 0.5f : 1.0f;
                label.text = entry.isNull ? "(missing)" : dt.ToString("HH:mm:ss yy-MM-dd");
                label.style.opacity = opacity;
            };
        }

        private void SortByName()
        {
            listSource.entries = listSource.entries.OrderBy(e => e.objectName).ToList();
            if (shouldReverseNameSort)
            {
                listSource.entries.Reverse();
                shouldReverseMissingSort = false;
                shouldReverseTimeSort = false;
            }

            shouldReverseNameSort = !shouldReverseNameSort;
            RefreshView();
        }

        private void SortByMissing()
        {
            listSource.entries = listSource.entries.OrderBy(e => e.isNull).ToList();
            if (shouldReverseMissingSort)
            {
                listSource.entries.Reverse();
                shouldReverseNameSort = false;
                shouldReverseTimeSort = false;
            }

            shouldReverseMissingSort = !shouldReverseMissingSort;
            RefreshView();
        }

        private void SortByTime()
        {
            listSource.entries = listSource.entries.OrderBy(e => e.lastSelected).ToList();
            if (shouldReverseTimeSort)
            {
                listSource.entries.Reverse();
                shouldReverseNameSort = false;
                shouldReverseMissingSort = false;
            }

            shouldReverseTimeSort = !shouldReverseTimeSort;
            RefreshView();
        }

        private static void PingAsset(string guid)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            EditorGUIUtility.PingObject(obj);
        }
    }
}
