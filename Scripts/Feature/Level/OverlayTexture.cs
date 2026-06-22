using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class OverlayTexture : MonoBehaviour
{
    [Header("--- [RANDOM TRIGGER (랜덤 적용)] ---")]
    public bool randomizeNow = false;

    [Header("--- [BASE LAYER (바탕 텍스처 리스트)] ---")]
    public List<Material> baseMaterials = new List<Material>();
    public List<Texture2D> baseTextures = new List<Texture2D>();
    public Color baseColor = Color.white;
    public Vector2 baseTiling = Vector2.one;
    [Range(0, 1)] public float baseMetallic = 0f;
    [Range(0, 1)] public float baseSmoothness = 0.5f;

    [Header("--- [OVERLAY LAYER (오버레이 텍스처 리스트)] ---")]
    public List<Material> overlayMaterials = new List<Material>();
    public List<Texture2D> overlayTextures = new List<Texture2D>();
    public Color overlayColor = Color.white;
    public Vector2 overlayTiling = Vector2.one;
    [Range(0, 1)] public float overlayMetallic = 0f;
    [Range(0, 1)] public float overlaySmoothness = 0.8f;

    [Header("--- [OVERLAY INTENSITY (투명도)] ---")]
    [Range(0, 1)] public float myOpacity = 1f;

    [SerializeField, HideInInspector] private int activeBaseMatIndex = -1;
    [SerializeField, HideInInspector] private int activeBaseTexIndex = -1;
    [SerializeField, HideInInspector] private int activeOverlayMatIndex = -1;
    [SerializeField, HideInInspector] private int activeOverlayTexIndex = -1;

    private void OnValidate()
    {
        if (randomizeNow)
        {
            RollRandom();
            randomizeNow = false;
        }
        ApplyTextures();
    }

    private void Start() { ApplyTextures(); }

    public void RollRandom()
    {
        if (baseMaterials != null && baseMaterials.Count > 0) activeBaseMatIndex = Random.Range(0, baseMaterials.Count);
        if (baseTextures != null && baseTextures.Count > 0) activeBaseTexIndex = Random.Range(0, baseTextures.Count);

        if (overlayMaterials != null && overlayMaterials.Count > 0) activeOverlayMatIndex = Random.Range(0, overlayMaterials.Count);
        if (overlayTextures != null && overlayTextures.Count > 0) activeOverlayTexIndex = Random.Range(0, overlayTextures.Count);
    }

    private void ApplyTextures()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);

        // ==========================================
        // 1. BASE LAYER 처리
        // ==========================================
        Texture baseTex = null;
        Color bColor = baseColor;

        if (activeBaseMatIndex >= 0 && activeBaseMatIndex < baseMaterials.Count)
        {
            Material bMat = baseMaterials[activeBaseMatIndex];
            if (bMat != null)
            {
                if (bMat.HasProperty("_BaseMap")) baseTex = bMat.GetTexture("_BaseMap");
                else if (bMat.HasProperty("_MainTex")) baseTex = bMat.GetTexture("_MainTex");

                if (bMat.HasProperty("_BaseColor")) bColor = bMat.GetColor("_BaseColor");
                else if (bMat.HasProperty("_Color")) bColor = bMat.GetColor("_Color");
            }
        }

        if (baseTex == null && activeBaseTexIndex >= 0 && activeBaseTexIndex < baseTextures.Count)
        {
            baseTex = baseTextures[activeBaseTexIndex];
        }

        if (baseTex != null) propBlock.SetTexture("_BaseTex", baseTex);
        propBlock.SetColor("_BaseColor", bColor);
        propBlock.SetVector("_BaseTiling", baseTiling);

        // ==========================================
        // 2. OVERLAY LAYER 처리
        // ==========================================
        Texture overlayTex = null;
        Color oColor = overlayColor;

        if (activeOverlayMatIndex >= 0 && activeOverlayMatIndex < overlayMaterials.Count)
        {
            Material oMat = overlayMaterials[activeOverlayMatIndex];
            if (oMat != null)
            {
                if (oMat.HasProperty("_BaseMap")) overlayTex = oMat.GetTexture("_BaseMap");
                else if (oMat.HasProperty("_MainTex")) overlayTex = oMat.GetTexture("_MainTex");

                if (oMat.HasProperty("_BaseColor")) oColor = oMat.GetColor("_BaseColor");
                else if (oMat.HasProperty("_Color")) oColor = oMat.GetColor("_Color");
            }
        }

        if (overlayTex == null && activeOverlayTexIndex >= 0 && activeOverlayTexIndex < overlayTextures.Count)
        {
            overlayTex = overlayTextures[activeOverlayTexIndex];
        }

        if (overlayTex != null) propBlock.SetTexture("_OverlayTex", overlayTex);
        propBlock.SetColor("_OverlayColor", oColor);
        propBlock.SetVector("_OverlayTiling", overlayTiling);

        // ==========================================
        // 3. 머티리얼 질감 및 프로퍼티 제어
        // ==========================================
        propBlock.SetFloat("_BaseMetallic", baseMetallic);
        propBlock.SetFloat("_BaseSmoothness", baseSmoothness);
        propBlock.SetFloat("_OverlayMetallic", overlayMetallic);
        propBlock.SetFloat("_OverlaySmoothness", overlaySmoothness);
        propBlock.SetFloat("_OverlayInten", myOpacity);

        renderer.SetPropertyBlock(propBlock);
    }
}