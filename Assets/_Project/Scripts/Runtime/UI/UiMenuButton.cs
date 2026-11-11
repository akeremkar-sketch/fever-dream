using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FeverDream.UI
{
    /// <summary>
    /// Menu button feel. Moving the mouse onto a button selects it, so the mouse, the keyboard and a gamepad always agree on
    /// a single highlighted button; the selected button grows a little and shows its marker. It also passes UI Cancel
    /// (Esc, gamepad B) up to the menu that owns it, because the UI module only sends Cancel to the selected object.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class UiMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, ISelectHandler, IDeselectHandler, ICancelHandler
    {
        [SerializeField, Min(1f)] private float selectedScale = 1.07f;
        [SerializeField, Min(0.1f)] private float speed = 12f;
        [Tooltip("Optional icon shown only while the button is selected.")]
        [SerializeField] private GameObject marker;

        private Selectable selectable;
        private bool selected;
        private Vector2 pointerAtEnable;
        private bool pointerMoved;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            SetMarker(false);
        }

        private void OnEnable()
        {
            // A button can be enabled already selected (the menu selects one before its page is shown).
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            SetMarker(selected);
            // A menu often opens right under a resting pointer (the freed cursor sits at the screen centre).
            // That must not move the selection the menu just made; only a real mouse move does.
            pointerAtEnable = Pointer.current != null ? Pointer.current.position.ReadValue() : new Vector2(float.NaN, float.NaN);
            pointerMoved = false;
        }

        private void OnDisable()
        {
            selected = false;
            transform.localScale = Vector3.one;
            SetMarker(false);
        }

        public void OnPointerEnter(PointerEventData eventData) => HoverSelect(eventData);

        public void OnPointerMove(PointerEventData eventData) => HoverSelect(eventData);

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            SetMarker(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            SetMarker(false);
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (transform.parent != null)
            {
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.cancelHandler);
            }
        }

        private void HoverSelect(PointerEventData eventData)
        {
            if (!pointerMoved)
            {
                if (eventData.position == pointerAtEnable)
                {
                    return;
                }
                pointerMoved = true;
            }
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject != gameObject && selectable.IsInteractable())
            {
                eventSystem.SetSelectedGameObject(gameObject);
            }
        }

        private void Update()
        {
            float target = selected ? selectedScale : 1f;
            float scale = Mathf.MoveTowards(transform.localScale.x, target, speed * (selectedScale - 1f) * Time.unscaledDeltaTime);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetMarker(bool visible)
        {
            if (marker != null)
            {
                marker.SetActive(visible);
            }
        }
    }
}
