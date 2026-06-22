using UnityEngine;
using UnityEngine.Android;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SoundTensionProvider : MonoBehaviour, ITensionProvider
{
    [Header("--- 비명 감지 설정 ---")]
    public float dbThreshold = 80f;
    public float hzThreshold = 80f;
    public float instantScore = 30f;

    [Header("--- 시스템 설정 ---")]
    public float screamCooldown = 2.0f;
    private float lastScreamTime = -10f;

    private string micDevice;
    private AudioClip micClip;
    private AudioSource audioSource;
    private bool isInitialized = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    IEnumerator Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.Microphone));
        }
#endif
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 1, 44100);

            audioSource.clip = micClip;
            audioSource.loop = true;

            // 마이크 입력 소리 차단을 위해 AudioMixer의 Output 볼륨 제어 권장
            audioSource.mute = false;
            audioSource.volume = 1.0f;

            while (!(Microphone.GetPosition(micDevice) > 0)) yield return null;

            audioSource.Play();

            isInitialized = true;
            Debug.Log("<color=green>[Sound] 주파수 분석 가동 (Mute 해제 상태)</color>");
        }
    }

    public void StartCalibration() { }
    public void CollectCalibrationData() { }
    public void FinishCalibration() { }
    public float GetRawStressScore() { return 0; }

    public float GetInstantAddition()
    {
        if (!isInitialized || !Microphone.IsRecording(micDevice)) return 0;

        float vol = GetRMSVolume();
        if (vol <= 0.0001f) return 0;

        float db = 20 * Mathf.Log10(vol / 0.1f) + 93.0f;
        float hz = GetPeakFrequency();

        if (db >= dbThreshold && hz >= hzThreshold)
        {
            if (Time.time >= lastScreamTime + screamCooldown)
            {
                lastScreamTime = Time.time;
                return instantScore;
            }
        }
        return 0;
    }

    public string GetActiveReason()
    {
        return (Time.time < lastScreamTime + 0.5f) ? "비명 감지!" : "";
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
        float[] spectrum = new float[1024];
        // AudioSource가 재생(Play) 상태일 때 스펙트럼 데이터 수집 가능
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float maxV = 0;
        int maxN = 0;
        for (int i = 0; i < 1024; i++)
        {
            if (spectrum[i] > maxV)
            {
                maxV = spectrum[i];
                maxN = i;
            }
        }
        return maxN * (AudioSettings.outputSampleRate / 2f) / 1024f;
    }
}