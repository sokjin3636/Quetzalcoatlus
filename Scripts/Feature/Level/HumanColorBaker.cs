using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HumanColorBaker : MonoBehaviour
{
    [Header("색을 랜덤하게 바꿀 자식 오브젝트들")]
    [Tooltip("피부, 상의, 바지 등의 Renderer 컴포넌트 리스트")]
    public List<Renderer> targetRenderers;

    [ContextMenu("굽기")]
    public void BakeColors()
    {
        if (targetRenderers == null || targetRenderers.Count == 0)
        {
            Debug.LogWarning("[HumanColorBaker] 리스트가 비어있습니다! 자식 오브젝트의 Renderer를 넣어주세요.");
            return;
        }

#if UNITY_EDITOR
        // Undo 시스템 등록
        Undo.RecordObjects(targetRenderers.ToArray(), "Bake Human Colors");
#endif

        foreach (var renderer in targetRenderers)
        {
            if (renderer == null || renderer.sharedMaterial == null) continue;

            Material currentMat = renderer.sharedMaterial;

            // 원본 머티리얼 오염 방지를 위한 인스턴스 복사 (기존 구워진 머티리얼은 재사용)
            if (!currentMat.name.StartsWith("Baked_"))
            {
                Material uniqueMat = new Material(currentMat);
                uniqueMat.name = "Baked_" + currentMat.name;
                renderer.sharedMaterial = uniqueMat;
                currentMat = uniqueMat;
            }

            // HSV 기반 무작위 색상 생성 (명도 및 채도 제한 적용)
            Color randomColor = Random.ColorHSV(0f, 1f, 0.4f, 1f, 0.5f, 1f);

            // URP 및 Built-in 셰이더 프로퍼티 대응 색상 변경
            if (currentMat.HasProperty("_BaseColor"))
            {
                currentMat.SetColor("_BaseColor", randomColor);
            }
            else if (currentMat.HasProperty("_Color"))
            {
                currentMat.SetColor("_Color", randomColor);
            }

#if UNITY_EDITOR
            // 씬 저장 누락 방지를 위한 Dirty 플래그 설정
            EditorUtility.SetDirty(renderer);
#endif
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
        Debug.Log("<color=green>[HumanColorBaker] 색상 굽기 완료!</color>");
#endif
    }
}