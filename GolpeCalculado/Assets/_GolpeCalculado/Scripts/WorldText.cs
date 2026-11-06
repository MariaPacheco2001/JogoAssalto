using UnityEngine;
namespace GolpeCalculado
{
    public sealed class WorldText : MonoBehaviour
    {
        public Material template;
        Material instance;TextMesh text;
        void OnEnable(){text=GetComponent<TextMesh>();instance=new Material(template);GetComponent<Renderer>().sharedMaterial=instance;Refresh(text.font);Font.textureRebuilt+=Refresh;}
        void Refresh(Font font){if(text!=null&&font==text.font&&instance!=null)instance.mainTexture=font.material.mainTexture;}
        void OnDisable(){Font.textureRebuilt-=Refresh;if(instance!=null)Destroy(instance);}
    }
}
