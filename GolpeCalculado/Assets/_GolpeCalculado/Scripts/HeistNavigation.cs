using UnityEngine;
using UnityEngine.AI;
namespace GolpeCalculado
{
    [DefaultExecutionOrder(-1000)]
    public sealed class HeistNavigation : MonoBehaviour
    {
        public NavMeshData data;
        NavMeshDataInstance instance;
        void OnEnable() { if(data!=null)instance=NavMesh.AddNavMeshData(data); }
        void OnDisable() { if(instance.valid)instance.Remove(); }
    }
}
