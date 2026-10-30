using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The final opening image stays up across scene loading, then gives way to the
// real reset-loop cutscene. It does not load scenes or mutate story/save state.
public sealed class StoryRewindBridge : MonoBehaviour
{
    public CanvasGroup fade;
    private bool armed,arriving;
    private Scene previousScene;
    private float armedAt;
    public void Arm()
    {armed=true;previousScene=SceneManager.GetActiveScene();armedAt=Time.realtimeSinceStartup;SceneManager.sceneLoaded+=SceneLoaded;}
    private void SceneLoaded(Scene scene,LoadSceneMode mode)
    {if(armed&&!arriving&&scene.name=="Level_02"&&scene!=previousScene){arriving=true;StartCoroutine(RevealOpening());}}
    private IEnumerator RevealOpening()
    {
        yield return null;yield return null;yield return new WaitForSecondsRealtime(.2f);
        float t=0;while(t<.35f){t+=Time.unscaledDeltaTime;if(fade!=null)fade.alpha=1-Mathf.SmoothStep(0,1,t/.35f);yield return null;}Destroy(gameObject);
    }
    private void Update(){if(armed&&Time.realtimeSinceStartup-armedAt>12)Destroy(gameObject);}
    private void OnDestroy()=>SceneManager.sceneLoaded-=SceneLoaded;
}
