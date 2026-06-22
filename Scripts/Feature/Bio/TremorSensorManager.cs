using UnityEngine;
using UnityEngine.XR;
using System.IO;
using System.Text;
using System.Collections.Generic;

// ==========================================================
// Tremor Detection System (Static Baseline Version)
// - Epsilon 보정: 영점 나누기 오류 방지
// - 고정 임계값: 캘리브레이션 이후 영점 고정
// - 히스테리시스: 감지 상태 전환 플리커링(Flickering) 방지
// ==========================================================

public class TremorSensorManager : MonoBehaviour, ITensionProvider
{
    [Header("Sensor & XR")]
    public float sampleRate = 90f;
    public XRNode tensionHand = XRNode.RightHand;

    [Header("Detection & Tuning")]
    public float ratioThresholdK = 2.5f;
    public float maxMotionEnergy = 20.0f;

    [Header("Calibration Settings")]
    public float calibrationTime = 10f;
    private float _calibrationTimer = 0f;
    public bool isCalibrating = true;

    [Header("Continuous Score Settings (Max 1.0)")]
    public float expectedMaxExcess = 15.0f;

    [Header("Instant Burst Settings")]
    public float instantSpikeRatio = 20.0f;
    public float instantMultiplier = 0.5f;

    [Header("Output")]
    public float tremorEnergy;
    public float motionEnergy;
    public float ratio;
    public float threshold;
    public float confidence;
    public bool detected;

    [Header("WESAD Playback Injection")]
    public bool useWesadData = false;
    public float wesadScale = 50f;
    private List<Vector3> _wesadBaseline = new List<Vector3>();
    private List<Vector3> _wesadStress = new List<Vector3>();
    private int _playbackIndex = 0;
    private bool _isPlayingStress = false;

    [Header("Recording & Testing")]
    public float recordDuration = 60f;
    private bool _isRecording = false;
    private float _recordStartTime = 0f;
    private StringBuilder _csvData;
    private float _userMarker = 0f;

    private HeartRateTensionProvider _hrProvider;

    private float _instantScoreBuffer = 0f;
    private bool _isSpiking = false;

    // Filters
    Biquad _bp1X, _bp2X, _bp1Y, _bp2Y, _bp1Z, _bp2Z;
    Biquad _lpX, _lpY, _lpZ;

    EMA _emaTremor = new EMA(0.05f);
    EMA _emaMotion = new EMA(0.05f);

    AdaptiveStat _ratioStat = new AdaptiveStat();

    void Start()
    {
        float Q = 2.0f;

        _bp1X = new Biquad(10f, Q, sampleRate, Biquad.Type.Bandpass);
        _bp2X = new Biquad(10f, Q, sampleRate, Biquad.Type.Bandpass);
        _bp1Y = new Biquad(10f, Q, sampleRate, Biquad.Type.Bandpass);
        _bp2Y = new Biquad(10f, Q, sampleRate, Biquad.Type.Bandpass);
        _bp1Z = new Biquad(10f, Q, sampleRate, Biquad.Type.Bandpass);
        _bp2Z = new Biquad(10f, Q, sampleRate, Biquad.Type.Bandpass);

        _lpX = new Biquad(3f, 0.7f, sampleRate, Biquad.Type.Lowpass);
        _lpY = new Biquad(3f, 0.7f, sampleRate, Biquad.Type.Lowpass);
        _lpZ = new Biquad(3f, 0.7f, sampleRate, Biquad.Type.Lowpass);

        _hrProvider = GetComponent<HeartRateTensionProvider>();

        LoadWesadData("WESAD_Baseline", _wesadBaseline);
        LoadWesadData("WESAD_Stress", _wesadStress);
    }

    void LoadWesadData(string fileName, List<Vector3> targetList)
    {
        TextAsset csvData = Resources.Load<TextAsset>(fileName);
        if (csvData == null) return;

        string[] lines = csvData.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');
            if (values.Length >= 3)
            {
                if (float.TryParse(values[0], out float x) &&
                    float.TryParse(values[1], out float y) &&
                    float.TryParse(values[2], out float z))
                {
                    targetList.Add(new Vector3(x, y, z));
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!_isRecording) StartRecording();
            else StopRecording();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            _userMarker = 10f;
        }

        if (useWesadData)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _isPlayingStress = false;
                _playbackIndex = 0;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _isPlayingStress = true;
                _playbackIndex = 0;
            }
        }
    }

    void FixedUpdate()
    {
        Vector3 w = Vector3.zero;

        if (useWesadData)
        {
            List<Vector3> currentList = _isPlayingStress ? _wesadStress : _wesadBaseline;

            if (currentList.Count > 0)
            {
                w = currentList[_playbackIndex % currentList.Count] * wesadScale;
                _playbackIndex++;
            }
        }
        else
        {
            if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand)
                .TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 deviceW))
            {
                w = deviceW;
            }
        }

        ProcessSignal(w);
    }

    void ProcessSignal(Vector3 w)
    {
        float tx = _bp2X.Process(_bp1X.Process(w.x));
        float ty = _bp2Y.Process(_bp1Y.Process(w.y));
        float tz = _bp2Z.Process(_bp1Z.Process(w.z));

        float tremorInstant = tx * tx + ty * ty + tz * tz;
        tremorEnergy = _emaTremor.Process(tremorInstant);

        float mx = _lpX.Process(w.x);
        float my = _lpY.Process(w.y);
        float mz = _lpZ.Process(w.z);

        float motionInstant = mx * mx + my * my + mz * mz;
        motionEnergy = _emaMotion.Process(motionInstant);

        confidence = Mathf.Clamp01(1f - motionEnergy / maxMotionEnergy);

        float epsilon = 1e-6f;
        ratio = tremorEnergy / (motionEnergy + epsilon);
        float weightedRatio = ratio * confidence;

        if (isCalibrating)
        {
            _calibrationTimer += Time.fixedDeltaTime;
            _ratioStat.Update(weightedRatio);

            if (_calibrationTimer >= calibrationTime)
            {
                isCalibrating = false;
                // 캘리브레이션 완료 시 임계값 확정
                threshold = _ratioStat.Mean + ratioThresholdK * _ratioStat.Std;
                Debug.Log("Tremor Sensor Calibration Complete. Threshold Locked at: " + threshold);
            }
            detected = false;
        }
        else
        {
            // 임계값 고정 이후 통계 업데이트 중단

            float releaseMargin = 0.7f;

            if (!detected)
            {
                detected = weightedRatio > threshold;
            }
            else
            {
                detected = weightedRatio > (threshold * releaseMargin);
            }

            if (detected)
            {
                if (ratio > instantSpikeRatio && confidence > 0.4f)
                {
                    if (!_isSpiking)
                    {
                        float burstScore = (ratio - threshold) * instantMultiplier;
                        _instantScoreBuffer = Mathf.Max(5.0f, burstScore);
                        _isSpiking = true;
                    }
                }
                else if (ratio < instantSpikeRatio * 0.8f)
                {
                    _isSpiking = false;
                }
            }
            else
            {
                _isSpiking = false;
            }
        }

        if (_isRecording)
        {
            float t = Time.fixedTime - _recordStartTime;

            if (t >= recordDuration)
            {
                StopRecording();
            }
            else
            {
                float currentTremorScore = GetRawStressScore();
                float hrScore = (_hrProvider != null) ? _hrProvider.GetRawStressScore() : 0f;

                _csvData.AppendLine(
                    $"{t:F3}," +
                    $"{w.x:F4},{w.y:F4},{w.z:F4}," +
                    $"{tremorEnergy:F6},{motionEnergy:F6}," +
                    $"{ratio:F4},{confidence:F4},{threshold:F4}," +
                    $"{currentTremorScore:F4},{_userMarker:F1},{hrScore:F4}"
                );

                _userMarker = 0f;
            }
        }
    }

    void StartRecording()
    {
        _isRecording = true;
        _recordStartTime = Time.fixedTime;
        _csvData = new StringBuilder();
        _csvData.AppendLine("Time,wx,wy,wz,Tremor,Motion,Ratio,Confidence,Threshold,Tremor_Score,User_Marker,HR_Score");
    }

    void StopRecording()
    {
        if (!_isRecording) return;
        _isRecording = false;
        string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string file = Path.Combine(path, $"Tremor_Log_{System.DateTime.Now:HHmmss}.csv");
        File.WriteAllText(file, _csvData.ToString());
    }

    public void StartCalibration()
    {
        _ratioStat.Reset();
        _calibrationTimer = 0f;
        isCalibrating = true;
    }

    public void CollectCalibrationData() { }
    public void FinishCalibration() { }

    public float GetRawStressScore()
    {
        if (isCalibrating || !detected || threshold <= 0) return 0f;

        float excess = ratio - threshold;
        if (excess <= 0) return 0f;

        float normalizedIntensity = Mathf.Clamp01(excess / expectedMaxExcess);
        return normalizedIntensity * confidence;
    }

    public float GetInstantAddition()
    {
        if (isCalibrating) return 0f;

        if (_instantScoreBuffer > 0f)
        {
            float scoreToReturn = _instantScoreBuffer;
            _instantScoreBuffer = 0f;
            return scoreToReturn;
        }
        return 0f;
    }

    public string GetActiveReason()
    {
        return (detected && threshold > 0 && confidence > 0.1f) ? "미세떨림 감지" : "";
    }
}

public class Biquad
{
    public enum Type { Bandpass, Lowpass }
    float b0, b1, b2, a1, a2;
    float x1, x2, y1, y2;

    public Biquad(float freq, float Q, float fs, Type type)
    {
        float w0 = 2 * Mathf.PI * freq / fs;
        float alpha = Mathf.Sin(w0) / (2 * Q);
        float cos = Mathf.Cos(w0);
        float a0;

        if (type == Type.Bandpass)
        {
            b0 = alpha; b1 = 0; b2 = -alpha;
        }
        else
        {
            b0 = (1 - cos) / 2; b1 = 1 - cos; b2 = (1 - cos) / 2;
        }

        a0 = 1 + alpha; a1 = -2 * cos; a2 = 1 - alpha;
        b0 /= a0; b1 /= a0; b2 /= a0;
        a1 /= a0; a2 /= a0;
    }

    public float Process(float x)
    {
        float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
        x2 = x1; x1 = x; y2 = y1; y1 = y;
        return y;
    }
}

public class EMA
{
    float a;
    float y;
    public EMA(float alpha) { a = Mathf.Clamp01(alpha); }
    public float Process(float x)
    {
        y = a * x + (1 - a) * y;
        return y;
    }
}

// 동적 데이터 평균 및 분산 누적 산출 클래스
public class AdaptiveStat
{
    private float mean = 0f;
    private float variance = 0f;
    private int count = 0;

    public void Update(float x)
    {
        count++;
        float delta = x - mean;
        mean += delta / count;
        float delta2 = x - mean;
        variance += delta * delta2;
    }

    public void Reset()
    {
        mean = 0f;
        variance = 0f;
        count = 0;
    }

    public float Mean => mean;
    public float Std => count > 1 ? Mathf.Sqrt(variance / (count - 1)) : 0f;
}