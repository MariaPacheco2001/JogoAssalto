using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using GolpeCalculado;

public static class PrototypeBuilder
{
    const string Root = "Assets/_GolpeCalculado";
    static Material concrete, asphalt, steel, paint, black, yellow, glass, white, brick;
    static GameObject Piece(string name, Vector3 p, Vector3 scale, Material mat, Transform parent = null, PrimitiveType shape = PrimitiveType.Cube, bool collision = true)
    {
        var g = GameObject.CreatePrimitive(shape); g.name = name; g.transform.SetParent(parent, false);
        g.transform.localPosition = p; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collision) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    static Material Mat(string name, Color color, float metal = 0, float smooth = .2f)
    {
        var m = new Material(Shader.Find("Standard")) { name = name, color = color };
        m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", smooth);
        AssetDatabase.CreateAsset(m, Root + "/Art/" + name + ".mat"); return m;
    }
    static void Label(string text, Vector3 p, float size, Transform parent = null, float yaw = 180)
    {
        var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var tm = go.AddComponent<TextMesh>(); tm.text = text; tm.fontSize = 64; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(.95f,.88f,.68f);
    }
    static void Lamp(Vector3 p, Transform parent)
    {
        Piece("Poste", p + Vector3.up * 3, new Vector3(.14f,6,.14f), steel, parent);
        Piece("Braço", p + new Vector3(-.7f,5.9f,0), new Vector3(1.5f,.1f,.15f), steel, parent);
        var head = Piece("Luminária", p + new Vector3(-1.4f,5.85f,0), new Vector3(.55f,.12f,.3f), white, parent, PrimitiveType.Cube, false);
        var light = head.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1,.76f,.46f); light.intensity = 2; light.range = 15;
    }
    static AudioClip EngineSound()
    {
        const int rate = 22050; int count = rate * 2;
        string path = Root + "/Audio/EnginePrototype.wav";
        using (var w = new BinaryWriter(File.Create(path)))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + count * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(count * 2);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float v = Mathf.Sin(t * 2 * Mathf.PI * 55) * .45f + Mathf.Sin(t * 2 * Mathf.PI * 110) * .2f + Mathf.Sin(t * 2 * Mathf.PI * 165) * .1f;
                w.Write((short)(v * 20000));
            }
        }
        AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    [MenuItem("Golpe Calculado/Construir cena Marco 1")]
    public static void Create()
    {
        if (File.Exists(Root + "/Scenes/M01_MovimentoConducao.unity")) { Debug.Log("Cena já existe. Construção ignorada para preservar alterações."); return; }
        foreach (string folder in new[] { "Scenes", "Art", "Prefabs", "Audio" }) Directory.CreateDirectory(Root + "/" + folder);
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        concrete = Mat("Betão", new Color(.37f,.39f,.4f)); asphalt = Mat("Asfalto",new Color(.105f,.12f,.135f));
        steel = Mat("Aço",new Color(.2f,.24f,.27f), .7f,.38f); paint = Mat("Carrinha Cinza",new Color(.58f,.61f,.57f), .35f,.48f);
        black = Mat("Borracha",new Color(.025f,.031f,.035f)); yellow = Mat("Sinalização",new Color(.88f,.59f,.12f));
        glass = Mat("Vidro Escuro",new Color(.08f,.17f,.2f), .6f,.75f); white = Mat("Marfim",new Color(.8f,.79f,.7f)); brick = Mat("Tijolo",new Color(.34f,.23f,.18f));
        RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.4f,.46f,.53f); RenderSettings.ambientEquatorColor = new Color(.27f,.3f,.34f); RenderSettings.ambientGroundColor = new Color(.16f,.17f,.18f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.34f,.39f,.44f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .005f;
        var sun = new GameObject("Sol de fim de tarde").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1,.84f,.65f); sun.intensity = 1.25f; sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(28,-38,0);
        var sky = new Material(Shader.Find("Skybox/Procedural")); sky.SetColor("_SkyTint", new Color(.48f,.53f,.6f)); sky.SetFloat("_Exposure", .8f); AssetDatabase.CreateAsset(sky, Root + "/Art/Ceu.mat"); RenderSettings.skybox = sky; RenderSettings.sun = sun;
        var world = new GameObject("Ambiente — pátio industrial").transform;
        Piece("Terreno",new Vector3(0,-.3f,10),new Vector3(130,.6f,150),concrete,world);
        Piece("Rua",new Vector3(0,.008f,8),new Vector3(15,.02f,98),asphalt,world);
        for (int z = -35; z < 55; z += 7) Piece("Eixo da via",new Vector3(0,.028f,z),new Vector3(.12f,.015f,2.7f),white,world,PrimitiveType.Cube,false);
        for (int side = -1; side <= 1; side += 2)
        {
            Piece("Passeio",new Vector3(side*8.5f,.1f,8),new Vector3(2,.2f,98),concrete,world);
            for (int z = -35; z < 55; z += 12)
            {
                if (side == 1 && z > 20) continue;
                Piece("Muro",new Vector3(side*25,1.2f,z),new Vector3(.4f,2.4f,12),brick,world);
                Piece("Pilar",new Vector3(side*25,1.5f,z-6),new Vector3(.6f,3,.6f),concrete,world);
            }
        }
        // Generous turning area; narrow kerbs do not cross the access to the parking bay.
        Piece("Pátio de estacionamento",new Vector3(9,.012f,40),new Vector3(25,.025f,28),asphalt,world);
        var parking = new GameObject("Estacionamento alvo").transform; parking.position = new Vector3(4,.04f,36);
        for (int s = -1; s <= 1; s += 2) Piece("Limite amarelo",new Vector3(s*1.75f,0,0),new Vector3(.12f,.015f,7.8f),yellow,parking,PrimitiveType.Cube,false);
        Piece("Topo do lugar",new Vector3(0,0,3.9f),new Vector3(3.6f,.015f,.12f),yellow,parking,PrimitiveType.Cube,false);
        Label("CARGAS\n01",new Vector3(4,.04f,36),.17f, null, 0);
        GameObject.Find("CARGAS\n01").transform.rotation = Quaternion.Euler(90,0,0);
        Piece("Batente",new Vector3(4,.09f,40.3f),new Vector3(2.1f,.18f,.25f),concrete,world);
        // Warehouse massing with two visible entrances, kept closed in the movement test.
        Piece("Armazém",new Vector3(23,4,3),new Vector3(23,8,42),concrete,world);
        Piece("Cobertura",new Vector3(23,8.15f,3),new Vector3(24,.3f,43),steel,world);
        for(int z=-16;z<24;z+=3) Piece("Nervura fachada",new Vector3(11.43f,4,z),new Vector3(.12f,8,.12f),steel,world);
        Piece("Portão de cargas",new Vector3(11.36f,2.1f,8),new Vector3(.12f,4.2f,5),steel,world);
        Piece("Porta de serviço",new Vector3(11.32f,1.1f,-10),new Vector3(.15f,2.2f,1.15f),steel,world);
        Label("LOGÍSTICA  /  B",new Vector3(11.2f,6,5),.16f,null,90);
        Label("PÁTIO 01",new Vector3(4,2.3f,45),.16f,null,0);
        Piece("Placa pátio",new Vector3(4,2.3f,45.08f),new Vector3(4.2f,.85f,.1f),steel,world);
        for(int side=-1;side<=1;side+=2) Piece("Suporte placa",new Vector3(4+side*1.5f,1.05f,45.08f),new Vector3(.09f,2.1f,.09f),steel,world);
        for(int z=-24;z<=40;z+=22) Lamp(new Vector3(8.3f,0,z),world);
        for(int z=-20;z<40;z+=16)
        {
            var building = Piece("Oficina",new Vector3(-17,3,z),new Vector3(12,6,11),brick,world);
            Piece("Cobertura oficina",new Vector3(-17,6.1f,z),new Vector3(12.4f,.2f,11.4f),steel,world);
            for(int k=-1;k<=1;k++) Piece("Janela",new Vector3(-10.94f,3.6f,z+k*2.5f),new Vector3(.05f,1.6f,1.8f),glass,world);
        }
        // Collision boundaries are represented physically, rather than invisible walls.
        Piece("Muro fundo",new Vector3(0,1.1f,58),new Vector3(56,2.2f,.4f),brick,world);
        Piece("Muro início",new Vector3(0,1.1f,-43),new Vector3(56,2.2f,.4f),brick,world);
        for(int i=0;i<5;i++) { Piece("Pallet",new Vector3(16+i*1.4f,.09f,31),new Vector3(1.1f,.18f,1.1f),brick,world); Piece("Carga",new Vector3(16+i*1.4f,.7f,31),Vector3.one,yellow,world); }
        Piece("Obstáculo teste",new Vector3(-5,.5f,-16),new Vector3(1,1,1),concrete,world);
        var vanObject = new GameObject("Carrinha"); var v = vanObject.AddComponent<VanMotor>();
        var rb = vanObject.GetComponent<Rigidbody>(); rb.mass = 1850; rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; rb.linearDamping = .08f; rb.angularDamping = .8f;
        var vt = vanObject.transform;
        var collider = vanObject.AddComponent<BoxCollider>(); collider.center = new Vector3(0,1.25f,0); collider.size = new Vector3(1.9f,1.65f,4.65f);
        Piece("Chassis",new Vector3(0,.67f,0),new Vector3(1.92f,.4f,4.65f),black,vt,PrimitiveType.Cube,false);
        Piece("Caixa carga",new Vector3(0,1.5f,-.8f),new Vector3(1.9f,1.65f,3.05f),paint,vt,PrimitiveType.Cube,false);
        Piece("Capot",new Vector3(0,1.06f,1.8f),new Vector3(1.9f,.55f,1.05f),paint,vt,PrimitiveType.Cube,false);
        Piece("Tejadilho cabine",new Vector3(0,2.27f,1),new Vector3(1.9f,.12f,1.6f),paint,vt,PrimitiveType.Cube,false);
        for(int side=-1;side<=1;side+=2)
        {
            Piece("Porta cabine",new Vector3(side*.92f,1.04f,1),new Vector3(.09f,.75f,1.5f),paint,vt,PrimitiveType.Cube,false);
            Piece("Pilar para-brisas",new Vector3(side*.87f,1.84f,1.72f),new Vector3(.09f,.9f,.09f),paint,vt,PrimitiveType.Cube,false);
            Piece("Pilar traseiro",new Vector3(side*.88f,1.85f,.3f),new Vector3(.09f,.85f,.12f),paint,vt,PrimitiveType.Cube,false);
            Piece("Retrovisor",new Vector3(side*1.1f,1.63f,1.5f),new Vector3(.26f,.3f,.13f),black,vt,PrimitiveType.Cube,false);
            Piece("Pega",new Vector3(side*.976f,1.37f,.57f),new Vector3(.04f,.04f,.21f),black,vt,PrimitiveType.Cube,false);
            Piece("Banco",new Vector3(side*.44f,.98f,.73f),new Vector3(.5f,.25f,.55f),black,vt,PrimitiveType.Cube,false);
            Piece("Encosto",new Vector3(side*.44f,1.34f,.42f),new Vector3(.5f,.7f,.16f),black,vt,PrimitiveType.Cube,false);
            Piece("Farol",new Vector3(side*.64f,1.08f,2.335f),new Vector3(.4f,.2f,.03f),white,vt,PrimitiveType.Cube,false);
            Piece("Ótica traseira",new Vector3(side*.78f,1.05f,-2.34f),new Vector3(.16f,.43f,.03f),yellow,vt,PrimitiveType.Cube,false);
        }
        Piece("Grelha",new Vector3(0,.82f,2.345f),new Vector3(.9f,.25f,.03f),black,vt,PrimitiveType.Cube,false);
        Piece("Para-choques",new Vector3(0,.59f,2.37f),new Vector3(1.92f,.15f,.15f),steel,vt,PrimitiveType.Cube,false);
        Piece("Tablier",new Vector3(0,1.29f,1.51f),new Vector3(1.7f,.2f,.38f),black,vt,PrimitiveType.Cube,false);
        var steeringWheel = Piece("Volante",new Vector3(-.46f,1.38f,1.22f),new Vector3(.3f,.035f,.3f),steel,vt,PrimitiveType.Cylinder,false); steeringWheel.transform.localRotation = Quaternion.Euler(65,0,0);
        v.wheels = new WheelCollider[4]; v.wheelVisuals = new Transform[4];
        for(int i=0;i<4;i++)
        {
            float x=i%2==0 ? -.94f : .94f, z=i<2 ? 1.48f : -1.5f;
            var wheel = new GameObject("Suspensão " + i); wheel.transform.SetParent(vt,false); wheel.transform.localPosition = new Vector3(x,.55f,z);
            var wc=wheel.AddComponent<WheelCollider>(); wc.radius=.36f; wc.mass=24; wc.suspensionDistance=.2f; wc.wheelDampingRate=1.5f;
            var spring=wc.suspensionSpring; spring.spring=50000; spring.damper=6500; spring.targetPosition=.5f; wc.suspensionSpring=spring;
            var fr=wc.forwardFriction; fr.stiffness=1.5f; wc.forwardFriction=fr;
            var sf=wc.sidewaysFriction; sf.stiffness=1.6f; wc.sidewaysFriction=sf; v.wheels[i]=wc;
            var tyre=Piece("Pneu " + i,new Vector3(x,.45f,z),new Vector3(.72f,.12f,.72f),black,vt,PrimitiveType.Cylinder,false); tyre.transform.localRotation=Quaternion.Euler(0,0,90); v.wheelVisuals[i]=tyre.transform;
            Piece("Jante",new Vector3(0,.01f,0),new Vector3(.63f,1.08f,.63f),steel,tyre.transform,PrimitiveType.Cylinder,false);
        }
        Transform Marker(string name,Vector3 p) { var g=new GameObject(name).transform; g.SetParent(vt,false); g.localPosition=p; return g; }
        v.seat=Marker("Olhos condutor",new Vector3(-.44f,1.76f,1.05f)); v.entry=Marker("Entrada condutor",new Vector3(-1.03f,.65f,.9f));
        v.leftExit=Marker("Saída esquerda",new Vector3(-1.7f,.3f,.9f)); v.rightExit=Marker("Saída direita",new Vector3(1.7f,.3f,.9f));
        var audio=vanObject.AddComponent<AudioSource>(); audio.clip=EngineSound(); audio.loop=true; audio.playOnAwake=true; audio.spatialBlend=.7f; audio.minDistance=4; audio.maxDistance=30; audio.volume=.03f; v.engine=audio;
        var prefab=PrefabUtility.SaveAsPrefabAsset(vanObject,Root + "/Prefabs/Carrinha.prefab"); Object.DestroyImmediate(vanObject);
        vanObject=(GameObject)PrefabUtility.InstantiatePrefab(prefab); vanObject.transform.position=new Vector3(0,.05f,-24); v=vanObject.GetComponent<VanMotor>();
        var playerObject=new GameObject("Jogador"); var cc=playerObject.AddComponent<CharacterController>(); cc.height=1.8f; cc.radius=.3f; cc.center=Vector3.up*.9f; cc.stepOffset=.28f; cc.slopeLimit=45; cc.skinWidth=.035f;
        var p=playerObject.AddComponent<PlayerMotor>(); var cameraObject=new GameObject("Vista primeira pessoa"); cameraObject.tag="MainCamera"; cameraObject.transform.SetParent(playerObject.transform,false); cameraObject.transform.localPosition=Vector3.up*1.65f;
        p.view=cameraObject.AddComponent<Camera>(); p.view.nearClipPlane=.06f; p.view.farClipPlane=250; p.view.fieldOfView=75; p.view.backgroundColor=new Color(.34f,.39f,.44f); cameraObject.AddComponent<AudioListener>();
        var pp=PrefabUtility.SaveAsPrefabAsset(playerObject,Root + "/Prefabs/Jogador.prefab"); Object.DestroyImmediate(playerObject);
        playerObject=(GameObject)PrefabUtility.InstantiatePrefab(pp); playerObject.transform.position=new Vector3(-3,.1f,-23.1f); playerObject.transform.rotation=Quaternion.Euler(0,90,0); p=playerObject.GetComponent<PlayerMotor>();
        var session=new GameObject("Sessão Marco 1").AddComponent<PrototypeSession>(); session.player=p; session.van=v; session.parking=parking;
        new GameObject("Validação automática").AddComponent<RuntimeValidation>();
        QualitySettings.vSyncCount=1; QualitySettings.shadowDistance=80; QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowResolution=ShadowResolution.High;
        PlayerSettings.companyName="Rita e Maria"; PlayerSettings.productName="Golpe Calculado"; PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true; PlayerSettings.colorSpace=ColorSpace.Linear;
        EditorSettings.serializationMode=SerializationMode.ForceText;
        EditorSceneManager.SaveScene(scene,Root + "/Scenes/M01_MovimentoConducao.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root + "/Scenes/M01_MovimentoConducao.unity",true)};
        AssetDatabase.SaveAssets(); Debug.Log("MARCO1_SCENE_CREATED");
    }
    [MenuItem("Golpe Calculado/Build Windows")]
    public static void Build()
    {
        var output=System.Environment.GetEnvironmentVariable("GOLPE_BUILD_PATH"); if(string.IsNullOrEmpty(output)) output="../Builds/Windows/GolpeCalculado.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{Root+"/Scenes/M01_MovimentoConducao.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
        Debug.Log("MARCO1_BUILD: "+report.summary.result+" errors="+report.summary.totalErrors);
        if(Application.isBatchMode) EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
    public static void FixSignAndBuild()
    {
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/M01_MovimentoConducao.unity");
        var sign=GameObject.Find("PÁTIO 01"); sign.transform.rotation=Quaternion.identity;
        steel=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Aço.mat");
        var world=GameObject.Find("Ambiente — pátio industrial").transform;
        if(GameObject.Find("Placa pátio")==null)
        {
            Piece("Placa pátio",new Vector3(4,2.3f,45.08f),new Vector3(4.2f,.85f,.1f),steel,world);
            for(int side=-1;side<=1;side+=2) Piece("Suporte placa",new Vector3(4+side*1.5f,1.05f,45.08f),new Vector3(.09f,2.1f,.09f),steel,world);
        }
        EditorSceneManager.SaveScene(scene); Build();
    }
}
