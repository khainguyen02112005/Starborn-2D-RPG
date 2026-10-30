using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A short visual rewind, not a backwards simulation of gameplay/save data.
// Frames are captured from this game; only the transparent film gate is new art.
public sealed class StoryFilmRewind : MonoBehaviour
{
    public bool IsPlaying { get; private set; }
    public int HistoryCount => history.Count;
    public int DisplayedFrameIndex { get; private set; }
    public StoryRewindBridge Bridge { get; private set; }
    private Level04Controller owner;
    private readonly List<Texture2D> history=new List<Texture2D>();
    private static readonly Rect Aperture=Rect.MinMaxRect(.09f,.133f,.91f,.868f);

    public void Bind(Level04Controller source)
    {
        owner=source;StartCoroutine(RecordHistory());
    }
    private IEnumerator RecordHistory()
    {
        while(true)
        {
            yield return new WaitForSecondsRealtime(.65f);
            if(IsPlaying||owner==null||!owner.CanRecordRewind)continue;
            yield return new WaitForEndOfFrame();
            if(IsPlaying||owner==null||!owner.CanRecordRewind)continue;
            Texture2D full=ScreenCapture.CaptureScreenshotAsTexture();
            if(full==null)continue;
            // Nearest-neighbour copy preserves the original pixel-art colours;
            // no gamma-changing blit, repainting or generated character stand-ins.
            Color32[] source=full.GetPixels32();const int width=480,height=270;
            var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[y*width+x]=source[Mathf.Min(full.height-1,y*full.height/height)*full.width+Mathf.Min(full.width-1,x*full.width/width)];
            var frame=new Texture2D(width,height,TextureFormat.RGBA32,false){name="StoryFilmFrame",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};frame.SetPixels32(pixels);frame.Apply(false,false);Destroy(full);
            history.Add(frame);if(history.Count>40){Destroy(history[0]);history.RemoveAt(0);}
        }
    }
    private sealed class Cell
    {
        public RectTransform root;
        public RawImage picture;
        public CanvasGroup group;
    }
    private static RectTransform RectNode(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
    }
    private Cell MakeCell(Transform parent,Sprite film,float width,float height)
    {
        RectTransform rect=RectNode("RewindFilmCell",parent,Vector2.one*.5f,Vector2.one*.5f);rect.sizeDelta=new Vector2(width,height);
        var group=rect.gameObject.AddComponent<CanvasGroup>();
        RectTransform aperture=RectNode("ApertureViewport",rect,Aperture.min,Aperture.max);aperture.gameObject.AddComponent<RectMask2D>();
        var backing=RectNode("ApertureBlack",aperture,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();backing.color=Color.black;backing.raycastTarget=false;
        var image=RectNode("ActualGameFrame",aperture,Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();image.raycastTarget=false;image.color=new Color(.98f,.95f,.88f,1);
        var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=16f/9f;
        var border=RectNode("GeneratedFilmGate",rect,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();border.sprite=film;border.raycastTarget=false;
        return new Cell{root=rect,picture=image,group=group};
    }
    public IEnumerator Play(bool mage,TMP_FontAsset font)
    {
        if(IsPlaying)yield break;IsPlaying=true;
        Sprite film=Resources.Load<Sprite>("StoryRewind/FilmFrame");
        Texture2D opening=Resources.Load<Texture2D>(mage?"StoryRewind/OpeningMage":"StoryRewind/OpeningKnight");
        if(opening==null)throw new System.InvalidOperationException("Missing actual village opening frame");
        var root=new GameObject("StoryFilmRewindOverlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
        DontDestroyOnLoad(root);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;CinematicUiLayer.Configure(root,36000,true);
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        CanvasGroup rootGroup=root.GetComponent<CanvasGroup>();rootGroup.blocksRaycasts=true;rootGroup.interactable=false;
        var black=RectNode("RewindMatte",root.transform,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();black.color=new Color(.012f,.012f,.015f,1);black.raycastTarget=true;
        RawImage background=RectNode("MemoryBackdrop",root.transform,Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();background.raycastTarget=false;background.texture=history.Count>0?history[history.Count-1]:opening;background.color=new Color(.36f,.34f,.3f,1);
        RectTransform transport=RectNode("FilmTransport",root.transform,Vector2.zero,Vector2.one);transport.gameObject.AddComponent<RectMask2D>();Canvas.ForceUpdateCanvases();
        float width=root.GetComponent<RectTransform>().rect.width*.86f;float cellHeight=width/1.5f;
        var cells=new[]{MakeCell(transport,film,width,cellHeight),MakeCell(transport,film,width,cellHeight),MakeCell(transport,film,width,cellHeight)};
        const float length=3.35f;float elapsed=0;
        while(elapsed<length)
        {
            elapsed+=Time.unscaledDeltaTime;rootGroup.alpha=Mathf.Clamp01(elapsed/.15f);
            float rewind=Mathf.Clamp01((elapsed-.2f)/2.4f);float settle=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-2.6f)/.75f));
            DisplayedFrameIndex=history.Count>0?Mathf.Clamp(Mathf.FloorToInt((history.Count-1)*(1-rewind)),0,history.Count-1):0;
            Texture image=history.Count>0?history[DisplayedFrameIndex]:opening;
            float stride=width+24f;
            float scroll=Mathf.Repeat(elapsed*stride*Mathf.Lerp(3.5f,8f,rewind),stride)*(1-settle);
            for(int i=0;i<cells.Length;i++)
            {
                Cell cell=cells[i];int index=history.Count>0?Mathf.Clamp(DisplayedFrameIndex+i-1,0,history.Count-1):0;
                cell.picture.texture=settle>0?opening:history.Count>0?history[index]:opening;
                cell.root.anchoredPosition=new Vector2((i-1)*stride+scroll,Mathf.Sin(elapsed*39)*1.4f*(1-settle));
                cell.root.localScale=Vector3.one*Mathf.Lerp(1,1.12f,settle);cell.group.alpha=1-settle;
            }
            background.texture=settle>0?opening:image;background.color=Color.Lerp(new Color(.36f,.34f,.3f,1),Color.white,settle);
            yield return null;
        }
        foreach(Cell cell in cells)cell.root.gameObject.SetActive(false);background.texture=opening;background.color=Color.white;
        Bridge=root.AddComponent<StoryRewindBridge>();Bridge.fade=rootGroup;
    }
    private void OnDestroy()
    {foreach(Texture2D frame in history)if(frame!=null)Destroy(frame);history.Clear();}
}
