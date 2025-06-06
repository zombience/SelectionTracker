using UnityEngine;
using UnityEngine.UIElements;

namespace IEDLabs.EditorUtilities
{
    public class CellButton : VisualElement
    {
        private System.Action onButtonClick;

        public CellButton()
        {
            var container = SelectionTrackerUtils.LoadMatchingUxml(nameof(CellButton));
            container.CloneTree(this);
        }

        public void SetText(string buttonText)
        {
            var button = this.Q<Button>("button");
            button.text = buttonText;
        }

        public void SetImage(Texture icon)
        {
            var image = this.Q<Image>("icon");
            image.image = icon;
        }

        public void SetButtonAction(System.Action onClick)
        {
            var button = this.Q<Button>("button");
            button.clicked -= onButtonClick;
            button.clicked += onClick;
            onButtonClick = onClick;
        }

        public void SetupCellButton(string buttonText, Texture icon, System.Action onClick)
        {
            SetText(buttonText);
            SetImage(icon);
            SetButtonAction(onClick);
        }
    }
}
