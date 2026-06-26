using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbientMusic : MonoBehaviour
{
    [Header("Musique")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float musicVolume = 0.4f; // multiplié par le volume global (Options)

    private AudioSource _source;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.clip = musicClip;
        _source.loop = true;
        _source.playOnAwake = false;
        _source.volume = musicVolume;
        _source.spatialBlend = 0f; // son 2D, pas spatialisé

        if (musicClip != null)
            _source.Play();
    }
}