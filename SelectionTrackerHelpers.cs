using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

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

            entries.Insert(0, entry);
            //Debug.Log($"#SELECTION_TRACKER# adding entry: {entry}");
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

            entries.Insert(0, entry);
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

        // track deleted assets to handle display across multiple lists/views
        public bool isNull;

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

    internal static class SelectionTrackerUtils
    {
        internal static Background GetBgForAsset(string guid)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            var specialCaseIconName = CheckSpecialTypes(assetType);
            Texture2D icon;
            if (!string.IsNullOrEmpty(specialCaseIconName))
            {
                icon = EditorGUIUtility.IconContent(specialCaseIconName).image as Texture2D;
            }
            else
            {
                GUIContent content = EditorGUIUtility.ObjectContent(null, assetType);
                icon = content.image as Texture2D;
            }
            return Background.FromTexture2D(icon);
        }

        // icon names: https://github.com/halak/unity-editor-icons
        // https://github.com/ErnSur/unity-editor-icons
        private static string CheckSpecialTypes(Type assetType)
        {
            if (assetType == typeof(DefaultAsset))
            {
                return "Folder Icon";
            }
            if (assetType == typeof(MonoScript))
            {
                return "cs Script Icon";
            }

            return string.Empty;
        }

#region IO Helpers

        /// <summary>
        /// loads uxml files based on the C# class name
        /// uxml files must be named exactly the same as C#
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        internal static VisualTreeAsset LoadMatchingUxml(string className)
        {
            className = className.ToLower();
            var assets = AssetDatabase
                .FindAssets(className);

            var asset = assets
                .Select(a => AssetDatabase.GUIDToAssetPath(a))
                .FirstOrDefault(p => p.ToLower().EndsWith($"{className}.uxml"));

            if (!string.IsNullOrEmpty(asset))
            {
                return AssetDatabase .LoadAssetAtPath<VisualTreeAsset>(asset);
            }

            Debug.LogError($"#SELECTION_TRACKER# couldn't find uxml file for {className}");
            return null;

        }

        internal static void SaveSelectionHistory(SelectionTrackerData selectionDataObj)
        {
            string jsonData = JsonUtility.ToJson(selectionDataObj);
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            using StreamWriter sw = new(FullFilePath);
            sw.Write(jsonData);

            //Debug.Log($"#SELECTION_TRACKER# writing data to {FullFilePath}");
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


#region path

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

#endregion // path
#endregion // IO Helpers

    }
}
