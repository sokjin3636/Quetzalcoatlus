using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;

public class PhoneEmbeddingExtractor : MonoBehaviour
{
    [Header("Model")]
    public ModelAsset modelAsset;

    [Header("Input")]
    public RenderTexture phoneRenderTexture;

    [Header("Settings")]
    public BackendType backendType = BackendType.GPUCompute;
    public int inputWidth = 224;
    public int inputHeight = 224;

    [Header("Inference Timing")]
    public bool enableAutoInference = true;
    [Min(0.05f)] public float inferenceInterval = 0.3f;

    [Header("Links")]
    public VectorDatabaseMatcher matcher;
    public NavigationGraphLoader graphLoader;

    [Header("Local Tracking")]
    public bool useLocalTracking = true;
    public int localHopCount = 2;
    public float localMinSimilarity = 0.75f;
    public float globalRelocalizeGap = 0.10f;

    [Header("Debug")]
    public bool logResult = true;
    public bool logCandidateInfo = false;

    private Model runtimeModel;
    private Worker worker;
    private float timer;
    private bool running;

    public float[] lastEmbedding;
    public VectorDatabaseMatcher.MatchResult lastMatch;

    [SerializeField]
    private string currentNodeId = "";

    public string CurrentNodeId => currentNodeId;
    public string CurrentFace => lastMatch != null ? lastMatch.face : "";
    public float CurrentSimilarity => lastMatch != null ? lastMatch.similarity : 0f;
    public bool IsRunningInference => running;

    void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("modelAsset이 비어 있습니다.");
            return;
        }

        // Unity Sentis 추론 엔진 모델 로드 및 워커 초기화
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, backendType);
    }

    void Update()
    {
        if (!enableAutoInference) return;
        if (worker == null || phoneRenderTexture == null || matcher == null || running) return;

        timer += Time.deltaTime;

        if (timer >= inferenceInterval)
        {
            timer = 0f;
            _ = RunInference();
        }
    }

    // 비동기 텐서 연산을 통한 임베딩 추출 로직
    [ContextMenu("Run Inference Now")]
    public async Awaitable RunInference()
    {
        if (running) return;

        running = true;

        try
        {
            using Tensor<float> inputTensor = TextureConverter.ToTensor(
                phoneRenderTexture,
                new TextureTransform()
                    .SetDimensions(inputWidth, inputHeight, 3)
                    .SetTensorLayout(TensorLayout.NCHW)
            );

            worker.Schedule(inputTensor);

            Tensor<float> output = worker.PeekOutput() as Tensor<float>;

            if (output == null) return;

            using Tensor<float> cpuOutput = await output.ReadbackAndCloneAsync();

            int len = cpuOutput.shape.length;
            lastEmbedding = new float[len];

            for (int i = 0; i < len; i++)
            {
                lastEmbedding[i] = cpuOutput[i];
            }

            VectorDatabaseMatcher.MatchResult result = EstimateCurrentLocation(lastEmbedding);

            if (result != null)
            {
                lastMatch = result;
                currentNodeId = result.id;
            }
        }
        finally
        {
            running = false;
        }
    }

    // 추출된 벡터를 기반으로 DB 비교를 통한 현재 위치 추정
    private VectorDatabaseMatcher.MatchResult EstimateCurrentLocation(float[] queryVector)
    {
        if (!useLocalTracking)
        {
            return matcher.FindBestMatch(queryVector);
        }

        VectorDatabaseMatcher.MatchResult localMatch = null;
        VectorDatabaseMatcher.MatchResult globalMatch = null;

        bool canUseLocal =
            !string.IsNullOrEmpty(currentNodeId) &&
            graphLoader != null &&
            graphLoader.HasNode(currentNodeId);

        if (canUseLocal)
        {
            HashSet<string> candidateNodes = graphLoader.GetNodesWithinHops(currentNodeId, localHopCount);
            localMatch = matcher.FindBestMatchInNodeSet(queryVector, candidateNodes);
        }
        else
        {
            globalMatch = matcher.FindBestMatch(queryVector);
        }

        // 로컬 매치 결과의 신뢰도 저하 시 글로벌 매치 폴백(Fallback) 적용
        if (localMatch != null)
        {
            VectorDatabaseMatcher.MatchResult finalResult = localMatch;

            if (localMatch.similarity < localMinSimilarity)
            {
                globalMatch = matcher.FindBestMatch(queryVector);

                if (globalMatch != null &&
                    globalMatch.similarity > localMatch.similarity + globalRelocalizeGap)
                {
                    finalResult = globalMatch;
                }
            }

            return finalResult;
        }

        if (globalMatch == null)
        {
            globalMatch = matcher.FindBestMatch(queryVector);
        }

        return globalMatch;
    }

    public void SetInferenceInterval(float seconds)
    {
        inferenceInterval = Mathf.Max(0.05f, seconds);
    }

    public void EnableAutoInference() { enableAutoInference = true; }
    public void DisableAutoInference() { enableAutoInference = false; }

    public void ResetTracking()
    {
        currentNodeId = "";
        lastMatch = null;
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}