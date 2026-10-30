using UnityEngine;

public class PlayerFootstepSFX : MonoBehaviour
{
    [SerializeField] private AudioSource footstepSource;

    public void PlayFootstep()
    {
        if (GameAudioManager.IsReady)
        {
            GameAudioManager.PlayFootstep(transform.position);
            return;
        }
        if (footstepSource != null) footstepSource.Play();
    }
}
