using UnityEngine;
using UnityEngine.UIElements;

namespace IEDLabs.EditorUtilities
{
    public class CellButton : VisualElement
    {
        public Button Button => button;
        private Button button;
        private System.Action onButtonClick;

        public CellButton(bool includeIcon = true)
        {
            var container = SelectionTrackerUtils.LoadMatchingUxml(nameof(CellButton));
            container.CloneTree(this);
            button = this.Q<Button>("button");
            button.clicked += ExecuteButtonClick;

            if (!includeIcon)
            {
                var image = this.Q<Image>("icon");
                image.RemoveFromHierarchy();
            }
        }

        public void SetText(string buttonText)
        {
            button.text = buttonText;
        }

        public void SetImage(Texture icon)
        {
            var image = this.Q<Image>("icon");
            if (image == null)
            {
                return;
            }
            image.image = icon;
        }

        public void SetButtonAction(System.Action onClick)
        {
            onButtonClick = onClick;
        }

        public void SetupCellButton(string buttonText, Texture icon, System.Action onClick)
        {
            SetText(buttonText);
            SetImage(icon);
            SetButtonAction(onClick);
        }

        private void ExecuteButtonClick()
        {
            onButtonClick?.Invoke();
        }
    }
}
