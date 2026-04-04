using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SnakeDefender
{
    [DefaultExecutionOrder(-100)]
    public class WorldSegmentUIManager : MonoBehaviour
    {
        public static WorldSegmentUIManager Instance { get; private set; }

        [Header("필수 참조")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform canvasRect;
        [Tooltip("HP·데미지 숫자의 부모 Rect. 비우면 캔버스 루트에 배치. 전체 화면 패널 권장.")]
        [SerializeField] private RectTransform worldHudRoot;

        [Header("선택 참조")]
        [Tooltip("비우면 Main Camera.")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector3 worldOffset = new Vector3(0.06f, 0.35f, 0f);

        [Header("프리팹")]
        [Tooltip("몸통 위 HP 표시용 프리팹(TextMeshPro 등).")]
        [FormerlySerializedAs("hpBarPrefab")]
        [SerializeField] private GameObject hpTextPrefab;
        [Tooltip("피해 시 표시할 데미지 숫자 프리팹. HP와 동일 구조로 재사용 가능.")]
        [SerializeField] private GameObject damageTextPrefab;

        [Header("데미지 색")]
        [SerializeField] private Color damageColor = Color.white;

        private readonly List<Entry> entries = new List<Entry>();
        private readonly Stack<DamageFloatText> damagePool = new Stack<DamageFloatText>();
        private Camera cachedUiCamera;

        // 몸통 HP랑 데미지 숫자 붙일 부모. worldHudRoot 비었으면 canvasRect 씀.
        private RectTransform LayoutRect => worldHudRoot != null ? worldHudRoot : canvasRect;

        private class Entry
        {
            public SnakeEnemySegment Segment;
            public RectTransform Root;
            public SegmentHpBarView View;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rootCanvas == null || canvasRect == null || hpTextPrefab == null || damageTextPrefab == null)
            {
                Debug.LogWarning("[WorldSegmentUIManager] Missing required references (Canvas/Rect/HP Text/Damage Text).", this);
            }

            if (worldHudRoot != null && canvasRect != null && worldHudRoot != canvasRect && !worldHudRoot.IsChildOf(canvasRect))
            {
                Debug.LogWarning("[WorldSegmentUIManager] worldHudRoot should be a child of the same Canvas as canvasRect.", this);
            }
        }
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (canvasRect == null && rootCanvas != null)
            {
                canvasRect = rootCanvas.transform as RectTransform;
            }

            CacheUiCamera();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry e = entries[i];
                if (e.Segment == null || e.Root == null)
                {
                    if (e.Root != null)
                    {
                        Destroy(e.Root.gameObject);
                    }

                    entries.RemoveAt(i);
                    continue;
                }

                bool show = e.Segment.gameObject.activeInHierarchy && e.Segment.enabled && e.Segment.CanBeDamaged;
                if (e.Root.gameObject.activeSelf != show)
                {
                    e.Root.gameObject.SetActive(show);
                }
                if (!show)
                {
                    continue;
                }

                Vector3 worldPos = e.Segment.transform.position + worldOffset;
                Vector3 screenPos = worldCamera != null
                    ? worldCamera.WorldToScreenPoint(worldPos)
                    : worldPos;

                if (screenPos.z < 0f)
                {
                    e.Root.gameObject.SetActive(false);
                    continue;
                }

                RectTransform layout = LayoutRect;
                if (layout == null)
                {
                    continue;
                }

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        layout, screenPos, cachedUiCamera, out Vector2 localPoint))
                {
                    e.Root.anchoredPosition = localPoint;
                }

                if (e.View != null)
                {
                    e.View.SetHp(e.Segment.CurrentHp);
                }
            }
        }

        public void Register(SnakeEnemySegment segment)
        {
            RectTransform layout = LayoutRect;
            if (segment == null || hpTextPrefab == null || layout == null)
            {
                return;
            }

            GameObject go = Instantiate(hpTextPrefab, layout);
            RectTransform rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            SegmentHpBarView view = go.GetComponent<SegmentHpBarView>();
            entries.Add(new Entry
            {
                Segment = segment,
                Root = rt,
                View = view
            });

            if (view != null)
            {
                view.SetHp(segment.CurrentHp);
            }
        }

        public void Unregister(SnakeEnemySegment segment)
        {
            if (segment == null)
            {
                return;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Segment == segment)
                {
                    if (entries[i].Root != null)
                    {
                        Destroy(entries[i].Root.gameObject);
                    }

                    entries.RemoveAt(i);
                    return;
                }
            }
        }

        public void NotifyDamage(SnakeEnemySegment segment, float amount, Vector3 worldPosition)
        {
            RectTransform layout = LayoutRect;
            if (damageTextPrefab == null || layout == null)
            {
                return;
            }

            DamageFloatText popup = GetDamagePopup();
            if (popup == null)
            {
                return;
            }

            RectTransform rt = popup.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.SetParent(layout, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            Vector3 screenPos = worldCamera != null
                ? worldCamera.WorldToScreenPoint(worldPosition + worldOffset)
                : worldPosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    layout, screenPos, cachedUiCamera, out Vector2 localPoint) && rt != null)
            {
                rt.anchoredPosition = localPoint;
            }

            popup.gameObject.SetActive(true);
            popup.Play(amount, damageColor);
        }

        private DamageFloatText GetDamagePopup()
        {
            if (damagePool.Count > 0)
            {
                return damagePool.Pop();
            }

            RectTransform layout = LayoutRect;
            if (damageTextPrefab == null || layout == null)
            {
                return null;
            }

            GameObject go = Instantiate(damageTextPrefab, layout);
            return go.GetComponent<DamageFloatText>();
        }

        public void ReturnDamagePopup(DamageFloatText popup)
        {
            if (popup == null)
            {
                return;
            }

            RectTransform layout = LayoutRect;
            if (layout != null)
            {
                popup.transform.SetParent(layout, false);
            }
            else
            {
                popup.transform.SetParent(transform, false);
            }

            damagePool.Push(popup);
        }

        private void CacheUiCamera()
        {
            cachedUiCamera = null;
            Canvas canvas = rootCanvas != null ? rootCanvas : (canvasRect != null ? canvasRect.GetComponentInParent<Canvas>() : null);
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                cachedUiCamera = canvas.worldCamera;
            }
        }
    }
}
