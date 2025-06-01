using System.Collections.Generic;

using UnityEngine;

namespace IEDLabs.EditorUtilities
{
    public class SelectionTrackerStorage : ScriptableObject
    {
        public int historyLength = 20;
        public List<SelectionEntry>
            history,
            pinned;
    }

    [System.Serializable]
    public class SelectionEntry
    {
        public string
            guid,
            objectName;

        public ulong
            lastSelected;
    }
}
