using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;
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

        private const int autoSaveInterval = 10;
        private DateTime lastInteractionTime;
        private Task timeoutTask;
        private CancellationTokenSource cts;

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
            cts.Cancel();
        }

#endregion // window lifecycle

        private void BuildDisplay()
        {
            selectionData = Utils.LoadSelectionHistory();
            rootVisualElement.Add(root);

            pinnedView = new (selectionData.pinned.entries, "pinned items", "unpin", UnPinEntry, RemoveMissingEntry);
            historyView = new (selectionData.history.entries, "selection history", "pin", PinEntry, RemoveMissingEntry);

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
            RefreshViews();
        }

        private void UnPinEntry(SelectionEntry entryToRemove)
        {
            selectionData.pinned.entries.RemoveAll(e => e.guid == entryToRemove.guid);
            RefreshViews();
        }

        private void PinEntry(SelectionEntry entryToPin)
        {
            selectionData.pinned.AddEntry(entryToPin);
            RefreshViews();
        }

        private void RemoveMissingEntry(SelectionEntry entryToRemove)
        {
            selectionData.history.entries.RemoveAll(e => e.guid == entryToRemove.guid);
            UnPinEntry(entryToRemove);
        }

        private void RefreshViews()
        {
            pinnedView?.RefreshView();
            historyView?.RefreshView();
            HandleInteraction();
        }

        private void HandleInteraction()
        {
            // let task run to completion
            if (timeoutTask is { IsCompleted: false, IsFaulted: false })
            {
                return;
            }

            lastInteractionTime = DateTime.UtcNow;
            cts?.Dispose();
            cts = new();
            timeoutTask = Task.Run(AwaitTimeout, cts.Token);
            timeoutTask.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    Debug.Log("#SELECTION_TRACKER#: Async task completed successfully (final state check).");
                }
                else if (t.IsCanceled)
                {
                    Debug.Log("#SELECTION_TRACKER#: Async task was cancelled (final state check).");
                }
                else if (t.IsFaulted)
                {
                    Debug.LogError($"#SELECTION_TRACKER#: Async task failed (final state check): {t.Exception?.InnerException?.Message ?? t.Exception?.Message}");
                }

                timeoutTask = null;
            });
        }

        private async Task AwaitTimeout()
        {
            try
            {
                Debug.Log($"TaskManagerExample: Async task started at {DateTime.Now.ToLongTimeString()} on thread {Thread.CurrentThread.ManagedThreadId}. Waiting for {autoSaveInterval} seconds...");
                await Task.Delay(TimeSpan.FromSeconds(autoSaveInterval), cts.Token);
                cts.Token.ThrowIfCancellationRequested();

                EditorApplication.delayCall += SaveAfterTimeout;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("TaskManagerExample: Task was explicitly cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"#SELECTION_TRACKER#: Task encountered an error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                cts?.Dispose();
                cts = null;
            }
        }

        private void SaveAfterTimeout()
        {
            Utils.SaveSelectionHistory(selectionData);
        }


#endregion // selection handling
    }
}
