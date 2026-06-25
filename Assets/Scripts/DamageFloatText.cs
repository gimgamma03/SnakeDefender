using System.Collections;
using TMPro;
using UnityEngine;

namespace SnakeDefender
{
    public class DamageFloatText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float riseSpeed = 120f;
        [SerializeField] private float lifetime = 0.7f;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public void Play(float damage, Color color)
        {
            if (text != null)
            {
                text.text = Mathf.CeilToInt(damage).ToString();
                text.color = color;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            StopAllCoroutines();
            StartCoroutine(FloatRoutine());
        }

        private IEnumerator FloatRoutine()
        {
            float t = 0f;
            Vector2 start = rectTransform.anchoredPosition;
            Color c = text != null ? text.color : Color.white;

            while (t < lifetime)
            {
                t += Time.deltaTime;
                rectTransform.anchoredPosition = start + new Vector2(0f, riseSpeed * t);
                float alpha = 1f - (t / lifetime);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = alpha;
                }
                else if (text != null)
                {
                    c.a = alpha;
                    text.color = c;
                }

                yield return null;
            }

            PooledObject pooled = GetComponent<PooledObject>();
            if (pooled != null)
            {
                pooled.ReturnToPool();
                yield break;
            }

            gameObject.SetActive(false);
        }
    }
}
