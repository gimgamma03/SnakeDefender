using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SnakeDefender
{
    [DefaultExecutionOrder(-100)]
    public class WorldSegmentUIManager : MonoBehaviour
    {
        public static WorldSegmentUIManager Instance { get; private set; }

        [Header("Required References")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform canvasRect;

        [Header("Optional References")]
        [Tooltip("If empty, Camera.main is used.")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector3 worldOffset = new Vector3(0.06f, 0.35f, 0f);

        [Header("Prefabs")]
        [Tooltip("몸통 HP 숫자(TMP). 슬라이더 불필요.")]
        [FormerlySerializedAs("hpBarPrefab")]
        [SerializeField] private GameObject hpTextPrefab;
        [SerializeField] private GameObject damageTextPrefab;

        [Header("Damage popup")]
        [SerializeField] private Color damageColor = Color.white;

        private readonly List<Entry> entries = new List<Entry>();
        private readonly Stack<DamageFloatText> damagePool = new Stack<DamageFloatText>();
        private Camera cachedUiCamera;

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

                if (canvasRect == null)
                {
                    continue;
                }

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screenPos, cachedUiCamera, out Vector2 localPoint))
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
            if (segment == null || hpTextPrefab == null || canvasRect == null)
            {
                return;
            }

            GameObject go = Instantiate(hpTextPrefab, canvasRect);
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
            if (damageTextPrefab == null || canvasRect == null)
            {
                return;
            }

            DamageFloatText popup = GetDamagePopup();
            RectTransform rt = popup.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.SetParent(canvasRect, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            Vector3 screenPos = worldCamera != null
                ? worldCamera.WorldToScreenPoint(worldPosition + worldOffset)
                : worldPosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, cachedUiCamera, out Vector2 localPoint) && rt != null)
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

            GameObject go = Instantiate(damageTextPrefab, transform);
            return go.GetComponent<DamageFloatText>();
        }

        public void ReturnDamagePopup(DamageFloatText popup)
        {
            if (popup == null)
            {
                return;
            }

            popup.transform.SetParent(transform, false);
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
