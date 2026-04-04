using System.Collections;
using UnityEngine;

namespace SnakeDefender
{
    public class SnakeEnemySegment : MonoBehaviour
    {
        [SerializeField] private float maxHp = 30f;

        [Header("피격 피드백 (자식 스프라이트 포함, 세그먼트 단위)")]
        [SerializeField] private Color hitTint = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float hitFlashDuration = 0.1f;

        private float currentHp;
        private SnakeEnemy owner;
        private bool initialized;
        private bool canBeDamaged;

        private SpriteRenderer[] visualRenderers;
        private Color[] originalColors;
        private Coroutine hitFlashRoutine;
        private bool isFlashing;

        public bool CanBeDamaged => canBeDamaged;
        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;

        public void Initialize(SnakeEnemy enemyOwner, float hp, bool damageable = true)
        {
            owner = enemyOwner;
            maxHp = hp;
            currentHp = maxHp;
            canBeDamaged = damageable;
            initialized = true;
            CacheVisualRenderers();
        }

        public void TakeDamage(float amount)
        {
            if (!initialized || owner == null || !canBeDamaged || amount <= 0f)
            {
                return;
            }

            currentHp -= amount;
            PlayHitFlash();
            WorldSegmentUIManager.Instance?.NotifyDamage(this, amount, transform.position);

            if (currentHp <= 0f)
            {
                owner.OnSegmentDestroyed(this);
            }
        }

        private void CacheVisualRenderers()
        {
            visualRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalColors = new Color[visualRenderers.Length];
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                originalColors[i] = visualRenderers[i].color;
            }
        }

        private void PlayHitFlash()
        {
            // 디스크는 Initialize 뒤에 생길 수 있어서. 플래시 도는 중에 다시 캐시하면 색 꼬여서 빨간색 고정되는 거 방지하려는 거임.
            if (!isFlashing)
            {
                CacheVisualRenderers();
            }

            if (visualRenderers == null || visualRenderers.Length == 0)
            {
                return;
            }

            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
            }

            hitFlashRoutine = StartCoroutine(HitFlashCoroutine());
        }

        private IEnumerator HitFlashCoroutine()
        {
            isFlashing = true;
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                if (visualRenderers[i] != null)
                {
                    visualRenderers[i].color = hitTint;
                }
            }

            yield return new WaitForSeconds(hitFlashDuration);

            for (int i = 0; i < visualRenderers.Length; i++)
            {
                if (visualRenderers[i] != null && i < originalColors.Length)
                {
                    visualRenderers[i].color = originalColors[i];
                }
            }

            hitFlashRoutine = null;
            isFlashing = false;
        }

        private void OnDestroy()
        {
            WorldSegmentUIManager.Instance?.Unregister(this);
        }

        private void OnDisable()
        {
            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
                hitFlashRoutine = null;
            }

            isFlashing = false;
            if (visualRenderers != null && originalColors != null)
            {
                for (int i = 0; i < visualRenderers.Length; i++)
                {
                    if (visualRenderers[i] != null && i < originalColors.Length)
                    {
                        visualRenderers[i].color = originalColors[i];
                    }
                }
            }
        }
    }
}
