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
        public static int HistoryLength = 20;
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
            var entry = new SelectionEntry()
            {
                guid = itemGuid,
                objectName = item.name,
                lastSelected = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            return AddEntry(entry);
        }

        /// <summary>
        /// allow passing formed entry from one list to another
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool AddEntry(SelectionEntry entry)
        {
            entries.RemoveAll(e => e.guid == entry.guid);
            RemoveExcessItems(true);
            entries.Insert(0, entry);
            return true;
        }

        public bool RemoveEntry(string guid)
        {
            return entries.RemoveAll(e => e.guid == guid) > 0;
        }

        public bool RemoveEntry(SelectionEntry entry)
        {
            return RemoveEntry(entry.guid);
        }

        public void RemoveExcessItems(bool lessOne = false)
        {
            while (entries.Count > SelectionTrackerData.HistoryLength - (lessOne ? 1 : 0))
            {
                entries.RemoveAt(entries.Count - 1);
            }
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
        // icon names: https://github.com/halak/unity-editor-icons
        // https://github.com/ErnSur/unity-editor-icons
        internal static Background GetBgForAsset(string guid)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            GUIContent content;

            if (assetType == typeof(DefaultAsset))
            {
                content = EditorGUIUtility.IconContent("Folder Icon");
            }
            else
            {
                bool shoudLoadObj = ShouldGetIconFromLoadedObject(assetPath.Split(".")[^1]);
                // most icons can be loaded by type and don't require the loaded object
                Object objToLoad = shoudLoadObj ? AssetDatabase.LoadAssetAtPath<Object>(assetPath) : null;
                content = EditorGUIUtility.ObjectContent(objToLoad, assetType);
            }

            Texture2D icon = content.image as Texture2D;
            return Background.FromTexture2D(icon);
        }

        /// <summary>
        /// only load specific objects:
        /// some assets load incorrect icons if only passing EditorGUIUtility.ObjectConcent(null, assetType)
        /// loading all assets each time UI is interacted with is unnecessary
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        private static bool ShouldGetIconFromLoadedObject(string extension)
        {
            switch (extension)
            {
                case "asset":
                case "cs":
                case "inputactions":
                case "shadergraph":
                    return true;
            }

            return false;
        }



        private static string CheckSpecialTypes(Type assetType, string extension)
        {

            // these cases fail when using type lookup
            // .asset files are interpreted as MonoScript
            // Known missing extension match: inputactions
            var iconName = string.Empty;
            switch (extension)
            {
                case "cs":
                    iconName = "cs Script Icon";
                    break;
                case "asset":
                    iconName = "ScriptableObject Icon";
                    break;
                case "inputactions":
                    return iconName;
            }

            if (assetType == typeof(DefaultAsset))
            {
                iconName = "Folder Icon";
            }

            return iconName;
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
            string[] assets = AssetDatabase
                .FindAssets(className);

            string asset = assets
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

            using StreamReader streamReader = new (FullFilePath);
            string contents = streamReader.ReadToEnd();
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
            string projectName = s[^2];
            return projectName;
        }

#endregion // path
#endregion // IO Helpers

    }
}
