using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns a quick burst of colored particles at a canvas position.
/// Used by GameJuice for bloom, harvest, and order-fill celebrations.
/// Self-creates a dedicated overlay canvas (sort order 90) on first use.
/// </summary>
public class UIBurst : MonoBehaviour
{
    static UIBurst instance;

    Canvas burstCanvas;
    RectTransform burstRoot;

    // --- Public API ---

    /// <summary>
    /// Emit a radial burst of particles at the given RectTransform's screen position.
    /// </summary>
    public static void Emit(RectTransform source, Color color, int count = 8)
    {
        if (source == null) return;
        EnsureInstance();
        instance.StartCoroutine(instance.DoBurst(source, color, count));
    }

    // --- Setup ---

    static void EnsureInstance()
    {
        if (instance != null) return;

        var go = new GameObject("UIBurst");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UIBurst>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        instance.burstCanvas = canvas;
        instance.burstRoot = go.GetComponent<RectTransform>();
    }

    // --- Burst coroutine ---

    IEnumerator DoBurst(RectTransform source, Color color, int count)
    {
        // Convert source world position to burst canvas local position
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, source.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            burstRoot, screenPos, null, out Vector2 localPos);

        // Spawn particle images
        var rts    = new RectTransform[count];
        var imgs   = new Image[count];
        var dirs   = new Vector2[count];
        var speeds = new float[count];

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + Random.Range(-15f, 15f);
            float rad = angle * Mathf.Deg2Rad;
            dirs[i]   = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            speeds[i] = Random.Range(90f, 170f);

            var pGo = new GameObject("P");
            var rt = pGo.AddComponent<RectTransform>();
            rt.SetParent(burstRoot, false);
            rt.sizeDelta = new Vector2(9f, 9f);
            rt.anchoredPosition = localPos;

            var img = pGo.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            rts[i]  = rt;
            imgs[i] = img;
        }

        const float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / duration;
            float alpha = 1f - t;
            float scale = Mathf.Lerp(1f, 0.25f, t);

            for (int i = 0; i < count; i++)
            {
                if (rts[i] == null) continue;
                rts[i].anchoredPosition += dirs[i] * speeds[i] * Time.deltaTime;
                rts[i].localScale = Vector3.one * scale;
                var c = imgs[i].color;
                c.a = alpha;
                imgs[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
            if (rts[i] != null) Destroy(rts[i].gameObject);
    }
}
