using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Explicit opt-in styling. Never scans/repaints the book, item icons or VFX.
public static class GameUITheme
{
    public static readonly Color Cream=new Color(.95f,.9f,.76f,1);
    public static readonly Color Gold=new Color(.86f,.68f,.34f,1);
    public static readonly Color Muted=new Color(.65f,.65f,.56f,1);
    public static readonly Color Track=new Color(.07f,.085f,.07f,1);
    static Sprite panel,button,dialogue;
    static TMP_FontAsset font;
    static TMP_FontAsset readableFont;
    public static TMP_FontAsset ReadableFont
    {
        get
        {
            if(readableFont==null)readableFont=Resources.Load<TMP_FontAsset>("GameUI/BrassVillage/ReadableUIFont");
            return readableFont;
        }
    }
    static Sprite Load(string name)=>Resources.Load<Sprite>("GameUI/BrassVillage/"+name);
    public static void Surface(Image image,string role="Panel",float multiplier=1)
    {
        if(image==null)return;
        if(panel==null)panel=Load("Panel");if(button==null)button=Load("Button");if(dialogue==null)dialogue=Load("Dialogue");
        Sprite sprite=role=="Button"?button:role=="Dialogue"?dialogue:panel;
        if(sprite==null)return;image.sprite=sprite;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=multiplier;image.color=Color.white;
    }
    public static void Text(TMP_Text text,bool heading=false)
    {
        if(text==null)return;if(font==null)font=Resources.Load<TMP_FontAsset>("GameUI/BrassVillage/UIFont");var chosen=heading||ReadableFont==null?font:ReadableFont;if(chosen!=null)text.font=chosen;text.color=heading?Gold:Cream;text.faceColor=Color.white;text.raycastTarget=false;
    }
    public static void Button(Button button)
    {
        if(button!=null&&Application.isPlaying&&button.GetComponent<UISoundEmitter>()==null)button.gameObject.AddComponent<UISoundEmitter>();
        if(button==null)return;Surface(button.image,"Button",1.3f);var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.94f,.76f);colors.pressedColor=new Color(.68f,.6f,.42f);colors.selectedColor=new Color(1,.94f,.76f);colors.disabledColor=new Color(.4f,.4f,.35f,.65f);colors.fadeDuration=.1f;button.colors=colors;button.transition=Selectable.Transition.ColorTint;
        foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))Text(text);
    }
}
