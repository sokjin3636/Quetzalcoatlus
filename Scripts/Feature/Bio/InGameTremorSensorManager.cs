using UnityEngine;
using UnityEngine.XR;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class InGameTremorSensorManager : MonoBehaviour, ITensionProvider
{
    [Header("Sensor & XR")]
    public float sampleRate = 90f;
    public XRNode tensionHand = XRNode.RightHand;
    public XRNode phoneHand = XRNode.LeftHand;

    [Header("Detection & Tuning")]
    public float ratioThresholdK = 2.5f;
    public float maxMotionEnergy = 20.0f;

    [Header("Calibration Settings")]
    public float calibrationTime = 10f;
    private float _calibrationTimer = 0f;
    public bool isCalibrating = false;

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

    [Header("Phone Integration")]
    public PhonePowerController phoneController;
    [Tooltip("지정된 폰 모션 에너지를 초과 시 전원 강제 종료")]
    public float phoneShakeThreshold = 25.0f;

    private float _instantScoreBuffer = 0f;
    private bool _isSpiking = false;

    // 오른손 텐션 필터 (4차 필터 구성을 위한 2단 중첩)
    Biquad _bp1X, _bp2X, _bp1Y, _bp2Y, _bp1Z, _bp2Z;
    Biquad _lp1X, _lp2X, _lp1Y, _lp2Y, _lp1Z, _lp2Z;

    // 왼손 폰 제어용 필터
    Biquad _phoneLpX, _phoneLpY, _phoneLpZ;
    EMA _emaPhoneMotion = new EMA(0.05f);

    EMA _emaTremor = new EMA(0.05f);
    EMA _emaMotion = new EMA(0.05f);

    AdaptiveStat _ratioStat = new AdaptiveStat();

    void Start()
    {
        // 물리 연산 주기에 맞춰 샘플레이트 동기화
        sampleRate = 1.0f / Time.fixedDeltaTime;

        // 분자(Tremor) 연산용 4차 밴드패스 필터 설정
        float bpQ = 1.4f;
        _bp1X = new Biquad(10f, bpQ, sampleRate, Biquad.Type.Bandpass);
        _bp2X = new Biquad(10f, bpQ, sampleRate, Biquad.Type.Bandpass);
        _bp1Y = new Biquad(10f, bpQ, sampleRate, Biquad.Type.Bandpass);
        _bp2Y = new Biquad(10f, bpQ, sampleRate, Biquad.Type.Bandpass);
        _bp1Z = new Biquad(10f, bpQ, sampleRate, Biquad.Type.Bandpass);
        _bp2Z = new Biquad(10f, bpQ, sampleRate, Biquad.Type.Bandpass);

        // 분모(Motion) 연산용 4차 로우패스 필터 설정
        float lpQ = 0.707f;
        _lp1X = new Biquad(3f, lpQ, sampleRate, Biquad.Type.Lowpass);
        _lp2X = new Biquad(3f, lpQ, sampleRate, Biquad.Type.Lowpass);
        _lp1Y = new Biquad(3f, lpQ, sampleRate, Biquad.Type.Lowpass);
        _lp2Y = new Biquad(3f, lpQ, sampleRate, Biquad.Type.Lowpass);
        _lp1Z = new Biquad(3f, lpQ, sampleRate, Biquad.Type.Lowpass);
        _lp2Z = new Biquad(3f, lpQ, sampleRate, Biquad.Type.Lowpass);

        // 스마트폰 모션 필터 설정
        _phoneLpX = new Biquad(15f, lpQ, sampleRate, Biquad.Type.Lowpass);
        _phoneLpY = new Biquad(15f, lpQ, sampleRate, Biquad.Type.Lowpass);
        _phoneLpZ = new Biquad(15f, lpQ, sampleRate, Biquad.Type.Lowpass);

        LoadWesadData("WESAD_Baseline", _wesadBaseline);
        LoadWesadData("WESAD_Stress", _wesadStress);

        isCalibrating = false;
        threshold = DataManager.BaseTremorEnergy;
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

        if (Input.GetKeyDown(KeyCode.M)) _userMarker = 10f;

        if (useWesadData)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) { _isPlayingStress = false; _playbackIndex = 0; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { _isPlayingStress = true; _playbackIndex = 0; }
        }
    }

    void FixedUpdate()
    {
        Vector3 tensionW = Vector3.zero;
        Vector3 phoneW = Vector3.zero;

        if (useWesadData)
        {
            List<Vector3> currentList = _isPlayingStress ? _wesadStress : _wesadBaseline;
            if (currentList.Count > 0)
            {
                tensionW = currentList[_playbackIndex % currentList.Count] * wesadScale;
                phoneW = tensionW;
                _playbackIndex++;
            }
        }
        else
        {
            if (InputDevices.GetDeviceAtXRNode(tensionHand)
                .TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 w1))
            {
                tensionW = w1;
            }

            if (InputDevices.GetDeviceAtXRNode(phoneHand)
                .TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 w2))
            {
                phoneW = w2;
            }
        }

        ProcessSignal(tensionW, phoneW);
    }

    void ProcessSignal(Vector3 tensionW, Vector3 phoneW)
    {
        // 1. 미세떨림 에너지 산출
        float tx = _bp2X.Process(_bp1X.Process(tensionW.x));
        float ty = _bp2Y.Process(_bp1Y.Process(tensionW.y));
        float tz = _bp2Z.Process(_bp1Z.Process(tensionW.z));

        float tremorInstant = tx * tx + ty * ty + tz * tz;
        tremorEnergy = _emaTremor.Process(tremorInstant);

        // 2. 모션 에너지 산출
        float mx = _lp2X.Process(_lp1X.Process(tensionW.x));
        float my = _lp2Y.Process(_lp1Y.Process(tensionW.y));
        float mz = _lp2Z.Process(_lp1Z.Process(tensionW.z));

        float motionInstant = mx * mx + my * my + mz * mz;
        motionEnergy = _emaMotion.Process(motionInstant);

        confidence = Mathf.Clamp01(1f - motionEnergy / maxMotionEnergy);

        // 3. 스마트폰 모션 제어 기믹
        float px = _phoneLpX.Process(phoneW.x);
        float py = _phoneLpY.Process(phoneW.y);
        float pz = _phoneLpZ.Process(phoneW.z);

        float phoneMotionInstant = px * px + py * py + pz * pz;

        if (phoneMotionInstant > phoneShakeThreshold)
        {
            if (phoneController != null && phoneController.IsPhoneOn && !phoneController.IsPowerLocked)
            {
                phoneController.ForcePowerOffForSeconds(phoneController.defaultPowerLockSeconds);
            }
        }

        // 4. 텐션 비율(Ratio) 연산
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
                threshold = _ratioStat.Mean + ratioThresholdK * _ratioStat.Std;
            }
            detected = false;
        }
        else
        {
            float releaseMargin = 0.7f;

            if (!detected) detected = weightedRatio > threshold;
            else detected = weightedRatio > (threshold * releaseMargin);

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
                else if (ratio < instantSpikeRatio * 0.8f) _isSpiking = false;
            }
            else _isSpiking = false;
        }

        // CSV 로깅
        if (_isRecording)
        {
            float t = Time.fixedTime - _recordStartTime;
            if (t >= recordDuration) StopRecording();
            else
            {
                float currentTremorScore = GetRawStressScore();
                _csvData.AppendLine($"{t:F3},{tensionW.x:F4},{tensionW.y:F4},{tensionW.z:F4},{tremorEnergy:F6},{motionEnergy:F6},{ratio:F4},{confidence:F4},{threshold:F4},{currentTremorScore:F4},{_userMarker:F1},0.0");
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

    public void StartCalibration() { }
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

    public string GetActiveReason() => (detected && threshold > 0 && confidence > 0.1f) ? "미세떨림 감지" : "";
}