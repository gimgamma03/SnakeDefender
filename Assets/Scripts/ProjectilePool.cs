using UnityEngine;

namespace SnakeDefender
{
    // ObjectPool_Projectile 오브젝트에 붙여두고, 내부적으로 GameObjectPoolManager의 Projectile 풀을 사용.
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private GameObjectPoolManager poolManager;
        [SerializeField] private PoolId projectilePoolId = PoolId.Projectile;
        [SerializeField] private Transform spawnParentOverride;

        private void OnEnable()
        {
            if (poolManager == null)
            {
                poolManager = GameObjectPoolManager.Instance;
            }
        }

        // 꺼낸 다음에 바깥에서 Initialize 호출.
        public VerticalProjectile Get(Vector3 position, Quaternion rotation)
        {
            if (poolManager == null)
            {
                return null;
            }

            Transform parent = spawnParentOverride != null ? spawnParentOverride : transform;
            GameObject pooledObject = poolManager.Spawn(projectilePoolId, parent);
            if (pooledObject == null)
            {
                return null;
            }

            VerticalProjectile p = pooledObject.GetComponent<VerticalProjectile>();
            if (p == null)
            {
                poolManager.Return(pooledObject);
                return null;
            }

            p.BindPool(this);
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
            if (poolManager == null)
            {
                Destroy(projectile.gameObject);
                return;
            }

            poolManager.Return(projectile.gameObject);
        }
    }
}
