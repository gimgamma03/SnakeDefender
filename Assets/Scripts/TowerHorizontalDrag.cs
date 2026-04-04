using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SnakeDefender
{
    // 플레이어(타워) 루트를 터치·드래그로 좌우만 이동. EnemySpawner의 finalGoalTarget이 이 Transform이면 뱀 목표도 같이 움직임.
    [DefaultExecutionOrder(-20)]
    public class TowerHorizontalDrag : MonoBehaviour
    {
        [SerializeField] private float pickupRadius = 1.5f;
        [SerializeField] private float horizontalMargin = 0.35f;
        [SerializeField] private bool blockWhenPointerOverUi = true;

        private Camera _cam;
        private bool _dragging;
        private float _grabOffsetX;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                _dragging = false;
                return;
            }

            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam == null)
                {
                    return;
                }
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                var touch = Touchscreen.current.primaryTouch;
                ProcessPointer(
                    touch.position.ReadValue(),
                    touch.press.wasPressedThisFrame,
                    touch.press.wasReleasedThisFrame,
                    touch.touchId.ReadValue());
                return;
            }

            if (Mouse.current != null)
            {
                ProcessPointer(
                    Mouse.current.position.ReadValue(),
                    Mouse.current.leftButton.wasPressedThisFrame,
                    Mouse.current.leftButton.wasReleasedThisFrame,
                    -1);
            }
        }

        private void ProcessPointer(Vector2 screenPos, bool pressedThisFrame, bool releasedThisFrame, int pointerId)
        {
            if (pressedThisFrame && !_dragging && blockWhenPointerOverUi && EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(pointerId))
            {
                return;
            }

            if (pressedThisFrame && !_dragging)
            {
                Vector3 world = ScreenToWorld(screenPos);
                if (IsOverPickup(world))
                {
                    _dragging = true;
                    _grabOffsetX = transform.position.x - world.x;
                }
            }

            if (!_dragging)
            {
                return;
            }

            if (releasedThisFrame)
            {
                _dragging = false;
                return;
            }

            Vector3 w = ScreenToWorld(screenPos);
            float x = w.x + _grabOffsetX;
            x = ClampX(x);
            Vector3 p = transform.position;
            p.x = x;
            transform.position = p;
        }

        private bool IsOverPickup(Vector3 world)
        {
            float dx = world.x - transform.position.x;
            float dy = world.y - transform.position.y;
            return dx * dx + dy * dy <= pickupRadius * pickupRadius;
        }

        private float ClampX(float x)
        {
            float halfWidth = _cam.orthographicSize * _cam.aspect;
            float cx = _cam.transform.position.x;
            float minX = cx - halfWidth + horizontalMargin;
            float maxX = cx + halfWidth - horizontalMargin;
            return Mathf.Clamp(x, minX, maxX);
        }

        private Vector3 ScreenToWorld(Vector2 screen)
        {
            float zPlane = Mathf.Abs(_cam.transform.position.z - transform.position.z);
            Vector3 w = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, zPlane));
            w.z = transform.position.z;
            return w;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
#endif
    }
}
