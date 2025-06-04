using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using Object = UnityEngine.Object;

namespace IEDLabs.EditorUtilities
{
    [Serializable]
    public class SelectionTrackerData
    {
        public int historyLength = 20;
        public SelectionCollection
            history = new(),
            pinned = new();
    }

    [Serializable]
    public class SelectionCollection
    {
        public List<SelectionEntry> entries = new();

        public bool AddEntry(Object item, string itemGuid)
        {
            if (entries.Any(e => e.guid == itemGuid))
            {
                return false;
            }

            var entry = new SelectionEntry()
            {
                guid = itemGuid,
                objectName = item.name,
                lastSelected = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            entries.Add(entry);
            return true;
        }
    }

    [Serializable]
    public class SelectionEntry
    {
        public string
            guid,
            objectName;

        public long
            lastSelected;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return !string.IsNullOrEmpty(guid) ? guid.GetHashCode() : -1;
        }
    }

#region IO Helpers
    internal static class SelectionTrackerUtils
    {

#region pathing

        private static string DataDirectory =>
            Path.Combine(Application.persistentDataPath, "SelectionTracker", GetProjectName());

        private static string FullFilePath =>  Path.Combine(DataDirectory, "selectionHistory.dat");

        private static string GetProjectName()
        {
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            return projectName;
        }

#endregion // pathing

        internal static void SaveSelectionHistory(SelectionTrackerData selectionDataObj)
        {
            string jsonData = JsonUtility.ToJson(selectionDataObj);
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            using StreamWriter sw = new(FullFilePath);
            sw.Write(jsonData);
            Debug.Log($"#SELECTION_TRACKER# writing data to {FullFilePath}");
        }

        internal static SelectionTrackerData LoadSelectionHistory()
        {
            var returnObject = new SelectionTrackerData();
            Debug.Log($"#SELECTION_TRACKER# attempting to load data from {FullFilePath}");
            if (!File.Exists(FullFilePath))
            {
                SaveSelectionHistory(returnObject);
                return returnObject;
            }

            using var streamReader = new StreamReader(FullFilePath);
            string contents;
            contents = streamReader.ReadToEnd();
            try
            {
                returnObject = JsonUtility.FromJson<SelectionTrackerData>(contents);

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return returnObject;
        }

        internal static void ClearData()
        {
            if (!File.Exists(FullFilePath))
            {
                return;
            }

            using FileStream fileStream = File.Open(FullFilePath, FileMode.Open);
            fileStream.SetLength(0L);
        }
    }
#endregion // IO Helpers
}
