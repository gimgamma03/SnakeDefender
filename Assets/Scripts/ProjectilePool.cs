using System.Collections.Generic;
using UnityEngine;

namespace SnakeDefender
{
    // VerticalProjectile 한 종류만 담는 로컬 풀임. 태그 싱글톤 없고 프리팹만 인스펙터에 넣으면 됨. 타워에서 Get 하고 총알은 Initialize 부르면 됨.
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

        // 꺼낸 다음에 바깥에서 Initialize 호출해야 함.
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
