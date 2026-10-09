using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arcade
{
    /// <summary>Unscaled, clearly visible feedback for both mouse and keyboard menus.</summary>
    public sealed class ArcadeMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image Border;
        public Image Background;
        public Image Action;
        public Color BaseColor;
        public Color Accent;
        public bool Primary;
        private bool hovered;
        private bool selected;
        private bool pressed;

        private void Update()
        {
            bool focused = hovered || selected;
            float blend = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            Color fill = Primary ? Accent : focused ? Color.Lerp(BaseColor, Accent, .10f) : BaseColor;
            if (pressed) fill = Color.Lerp(fill, Color.black, .14f);
            Background.color = Color.Lerp(Background.color, fill, blend);
            Border.color = Color.Lerp(Border.color, focused ? Accent : new Color32(49, 65, 85, 255), blend);
            if (Action != null) Action.color = Color.Lerp(Action.color, focused ? Color.Lerp(Accent, Color.white, .24f) : Accent, blend);
            float scale = pressed ? .985f : focused ? 1.008f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale, blend);
        }

        private void OnDisable() { hovered = false; selected = false; pressed = false; transform.localScale = Vector3.one; }
        public void OnPointerEnter(PointerEventData data) { hovered = true; }
        public void OnPointerExit(PointerEventData data) { hovered = false; pressed = false; }
        public void OnSelect(BaseEventData data) { selected = true; }
        public void OnDeselect(BaseEventData data) { selected = false; pressed = false; }
        public void OnPointerDown(PointerEventData data) { pressed = true; }
        public void OnPointerUp(PointerEventData data) { pressed = false; }
    }
}
