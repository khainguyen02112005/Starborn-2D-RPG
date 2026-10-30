using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.Tilemaps;

// Each object breaks into horizontal signal ribbons. A ribbon stutters white,
// slips sideways, then is permanently missing while other ribbons stay intact.
// No whole-screen white veil, global opacity fade or granular erosion mask.
public sealed class Ending3SignalDeletion : MonoBehaviour
{
    public const float Duration = 10.8f;
    public float Elapsed { get; private set; }
    public bool Complete => Elapsed >= Duration;
    public int GroupCount => groups.Count;
    public int CompletedGroups { get; private set; }
    public int ActiveGroups { get; private set; }
    private readonly List<Group> groups = new List<Group>();
    private readonly Dictionary<Renderer,Material[]> originals = new Dictionary<Renderer,Material[]>();
    private readonly List<Surface> surfaces = new List<Surface>();
    private readonly Dictionary<Transform,int> descendantCounts = new Dictionary<Transform,int>();
    private Camera worldCamera;
    private Color previousBackground;
    private sealed class Group
    {
        public float start, length=2.85f, seed;
        public bool terrain, sky, visible;
        public Bounds bounds;
        public readonly List<Renderer> renderers=new List<Renderer>();
    }
    private sealed class Surface
    {
        public Material material;
        public SpriteRenderer renderer;
        public Sprite sprite;
    }
    public static Ending3SignalDeletion Begin(Transform hero,List<Renderer> renderers)
    {
        var go=new GameObject("Ending3_ObjectSignalFailures");var effect=go.AddComponent<Ending3SignalDeletion>();effect.Initialize(hero,renderers);return effect;
    }
    private Transform GroupRoot(Renderer renderer)
    {
        foreach(var c in renderer.GetComponentsInParent<MonoBehaviour>())
            if(c is NPCDialogue||c is NPCShop||c is NPCBlacksmith||c is EnemyAI||c is StorageChest||c is ScenePortal)return c.transform;
        Transform node=renderer.transform;
        while(node.parent!=null)
        {
            Transform parent=node.parent;
            if(parent.GetComponent<MapSceneLayout>()!=null||parent.GetComponent<Grid>()!=null||parent.name.StartsWith("LAYER"))break;
            if(!descendantCounts.TryGetValue(parent,out int count)){count=parent.GetComponentsInChildren<Renderer>(true).Length;descendantCounts[parent]=count;}
            if(count>18)break;
            node=parent;
        }
        return node;
    }
    private void Initialize(Transform hero,List<Renderer> renderers)
    {
        var template=Resources.Load<Material>("Ending3/SignalDeletion");
        if(template==null)throw new System.InvalidOperationException("Missing ending 3 signal material");
        worldCamera=Camera.main;if(worldCamera!=null){previousBackground=worldCamera.backgroundColor;worldCamera.backgroundColor=Color.white;}
        var roots=new Dictionary<Transform,Group>();var random=new System.Random(2711);
        foreach(Renderer renderer in renderers)
        {
            if(renderer==null||(hero!=null&&renderer.transform.IsChildOf(hero)))continue;
            bool terrain=renderer is TilemapRenderer;
            Transform key=terrain?renderer.transform:GroupRoot(renderer);
            if(!roots.TryGetValue(key,out Group group))
            {
                group=new Group{terrain=terrain,sky=renderer.GetComponentInParent<PixelSkyBackdrop>()!=null,seed=(float)random.NextDouble()*97+2};
                group.bounds=renderer.bounds;
                roots.Add(key,group);groups.Add(group);
            }
            else group.bounds.Encapsulate(renderer.bounds);
            group.renderers.Add(renderer);
        }
        var visible=new List<Group>();var outside=new List<Group>();
        Plane[] frustum=worldCamera!=null?GeometryUtility.CalculateFrustumPlanes(worldCamera):null;
        foreach(Group group in groups)
        {
            group.visible=frustum!=null&&GeometryUtility.TestPlanesAABB(frustum,group.bounds);
            if(!group.terrain&&!group.sky)(group.visible?visible:outside).Add(group);
        }
        Shuffle(visible,random);Shuffle(outside,random);
        for(int i=0;i<visible.Count;i++){visible[i].start=.15f+2.85f*i/Mathf.Max(1,visible.Count-1);visible[i].length=2.65f+(float)random.NextDouble()*.5f;}
        foreach(Group group in outside){group.start=.1f+(float)random.NextDouble()*3.6f;group.length=2.75f;}
        foreach(Group group in groups)
        {
            if(group.terrain){group.start=5.25f;group.length=3.1f;}
            if(group.sky){group.start=8.35f;group.length=1.45f;}
            Vector4 objectRect=new Vector4(group.bounds.min.x,group.bounds.min.y,Mathf.Max(.01f,group.bounds.size.x),Mathf.Max(.01f,group.bounds.size.y));
            float lineCount=group.terrain||group.sky?24:Mathf.Clamp(Mathf.Round(group.bounds.size.y*11),9,48);
            foreach(Renderer renderer in group.renderers)
            {
                if(!(renderer is SpriteRenderer)&&!(renderer is TilemapRenderer))continue;
                Material[] old=renderer.sharedMaterials;originals.Add(renderer,old);var replacements=new Material[old.Length];
                for(int i=0;i<old.Length;i++)
                {
                    var mat=new Material(template);replacements[i]=mat;
                    if(old[i]!=null)
                    {
                        if(old[i].HasProperty("_MainTex"))mat.mainTexture=old[i].mainTexture;
                        if(old[i].HasProperty("_Color"))mat.color=old[i].color;
                        if(old[i].shader.name=="Story/WorldPixelDissolve")
                        {
                            mat.SetFloat("_Progress",old[i].GetFloat("_Progress"));mat.SetFloat("_PulseCount",old[i].GetFloat("_PulseCount"));var holes=old[i].GetVectorArray("_ErasePulses");if(holes!=null&&holes.Length>0)mat.SetVectorArray("_ErasePulses",holes);
                        }
                    }
                    mat.SetFloat("_StartAt",group.start);mat.SetFloat("_Duration",group.length);mat.SetFloat("_Seed",group.seed);mat.SetFloat("_Chunked",group.terrain||group.sky?1:0);mat.SetFloat("_ShiftUV",group.terrain||group.sky?0:1);mat.SetVector("_ObjectRect",objectRect);mat.SetFloat("_LineCount",lineCount);
                    var surface=new Surface{material=mat,renderer=renderer as SpriteRenderer};RefreshSprite(surface);surfaces.Add(surface);
                }
                renderer.sharedMaterials=replacements;
            }
        }
    }
    private static void Shuffle(List<Group> list,System.Random random)
    {for(int i=list.Count-1;i>0;i--){int j=random.Next(i+1);Group temp=list[i];list[i]=list[j];list[j]=temp;}}
    private static void RefreshSprite(Surface surface)
    {
        if(surface.renderer==null||surface.renderer.sprite==surface.sprite)return;surface.sprite=surface.renderer.sprite;
        if(surface.sprite!=null)surface.material.SetVector("_SourceRect",DataUtility.GetOuterUV(surface.sprite));
    }
    public void Step(float delta)
    {
        Elapsed+=Mathf.Clamp(delta,0,.1f);CompletedGroups=ActiveGroups=0;
        foreach(Surface surface in surfaces){RefreshSprite(surface);surface.material.SetFloat("_Clock",Elapsed);}
        foreach(Group group in groups)
        {
            float end=group.start+group.length+(group.terrain||group.sky?.9f:0);
            if(Elapsed>=end)
            {
                CompletedGroups++;foreach(Renderer renderer in group.renderers)if(renderer!=null)renderer.enabled=false;
            }
            else if(Elapsed>=group.start)ActiveGroups++;
        }
    }
    private void OnDestroy()
    {
        foreach(var pair in originals)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;
        foreach(Surface surface in surfaces)if(surface.material!=null)Destroy(surface.material);
        if(!Complete)
        {
            foreach(Group group in groups)foreach(Renderer renderer in group.renderers)if(renderer!=null)renderer.enabled=true;
            if(worldCamera!=null)worldCamera.backgroundColor=previousBackground;
        }
    }
}
