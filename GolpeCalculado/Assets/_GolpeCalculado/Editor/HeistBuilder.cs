using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using GolpeCalculado;

public static class HeistBuilder
{
    const string Root="Assets/_GolpeCalculado";
    public const string ScenePath=Root+"/Scenes/M02_AssaltoArmazem.unity";
    static Transform world,interactions;
    static Material concrete,steel,wood,amber,paper,dark,red,blue;
    static List<Light> bLights=new List<Light>(),cLights=new List<Light>();
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,bool collider=true,Transform parent=null)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent==null?world:parent);
        g.transform.position=p;g.transform.localRotation=Quaternion.identity;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    static Material Mat(string name,Color color,float rough=.2f)
    {
        string path=Root+"/Art/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m!=null)return m;
        m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",rough);
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static void Sign(string text,Vector3 p,float size=.12f,float yaw=0)
    {
        var go=new GameObject("Sinal "+text);go.transform.SetParent(world);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var tm=go.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.91f,.85f,.68f);
    }
    static HeistItem Item(string id,string label,ItemKind kind,Vector3 p,Vector3 size,Material mat,string requires="")
    {
        var go=Box(label,p,size,mat,true,interactions);var item=go.AddComponent<HeistItem>();item.id=id;item.label=label;item.kind=kind;item.requires=requires;return item;
    }
    static HeistItem Door(string id,string label,Vector3 p,Vector3 size,string key,bool noise=false)
    {
        var item=Item(id,label,ItemKind.Door,p,size,steel,key);item.canSlide=true;item.openPosition=p+Vector3.up*3;item.noisy=noise;
        var obstacle=item.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one;obstacle.carving=true;
        return item;
    }
    static void Document(string id,string name,string text,Vector3 p,float yaw=0)
    {
        Box("Mesa "+name,p+Vector3.down*.08f,new Vector3(1.6f,.12f,.85f),wood);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Perna mesa",p+new Vector3(x*.64f,-.62f,z*.3f),new Vector3(.07f,.96f,.07f),steel);
        var item=Item(id,name,ItemKind.Document,p,new Vector3(.6f,.05f,.44f),paper);item.text=text;
        for(int i=0;i<5;i++)Box("Texto do documento",p+new Vector3(0,.027f,-.13f+i*.056f),new Vector3(.43f,.006f,.009f),dark,false);
        Sign(name.ToUpperInvariant(),p+new Vector3(0,.65f,.33f),.045f,yaw);
    }
    static void Shelf(Vector3 p,float yaw=0)
    {
        var group=new GameObject("Estante").transform;group.SetParent(world);group.position=p;group.rotation=Quaternion.Euler(0,yaw,0);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
        {var g=Box("Montante",Vector3.zero,new Vector3(.12f,3.8f,.12f),steel,true,group);g.transform.localPosition=new Vector3(x*1.8f,1.9f,z*.5f);}
        for(int y=0;y<3;y++)
        {
            var shelf=Box("Prateleira",Vector3.zero,new Vector3(3.8f,.12f,1.2f),wood,true,group);shelf.transform.localPosition=new Vector3(0,.3f+y*1.3f,0);
            for(int x=-1;x<=1;x++){var load=Box("Caixa selada",Vector3.zero,new Vector3(.9f,.85f,.85f),wood,true,group);load.transform.localPosition=new Vector3(x*1.15f,.78f+y*1.3f,0);}
        }
    }
    static Light Ceiling(Vector3 p,int circuit)
    {
        Box("Luminária",p,new Vector3(1.3f,.1f,.25f),paper,false);
        var l=new GameObject("Luz zona "+circuit).AddComponent<Light>();l.transform.SetParent(world);l.transform.position=p+Vector3.down*.25f;l.type=LightType.Point;
        l.range=13;l.intensity=2.6f;l.color=circuit==0?new Color(.82f,.9f,1):new Color(1,.81f,.54f);l.shadows=LightShadows.Soft;
        (circuit==0?bLights:cLights).Add(l);return l;
    }
    static void Camera(HeistGame game,Vector3 p,float yaw,int circuit)
    {
        var root=new GameObject("Câmara zona "+circuit);root.transform.SetParent(world);root.transform.position=p;
        var lens=Box("Cabeça da câmara",p,new Vector3(.32f,.25f,.5f),paper,false,root.transform);
        var led=Box("LED",p+Vector3.forward*.26f,new Vector3(.07f,.07f,.025f),amber,false,lens.transform);
        var c=root.AddComponent<HeistCamera>();c.game=game;c.lens=lens.transform;c.led=led.GetComponent<Renderer>();c.baseYaw=yaw;c.circuit=circuit;
        Sign("VIDEOVIGILÂNCIA",p+Vector3.down*.6f,.045f,yaw);
    }
    [MenuItem("Golpe Calculado/Criar missão do armazém")]
    public static void Create()
    {
        if(File.Exists(ScenePath)){Debug.Log("A missão já existe; criação ignorada.");return;}
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/M01_MovimentoConducao.unity");
        EditorSceneManager.SaveScene(scene,ScenePath);
        Object.DestroyImmediate(Object.FindFirstObjectByType<PrototypeSession>().gameObject);
        Object.DestroyImmediate(Object.FindFirstObjectByType<RuntimeValidation>().gameObject);
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(t==null)continue;
            if(new[]{"Armazém","Cobertura","Nervura fachada","Portão de cargas","Porta de serviço","LOGÍSTICA  /  B","Pallet","Carga"}.Contains(t.name))Object.DestroyImmediate(t.gameObject);
        }
        world=new GameObject("Ambiente da missão").transform;interactions=new GameObject("Interações da missão").transform;
        concrete=Mat("Betão industrial",new Color(.38f,.4f,.38f));steel=Mat("Metal industrial",new Color(.18f,.24f,.27f),.45f);
        wood=Mat("Madeira caixas",new Color(.43f,.32f,.18f));amber=Mat("Amarelo segurança",new Color(.9f,.63f,.14f));
        paper=Mat("Papel e luminárias",new Color(.87f,.86f,.72f));dark=Mat("Borracha escura",new Color(.025f,.035f,.04f));red=Mat("Alarme vermelho",new Color(.7f,.06f,.035f));blue=Mat("Vestuário vigilante",new Color(.035f,.07f,.12f));
        Texture2D texture=new Texture2D(128,128);var pixels=new Color[128*128];
        for(int y=0;y<128;y++)for(int x=0;x<128;x++){float shade=.78f+Mathf.PerlinNoise(x*.35f,y*.35f)*.22f;pixels[y*128+x]=new Color(shade,shade,shade);}
        texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,Root+"/Art/TexturaBetao.asset");concrete.mainTexture=texture;concrete.mainTextureScale=new Vector2(8,8);
        Box("Piso armazém",new Vector3(36,.01f,31),new Vector3(48,.06f,62),concrete);
        // Two physical west entrances: loading at z=10 and service at z=49.
        Box("Fachada oeste sul",new Vector3(12,2.8f,4),new Vector3(.35f,5.6f,8),concrete);
        Box("Fachada oeste centro",new Vector3(12,2.8f,30),new Vector3(.35f,5.6f,36),concrete);
        Box("Fachada oeste norte",new Vector3(12,2.8f,56),new Vector3(.35f,5.6f,12),concrete);
        Box("Lintel cargas",new Vector3(12,4.4f,10),new Vector3(.35f,2.4f,4),concrete);
        Box("Lintel serviço",new Vector3(12,4.1f,49),new Vector3(.35f,3,2),concrete);
        Box("Parede este",new Vector3(60,2.8f,31),new Vector3(.35f,5.6f,62),concrete);
        Box("Parede sul",new Vector3(36,2.8f,0),new Vector3(48,5.6f,.35f),concrete);
        Box("Parede norte",new Vector3(36,2.8f,62),new Vector3(48,5.6f,.35f),concrete);
        Box("Cobertura armazém",new Vector3(36,5.65f,31),new Vector3(48.5f,.2f,62.5f),steel);
        Door("loadingDoor","portão de cargas",new Vector3(12,1.55f,10),new Vector3(.2f,3.1f,3.8f),"crowbar",true);
        Door("serviceDoor","porta de serviço",new Vector3(12,1.25f,49),new Vector3(.15f,2.5f,1.8f),"staffKey");
        Sign("ENTRADA DE CARGAS",new Vector3(11.76f,3.9f,10),.12f,90);
        Sign("SERVIÇO",new Vector3(11.76f,3.4f,49),.09f,90);
        // Interior partitions preserve multiple connected routes.
        Box("Divisória sul oeste",new Vector3(17,2.1f,20),new Vector3(10,4.2f,.25f),concrete);
        Box("Divisória sul este",new Vector3(43,2.1f,20),new Vector3(34,4.2f,.25f),concrete);
        Box("Divisória central sul",new Vector3(32,2.1f,4),new Vector3(.25f,4.2f,8),concrete);
        Box("Divisória central intermédia",new Vector3(32,2.1f,19.5f),new Vector3(.25f,4.2f,17),concrete);
        Box("Divisória central norte",new Vector3(32,2.1f,37.5f),new Vector3(.25f,4.2f,13),concrete);
        Door("officeDoor","porta do escritório",new Vector3(32,1.25f,29.5f),new Vector3(.16f,2.5f,2.6f),"officeKey");
        Box("Parede arquivo esquerda",new Vector3(38.5f,2.1f,44),new Vector3(13,4.2f,.3f),concrete);
        Box("Parede arquivo direita",new Vector3(54,2.1f,44),new Vector3(12,4.2f,.3f),concrete);
        Box("Divisória arquivo oeste",new Vector3(32,2.1f,53),new Vector3(.25f,4.2f,18),concrete);
        var vault=Door("vaultDoor","porta do arquivo de valores",new Vector3(46.5f,1.45f,44),new Vector3(2.8f,2.9f,.25f),"vaultAccess");
        Sign("RECEÇÃO DE CARGAS",new Vector3(22,3.6f,.23f),.14f,180);
        Sign("MANUTENÇÃO",new Vector3(42,3.4f,19.8f),.12f);
        Sign("ESCRITÓRIO / ARQUIVO",new Vector3(31.8f,3.4f,29.5f),.09f,90);
        Sign("ARQUIVO DE VALORES",new Vector3(46.5f,3.5f,43.75f),.12f);
        Sign("CORREDOR B",new Vector3(22,3.5f,44),.13f);
        Sign("VESTIÁRIO",new Vector3(20,3.3f,61.7f),.12f);
        // Storage, cover and room furniture.
        foreach(var p in new[]{new Vector3(17,0,7),new Vector3(17,0,15),new Vector3(27,0,5),new Vector3(27,0,15),new Vector3(18,0,28),new Vector3(28,0,36),new Vector3(18,0,44),new Vector3(28,0,56),new Vector3(54,0,6),new Vector3(54,0,17),new Vector3(37,0,36),new Vector3(55,0,36),new Vector3(39,0,55),new Vector3(56,0,55)})Shelf(p,90);
        for(int z=24;z<60;z+=8){Box("Faixa de segurança",new Vector3(24,.052f,z),new Vector3(.1f,.01f,3),amber,false);}
        for(int z=6;z<62;z+=12)for(int x=20;x<60;x+=16)Ceiling(new Vector3(x,4.9f,z),x<32?0:1);
        var game=new GameObject("Missão Golpe Calculado").AddComponent<HeistGame>();
        game.player=Object.FindFirstObjectByType<PlayerMotor>();game.van=Object.FindFirstObjectByType<VanMotor>();game.vaultDoor=vault;
        game.circuitBLights=bLights.ToArray();game.circuitCLights=cLights.ToArray();
        game.player.transform.position=new Vector3(-3,.1f,-23.1f);game.player.transform.rotation=Quaternion.Euler(0,90,0);
        var hideout=new GameObject("Esconderijo");hideout.transform.position=new Vector3(0,0,-31);game.hideout=hideout.transform;
        Sign("ESCONDERIJO",new Vector3(-8,2.8f,-31),.14f,-90);
        for(int s=-1;s<=1;s+=2)Box("Lugar esconderijo",new Vector3(s*2,.032f,-31),new Vector3(.15f,.02f,9),amber,false);
        var load=Item("loadVan","Carregar mala na carrinha",ItemKind.Load,game.van.transform.position+new Vector3(0,1,-2.4f),new Vector3(1.8f,1.4f,.08f),steel);
        load.transform.SetParent(game.van.transform,true);
        Item("crowbar","Pé-de-cabra",ItemKind.Tool,new Vector3(7,.85f,-5),new Vector3(.75f,.08f,.08f),red);
        Box("Caixa ferramentas exterior",new Vector3(7,.4f,-5),new Vector3(1.25f,.8f,.8f),steel);
        Sign("FERRAMENTAS",new Vector3(7,1.6f,-4.7f),.06f);
        Item("staffKey","Chave de serviço",ItemKind.Key,new Vector3(9,1.1f,55),new Vector3(.18f,.1f,.35f),amber);
        Box("Armário exterior",new Vector3(9,.5f,55),new Vector3(.8f,1,.6f),steel);
        Sign("PIQUETE / CHAVES",new Vector3(9,1.9f,55.2f),.055f);
        Item("officeKey","Chave do escritório",ItemKind.Key,new Vector3(17,1.06f,56),new Vector3(.2f,.1f,.3f),amber);
        Box("Banco vestiário",new Vector3(17,.5f,56),new Vector3(2,1,.8f),wood);
        Item("fuse","Fusível 16 A",ItemKind.Fuse,new Vector3(51,1.1f,10),new Vector3(.28f,.18f,.3f),amber,"crowbar");
        Box("Caixa manutenção",new Vector3(51,.5f,10),new Vector3(1.1f,1,.8f),steel);
        Sign("FUSÍVEIS 16 A",new Vector3(51,1.8f,10.4f),.06f);
        Item("socket","Instalar fusível auxiliar",ItemKind.FuseSocket,new Vector3(42,1.4f,43.6f),new Vector3(.55f,.7f,.2f),amber);
        var keypad=Item("keypad","Introduzir código",ItemKind.Keypad,new Vector3(49,1.45f,43.6f),new Vector3(.35f,.45f,.16f),blue);
        var cb=Item("circuitB","Alternar quadro Corredor B",ItemKind.Power,new Vector3(13,1.4f,24),new Vector3(.35f,.85f,.65f),steel);cb.circuit=0;
        var cc=Item("circuitC","Alternar quadro Oficina C",ItemKind.Power,new Vector3(39,1.4f,.45f),new Vector3(.65f,.85f,.25f),steel);cc.circuit=1;
        Sign("CORREDOR B\nLUZES + CCTV",new Vector3(13.3f,2.2f,24),.055f,-90);Sign("OFICINA C\nLUZES + CCTV",new Vector3(39,2.2f,.7f),.055f,180);
        Item("alarmReset","Silenciar alarme de reserva",ItemKind.AlarmReset,new Vector3(13,1.35f,40),new Vector3(.35f,.6f,.55f),red,"officeKey");
        Document("outsideNote","Aviso de manutenção","O portão de cargas está preso. A equipa deixou o pé-de-cabra na caixa vermelha junto à fachada sul.\n\nA porta de serviço, na outra ponta da fachada, abre com a chave do piquete. O armário de chaves fica no exterior, junto à esquina norte.\n\nNão confundam os quadros das luzes com a alimentação de reserva do arquivo.",new Vector3(8,1.1f,2));
        Document("cais","Registo de cais","EXPEDIÇÃO NC-17\n\nCais atribuído: 07.\nO turno é registado no vestiário. O dia de expedição está na ficha da manutenção.\n\nO acesso ao escritório exige a chave deixada no banco do vestiário.",new Vector3(24,1.1f,15));
        Document("turno","Quadro de turnos","LOGÍSTICA — ESCALA DE SERVIÇO\n\nTurno da remessa NC-17: 02.\n\nSe perderem o código do arquivo, usem os registos da remessa na ordem indicada junto do terminal. Os pares mantêm os zeros iniciais.",new Vector3(22,1.1f,57));
        Document("dia","Ficha de manutenção","ALIMENTAÇÃO AUXILIAR DO ARQUIVO\n\nFusível necessário: 16 A. A caixa está fechada e a tampa pode ser levantada com o pé-de-cabra.\n\nDia de expedição da remessa NC-17: 13.\nA alimentação auxiliar do terminal não é o circuito das câmaras.",new Vector3(43,1.1f,15));
        Document("manifest","Manifesto NC-17","CONTRATO NC-17\n\nMala cinzenta, etiqueta NC-17, arquivo de valores. Confirmar a referência antes da recolha.\n\nA base tem sensor de remoção e bateria independente. Quando a mala sai, a segurança muda. O comando de silêncio fica no Corredor B e exige a chave do escritório.",new Vector3(43,1.1f,31));
        Document("order","Ordem dos registos","TERMINAL DO ARQUIVO\n\nCódigo de seis algarismos: TURNO + CAIS + DIA.\n\nConsultar quadro de turnos no vestiário, registo no cais de cargas e ficha na manutenção. Usar sempre dois algarismos por registo.\n\nInstalar o fusível de 16 A na caixa ao lado antes de usar o terminal.",new Vector3(51,1.1f,40));
        Document("cameraNote","Procedimento CCTV","CORREDOR B\n\nAs câmaras partilham alimentação com a iluminação identificada no quadro. Qualquer falha deve ser investigada no local.\n\nUm alerta de câmara contém a última posição observada; o vigilante deve verificar essa posição.",new Vector3(20,1.1f,39));
        var suitcase=Item("case","Apanhar mala NC-17",ItemKind.Case,new Vector3(47,1.08f,55),new Vector3(.72f,.4f,.5f),steel);game.caseItem=suitcase;
        Box("Mesa da mala",new Vector3(47,.7f,55),new Vector3(1.6f,.3f,.9f),dark);
        Sign("NC-17 / SENSOR ATIVO",new Vector3(47,1.8f,55.3f),.06f);
        for(int i=0;i<6;i++)Item("bottle"+i,"Apanhar garrafa",ItemKind.Throwable,new Vector3(i<3?21:40,.35f,i<3?8+i*10:8+(i-3)*11),new Vector3(.13f,.6f,.13f),blue);
        var crate=Item("crate","Empurrar caixa",ItemKind.Crate,new Vector3(24,.55f,24),new Vector3(1,1,1),wood);var crb=crate.gameObject.AddComponent<Rigidbody>();crb.mass=35;crb.linearDamping=2;crb.constraints=RigidbodyConstraints.FreezeRotation;
        // Humanoid placeholder and navigation; visual replacement remains a later task.
        var guardRoot=new GameObject("Vigilante");guardRoot.transform.position=new Vector3(24,.06f,34);
        var agent=guardRoot.AddComponent<NavMeshAgent>();agent.enabled=false;agent.radius=.32f;agent.height=1.8f;agent.angularSpeed=180;agent.acceleration=7;agent.stoppingDistance=.7f;
        Box("Tronco",guardRoot.transform.position+Vector3.up*1.1f,new Vector3(.5f,.75f,.3f),blue,false,guardRoot.transform);
        Box("Cabeça",guardRoot.transform.position+Vector3.up*1.7f,new Vector3(.28f,.32f,.27f),wood,false,guardRoot.transform);
        for(int s=-1;s<=1;s+=2){Box("Perna",guardRoot.transform.position+new Vector3(s*.15f,.42f,0),new Vector3(.2f,.8f,.23f),dark,false,guardRoot.transform);Box("Braço",guardRoot.transform.position+new Vector3(s*.34f,1.1f,0),new Vector3(.14f,.7f,.19f),blue,false,guardRoot.transform);}
        var badge=Box("Indicador estado",guardRoot.transform.position+new Vector3(0,1.32f,.17f),new Vector3(.16f,.1f,.03f),amber,false,guardRoot.transform);
        var guard=guardRoot.AddComponent<HeistGuard>();guard.game=game;guard.indicator=badge.GetComponent<Renderer>();game.guard=guard;game.guardResetPosition=guardRoot.transform.position;
        var points=new[]{new Vector3(24,0,34),new Vector3(24,0,16),new Vector3(39,0,10),new Vector3(49,0,14),new Vector3(24,0,51)};
        guard.patrol=points.Select((p,i)=>{var t=new GameObject("Ronda "+i).transform;t.position=p;t.SetParent(world);return t;}).ToArray();
        Camera(game,new Vector3(28,3.5f,22),180,0);Camera(game,new Vector3(56,3.5f,22),0,1);
        var alarm=new GameObject("Luz alarme reserva").AddComponent<Light>();alarm.transform.position=new Vector3(46.5f,3.6f,43);alarm.color=Color.red;alarm.range=15;alarm.intensity=3;alarm.enabled=false;game.alarmLights=new[]{alarm};
        RenderSettings.ambientSkyColor=new Color(.25f,.3f,.38f);RenderSettings.ambientEquatorColor=new Color(.19f,.22f,.26f);
        var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);sun.intensity=.65f;
        BakeNavigation();
        new GameObject("Validação da missão").AddComponent<HeistValidation>();
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(Root+"/Scenes/M01_MovimentoConducao.unity",true)};
        Debug.Log("HEIST_SCENE_CREATED");
    }
    static void BakeNavigation()
    {
        var sources=new List<NavMeshBuildSource>();
        foreach(var box in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if(box.attachedRigidbody!=null||box.GetComponentInParent<HeistItem>()!=null)continue;
            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=0});
        }
        var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.35f;settings.agentHeight=1.8f;settings.agentClimb=.28f;
        var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(15,3,15),new Vector3(150,25,160)),Vector3.zero,Quaternion.identity);
        if(data==null)throw new System.Exception("Falhou criação da navegação.");
        var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(Root+"/Art/ArmazemNavigation.asset");
        if(existing==null)AssetDatabase.CreateAsset(data,Root+"/Art/ArmazemNavigation.asset");
        else {EditorUtility.CopySerialized(data,existing);Object.DestroyImmediate(data);data=existing;}
        var navigation=Object.FindFirstObjectByType<HeistNavigation>();
        if(navigation==null)navigation=new GameObject("Navegação armazém").AddComponent<HeistNavigation>();
        navigation.data=data;
    }
    public static void TuneExisting()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(t==null)continue;
            if(t.parent!=null&&t.parent.name=="Estante")t.localRotation=Quaternion.identity;
            if((t.name=="Muro"||t.name=="Pilar")&&t.position.x>12)Object.DestroyImmediate(t.gameObject);
            else if(t.name=="Muro fundo"){t.position=new Vector3(-7,1.1f,58);t.localScale=new Vector3(38,2.2f,.4f);}
            if(t!=null&&t.name.StartsWith("Mesa ")&&t.name!="Mesa da mala")t.position=new Vector3(t.position.x,1.02f,t.position.z);
            if(t!=null&&t.name=="Perna mesa"){t.position=new Vector3(t.position.x,.48f,t.position.z);t.localScale=new Vector3(.07f,.96f,.07f);}
        }
        if(GameObject.Find("Base suporte mala")==null)
        {
            world=GameObject.Find("Ambiente da missão").transform;
            dark=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Borracha escura.mat");
            Box("Base suporte mala",new Vector3(47,.3f,55),new Vector3(1,.6f,.7f),dark);
        }
        Object.FindFirstObjectByType<HeistGuard>().GetComponent<NavMeshAgent>().enabled=false;
        var shader=Shader.Find("GolpeCalculado/WorldText");var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/TextoComProfundidade.mat");
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,Root+"/Art/TextoComProfundidade.mat");}
        foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            var component=text.GetComponent<WorldText>();if(component==null)component=text.gameObject.AddComponent<WorldText>();component.template=mat;
        }
        var crate=Object.FindObjectsByType<HeistItem>(FindObjectsSortMode.None).First(i=>i.id=="crate");
        if(crate.GetComponent<NavMeshObstacle>()==null){var obstacle=crate.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one;obstacle.carving=true;}
        BakeNavigation();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEIST_LAYOUT_TUNED");
    }
    public static void Build()
    {
        Directory.CreateDirectory("../Builds/AssaltoWindows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath,Root+"/Scenes/M01_MovimentoConducao.unity"},target=BuildTarget.StandaloneWindows64,locationPathName="../Builds/AssaltoWindows/GolpeCalculado.exe",options=BuildOptions.None});
        Debug.Log("HEIST_BUILD "+report.summary.result+" errors="+report.summary.totalErrors);
        if(Application.isBatchMode)EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
}
