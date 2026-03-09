using System.Collections;
using UnityEngine;

/// <summary>
/// Adds a fade-in + subtle scale-up animation whenever the panel is opened.
/// Add this component to any panel root in Awake() — it self-configures.
/// Close/SetActive(false) remains instant; only the open is animated.
/// </summary>
public class PanelTransition : MonoBehaviour
{
    [SerializeField] float fadeDuration = 0.18f;
    [SerializeField] float scaleFrom = 0.95f;

    CanvasGroup cg;
    Vector3 restingScale;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        restingScale = transform.localScale;
    }

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        cg.alpha = 0f;
        transform.localScale = restingScale * scaleFrom;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float ease = 1f - (1f - t) * (1f - t); // ease-out quad
            cg.alpha = ease;
            transform.localScale = restingScale * Mathf.Lerp(scaleFrom, 1f, ease);
            yield return null;
        }

        cg.alpha = 1f;
        transform.localScale = restingScale;
    }
}
