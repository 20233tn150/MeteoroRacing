using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>Marca una instancia con el prefab del que salió, para poder devolverla al pool.</summary>
    [DisallowMultipleComponent]
    public sealed class PooledTag : MonoBehaviour
    {
        [HideInInspector] public GameObject sourcePrefab;
    }
}
