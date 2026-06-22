using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CorpseImpactSound : MonoBehaviour
{
    private AudioSource audioSource;
    private bool hasPlayed = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f;
        audioSource.playOnAwake = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        // 물리 충돌 상대 속도 임계치 도달 여부 검사
        if (!hasPlayed && collision.relativeVelocity.magnitude > 2.0f)
        {
            audioSource.Play();
            hasPlayed = true;
        }
    }
}