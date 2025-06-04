using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unity.AppUI.UI;

using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

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
#region window lifecycle

        [UnityEditor.MenuItem("Window/Selection Tracker")]
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

            var pinnedView = new MclView(selectionData.history.entries);
            root.Add(pinnedView);
            //pinnedView .itemsSource = selectionData.pinned.entries;
            // pinnedView.columns["name"].makeCell = () => new Label();
            //pinnedView.columns["object"].makeCell =

            var historyView = root.Q<MclView>("historyView");
            // historyView.itemsSource = selectionData.history.entries;
            // historyView.columns["name"].makeCell = () => new Label();
        }

#region selection handling

        private void OnSelectionChange()
        {
            var activeObject = Selection.GetFiltered<Object>(SelectionMode.Assets).FirstOrDefault();
            if (!activeObject)
            {
                return;
            }

            // ignore non-asset items
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(activeObject, out var guid, out long id))
            {
                return;
            }
            Debug.Log($"#EDITORWINDO# selection changed to: {activeObject.name} of type: {activeObject.GetType()}, guid: {guid}");
            selectionData.history.AddEntry(activeObject, guid);
            SelectionTrackerUtils.SaveSelectionHistory(selectionData);
        }
#endregion // selection handling

    }
}
