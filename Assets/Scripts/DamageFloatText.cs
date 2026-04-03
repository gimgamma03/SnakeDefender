using System.Collections;
using TMPro;
using UnityEngine;

namespace SnakeDefender
{
    public class DamageFloatText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float riseSpeed = 120f;
        [SerializeField] private float lifetime = 0.7f;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void Play(float damage, Color color)
        {
            if (text != null)
            {
                text.text = Mathf.CeilToInt(damage).ToString();
                text.color = color;
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
                if (text != null)
                {
                    c.a = 1f - (t / lifetime);
                    text.color = c;
                }

                yield return null;
            }

            gameObject.SetActive(false);
            WorldSegmentUIManager.Instance?.ReturnDamagePopup(this);
        }
    }
}
