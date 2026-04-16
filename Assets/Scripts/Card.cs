using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer cardSprite;
    [SerializeField] private SpriteRenderer highlightBorder;
    [SerializeField] private int normalSortingOrder = 0;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float maxRotationAngle = 45f;

    [Header("Movement Settings")]
    [SerializeField] private float dragSpeed = 1f;
    [SerializeField] private bool snapToGrid = false;
    [SerializeField] private float gridSize = 0.1f;

    private bool isDragging = false;
    private bool isHighlighted = false;
    private Vector3 offset;
    private int originalSortingOrder;
    private float originalZRotation;

    private void Start()
    {
        // Инициализация компонентов
        if (highlightBorder != null)
        {
            highlightBorder.gameObject.SetActive(false);
        }
        cardSprite = GetComponent<SpriteRenderer>();
        originalSortingOrder = cardSprite.sortingOrder;
        originalZRotation = transform.rotation.eulerAngles.z;
    }

    // Обработка наведения курсора
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer Enter");
        isHighlighted = true;
        if (highlightBorder != null)
        {
            highlightBorder.gameObject.SetActive(true);
        }
    }

    // Обработка ухода курсора
    public void OnPointerExit(PointerEventData eventData)
    {
        isHighlighted = false;
        if (highlightBorder != null && !isDragging)
        {
            highlightBorder.gameObject.SetActive(false);
        }
    }

    // Обработка нажатия на карту
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = true;

            // Вычисляем смещение для плавного перемещения
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            offset = transform.position - mouseWorldPos;
        }
    }

    // Обработка отпускания кнопки мыши
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = false;
            cardSprite.sortingOrder = originalSortingOrder;

            if (!isHighlighted && highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(false);
            }
        }
    }

    // Обработка перетаскивания
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && eventData.button == PointerEventData.InputButton.Left)
        {
            Vector3 newPosition = GetMouseWorldPosition() + offset;

            if (snapToGrid)
            {
                newPosition.x = Mathf.Round(newPosition.x / gridSize) * gridSize;
                newPosition.y = Mathf.Round(newPosition.y / gridSize) * gridSize;
            }

            transform.position = Vector3.Lerp(transform.position, newPosition, dragSpeed * Time.deltaTime);
        }
    }

    // Обработка прокрутки колесика мыши
    public void OnScroll(PointerEventData eventData)
    {
        if (!isHighlighted) return;

        float scrollDelta = eventData.scrollDelta.y;

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            // Изменение порядка в слое при удержании Ctrl
            cardSprite.sortingOrder += (int)Mathf.Sign(scrollDelta);
            originalSortingOrder = cardSprite.sortingOrder;
        }
        else
        {
            // Изменение вращения по Z
            float currentRotation = transform.rotation.eulerAngles.z;
            float newRotation = currentRotation + scrollDelta * rotationSpeed;

            // Ограничиваем угол вращения
            if (newRotation > 180f) newRotation -= 360f;
            newRotation = Mathf.Clamp(newRotation, -maxRotationAngle, maxRotationAngle);

            transform.rotation = Quaternion.Euler(0f, 0f, newRotation);
        }
    }

    // Вспомогательный метод для получения позиции мыши в мировых координатах
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    // Публичные методы для управления подсветкой
    public void EnableHighlight()
    {
        if (highlightBorder != null)
        {
            highlightBorder.gameObject.SetActive(true);
            isHighlighted = true;
        }
    }

    public void DisableHighlight()
    {
        if (highlightBorder != null)
        {
            highlightBorder.gameObject.SetActive(false);
            isHighlighted = false;
        }
    }

    // Метод для сброса вращения
    public void ResetRotation()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, originalZRotation);
    }

    // Метод для сброса порядка в слое
    public void ResetSortingOrder()
    {
        cardSprite.sortingOrder = normalSortingOrder;
        originalSortingOrder = normalSortingOrder;
    }
}