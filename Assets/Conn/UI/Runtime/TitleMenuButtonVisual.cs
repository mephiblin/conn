using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Conn.UI.Runtime
{
    public sealed class TitleMenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private Text marker;

        private bool pointerInside;
        private bool selected;

        public void Bind(Text labelText, Text markerText)
        {
            label = labelText;
            marker = markerText;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(gameObject);
            }

            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            Apply();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Apply();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            Apply();
        }

        private void Apply()
        {
            var active = selected || pointerInside;
            transform.localScale = active ? Vector3.one * 1.06f : Vector3.one;

            if (label != null)
            {
                label.color = active ? new Color(1f, 0.15f, 0.24f, 1f) : Color.white;
            }

            if (marker != null)
            {
                marker.text = active ? "◇" : string.Empty;
                marker.color = new Color(1f, 0.15f, 0.24f, 1f);
            }
        }
    }
}
