using UnityEngine;

namespace PrisonLife.FX
{
    /// <summary>
    /// Null-safe SFX helpers. One-shot uses PlayClipAtPoint (no AudioSource setup needed).
    /// Loop helpers manage a runtime AudioSource on a host GameObject.
    /// </summary>
    public static class SfxPlayer
    {
        public static void PlayOneShot(AudioClip clip, Vector3 worldPos, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, worldPos, Mathf.Clamp01(volume));
        }

        public static void EnsureLoop(ref AudioSource src, GameObject host, AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                if (src != null && src.isPlaying) src.Stop();
                return;
            }
            if (src == null)
            {
                src = host.AddComponent<AudioSource>();
                src.loop = true;
                src.playOnAwake = false;
                src.spatialBlend = 1f;
            }
            if (src.clip != clip) src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            if (!src.isPlaying) src.Play();
        }

        public static void StopLoop(AudioSource src)
        {
            if (src != null && src.isPlaying) src.Stop();
        }
    }
}
