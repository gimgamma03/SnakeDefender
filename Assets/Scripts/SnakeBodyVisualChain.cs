using UnityEngine;

namespace SnakeDefender
{
    // 몸통 한 칸(SnakeEnemySegment)에 붙이는 거고, 경로 따라 디스크 여러 개 겹쳐 보이게 배치하는 거임. 맞는 건 루트 세그먼트랑 콜라이더만 씀.
    public class SnakeBodyVisualChain : MonoBehaviour
    {
        [SerializeField] private int discCount = 4;
        [SerializeField] private float discSpacingAlongPath = 0.12f;
        [SerializeField] private GameObject discPrefab;
        [SerializeField] private int discOrderStep = -1;
        [SerializeField] private bool autoAssignDiscSortingOrder = true;

        private Transform[] discs;
        private SpriteRenderer[][] discRenderers;
        private int[] lastDiscSortingOrders;
        private bool built;
        private int lastSegmentBaseSortingOrder = int.MinValue;

        public void Refresh(PathRoute pathRoute, float anchorDistanceAlongPath, Transform finalGoalTarget = null, float routeLength = 0f, int segmentBaseSortingOrder = 0)
        {
            if (pathRoute == null || discPrefab == null || discCount <= 0)
            {
                return;
            }

            EnsureDiscs(pathRoute);

            float pathLen = routeLength > 0f ? routeLength : pathRoute.TotalLength;
            float finalLegLength = CalculateFinalLegLength(pathRoute, finalGoalTarget);

            for (int k = 0; k < discs.Length; k++)
            {
                if (discs[k] == null)
                {
                    continue;
                }

                float d = Mathf.Max(0f, anchorDistanceAlongPath - (discSpacingAlongPath * k));
                Vector3 pos = GetTrackPoint(pathRoute, d, pathLen, finalGoalTarget, finalLegLength);
                discs[k].SetPositionAndRotation(pos, Quaternion.identity);
                if (autoAssignDiscSortingOrder)
                {
                    ApplyDiscSortingOrder(k, segmentBaseSortingOrder);
                }
            }

            lastSegmentBaseSortingOrder = segmentBaseSortingOrder;
        }

        private void EnsureDiscs(PathRoute pathRoute)
        {
            if (built && discs != null && discs.Length == discCount)
            {
                return;
            }

            if (discs != null)
            {
                for (int i = 0; i < discs.Length; i++)
                {
                    if (discs[i] != null)
                    {
                        Destroy(discs[i].gameObject);
                    }
                }
            }

            discs = new Transform[discCount];
            discRenderers = new SpriteRenderer[discCount][];
            lastDiscSortingOrders = new int[discCount];
            for (int i = 0; i < discCount; i++)
            {
                GameObject go = Instantiate(discPrefab, transform);
                go.name = $"Disc_{i}";

                SnakeEnemySegment seg = go.GetComponent<SnakeEnemySegment>();
                if (seg != null && seg != GetComponent<SnakeEnemySegment>())
                {
                    Destroy(seg);
                }

                discs[i] = go.transform;
                discRenderers[i] = go.GetComponentsInChildren<SpriteRenderer>(true);
                lastDiscSortingOrders[i] = int.MinValue;
            }

            built = true;
            lastSegmentBaseSortingOrder = int.MinValue;
        }

        private void ApplyDiscSortingOrder(int discIndex, int segmentBaseSortingOrder)
        {
            if (discRenderers == null || discIndex < 0 || discIndex >= discRenderers.Length)
            {
                return;
            }

            int sortingOrder = segmentBaseSortingOrder + (discIndex * discOrderStep);
            if (lastSegmentBaseSortingOrder == segmentBaseSortingOrder &&
                lastDiscSortingOrders != null &&
                discIndex < lastDiscSortingOrders.Length &&
                lastDiscSortingOrders[discIndex] == sortingOrder)
            {
                return;
            }

            SpriteRenderer[] renderers = discRenderers[discIndex];
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                // 디스크 하나 안에서는 정렬 순서 같게 맞추는 거임.
                renderers[i].sortingOrder = sortingOrder;
            }

            if (lastDiscSortingOrders != null && discIndex < lastDiscSortingOrders.Length)
            {
                lastDiscSortingOrders[discIndex] = sortingOrder;
            }
        }

        private static float CalculateFinalLegLength(PathRoute pathRoute, Transform finalGoalTarget)
        {
            if (pathRoute == null || finalGoalTarget == null || pathRoute.WaypointCount <= 0)
            {
                return 0f;
            }

            Vector3 lastPoint = pathRoute.GetWaypointPosition(pathRoute.WaypointCount - 1);
            return Vector3.Distance(lastPoint, finalGoalTarget.position);
        }

        private static Vector3 GetTrackPoint(PathRoute pathRoute, float distance, float routeLength, Transform finalGoalTarget, float finalLegLength)
        {
            if (pathRoute == null)
            {
                return Vector3.zero;
            }

            if (distance <= routeLength || finalGoalTarget == null || finalLegLength <= Mathf.Epsilon || pathRoute.WaypointCount <= 0)
            {
                return pathRoute.GetPointAtDistance(distance);
            }

            Vector3 lastPoint = pathRoute.GetWaypointPosition(pathRoute.WaypointCount - 1);
            float extra = Mathf.Clamp(distance - routeLength, 0f, finalLegLength);
            float t = extra / finalLegLength;
            return Vector3.Lerp(lastPoint, finalGoalTarget.position, t);
        }

    }
}
