using UnityEngine;
using UnityEngine.Android;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SoundDetector : MonoBehaviour
{
    [Header("--- 감도 설정 ---")]
    public float volumeThreshold = 0.0005f;

    private string micDevice;
    private AudioClip micClip;
    private AudioSource audioSource;
    private float[] spectrumData = new float[1024];
    private bool isInitialized = false;

    IEnumerator Start()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.Microphone));
        }
#endif

        audioSource = GetComponent<AudioSource>();

        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            Debug.Log($"<color=cyan>[시스템] 마이크 연결: {micDevice}</color>");

            micClip = Microphone.Start(micDevice, true, 1, 44100);

            audioSource.clip = micClip;
            audioSource.loop = true;

            audioSource.mute = false;
            audioSource.volume = 1f;

            while (!(Microphone.GetPosition(micDevice) > 0)) yield return null;

            audioSource.Play();
            isInitialized = true;
            Debug.Log("<color=green>[시스템] 주파수 분석 준비 완료!</color>");
        }
    }

    void Update()
    {
        if (!isInitialized || micClip == null || !Microphone.IsRecording(micDevice)) return;

        float vol = GetRMSVolume();

        if (vol > volumeThreshold)
        {
            // 볼륨 데시벨(SPL) 변환 시 보정치 적용
            float db = 20 * Mathf.Log10(vol / 0.1f) + 93.0f;
            float hz = GetPeakFrequency();

            if (hz > 0)
            {
                Debug.Log($"<color=yellow>소리감지!</color> | dB: {db:F1} | <color=white>주파수: {hz:F0}Hz</color>");
            }
        }
    }

    private float GetRMSVolume()
    {
        float[] samples = new float[256];
        int startPos = Microphone.GetPosition(micDevice) - 256;
        if (startPos < 0) return 0;
        micClip.GetData(samples, startPos);
        float sum = 0;
        foreach (var s in samples) sum += s * s;
        return Mathf.Sqrt(sum / 256);
    }

    private float GetPeakFrequency()
    {
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        float maxV = 0;
        int maxN = 0;

        for (int i = 0; i < spectrumData.Length; i++)
        {
            if (spectrumData[i] > maxV)
            {
                maxV = spectrumData[i];
                maxN = i;
            }
        }
        return maxN * 22050f / 1024f;
    }
}