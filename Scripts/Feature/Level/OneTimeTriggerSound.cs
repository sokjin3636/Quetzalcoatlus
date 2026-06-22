using UnityEngine;

public class OneTimeTriggerSound : MonoBehaviour
{
    public AudioSource centerSpeaker;
    private bool hasPlayed = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasPlayed && other.CompareTag("Player"))
        {
            if (centerSpeaker != null)
            {
                // 중복 재생 방지를 위한 단발성 사운드 호출
                centerSpeaker.Play();
            }

            hasPlayed = true;
        }
    }
}