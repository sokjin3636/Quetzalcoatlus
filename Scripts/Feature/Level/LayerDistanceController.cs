using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class LayerDistanceController : MonoBehaviour
{
    // 레이어별 컬링 거리 설정 구조체
    [System.Serializable]
    public struct LayerDistanceSettings
    {
        [Tooltip("컬링 거리를 적용할 타겟 레이어")]
        public LayerMask targetLayer;

        [Tooltip("레이어 렌더링 한계 거리(미터)")]
        public float renderDistance;
    }

    [Header("--- 일반 기본 설정 ---")]
    [Tooltip("별도로 지정되지 않은 레이어의 기본 렌더링 거리")]
    public float defaultLayerDistance = 15f;

    [Header("--- 레이어별 개별 설정 리스트 ---")]
    public List<LayerDistanceSettings> customLayerSettings = new List<LayerDistanceSettings>();

    void Update()
    {
        Camera camera = GetComponent<Camera>();
        if (camera == null) return;

        float[] distances = new float[32];

        // 전체 레이어 기본 렌더링 거리 초기화
        float maxDistanceNeeded = defaultLayerDistance;
        for (int i = 0; i < 32; i++)
        {
            distances[i] = defaultLayerDistance;
        }

        // 사용자 지정 레이어별 개별 렌더링 거리 오버라이드
        if (customLayerSettings != null)
        {
            foreach (var setting in customLayerSettings)
            {
                int layerIndex = GetLayerIndexFromMask(setting.targetLayer);

                if (layerIndex >= 0 && layerIndex < 32)
                {
                    distances[layerIndex] = setting.renderDistance;

                    if (setting.renderDistance > maxDistanceNeeded)
                    {
                        maxDistanceNeeded = setting.renderDistance;
                    }
                }
            }
        }

        // 카메라 컬링 파라미터 최종 적용
        camera.farClipPlane = maxDistanceNeeded;
        camera.layerCullDistances = distances;
    }

    // LayerMask에서 단일 레이어 인덱스 추출
    private int GetLayerIndexFromMask(LayerMask mask)
    {
        int bitmask = mask.value;
        if (bitmask == 0) return -1;

        for (int i = 0; i < 32; i++)
        {
            if (((bitmask >> i) & 1) == 1)
            {
                return i;
            }
        }
        return -1;
    }
}