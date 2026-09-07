using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MixVerse
{
    public class ImageTouchHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private RectTransform _rectTransform;

        private Subject<Vector2> _onTouchPositionSubject = new Subject<Vector2>();
        public Observable<Vector2> OnTouchPositionAsObservable => _onTouchPositionSubject.AsObservable();

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ProcessTouch(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ProcessTouch(eventData);
        }

        private void ProcessTouch(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
            {
                // Rect の左下が (0,0)、右上が (Width, Height) なので、大きさで割れば UV になる
                float uvX = (localPoint.x - _rectTransform.rect.xMin) / _rectTransform.rect.width;
                float uvY = (localPoint.y - _rectTransform.rect.yMin) / _rectTransform.rect.height;

                Vector2 touchUV = new Vector2(uvX, uvY);
                _onTouchPositionSubject.OnNext(touchUV);
            }
        }
    }
}
