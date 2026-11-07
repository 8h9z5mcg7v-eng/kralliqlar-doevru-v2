using UnityEngine;
using System.Collections.Generic;

public sealed class KingdomBootstrap : MonoBehaviour
{
    Material stone, paleStone, roof, wood, grass, dirt, gold, plaster, leaves;
    Camera cam;
    float yaw = 42f, pitch = 48f, distance = 37f;
    Vector2 last;
    bool dragging;
    int timber = 1200, stoneCount = 900, food = 1500, coins = 250;
    string panel = "";
    string selectedBuilding = "";
    readonly Dictionary<string, int> buildingLevels = new Dictionary<string, int>();

    Material MakeMat(Color c, float gloss = .2f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c; m.SetFloat("_Glossiness", gloss); return m;
    }
    GameObject Part(string n, PrimitiveType type, Vector3 p, Vector3 s, Material m)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = n; g.transform.position = p; g.transform.localScale = s;
        g.GetComponent<Renderer>().sharedMaterial = m;
        var c = g.GetComponent<Collider>(); if (c) c.enabled = false;
        return g;
    }
    void Cube(string n, Vector3 p, Vector3 s, Material m) { Part(n, PrimitiveType.Cube, p, s, m); }
    GameObject MakeCone(string n, Vector3 p, float radius, float height, Material mat)
    {
        const int sides = 12;
        var vertices = new Vector3[sides + 1];
        var triangles = new int[sides * 6];
        vertices[0] = new Vector3(0, height, 0);
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            int t = i * 6;
            int next = (i + 1) % sides + 1;
            triangles[t] = 0; triangles[t + 1] = i + 1; triangles[t + 2] = next;
            triangles[t + 3] = sides + 0; triangles[t + 4] = next; triangles[t + 5] = i + 1;
        }
        var mesh = new Mesh();
        mesh.vertices = vertices;
        var faces = new int[sides * 3];
        for (int i = 0; i < sides; i++) { faces[i * 3] = 0; faces[i * 3 + 1] = i + 1; faces[i * 3 + 2] = (i + 1) % sides + 1; }
        mesh.triangles = faces; mesh.RecalculateNormals();
        var go = new GameObject(n);
        go.transform.position = p;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.antiAliasing = 2;
        QualitySettings.shadowDistance = 65;
        stone = MakeMat(new Color(.40f,.41f,.40f));
        paleStone = MakeMat(new Color(.62f,.59f,.51f));
        roof = MakeMat(new Color(.25f,.13f,.12f));
        wood = MakeMat(new Color(.28f,.16f,.09f));
        grass = MakeMat(new Color(.20f,.31f,.18f));
        dirt = MakeMat(new Color(.48f,.38f,.27f));
        gold = MakeMat(new Color(.78f,.53f,.17f),.5f);
        plaster = MakeMat(new Color(.69f,.58f,.43f));
        leaves = MakeMat(new Color(.13f,.25f,.12f));
        Lighting();
        Part("Kingdom terrain",PrimitiveType.Plane,new Vector3(0,-.2f,0),new Vector3(9,1,9),grass);
        Part("Raised bailey",PrimitiveType.Cube,new Vector3(0,.05f,0),new Vector3(31,.55f,27),dirt);
        Part("Inner lawn",PrimitiveType.Cube,new Vector3(0,.34f,0),new Vector3(29.8f,.16f,25.8f),grass);
        Walls();
        Keep();
        Building("ROYAL BARRACKS",new Vector3(-8,0,4),4.2f,2.6f,3.8f);
        Building("ROYAL STABLES",new Vector3(8,0,4),4.2f,2.3f,3.8f);
        Building("WOODCUTTERS LODGE",new Vector3(-8,0,-5),3.5f,2f,3.1f);
        Building("STONE QUARRY",new Vector3(7,0,-5),3.7f,2.1f,3.2f);
        Building("ROYAL MARKET",new Vector3(0,0,-7.2f),3.6f,2.2f,3f);
        Details();
        CameraSetup();
        LoadProgress();
        LoadProgress();
    }
    void Lighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.49f,.57f,.68f);
        RenderSettings.ambientEquatorColor = new Color(.38f,.4f,.35f);
        RenderSettings.ambientGroundColor = new Color(.18f,.16f,.13f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.57f,.63f,.66f);
        RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .007f;
        var sun = new GameObject("Warm sunlight").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.25f;
        sun.color = new Color(1f,.88f,.73f); sun.transform.rotation = Quaternion.Euler(48,-32,0);
        sun.shadows = LightShadows.Soft;
    }
    void Tower(Vector3 p, float h)
    {
        Part("Round defensive tower",PrimitiveType.Cylinder,p+Vector3.up*h/2,new Vector3(2.1f,h,2.1f),stone);
        Part("Tower crown",PrimitiveType.Cylinder,p+Vector3.up*(h+.1f),new Vector3(2.35f,.28f,2.35f),paleStone);
        MakeCone("Tower roof",p+Vector3.up*(h+1),1.1f,1.8f,roof);
        Cube("Tower arrow slit",p+new Vector3(0,h*.55f,-1.06f),new Vector3(.16f,.7f,.04f),roof);
        Cube("Tower gold band",p+Vector3.up*(h*.3f),new Vector3(2.15f,.12f,2.15f),gold);
    }
    void Walls()
    {
        float x=13.5f,z=11.6f,h=3.1f;
        Cube("North curtain wall",new Vector3(0,h/2,z),new Vector3(27,h,1.1f),paleStone);
        Cube("South curtain wall",new Vector3(0,h/2,-z),new Vector3(27,h,1.1f),paleStone);
        Cube("East curtain wall",new Vector3(x,h/2,0),new Vector3(1.1f,h,23),paleStone);
        Cube("West curtain wall",new Vector3(-x,h/2,0),new Vector3(1.1f,h,23),paleStone);
        for(float i=-12;i<=12;i+=1.5f) {
            if(Mathf.Abs(i)>2.4f) {
                Cube("North crenel",new Vector3(i,h+.25f,z),new Vector3(.72f,.55f,1.2f),paleStone);
                Cube("South crenel",new Vector3(i,h+.25f,-z),new Vector3(.72f,.55f,1.2f),paleStone);
            }
        }
        for(float i=-10;i<=10;i+=1.5f) {
            Cube("East crenel",new Vector3(x,h+.25f,i),new Vector3(1.2f,.55f,.72f),paleStone);
            Cube("West crenel",new Vector3(-x,h+.25f,i),new Vector3(1.2f,.55f,.72f),paleStone);
        }
        Tower(new Vector3(-x,0,-z),4.5f); Tower(new Vector3(x,0,-z),4.5f);
        Tower(new Vector3(-x,0,z),4.5f); Tower(new Vector3(x,0,z),4.5f);
        Cube("Gatehouse left",new Vector3(-2.1f,2.1f,-z),new Vector3(2.1f,4.2f,2.2f),stone);
        Cube("Gatehouse right",new Vector3(2.1f,2.1f,-z),new Vector3(2.1f,4.2f,2.2f),stone);
        Cube("Gate arch",new Vector3(0,4.05f,-z),new Vector3(6.1f,.8f,2.3f),paleStone);
        Cube("Wooden portcullis",new Vector3(0,1.5f,-10.5f),new Vector3(3.5f,3f,.16f),wood);
        for(float i=-1.5f;i<=1.5f;i+=.5f) Cube("Gate ironwork",new Vector3(i,1.5f,-10.38f),new Vector3(.07f,3f,.08f),gold);
    }
    void Keep()
    {
        Cube("Keep foundation",new Vector3(0,.65f,3),new Vector3(10,1.2f,9),stone);
        Cube("Royal keep lower hall",new Vector3(0,3.2f,3),new Vector3(8.2f,4f,7.2f),paleStone);
        Cube("Royal keep upper hall",new Vector3(0,6.1f,3),new Vector3(6.3f,2f,5.5f),plaster);
        for(int a=-1;a<=1;a+=2) for(int b=-1;b<=1;b+=2) Tower(new Vector3(a*4.1f,0,3+b*3.6f),7f);
        for(int i=-2;i<=2;i++) {
            Cube("Keep window",new Vector3(i*1.3f,3.3f,-.64f),new Vector3(.36f,.9f,.08f),roof);
            Cube("Window sill",new Vector3(i*1.3f,2.82f,-.6f),new Vector3(.52f,.1f,.1f),gold);
        }
        Cube("Royal balcony",new Vector3(0,5.2f,-1),new Vector3(3.6f,.25f,.8f),wood);
        Cube("Royal flag pole",new Vector3(0,9.4f,3),new Vector3(.1f,3f,.1f),gold);
        Cube("Royal crimson banner",new Vector3(.6f,9.2f,3),new Vector3(1.2f,1.7f,.08f),MakeMat(new Color(.48f,.06f,.06f)));
        var keepCollider = new GameObject("Keep selection collider");
        keepCollider.transform.SetParent(GameObject.Find("Royal keep lower hall").transform, true);
        keepCollider.transform.position = new Vector3(0,4,3);
        var kc = keepCollider.AddComponent<BoxCollider>(); kc.size = new Vector3(9,8,8);
    }
    void Building(string n,Vector3 p,float w,float h,float d)
    {
        Cube(n+" stone plinth",p+new Vector3(0,.35f,0),new Vector3(w+.5f,.7f,d+.5f),stone);
        Cube(n+" walls",p+new Vector3(0,.7f+h/2,0),new Vector3(w,h,d),plaster);
        Cube(n+" roof",p+new Vector3(0,h+1.25f,0),new Vector3(w+1.1f,1.1f,d+1.1f),roof);
        Cube(n+" timber door",p+new Vector3(0,1.15f,-d/2-.05f),new Vector3(.8f,1.5f,.12f),wood);
        for(int side=-1;side<=1;side+=2)
            Cube(n+" window",p+new Vector3(side*w*.28f,1.5f,-d/2-.06f),new Vector3(.4f,.62f,.1f),stone);
        Cube(n+" sign",p+new Vector3(0,2.5f,-d/2-.14f),new Vector3(1.05f,.32f,.1f),gold);
        var hit = new GameObject(n+" selection collider");
        hit.transform.position = p + new Vector3(0,1.5f,0);
        var box = hit.AddComponent<BoxCollider>(); box.size = new Vector3(w+1, h+2.5f, d+1);
    }
    void Tree(Vector3 p)
    {
        Part("Tree trunk",PrimitiveType.Cylinder,p+Vector3.up*.8f,new Vector3(.35f,1.6f,.35f),wood);
        Part("Tree crown",PrimitiveType.Sphere,p+Vector3.up*2.1f,new Vector3(2.2f,2.5f,2.2f),leaves);
        Part("Tree crown top",PrimitiveType.Sphere,p+new Vector3(.3f,3.05f,.1f),new Vector3(1.4f,1.5f,1.4f),leaves);
    }
    void Details()
    {
        Cube("Royal avenue",new Vector3(0,.44f,-1.5f),new Vector3(2,.08f,19),dirt);
        Cube("Market crossroad",new Vector3(0,.45f,-5.2f),new Vector3(19,.08f,1.5f),dirt);
        for(int i=0;i<4;i++) {
            Cube("Farm soil plot",new Vector3(-19+i*2.1f,.15f,2),new Vector3(1.65f,.2f,4.2f),dirt);
            for(int r=0;r<5;r++) Cube("Golden crop row",new Vector3(-19+i*2.1f,.28f,.4f+r*.75f),new Vector3(1.4f,.1f,.08f),gold);
        }
        for(int i=0;i<30;i++) { float a=i*2.399f, r=19+(i%5)*2.1f; Tree(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r)); }
        Part("Outer pond",PrimitiveType.Cylinder,new Vector3(19,-.03f,16),new Vector3(8,.1f,5),MakeMat(new Color(.12f,.3f,.34f),.7f));
        for(int i=0;i<8;i++) {
            float a=i*Mathf.PI/4; Vector3 p=new Vector3(Mathf.Cos(a)*8,.8f,Mathf.Sin(a)*7);
            Part("Lantern post",PrimitiveType.Cylinder,p+Vector3.up*.8f,new Vector3(.12f,1.6f,.12f),wood);
            Part("Lantern",PrimitiveType.Sphere,p+Vector3.up*1.7f,new Vector3(.3f,.3f,.3f),gold);
        }
    }
    void CameraSetup()
    {
        var go=new GameObject("Kingdom Camera"); cam=go.AddComponent<Camera>();
        cam.fieldOfView=36; cam.nearClipPlane=.1f; cam.farClipPlane=180;
        go.AddComponent<AudioListener>(); UpdateCamera();
    }
    void UpdateCamera()
    {
        if(!cam) return;
        float p=Mathf.Clamp(pitch,28,68)*Mathf.Deg2Rad;
        Vector3 target=new Vector3(0,1.2f,0);
        Vector3 off=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad)*Mathf.Cos(p),Mathf.Sin(p),Mathf.Cos(yaw*Mathf.Deg2Rad)*Mathf.Cos(p))*distance;
        cam.transform.position=target+off; cam.transform.LookAt(target);
    }
    void Update()
    {
        if(Input.touchCount==1) {
            Touch t=Input.GetTouch(0);
            if(t.phase==TouchPhase.Began){last=t.position;dragging=true;}
            else if(t.phase==TouchPhase.Moved&&dragging){Vector2 d=t.position-last;yaw+=d.x*.22f;pitch=Mathf.Clamp(pitch-d.y*.12f,28,68);last=t.position;UpdateCamera();}
            else if(t.phase==TouchPhase.Ended){if(dragging && (t.position-last).magnitude<18f) SelectAt(t.position);dragging=false;}
        } else if(Input.touchCount==2) {
            dragging=false; Touch a=Input.GetTouch(0),b=Input.GetTouch(1);
            float oldD=Vector2.Distance(a.position-a.deltaPosition,b.position-b.deltaPosition);
            float newD=Vector2.Distance(a.position,b.position);
            distance=Mathf.Clamp(distance-(newD-oldD)*.025f,20,56);UpdateCamera();
        } else if(Input.GetMouseButtonDown(0)){last=Input.mousePosition;dragging=true;}
        else if(Input.GetMouseButton(0)&&dragging){Vector2 d=(Vector2)Input.mousePosition-last;yaw+=d.x*.22f;pitch=Mathf.Clamp(pitch-d.y*.12f,28,68);last=Input.mousePosition;UpdateCamera();}
        if(Input.GetMouseButtonUp(0)){if(dragging && ((Vector2)Input.mousePosition-last).magnitude<8f) SelectAt(Input.mousePosition);dragging=false;}
        if(Mathf.Abs(Input.mouseScrollDelta.y)>.01f){distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*2,20,56);UpdateCamera();}
    }
    void SelectAt(Vector2 point)
    {
        if (!cam) return;
        Ray ray = cam.ScreenPointToRay(point);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 160f))
        {
            string objectName = hit.collider.gameObject.name;
            if (objectName.EndsWith(" selection collider"))
            {
                selectedBuilding = objectName.Replace(" selection collider", "");
                if (!buildingLevels.ContainsKey(selectedBuilding))
                    buildingLevels[selectedBuilding] = PlayerPrefs.GetInt("lvl_" + selectedBuilding, 1);
                panel = "";
            }
        }
    }

    void LoadProgress()
    {
        timber = PlayerPrefs.GetInt("timber", 1200);
        stoneCount = PlayerPrefs.GetInt("stone", 900);
        food = PlayerPrefs.GetInt("food", 1500);
        coins = PlayerPrefs.GetInt("coins", 250);
    }

    void SaveProgress()
    {
        PlayerPrefs.SetInt("timber", timber);
        PlayerPrefs.SetInt("stone", stoneCount);
        PlayerPrefs.SetInt("food", food);
        PlayerPrefs.SetInt("coins", coins);
        foreach (var item in buildingLevels)
            PlayerPrefs.SetInt("lvl_" + item.Key, item.Value);
        PlayerPrefs.Save();
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize=Mathf.Max(13,Screen.width/48);
        GUI.skin.button.fontSize=Mathf.Max(13,Screen.width/55);
        GUI.Box(new Rect(8,8,Screen.width-16,54),"");
        GUI.Label(new Rect(18,15,Screen.width*.42f,34),"KRALLIQLAR DÖVRÜ  •  LVL 1");
        GUI.Label(new Rect(Screen.width*.43f,15,Screen.width*.56f,34),"ODUN "+timber+"  DAŞ "+stoneCount+"  QİDA "+food+"  QIZIL "+coins);
        string[] nav={"QALA","ORDU","DÜNYA","İTTİFAQ"};
        float bw=(Screen.width-42)/4f;
        for(int i=0;i<nav.Length;i++) if(GUI.Button(new Rect(8+i*(bw+8),Screen.height-58,bw,48),nav[i])) {
            panel=nav[i];
            if(nav[i]=="ORDU")panel="Qoşunlar: 120 piyada • 80 oxçu • 40 süvari";
            if(nav[i]=="DÜNYA")panel="Dünya xəritəsi — növbəti inkişaf mərhələsi";
            if(nav[i]=="İTTİFAQ")panel="İttifaqlar — növbəti inkişaf mərhələsi";
        }
        GUI.Label(new Rect(12,68,Screen.width-24,28),"Şəhəri döndərmək üçün sürüşdür • yaxınlaşdırmaq üçün iki barmaq");
        if(selectedBuilding!="") {
            int lvl=buildingLevels.ContainsKey(selectedBuilding)?buildingLevels[selectedBuilding]:1;
            GUI.Box(new Rect(12,Screen.height-154,Screen.width-24,84),selectedBuilding+"  •  SƏVİYYƏ "+lvl);
            if(GUI.Button(new Rect(24,Screen.height-112,155,32),"İnkişaf et • 100 odun")) {
                if(timber>=100){timber-=100;buildingLevels[selectedBuilding]=lvl+1;SaveProgress();}
            }
            if(GUI.Button(new Rect(Screen.width-92,Screen.height-148,72,30),"BAĞLA"))selectedBuilding="";
        }
        else if(panel!="") {
            GUI.Box(new Rect(12,Screen.height-154,Screen.width-24,84),panel);
            if(GUI.Button(new Rect(Screen.width-92,Screen.height-148,72,30),"BAĞLA"))panel="";
            if(panel=="QALA"&&GUI.Button(new Rect(24,Screen.height-112,160,32),"İnkişaf et • 100 odun")) {
                if(timber>=100){timber-=100;coins+=10;SaveProgress();panel="Qala inkişaf etdi!";}
            }
        }
    }
}
