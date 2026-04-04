using System.Collections.Generic;
using UnityEngine;

namespace SnakeDefender
{
    /// <summary>
    /// 한 종류의 <see cref="VerticalProjectile"/>만 담는 로컬 풀. 태그/싱글톤 없이 인스펙터에 프리팹만 넣고,
    /// 타워 등에서 참조해 <see cref="Get"/> / 총알 쪽 <see cref="VerticalProjectile.ReleaseToPool"/>로 사용합니다.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private VerticalProjectile prefab;
        [SerializeField] private int prewarmCount = 24;
        [SerializeField] private bool allowExpand = true;

        private readonly Queue<VerticalProjectile> available = new Queue<VerticalProjectile>();
        private Transform poolRoot;

        private void Awake()
        {
            poolRoot = new GameObject("PooledProjectiles").transform;
            poolRoot.SetParent(transform, false);

            if (prefab == null)
            {
                return;
            }

            for (int i = 0; i < prewarmCount; i++)
            {
                VerticalProjectile p = CreatePooledInstance();
                p.gameObject.SetActive(false);
                available.Enqueue(p);
            }
        }

        private VerticalProjectile CreatePooledInstance()
        {
            VerticalProjectile p = Instantiate(prefab, poolRoot);
            p.BindPool(this);
            return p;
        }

        /// <summary>월드에 꺼낸 총알. 호출 측에서 <see cref="VerticalProjectile.Initialize"/> 호출.</summary>
        public VerticalProjectile Get(Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            VerticalProjectile p;
            if (available.Count > 0)
            {
                p = available.Dequeue();
            }
            else if (allowExpand)
            {
                p = CreatePooledInstance();
            }
            else
            {
                return null;
            }

            p.transform.SetPositionAndRotation(position, rotation);
            p.gameObject.SetActive(true);
            return p;
        }

        internal void Release(VerticalProjectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.ClearForPool();
            projectile.transform.SetParent(poolRoot, false);
            projectile.gameObject.SetActive(false);
            available.Enqueue(projectile);
        }
    }
}
