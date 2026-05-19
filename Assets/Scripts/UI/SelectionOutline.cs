using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectionOutline : MonoBehaviour
{
    [Tooltip("Image filha que serve como borda de seleção")]
    [SerializeField] private Image selectionBorder;

    private void Awake()
    {
        if (selectionBorder != null)
            selectionBorder.enabled = false;
    }

    private void Update()
    {
        if (EventSystem.current == null || selectionBorder == null) return;
        bool isSelected = EventSystem.current.currentSelectedGameObject == gameObject;
        selectionBorder.enabled = isSelected;
    }
}