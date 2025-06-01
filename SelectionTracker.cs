using System.IO;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace IEDLabs.EditorUtilities
{
    public class SelectionTracker : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset xmlAsset;

        [SerializeField]
        private ScriptableObject storageObject;

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

            VisualElement root = visualTree.Instantiate();
            var mclv = root.Q<MultiColumnListView>("mclView");
            rootVisualElement.Add(root);
            if (!storageObject)
            {
                CreateStorageObject();
                Debug.Log($"#SELECTION_TRACKER# {nameof(SelectionTrackerStorage)}.asset was not assigned to {nameof(SelectionTracker)} asset" +
                          $"Location the .asset file (should be in the same dir as this .cs file) and assign it to the\"storageObject\" field" +
                          $"in the inspector");
            }
            Selection.selectionChanged += OnSelectionChange;
        }

        private void OnDestroy()
        {
            Debug.Log($"#EDITORWINDOW# closed {this}");
            Selection.selectionChanged -= OnSelectionChange;
        }

#endregion // window lifecycle

        private void CreateStorageObject()
        {
            string scriptName = nameof(SelectionTrackerStorage);
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string scriptDirectory = Path.GetDirectoryName(scriptPath);
            string storageAssetPath = Path.Combine(scriptDirectory, $"{scriptName}.asset");
            storageObject = AssetDatabase.LoadAssetAtPath<SelectionTrackerStorage>(storageAssetPath);
            if (storageObject)
            {
                return;
            }

            storageObject = CreateInstance<SelectionTrackerStorage>();
            AssetDatabase.CreateAsset(storageObject, storageAssetPath);
        }

        private void OnSelectionChange()
        {
            Debug.Log($"#EDITORWINDO# selection changed to: {Selection.activeObject} of type: {Selection.activeObject.GetType()}");
        }
    }
}
