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

        private SelectionTrackerData selectionData;
        private MclView
            pinnedView,
            historyView;

        // use to prevent writing updated file to disk after every single interaction
        private const int interactionTimeoutInterval = 10;
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

            rootVisualElement.Clear();
            visualTree.CloneTree(rootVisualElement);

            BuildDisplay();
            Selection.selectionChanged += OnSelectionChange;
            cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            Selection.selectionChanged -= OnSelectionChange;
            Utils.SaveSelectionHistory(selectionData);
            cts?.Cancel();
        }

#endregion // window lifecycle

        private void BuildDisplay()
        {
            selectionData = Utils.LoadSelectionHistory();
            selectionData.history.HistoryLength = selectionData.historyLength;
            selectionData.history.RemoveExcessItems();

            pinnedView = rootVisualElement.Q<MclView>("mclpinned");
            historyView = rootVisualElement.Q<MclView>("mclhistory");
            pinnedView.InitializeView(selectionData.pinned, "pinned items", "unpin", UnPinEntry, RemoveMissingEntry);
            historyView.InitializeView(selectionData.history, "selection history", "pin", PinEntry, RemoveMissingEntry);

            var utilContainer = rootVisualElement.Q<VisualElement>("utilContainer");
            var button = utilContainer.Q<Button>("clearhistory");
            button.clicked -= ClearHistory;
            button.clicked += ClearHistory;

            var slider = utilContainer.Q<SliderInt>("historylength");
            slider.SetValueWithoutNotify(selectionData.historyLength);
            slider.UnregisterValueChangedCallback(SetHistoryLength);
            slider.RegisterValueChangedCallback(SetHistoryLength);
            RefreshViews();
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
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(activeObject, out string guid, out long id))
            {
                return;
            }

            // Debug.Log($"#EDITORWINDO# Selection changed to: {activeObject.name} (GUID: {guid})");
            selectionData.history.AddEntry(activeObject, guid);
            RefreshViews();
        }



        private void HandleInteraction(int timeout)
        {
            // let task run to completion
            if (timeoutTask is { IsCompleted: false, IsFaulted: false })
            {
                return;
            }

            cts?.Dispose();
            cts = new CancellationTokenSource();
            timeoutTask = Task.Run(() => AwaitTimeout(timeout), cts.Token);
            timeoutTask.ContinueWith(t =>
            {
                // if (t.IsCompletedSuccessfully)
                // {
                //     Debug.Log("#SELECTION_TRACKER#: Async task completed successfully (final state check).");
                // }
                // else if (t.IsCanceled)
                // {
                //     Debug.Log("#SELECTION_TRACKER#: Async task was cancelled (final state check).");
                // }
                // else
                if (t.IsFaulted)
                {
                    Debug.LogError($"#SELECTION_TRACKER#: Async task failed (final state check): {t.Exception?.InnerException?.Message ?? t.Exception?.Message}");
                }

                timeoutTask = null;
            });
        }

        private async Task AwaitTimeout(int timeout)
        {
            try
            {
                //Debug.Log($"TaskManagerExample: Async task started at {DateTime.Now.ToLongTimeString()} on thread {Thread.CurrentThread.ManagedThreadId}. Waiting for {interactionTimeoutInterval} seconds...");
                await Task.Delay(TimeSpan.FromSeconds(timeout), cts.Token);
                cts.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                //Debug.Log("#SELECTION_TRACKER#: Task was explicitly cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"#SELECTION_TRACKER#: Task encountered an error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                cts?.Dispose();
                cts = null;
                EditorApplication.delayCall += SaveAfterTimeout;
            }
        }

        private void SaveAfterTimeout()
        {
            Utils.SaveSelectionHistory(selectionData);
        }
#endregion // selection handling

#region child callbacks
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

        private void RefreshViews(int timeout = interactionTimeoutInterval)
        {
            selectionData.history.RemoveExcessItems();
            pinnedView?.RefreshView();
            historyView?.RefreshView();
            HandleInteraction(timeout);
        }

        private void ClearHistory()
        {
            selectionData.history.entries.Clear();
            RefreshViews();
        }

        private void SetHistoryLength(ChangeEvent<int> evt)
        {
            selectionData.historyLength = evt.newValue;
            selectionData.history.HistoryLength = evt.newValue;
            cts?.Cancel();
            RefreshViews(1);
        }

#endregion
    }
}
