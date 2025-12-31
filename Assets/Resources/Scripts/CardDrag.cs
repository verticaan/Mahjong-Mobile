using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Card Movement")]
    public float maxDragDistance = 200f;

    private RectTransform rectTransform;
    private Canvas canvas;

    private Vector2 defaultPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        defaultPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        defaultPosition = rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaY = eventData.delta.y / canvas.scaleFactor;
        float targetY = rectTransform.anchoredPosition.y + deltaY;
        
        targetY = Mathf.Clamp(targetY, defaultPosition.y,defaultPosition.y + maxDragDistance);

        rectTransform.anchoredPosition = new Vector2(defaultPosition.x, targetY);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition = defaultPosition;
    }
}
