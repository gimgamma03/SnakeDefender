using System.Collections.Generic;
using UnityEngine;

namespace SnakeDefender
{
    public enum PoolId
    {
        DamagePopup = 0
    }

    [DefaultExecutionOrder(-200)]
    public class GameObjectPoolManager : MonoBehaviour
    {
        public static GameObjectPoolManager Instance { get; private set; }

        [System.Serializable]
        private class PoolConfig
        {
            public PoolId id = PoolId.DamagePopup;
            public GameObject prefab;
            [Min(0)] public int initialSize = 8;
            public bool canExpand = true;
            public Transform defaultParent;
        }

        private sealed class PoolRuntime
        {
            public PoolConfig Config;
            public readonly Queue<PooledObject> Inactive = new Queue<PooledObject>();
        }

        [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();

        private readonly Dictionary<PoolId, PoolRuntime> poolMap = new Dictionary<PoolId, PoolRuntime>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public GameObject Spawn(PoolId id, Transform parentOverride = null)
        {
            if (!poolMap.TryGetValue(id, out PoolRuntime pool))
            {
                Debug.LogWarning($"[GameObjectPoolManager] Pool not found: {id}", this);
                return null;
            }

            PooledObject pooled = null;
            while (pool.Inactive.Count > 0 && pooled == null)
            {
                pooled = pool.Inactive.Dequeue();
            }

            if (pooled == null)
            {
                if (!pool.Config.canExpand)
                {
                    return null;
                }

                pooled = CreatePooledObject(pool);
                if (pooled == null)
                {
                    return null;
                }
            }

            Transform targetParent = parentOverride != null ? parentOverride : ResolveParent(pool.Config.defaultParent);
            if (targetParent != null)
            {
                pooled.transform.SetParent(targetParent, false);
            }

            pooled.IsInPool = false;
            pooled.gameObject.SetActive(true);
            return pooled.gameObject;
        }

        public void Return(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            PooledObject pooled = instance.GetComponent<PooledObject>();
            if (pooled == null || pooled.Owner != this)
            {
                Debug.LogWarning("[GameObjectPoolManager] Returned object is not owned by this pool manager.", instance);
                return;
            }

            if (pooled.IsInPool)
            {
                return;
            }

            if (!poolMap.TryGetValue(pooled.PoolId, out PoolRuntime pool))
            {
                Debug.LogWarning($"[GameObjectPoolManager] Missing runtime pool for {pooled.PoolId}", instance);
                return;
            }

            pooled.IsInPool = true;
            instance.SetActive(false);

            Transform defaultParent = ResolveParent(pool.Config.defaultParent);
            if (defaultParent != null)
            {
                instance.transform.SetParent(defaultParent, false);
            }
            else
            {
                instance.transform.SetParent(transform, false);
            }

            pool.Inactive.Enqueue(pooled);
        }

        private void InitializePools()
        {
            poolMap.Clear();

            for (int i = 0; i < poolConfigs.Count; i++)
            {
                PoolConfig config = poolConfigs[i];
                if (config == null || config.prefab == null)
                {
                    continue;
                }

                if (poolMap.ContainsKey(config.id))
                {
                    Debug.LogWarning($"[GameObjectPoolManager] Duplicate pool id: {config.id}", this);
                    continue;
                }

                PoolRuntime runtime = new PoolRuntime
                {
                    Config = config
                };

                poolMap.Add(config.id, runtime);

                for (int j = 0; j < config.initialSize; j++)
                {
                    PooledObject pooled = CreatePooledObject(runtime);
                    if (pooled == null)
                    {
                        break;
                    }

                    runtime.Inactive.Enqueue(pooled);
                }
            }
        }

        private PooledObject CreatePooledObject(PoolRuntime pool)
        {
            Transform parent = ResolveParent(pool.Config.defaultParent);
            GameObject go = Instantiate(pool.Config.prefab, parent != null ? parent : transform);
            go.SetActive(false);

            PooledObject pooled = go.GetComponent<PooledObject>();
            if (pooled == null)
            {
                pooled = go.AddComponent<PooledObject>();
            }

            pooled.Owner = this;
            pooled.PoolId = pool.Config.id;
            pooled.IsInPool = true;
            return pooled;
        }

        private Transform ResolveParent(Transform configuredParent)
        {
            return configuredParent != null ? configuredParent : transform;
        }
    }

    public class PooledObject : MonoBehaviour
    {
        public GameObjectPoolManager Owner { get; set; }
        public PoolId PoolId { get; set; }
        public bool IsInPool { get; set; } = true;

        public void ReturnToPool()
        {
            Owner?.Return(gameObject);
        }
    }
}
