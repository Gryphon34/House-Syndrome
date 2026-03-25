using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerFadeOut : MonoBehaviour
{
    [Header("Trigger Matching")]
    [Tooltip("기본적으로 이 태그를 가진 오브젝트(플레이어)가 트리거에 들어오면 fadeout 합니다.")]
    public string playerTag = "Player";

    [Tooltip("walking 플레이어 루트(선택). 설정하면 이 루트의 자식 콜라이더로도 판정합니다.")]
    public GameObject walkingPlayer;

    [Header("Fade Settings")]
    [Tooltip("트리거 진입 시 fadeout 걸리는 시간(초)")]
    public float fadeDuration = 0.5f;

    [Tooltip("fadeout이 끝나면 오브젝트를 비활성화합니다.")]
    public bool disableAfterFade = true;

    [Tooltip("fadeout 후 Collier를 비활성화합니다. (비활성화/파괴 중복 방지용)")]
    public bool disableColliderAfterFade = true;

    [Tooltip("URP Lit 등에서 알파 페이드가 보이도록, 가능한 경우 재질을 Transparent 모드로 바꿉니다.")]
    public bool forceTransparentMaterials = true;

    [Tooltip("이미 한 번 fadeout이 실행되면 다시 실행하지 않습니다.")]
    public bool oneShot = true;

    private bool _hasFaded;
    private Coroutine _fadeRoutine;

    private Renderer[] _renderers;
    private readonly List<MaterialAlphaInfo> _materials = new List<MaterialAlphaInfo>();

    [Serializable]
    private class MaterialAlphaInfo
    {
        public Material material;
        public bool usesBaseColor;
        public Color rgbWithOriginalAlpha;
    }

    void Awake()
    {
        if (walkingPlayer == null && !string.IsNullOrEmpty(playerTag))
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) walkingPlayer = p;
        }

        _renderers = GetComponentsInChildren<Renderer>(true);
        CacheMaterialAlphaInfos();
    }

    private void CacheMaterialAlphaInfos()
    {
        _materials.Clear();

        if (_renderers == null || _renderers.Length == 0) return;

        var uniqueMaterials = new HashSet<Material>();
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            // 주의: renderer.materials 접근은 material instance를 만들 수 있습니다(오브젝트 단위로 fade 하려는 의도에는 적합).
            var mats = _renderers[i].materials;
            if (mats == null) continue;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;
                uniqueMaterials.Add(mat);
            }
        }

        foreach (var mat in uniqueMaterials)
        {
            bool usesBaseColor = mat.HasProperty("_BaseColor");
            bool usesColor = mat.HasProperty("_Color");

            // 어떤 셰이더든 최소 material.color는 존재한다고 가정(멱등).
            Color c = usesBaseColor ? mat.GetColor("_BaseColor") : (usesColor ? mat.GetColor("_Color") : mat.color);

            _materials.Add(new MaterialAlphaInfo
            {
                material = mat,
                usesBaseColor = usesBaseColor,
                rgbWithOriginalAlpha = c
            });
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (oneShot && _hasFaded) return;
        if (!IsTargetPlayer(other)) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    private bool IsTargetPlayer(Collider other)
    {
        if (other == null) return false;

        var t = other.transform;
        if (walkingPlayer != null && t != null && t.IsChildOf(walkingPlayer.transform)) return true;

        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
        return false;
    }

    IEnumerator FadeOutRoutine()
    {
        _hasFaded = true;

        if (forceTransparentMaterials)
            ApplyTransparentModeIfSupported();

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, fadeDuration <= 0f ? 1f : timer / fadeDuration);
            ApplyAlpha(a);
            yield return null;
        }

        ApplyAlpha(0f);

        if (disableColliderAfterFade)
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        if (disableAfterFade)
            gameObject.SetActive(false);
    }

    private void ApplyTransparentModeIfSupported()
    {
        for (int i = 0; i < _materials.Count; i++)
        {
            var mat = _materials[i].material;
            if (mat == null) continue;

            // URP Lit: _Surface = 0 Opaque, 1 Transparent
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);

            // 흔히 같이 꺼주는 값들(프로퍼티 존재 시에만 적용)
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_AlphaToMask")) mat.SetFloat("_AlphaToMask", 0f);
        }
    }

    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < _materials.Count; i++)
        {
            var info = _materials[i];
            if (info == null || info.material == null) continue;

            if (info.material.HasProperty("_BaseColor"))
            {
                var baseC = info.material.GetColor("_BaseColor");
                info.material.SetColor("_BaseColor", new Color(baseC.r, baseC.g, baseC.b, alpha));
            }
            else if (info.material.HasProperty("_Color"))
            {
                var c = info.material.GetColor("_Color");
                info.material.SetColor("_Color", new Color(c.r, c.g, c.b, alpha));
            }
            else
            {
                // 폴백: color alpha를 건드려보되, 실제로 동작할지 여부는 셰이더에 달립니다.
                var c = info.material.color;
                info.material.color = new Color(c.r, c.g, c.b, alpha);
            }
        }
    }
}

