using UnityEngine;

namespace GolpeCalculado
{
    // Dá a cada placa de texto 3D (TextMesh) um material próprio que respeita a profundidade,
    // para o texto não aparecer por cima das paredes.
    public sealed class WorldText : MonoBehaviour
    {
        public Material template;

        Material instance;
        TextMesh text;

        void OnEnable()
        {
            text = GetComponent<TextMesh>();
            instance = new Material(template);
            GetComponent<Renderer>().sharedMaterial = instance;
            Refresh(text.font);
            Font.textureRebuilt += Refresh;
        }

        // A textura da fonte é refeita pelo Unity quando aparecem letras novas.
        void Refresh(Font font)
        {
            if (text != null && font == text.font && instance != null)
                instance.mainTexture = font.material.mainTexture;
        }

        void OnDisable()
        {
            Font.textureRebuilt -= Refresh;
            if (instance != null)
                Destroy(instance);
        }
    }
}
