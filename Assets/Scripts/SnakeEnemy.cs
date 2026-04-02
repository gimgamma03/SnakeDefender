using System.Collections.Generic;
using UnityEngine;

namespace SnakeDefender
{
    public class SnakeEnemy : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private PathRoute route;
        [SerializeField] private float moveSpeed = 1.5f;

        [Header("Shape")]
        [SerializeField] private SnakeEnemySegment headPrefab;
        [SerializeField] private SnakeEnemySegment bodyPrefab;
        [Tooltip("Gameplay body parts behind the head. Each part can use one prefab with many child sprites for visuals.")]
        [SerializeField] private int bodyCount = 10;
        [SerializeField] private float segmentSpacing = 0.45f;

        [Header("Stats")]
        [SerializeField] private float headHp = 40f;
        [SerializeField] private float bodyHp = 60f;
        [SerializeField] private int killScore = 1;

        private readonly List<SnakeEnemySegment> segments = new List<SnakeEnemySegment>();
        private GameFlowController gameFlow;
        private float headDistance;
        private bool dead;

        public void Initialize(PathRoute pathRoute, GameFlowController flow)
        {
            route = pathRoute;
            gameFlow = flow;
            headDistance = 0f;
            dead = false;
            BuildSegments();
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

            if (headDistance >= route.TotalLength)
            {
                dead = true;
                gameFlow?.NotifyEnemyReachedGoal();
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
            Destroy(destroyedSegment.gameObject);

            // Pull head/front body one slot backward after a body break.
            headDistance = Mathf.Max(0f, headDistance - segmentSpacing);
            RefreshSegmentPositions();

            // Head is a progress marker; enemy is considered dead when all body segments are gone.
            if (segments.Count <= 1)
            {
                dead = true;
                gameFlow?.NotifyEnemyKilled(killScore);
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

            if (headPrefab == null || bodyPrefab == null)
            {
                return;
            }

            SnakeEnemySegment head = Instantiate(headPrefab, transform);
            head.Initialize(this, headHp, false);
            segments.Add(head);

            for (int i = 0; i < bodyCount; i++)
            {
                SnakeEnemySegment body = Instantiate(bodyPrefab, transform);
                body.Initialize(this, bodyHp);
                // First body should follow the head immediately from start.
                body.gameObject.SetActive(i == 0);
                segments.Add(body);
            }
        }

        private void RefreshSegmentPositions()
        {
            if (route == null)
            {
                return;
            }

            float tangentSample = Mathf.Max(0.02f, segmentSpacing * 0.25f);
            float pathLen = route.TotalLength;

            for (int i = 0; i < segments.Count; i++)
            {
                SnakeEnemySegment segment = segments[i];
                if (segment == null)
                {
                    continue;
                }

                bool shouldBeActive = i <= 1 || headDistance >= segmentSpacing * i;
                if (segment.gameObject.activeSelf != shouldBeActive)
                {
                    segment.gameObject.SetActive(shouldBeActive);
                }

                if (!shouldBeActive)
                {
                    continue;
                }

                float dist = Mathf.Max(0f, headDistance - (segmentSpacing * i));
                Vector3 newPos = route.GetPointAtDistance(dist);
                segment.transform.position = newPos;

                Vector2 tangent = GetPathTangentForward(route, dist, pathLen, tangentSample);
                if (tangent.sqrMagnitude > 0.0001f)
                {
                    float z = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                    segment.transform.rotation = Quaternion.Euler(0f, 0f, z);
                }

                SnakeBodyVisualChain visualChain = segment.GetComponent<SnakeBodyVisualChain>();
                if (visualChain != null)
                {
                    visualChain.Refresh(route, dist);
                }
            }
        }

        private static Vector2 GetPathTangentForward(PathRoute pathRoute, float distanceAlongPath, float pathLength, float sampleDelta)
        {
            if (pathRoute == null || pathLength <= Mathf.Epsilon)
            {
                return Vector2.right;
            }

            float d0 = Mathf.Clamp(distanceAlongPath - sampleDelta, 0f, pathLength);
            float d1 = Mathf.Clamp(distanceAlongPath + sampleDelta, 0f, pathLength);
            if (Mathf.Approximately(d0, d1))
            {
                d0 = Mathf.Max(0f, distanceAlongPath - sampleDelta * 2f);
                d1 = Mathf.Min(pathLength, distanceAlongPath + sampleDelta * 2f);
            }

            Vector3 p0 = pathRoute.GetPointAtDistance(d0);
            Vector3 p1 = pathRoute.GetPointAtDistance(d1);
            Vector2 tangent = (Vector2)(p1 - p0);
            return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.right;
        }
    }
}
