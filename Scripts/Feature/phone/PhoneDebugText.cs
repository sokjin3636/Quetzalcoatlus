using TMPro;
using UnityEngine;

public class PhoneDebugText : MonoBehaviour
{
    public PhoneEmbeddingExtractor extractor;
    public SupportToRepresentativeMapper mapper;

    public TMP_Text debugText;

    void Update()
    {
        if (extractor == null || debugText == null) return;

        string supportNode = extractor.CurrentNodeId;
        string representativeNode = "";

        if (mapper != null && !string.IsNullOrEmpty(supportNode))
        {
            representativeNode = mapper.GetRepresentativeNode(supportNode);
        }

        string face = extractor.CurrentFace;
        float sim = extractor.CurrentSimilarity;

        // 현재 위치 및 인퍼런스 상태 UI 업데이트
        debugText.text =
            $"current location\n" +
            $"Support Node: {supportNode}\n" +
            $"Repre Node: {representativeNode}\n" +
            $"Face: {face}\n" +
            $"Similarity: {sim:F4}";
    }
}