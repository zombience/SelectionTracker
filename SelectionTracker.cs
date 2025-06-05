using System.Linq;

using UnityEngine;
using UnityEditor;

using UnityEngine.UIElements;

using Button = UnityEngine.UIElements.Button;

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
            root.Add(new Button(BuildDisplay));
            BuildDisplay();
            Selection.selectionChanged += OnSelectionChange;
        }

        private void OnDestroy()
        {
            Selection.selectionChanged -= OnSelectionChange;
            SelectionTrackerUtils.SaveSelectionHistory(selectionData);
        }

#endregion // window lifecycle

        private void BuildDisplay()
        {
            selectionData = SelectionTrackerUtils.LoadSelectionHistory();
            rootVisualElement.Add(root);

            pinnedView = new MclView(selectionData.pinned.entries, "pinned items", "unpin", UnPinEntry);
            root.Add(pinnedView);

            historyView = new MclView(selectionData.history.entries, "selection history", "pin", PinEntry);
            root.Add(historyView);
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
            SelectionTrackerUtils.SaveSelectionHistory(selectionData);
            RefreshViews();
        }

        private void UnPinEntry(SelectionEntry entryToRemove)
        {
            Debug.Log($"Attempting to remove '{entryToRemove.objectName}' (GUID: {entryToRemove.guid}) from pinned list.");
            selectionData.pinned.entries.RemoveAll(e => e.guid == entryToRemove.guid);
            SelectionTrackerUtils.SaveSelectionHistory(selectionData);
            RefreshViews();
        }

        private void PinEntry(SelectionEntry entryToPin)
        {
            Debug.Log($"Attempting to pin '{entryToPin.objectName}' (GUID: {entryToPin.guid}) from history list.");
            selectionData.pinned.AddEntry(entryToPin);
            SelectionTrackerUtils.SaveSelectionHistory(selectionData);
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
