using UnityEngine;

namespace GolpeCalculado
{
    // Só visual: troca o aspeto de um objeto conforme o nível (por exemplo a carrinha do Difícil).
    // O HeistGame chama Apply ao aplicar a dificuldade. Sem HeistGame (cena M01) fica o aspeto do Fácil/Normal.
    public sealed class DifficultyVisual : MonoBehaviour
    {
        [Tooltip("Objetos ativos no Fácil e no Normal.")] public GameObject[] easyNormal;
        [Tooltip("Objetos ativos só no Difícil.")] public GameObject[] hard;
        [Tooltip("Renderers que ficam escondidos no Difícil (o objeto continua ativo, por exemplo por causa do collider).")] public Renderer[] hiddenInHard;

        public void Apply(bool hardLevel)
        {
            foreach (var go in easyNormal)
                if (go != null) go.SetActive(!hardLevel);
            foreach (var go in hard)
                if (go != null) go.SetActive(hardLevel);
            foreach (var r in hiddenInHard)
                if (r != null) r.enabled = !hardLevel;
        }
    }
}
