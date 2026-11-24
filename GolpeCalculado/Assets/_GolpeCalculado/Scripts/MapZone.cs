using UnityEngine;

namespace GolpeCalculado
{
    // Zona do mapa do diário (US17), por exemplo o cais ou o escritório.
    // As caixas deste objeto (BoxCollider, Is Trigger) marcam a área no mundo; podem ser várias
    // quando a zona não é retangular (o pátio). A imagem no Canvas do diário (Map Image) só aparece
    // depois de o jogador entrar numa das caixas. O HeistGame verifica a posição do jogador em cada
    // frame, como nas zonas de checkpoint, por isso também funciona depois de um teletransporte.
    public sealed class MapZone : MonoBehaviour
    {
        [Tooltip("Identificador gravado nos checkpoints. Não mudar depois de haver gravações.")] public string id;
        [Tooltip("Nome da zona, igual ao que aparece no mapa.")] public string displayName;
        [Tooltip("Imagem desta zona no mapa do diário (Interface > Ecrãs > Diário > Mapa). Fica escondida até a zona ser revelada.")]
        public GameObject mapImage;

        BoxCollider[] areas;

        public bool Contains(Vector3 point)
        {
            if (areas == null)
                areas = GetComponents<BoxCollider>();
            foreach (var area in areas)
            {
                Vector3 local = area.transform.InverseTransformPoint(point) - area.center;
                Vector3 half = area.size * .5f;
                if (Mathf.Abs(local.x) < half.x && Mathf.Abs(local.y) < half.y && Mathf.Abs(local.z) < half.z)
                    return true;
            }
            return false;
        }
    }
}
