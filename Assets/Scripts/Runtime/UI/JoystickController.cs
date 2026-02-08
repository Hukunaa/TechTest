using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public enum JoystickBaseMode
    {
        Static,
        Dynamic,
        Floating
    }

    public class JoystickController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private RectTransform _joystick;
        [SerializeField] private RectTransform _joystickHandle;
        [SerializeField] private JoystickBaseMode _baseMode = JoystickBaseMode.Static;
        [SerializeField] private float _joystickRange = 55f;
        [SerializeField] private bool _snapHandleBack = true;
        [Range(0f, 1f)] [SerializeField] private float _deadZone = 0.1f;
        [Range(0, 16)] [SerializeField] private int _directionSnaps = 0;

        private Vector2 _inputDirection = Vector2.zero;
        private Vector2 _baseStartPos;
        private Canvas _parentCanvas;
        private bool _dragStartedInside = false;

        public Vector2 InputDirection => _inputDirection;
        public event Action OnTouchPressed;
        public event Action OnTouchRemoved;
        public event Action OnDirectionChanged;


        void Awake()
        {
            _baseStartPos = _joystick.anchoredPosition;
            _parentCanvas = GetComponentInParent<Canvas>();

            _joystick.gameObject.SetActive(true);
            _joystick.anchoredPosition = _baseStartPos;
            _joystickHandle.anchoredPosition = Vector2.zero;
            _inputDirection = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnTouchPressed?.Invoke();

            _joystick.gameObject.SetActive(true);

            if (_baseMode == JoystickBaseMode.Floating)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 newPos
                );
                _joystick.anchoredPosition = newPos;
            }

            if (_baseMode != JoystickBaseMode.Static)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_joystick.parent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 touchPoint
                );

                if (_snapHandleBack) _joystick.anchoredPosition = touchPoint;
                else
                {
                    _joystick.anchoredPosition = touchPoint - (_inputDirection * _joystickRange);
                    _joystickHandle.anchoredPosition = _inputDirection * _joystickRange;
                }
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystick, eventData.position, eventData.pressEventCamera, out Vector2 localPoint
            );
            _dragStartedInside = localPoint.magnitude <= _joystickRange;

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_baseMode == JoystickBaseMode.Static && !_dragStartedInside) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystick,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, _joystickRange);
            _joystickHandle.anchoredPosition = clamped;

            Vector2 rawInput = clamped / _joystickRange;
            _inputDirection = rawInput.magnitude < _deadZone ? Vector2.zero : rawInput;

            if (_inputDirection != Vector2.zero) OnDirectionChanged?.Invoke();

            if (_directionSnaps > 1 && _inputDirection != Vector2.zero)
            {
                float angle = Vector2.SignedAngle(Vector2.left, _inputDirection);
                float snapAngle = 360f / _directionSnaps;
                float snappedAngle = Mathf.Round(angle / snapAngle) * snapAngle;

                _inputDirection = Quaternion.Euler(0, 0, snappedAngle) * Vector2.left;
                _joystickHandle.anchoredPosition = _inputDirection * _joystickRange;
            }

            if (_baseMode == JoystickBaseMode.Dynamic && localPoint.magnitude > _joystickRange)
            {
                Vector2 offset = localPoint.normalized * (localPoint.magnitude - _joystickRange);
                _joystick.anchoredPosition += offset;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnTouchRemoved?.Invoke();

            if (_snapHandleBack)
            {
                _joystickHandle.anchoredPosition = Vector2.zero;
                _inputDirection = Vector2.zero;
            }

            if (_baseMode == JoystickBaseMode.Floating || _baseMode == JoystickBaseMode.Dynamic)
                _joystick.anchoredPosition = _baseStartPos;

            _joystick.gameObject.SetActive(true);
            _dragStartedInside = false;
        }
    }
}
