using UnityEngine;

public class PillarColorController : MonoBehaviour
{
    // 기둥 좌/우 개별 색상 설정
    public Color leftColor = Color.white;
    public Color rightColor = Color.gray;

    void Start()
    {
        ApplyColor();
    }

    // 에디터 상 실시간 색상 변경 적용 (OnValidate)
    void OnValidate()
    {
        ApplyColor();
    }

    void ApplyColor()
    {
        Renderer rd = GetComponent<Renderer>();
        MaterialPropertyBlock props = new MaterialPropertyBlock();

        // 셰이더 그래프 프로퍼티를 통한 색상 파라미터 주입
        props.SetColor("_Color1", leftColor);
        props.SetColor("_Color2", rightColor);

        rd.SetPropertyBlock(props);
    }
}