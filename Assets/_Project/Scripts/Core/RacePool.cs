using System.Collections.Generic;
using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Pool sencillo por prefab. Si un objeto se crea por código (sin prefab),
    /// RacePool.Release lo destruye en lugar de reciclarlo.
    /// </summary>
    public static class RacePool
    {
        static readonly Dictionary<GameObject, Queue<GameObject>> Pools =
            new Dictionary<GameObject, Queue<GameObject>>();

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (Pools.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                while (queue.Count > 0)
                {
                    GameObject pooled = queue.Dequeue();
                    if (pooled == null) continue;

                    pooled.transform.SetPositionAndRotation(position, rotation);
                    pooled.SetActive(true);
                    PooledTag tag = pooled.GetComponent<PooledTag>();
                    if (tag != null) tag.sourcePrefab = prefab;
                    return pooled;
                }
            }

            GameObject instance = Object.Instantiate(prefab, position, rotation);
            PooledTag newTag = instance.GetComponent<PooledTag>();
            if (newTag == null) newTag = instance.AddComponent<PooledTag>();
            newTag.sourcePrefab = prefab;
            return instance;
        }

        public static void Release(GameObject instance)
        {
            if (instance == null) return;

            PooledTag tag = instance.GetComponent<PooledTag>();
            if (tag == null || tag.sourcePrefab == null)
            {
                Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);
            if (!Pools.TryGetValue(tag.sourcePrefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                Pools[tag.sourcePrefab] = queue;
            }
            queue.Enqueue(instance);
        }

        public static void Clear() => Pools.Clear();
    }
}
