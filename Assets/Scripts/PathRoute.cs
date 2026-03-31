using UnityEngine;

namespace SnakeDefender
{
    public class PathRoute : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private bool rebuildEachFrameInEditor;

        private float[] segmentLengths;
        private float totalLength;
        private bool cacheBuilt;

        public int WaypointCount => waypoints == null ? 0 : waypoints.Length;
        public float TotalLength
        {
            get
            {
                BuildCacheIfNeeded();
                return totalLength;
            }
        }

        public Vector3 GetWaypointPosition(int index)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                return transform.position;
            }

            index = Mathf.Clamp(index, 0, waypoints.Length - 1);
            return waypoints[index].position;
        }

        public Vector3 GetPointAtDistance(float distance)
        {
            BuildCacheIfNeeded();

            if (waypoints == null || waypoints.Length == 0)
            {
                return transform.position;
            }

            if (waypoints.Length == 1)
            {
                return waypoints[0].position;
            }

            distance = Mathf.Clamp(distance, 0f, totalLength);
            float remaining = distance;

            for (int i = 0; i < segmentLengths.Length; i++)
            {
                float segmentLength = segmentLengths[i];
                if (remaining <= segmentLength || i == segmentLengths.Length - 1)
                {
                    Vector3 a = waypoints[i].position;
                    Vector3 b = waypoints[i + 1].position;
                    float t = segmentLength <= Mathf.Epsilon ? 0f : remaining / segmentLength;
                    return Vector3.Lerp(a, b, t);
                }

                remaining -= segmentLength;
            }

            return waypoints[waypoints.Length - 1].position;
        }

        private void Awake()
        {
            BuildCacheIfNeeded();
        }

        private void OnValidate()
        {
            cacheBuilt = false;
            if (rebuildEachFrameInEditor)
            {
                BuildCacheIfNeeded();
            }
        }

        private void BuildCacheIfNeeded()
        {
            if (cacheBuilt)
            {
                return;
            }

            if (waypoints == null || waypoints.Length < 2)
            {
                segmentLengths = new float[0];
                totalLength = 0f;
                cacheBuilt = true;
                return;
            }

            segmentLengths = new float[waypoints.Length - 1];
            totalLength = 0f;

            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null)
                {
                    segmentLengths[i] = 0f;
                    continue;
                }

                float len = Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
                segmentLengths[i] = len;
                totalLength += len;
            }

            cacheBuilt = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null)
                {
                    continue;
                }

                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}
