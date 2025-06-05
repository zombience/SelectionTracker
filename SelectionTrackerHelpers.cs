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
            // create an entry to rely on entry.GetHashCode for Contains()
            var entry = new SelectionEntry()
            {
                guid = itemGuid,
                objectName = item.name,
                lastSelected = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (entries.Contains(entry))
            {
                return false;
            }

            entries.Add(entry);
            Debug.Log($"#SELECTION_TRACKER# adding entry: {entry}");
            return true;
        }

        /// <summary>
        /// allow passing formed entry from one list to another
        /// will not add if already present
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool AddEntry(SelectionEntry entry)
        {
            if (entries.Contains(entry))
            {
                return false;
            }

            entries.Add(entry);
            return true;
        }

        public bool RemoveEntry(string guid)
        {
            SelectionEntry entry = new() { guid = guid };
            return RemoveEntry(entry);
        }

        public bool RemoveEntry(SelectionEntry entry)
        {
            return entries.RemoveAll(e => e.guid == entry.guid) > 0;
        }
    }

    [Serializable]
    public class SelectionEntry : IComparable<SelectionEntry>
    {
        public string
            guid,
            objectName;

        public long lastSelected;

        /// <inheritdoc />
        public int CompareTo(SelectionEntry other) => lastSelected.CompareTo(other.lastSelected);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is SelectionEntry other)
            {
                return string.Equals(guid, other.guid, StringComparison.Ordinal);
            }
            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return !string.IsNullOrEmpty(guid) ? guid.GetHashCode() : -1;
        }

        /// <inheritdoc />
        public override string ToString() => $"name: {objectName} guid: {guid}";
    }

#region IO Helpers
    internal static class SelectionTrackerUtils
    {

#region pathing

        private static string DataDirectory =>
            Path.Combine(Application.persistentDataPath, "SelectionTracker", GetProjectName());

        private static string FullFilePath =>
            Path.Combine(DataDirectory, "selectionHistory.dat");

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
