using UnityEngine;
using UnityEngine.UI;

public class CentreScaleScroller : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float maxScale = 1.2f;
    public float minScale = 0.8f;
    public float scaleDistance = 300f;

    RectTransform content;
    RectTransform viewport;

    void Start()
    {
        content = scrollRect.content;
        viewport = scrollRect.viewport;
    }

    void Update()
    {
        foreach (RectTransform item in content)
        {
            // Calculate the vertical distance between the item's centre and the viewport's centre (in world space)
            float distance = Mathf.Abs(viewport.TransformPoint(viewport.rect.center).y - item.TransformPoint(item.rect.center).y);

            // clamp distance between 0 and 1 based on scaleDistance
            float t = Mathf.Clamp01(distance / scaleDistance);
            float scale = Mathf.Lerp(maxScale, minScale, t);

            item.localScale = Vector3.one * scale;
        }
    }
}
