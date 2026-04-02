using UnityEngine;

namespace SnakeDefender
{
    /// <summary>
    /// 한 gameplay 몸통 칸(SnakeEnemySegment)에 붙여, 경로를 따라 겹쳐 보이는 디스크를 여러 개 배치합니다.
    /// 피격/체력은 루트의 SnakeEnemySegment(+ Collider)만 사용합니다.
    /// </summary>
    public class SnakeBodyVisualChain : MonoBehaviour
    {
        [SerializeField] private int discCount = 4;
        [SerializeField] private float discSpacingAlongPath = 0.12f;
        [SerializeField] private GameObject discPrefab;

        private Transform[] discs;
        private bool built;

        public void Refresh(PathRoute pathRoute, float anchorDistanceAlongPath)
        {
            if (pathRoute == null || discPrefab == null || discCount <= 0)
            {
                return;
            }

            EnsureDiscs(pathRoute);

            float pathLen = pathRoute.TotalLength;
            float sample = Mathf.Max(0.02f, discSpacingAlongPath * 0.25f);

            for (int k = 0; k < discs.Length; k++)
            {
                if (discs[k] == null)
                {
                    continue;
                }

                float d = Mathf.Max(0f, anchorDistanceAlongPath - (discSpacingAlongPath * k));
                Vector3 pos = pathRoute.GetPointAtDistance(d);
                discs[k].SetPositionAndRotation(pos, Quaternion.identity);

                Vector2 tan = TangentForward(pathRoute, d, pathLen, sample);
                if (tan.sqrMagnitude > 0.0001f)
                {
                    float z = Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg;
                    discs[k].rotation = Quaternion.Euler(0f, 0f, z);
                }
            }
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
            }

            built = true;
        }

        private static Vector2 TangentForward(PathRoute pathRoute, float distanceAlongPath, float pathLength, float sampleDelta)
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
            Vector2 t = (Vector2)(p1 - p0);
            return t.sqrMagnitude > 0.0001f ? t.normalized : Vector2.right;
        }
    }
}
