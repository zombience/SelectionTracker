using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Utils = IEDLabs.EditorUtilities.SelectionTrackerUtils;

namespace IEDLabs.EditorUtilities
{
    public class SelectionTracker : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset xmlAsset;

        private VisualElement root;
        private SelectionTrackerData selectionData;
        private MclView
            pinnedView,
            historyView;

#region window lifecycle

        [MenuItem("Window/Selection Tracker")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SelectionTracker>();
            wnd.titleContent = new ("Selection Tracker");
        }

        public void CreateGUI()
        {
            VisualTreeAsset visualTree = xmlAsset;
            if (!visualTree)
            {
                var scriptName = nameof(SelectionTracker);
                Debug.LogError($"{scriptName}.uxml not assigned in SelectionTracker.cs. " +
                               $"Locate {scriptName}.cs script and drag the {scriptName}.uxml asset into the 'xmlAsset' field");
                return;
            }

            root = visualTree.Instantiate();
            BuildDisplay();
            Selection.selectionChanged += OnSelectionChange;
        }

        private void OnDestroy()
        {
            Selection.selectionChanged -= OnSelectionChange;
            Utils.SaveSelectionHistory(selectionData);
        }

#endregion // window lifecycle

        private void BuildDisplay()
        {
            selectionData = Utils.LoadSelectionHistory();
            rootVisualElement.Add(root);

            pinnedView = new (selectionData.pinned.entries, "pinned items", "unpin", UnPinEntry);
            historyView = new (selectionData.history.entries, "selection history", "pin", PinEntry);

            var splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Vertical)
            {
                style =
                {
                    minHeight = 20,
                    flexGrow = 1
                }
            };

            splitView.Add(pinnedView);
            splitView.Add(historyView);
            rootVisualElement.Add(splitView);
        }

#region selection handling

        private void OnSelectionChange()
        {
            var activeObject = Selection.GetFiltered<Object>(SelectionMode.Assets).FirstOrDefault();
            if (!activeObject)
            {
                return;
            }

            // only handle assets from project window
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(activeObject, out var guid, out long id))
            {
                return;
            }

            // Debug.Log($"#EDITORWINDO# Selection changed to: {activeObject.name} (GUID: {guid})");
            selectionData.history.AddEntry(activeObject, guid);
            Utils.SaveSelectionHistory(selectionData);
            RefreshViews();
        }

        private void UnPinEntry(SelectionEntry entryToRemove)
        {
            selectionData.pinned.entries.RemoveAll(e => e.guid == entryToRemove.guid);
            Utils.SaveSelectionHistory(selectionData);
            RefreshViews();
        }

        private void PinEntry(SelectionEntry entryToPin)
        {
            selectionData.pinned.AddEntry(entryToPin);
            Utils.SaveSelectionHistory(selectionData);
            RefreshViews();
        }

        private void RefreshViews()
        {
            pinnedView?.RefreshView();
            historyView?.RefreshView();
        }

#endregion // selection handling
    }
}
