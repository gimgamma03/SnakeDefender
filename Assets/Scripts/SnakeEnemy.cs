using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SnakeDefender
{
    public class SnakeEnemy : MonoBehaviour
    {
        [Header("런타임 덮어쓰기 (스포너)")]
        [Tooltip("경로. 스포너 Initialize 시 설정. 비우면 프리팹 기본값.")]
        [SerializeField] private PathRoute route;
        [SerializeField] private float moveSpeed = 1.5f;
        [Tooltip("플레이어(타워). 비우면 머리 접촉 패배 판정을 하지 않음.")]
        [SerializeField] private Transform finalGoalTarget;

        [Header("조절")]
        [SerializeField] private float playerContactRadius = 0.2f;
        [SerializeField] private float playerContactDefeatDelay = 0.3f;

        [Header("필수 프리팹")]
        [SerializeField] private SnakeEnemySegment headPrefab;
        [SerializeField] private SnakeEnemySegment bodyPrefab;

        [Header("모양")]
        [Tooltip("머리 말고 몸통 몇 칸인지. 칸마다 프리팹 하나에 디스크 여러 개 얹어도 됨.")]
        [SerializeField] private int bodyCount = 10;
        [SerializeField] private float segmentSpacing = 0.45f;
        [SerializeField] private float firstBodySpacingMultiplier = 0.8f;
        [SerializeField] private int segmentSortingStride = 100;

        [Header("스탯")]
        [SerializeField] private float headHp = 40f;
        [SerializeField] private float bodyHp = 60f;
        [Tooltip("몸통 N개마다 HP 티어 구분(예: 앞에서부터 3개 단위).")]
        [SerializeField] private int bodyHpTierSegmentCount = 3;
        [Tooltip("티어 상승 시 bodyHp에 가산할 비율. 0.1이면 티어마다 +10%.")]
        [SerializeField] private float bodyHpBonusPerTier = 0.1f;
        [SerializeField] private int killScore = 1;

        [Header("머리 단계 / 분노")]
        [SerializeField] private Sprite phase1HeadSprite;
        [SerializeField] private Sprite phase2HeadSprite;
        [SerializeField] private int head1RageTriggerDestroyedBodies = 3;
        [SerializeField] private int phase2TriggerDestroyedBodies = 6;
        [SerializeField] private int head2RageTriggerDestroyedBodies = 9;
        [SerializeField] private float rageDuration = 2f;
        [SerializeField] private float rageSpeedMultiplier = 1.5f;
        [SerializeField] private Color rageTintColor = new Color(1f, 0.55f, 0.55f, 1f);

        private readonly List<SnakeEnemySegment> segments = new List<SnakeEnemySegment>();
        private readonly List<SegmentCache> segmentCaches = new List<SegmentCache>();
        private GameManager gameFlow;
        private float headDistance;
        private bool dead;
        private float routeLength;
        private float finalLegLength;
        private float baseMoveSpeed;
        private int destroyedBodyCount;

        private bool isPhase2;
        private bool isRaging;
        private bool hasTriggeredHead1Rage;
        private bool hasTriggeredHead2Rage;
        private bool pendingPhase2Transition;

        private SnakeEnemySegment headSegment;
        private SpriteRenderer headRenderer;
        private Color headBaseColor = Color.white;
        private Coroutine rageRoutine;
        private float playerContactElapsed;

        private class SegmentCache
        {
            public SnakeEnemySegment Segment;
            public SpriteRenderer RootRenderer;
            public SnakeBodyVisualChain VisualChain;
        }

        public int BodyCount => Mathf.Max(0, bodyCount);

        public void Initialize(PathRoute pathRoute, GameManager flow, Transform goalTarget = null, int bodyCountOverride = -1)
        {
            route = pathRoute;
            gameFlow = flow;
            if (bodyCountOverride >= 0)
            {
                bodyCount = bodyCountOverride;
            }
            if (goalTarget != null)
            {
                finalGoalTarget = goalTarget;
            }
            headDistance = 0f;
            dead = false;
            routeLength = route == null ? 0f : route.TotalLength;
            finalLegLength = CalculateFinalLegLength();
            baseMoveSpeed = moveSpeed;
            destroyedBodyCount = 0;
            isPhase2 = false;
            isRaging = false;
            hasTriggeredHead1Rage = false;
            hasTriggeredHead2Rage = false;
            pendingPhase2Transition = false;
            BuildSegments();
            CacheHeadRenderer();
            ApplyHeadSpriteForCurrentPhase();
            SetHeadTint(headBaseColor);
            RefreshSegmentPositions();
        }

        private void Update()
        {
            if (dead || route == null || segments.Count == 0)
            {
                return;
            }

            headDistance += moveSpeed * Time.deltaTime;
            RefreshSegmentPositions();

            if (HasHeadTouchedPlayer())
            {
                playerContactElapsed += Time.deltaTime;
                if (playerContactElapsed >= Mathf.Clamp(playerContactDefeatDelay, 0.01f, 5f))
                {
                    dead = true;
                    gameFlow?.NotifyPlayerDefeated();
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                playerContactElapsed = 0f;
            }

            if (headDistance >= GetTotalTrackLength())
            {
                dead = true;
                gameFlow?.NotifyPlayerDefeated();
                Destroy(gameObject);
            }
        }

        public void OnSegmentDestroyed(SnakeEnemySegment destroyedSegment)
        {
            if (dead || destroyedSegment == null)
            {
                return;
            }

            if (!destroyedSegment.CanBeDamaged)
            {
                return;
            }

            int index = segments.IndexOf(destroyedSegment);
            if (index < 0)
            {
                return;
            }

            segments.RemoveAt(index);
            if (index < segmentCaches.Count)
            {
                segmentCaches.RemoveAt(index);
            }
            Destroy(destroyedSegment.gameObject);
            destroyedBodyCount++;
            gameFlow?.NotifyEnemyKilled(killScore);
            PlayerUpgradeScheduler.Instance?.RegisterBodySegmentDestroyed();
            EvaluateHeadStateByDestroyedBodies();

            // 몸통 하나 터지면 앞쪽이 한 칸 당겨지는 느낌으로 headDistance 줄이는 거임.
            headDistance = Mathf.Max(0f, headDistance - segmentSpacing);
            RefreshSegmentPositions();

            // 머리는 진행용이고 몸통 다 없으면 이 스네이크는 끝난 거임.
            if (segments.Count <= 1)
            {
                dead = true;
                Destroy(gameObject);
            }
        }

        private void BuildSegments()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            segments.Clear();
            segmentCaches.Clear();

            if (headPrefab == null || bodyPrefab == null)
            {
                return;
            }

            SnakeEnemySegment head = Instantiate(headPrefab, transform);
            head.Initialize(this, headHp, false);
            AddSegment(head);
            headSegment = head;

            int tierSize = Mathf.Max(1, bodyHpTierSegmentCount);
            float bonus = Mathf.Max(0f, bodyHpBonusPerTier);
            for (int i = 0; i < bodyCount; i++)
            {
                SnakeEnemySegment body = Instantiate(bodyPrefab, transform);
                int tier = i / tierSize;
                float scaledHp = bodyHp * (1f + bonus * tier);
                body.Initialize(this, scaledHp);
                // 첫 몸통만 처음부터 켜두고 나머지는 거리 되면 켜짐.
                body.gameObject.SetActive(i == 0);
                AddSegment(body);
            }

            StartCoroutine(RegisterDamageableSegmentsWithUiNextFrame());
        }

        private IEnumerator RegisterDamageableSegmentsWithUiNextFrame()
        {
            yield return null;

            if (dead || WorldSegmentUIManager.Instance == null)
            {
                yield break;
            }

            foreach (SnakeEnemySegment seg in segments)
            {
                if (seg != null && seg.CanBeDamaged)
                {
                    WorldSegmentUIManager.Instance.Register(seg);
                }
            }
        }

        private void RefreshSegmentPositions()
        {
            if (route == null)
            {
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                SnakeEnemySegment segment = segments[i];
                if (segment == null)
                {
                    continue;
                }

                float activationDistance = GetDistanceBehindHead(i);
                bool shouldBeActive = i <= 1 || headDistance >= activationDistance;
                if (segment.gameObject.activeSelf != shouldBeActive)
                {
                    segment.gameObject.SetActive(shouldBeActive);
                }

                if (!shouldBeActive)
                {
                    continue;
                }

                float dist = Mathf.Max(0f, headDistance - GetDistanceBehindHead(i));
                Vector3 newPos = GetTrackPoint(dist);
                segment.transform.position = newPos;
                segment.transform.rotation = Quaternion.identity;

                int segmentBaseSortingOrder = -i * Mathf.Max(1, segmentSortingStride);
                SegmentCache cache = i < segmentCaches.Count ? segmentCaches[i] : null;
                SpriteRenderer rootRenderer = cache != null ? cache.RootRenderer : null;
                if (rootRenderer != null)
                {
                    rootRenderer.sortingOrder = segmentBaseSortingOrder;
                }

                SnakeBodyVisualChain visualChain = cache != null ? cache.VisualChain : null;
                if (visualChain != null)
                {
                    visualChain.Refresh(route, dist, finalGoalTarget, routeLength, segmentBaseSortingOrder);
                }
            }
        }

        private void AddSegment(SnakeEnemySegment segment)
        {
            segments.Add(segment);
            segmentCaches.Add(new SegmentCache
            {
                Segment = segment,
                RootRenderer = segment != null ? segment.GetComponent<SpriteRenderer>() : null,
                VisualChain = segment != null ? segment.GetComponent<SnakeBodyVisualChain>() : null
            });
        }

        private float CalculateFinalLegLength()
        {
            if (route == null || finalGoalTarget == null || route.WaypointCount <= 0)
            {
                return 0f;
            }

            Vector3 lastPoint = route.GetWaypointPosition(route.WaypointCount - 1);
            return Vector3.Distance(lastPoint, finalGoalTarget.position);
        }

        private float GetTotalTrackLength()
        {
            return routeLength + finalLegLength;
        }

        private Vector3 GetTrackPoint(float distance)
        {
            if (route == null)
            {
                return transform.position;
            }

            if (distance <= routeLength || finalGoalTarget == null || finalLegLength <= Mathf.Epsilon || route.WaypointCount <= 0)
            {
                return route.GetPointAtDistance(distance);
            }

            Vector3 lastPoint = route.GetWaypointPosition(route.WaypointCount - 1);
            float extra = Mathf.Clamp(distance - routeLength, 0f, finalLegLength);
            float t = extra / finalLegLength;
            return Vector3.Lerp(lastPoint, finalGoalTarget.position, t);
        }

        private float GetDistanceBehindHead(int segmentIndex)
        {
            if (segmentIndex <= 0)
            {
                return 0f;
            }

            float firstGap = segmentSpacing * Mathf.Clamp(firstBodySpacingMultiplier, 0.1f, 1f);
            if (segmentIndex == 1)
            {
                return firstGap;
            }

            return firstGap + (segmentIndex - 1) * segmentSpacing;
        }

        private bool HasHeadTouchedPlayer()
        {
            if (finalGoalTarget == null || headSegment == null)
            {
                return false;
            }

            float sqrDist = (headSegment.transform.position - finalGoalTarget.position).sqrMagnitude;
            float radius = Mathf.Max(0.01f, playerContactRadius);
            return sqrDist <= radius * radius;
        }

        private void EvaluateHeadStateByDestroyedBodies()
        {
            if (!hasTriggeredHead1Rage && destroyedBodyCount >= head1RageTriggerDestroyedBodies)
            {
                hasTriggeredHead1Rage = true;
                StartRage(isForPhase2: false);
                return;
            }

            if (!isPhase2 && destroyedBodyCount >= phase2TriggerDestroyedBodies)
            {
                if (isRaging)
                {
                    pendingPhase2Transition = true;
                }
                else
                {
                    EnterPhase2();
                }

                return;
            }

            if (isPhase2 && !hasTriggeredHead2Rage && destroyedBodyCount >= head2RageTriggerDestroyedBodies)
            {
                hasTriggeredHead2Rage = true;
                StartRage(isForPhase2: true);
            }
        }

        private void EnterPhase2()
        {
            isPhase2 = true;
            ApplyHeadSpriteForCurrentPhase();
            SetHeadTint(headBaseColor);
        }

        private void StartRage(bool isForPhase2)
        {
            if (rageRoutine != null)
            {
                StopCoroutine(rageRoutine);
            }

            rageRoutine = StartCoroutine(RageRoutine(isForPhase2));
        }

        private IEnumerator RageRoutine(bool isForPhase2)
        {
            isRaging = true;
            moveSpeed = baseMoveSpeed * rageSpeedMultiplier;
            SetHeadTint(rageTintColor);

            yield return new WaitForSeconds(rageDuration);

            isRaging = false;
            moveSpeed = baseMoveSpeed;
            SetHeadTint(headBaseColor);

            if (!isForPhase2 && !isPhase2 && (pendingPhase2Transition || destroyedBodyCount >= phase2TriggerDestroyedBodies))
            {
                pendingPhase2Transition = false;
                EnterPhase2();
            }

            rageRoutine = null;
        }

        private void CacheHeadRenderer()
        {
            if (headSegment == null)
            {
                return;
            }

            headRenderer = headSegment.GetComponentInChildren<SpriteRenderer>(true);
            if (headRenderer != null)
            {
                headBaseColor = headRenderer.color;
            }
        }

        private void ApplyHeadSpriteForCurrentPhase()
        {
            if (headRenderer == null)
            {
                return;
            }

            Sprite nextSprite = isPhase2 ? phase2HeadSprite : phase1HeadSprite;
            if (nextSprite != null)
            {
                headRenderer.sprite = nextSprite;
            }
        }

        private void SetHeadTint(Color color)
        {
            if (headRenderer != null)
            {
                headRenderer.color = color;
            }
        }

    }
}
