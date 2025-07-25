using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance;
    private Camera mainCam;

    private PlayerInput playerInput;
    private InputAction Move;
    private InputAction UnitCommand;
    private InputAction Shift;

    [Header("Movement")]
    public Vector2 moveInput { get; private set; }
    
    [Header("UnitCommand")]
    public Rect selectionRect { get; private set; }
    private bool isDragging;
    private bool isRMBPressed;
    private Vector2 selectionStartpos, currentMousePos;
    [SerializeField] private Image selectionBoxImage;
    [SerializeField] private float dragThreshold = 20f;
    [SerializeField] private LayerMask unitLayer;

    [Header("Alternative Keys")] 
    private bool isShiftPressed;
    
    private void Awake()
    {
        Instance = this;
        playerInput = new PlayerInput();
        Move = playerInput.Player.Move;
        UnitCommand = playerInput.Player.UnitCommand;
        Shift = playerInput.Player.Shift;
        mainCam = Camera.main;
    }

    void OnEnable()
    {
        playerInput.Enable();
        Move.performed += OnMovePerformed;
        Move.canceled += OnMoveCanceled;
        UnitCommand.performed += OnUnitCommandPerformed;
        UnitCommand.canceled += OnUnitCommandCanceled;
        Shift.performed += OnShiftPerformed;
        Shift.canceled += OnShiftCanceled;
    }

    void OnDisable()
    {
        playerInput.Disable();
        Move.performed -= OnMovePerformed;
        Move.canceled -= OnMoveCanceled;
        UnitCommand.performed -= OnUnitCommandPerformed;
        UnitCommand.canceled -= OnUnitCommandCanceled;
        Shift.performed -= OnShiftPerformed;
        Shift.canceled -= OnShiftCanceled;
    }

    void Update()
    {
        if (isRMBPressed)
        {
            currentMousePos = Mouse.current.position.ReadValue();
            float dragDistance = Vector2.Distance(selectionStartpos, currentMousePos);

            if (!isDragging && dragDistance > dragThreshold)
            {
                isDragging = true;
            }
        }
        
        if (isDragging)
        {
            if (!selectionBoxImage.enabled)
            {
                selectionBoxImage.enabled = true;
            }
            
            Vector2 size = currentMousePos - selectionStartpos;

            selectionRect  = new Rect(selectionStartpos, size);

            selectionBoxImage.rectTransform.position = selectionRect.center;
            selectionBoxImage.rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }
    
    private void OnUnitCommandPerformed(InputAction.CallbackContext context)
    {
        isRMBPressed = true;
        isDragging = false;
        selectionStartpos = Mouse.current.position.ReadValue();
    }    
    
    private void OnUnitCommandCanceled(InputAction.CallbackContext context)
    {
        if (isDragging)
        {
            UnitCommandManager.Instance.SelectUnitsInArea(selectionRect);
        }
        else
        {
            if (!isPlayerHit())
            {
                UnitCommandManager.Instance.RequestUnitMove(GetPosition());
            }
        }

        selectionBoxImage.enabled = false;
        isRMBPressed = false;
        isDragging = false;
    }

    private void OnShiftPerformed(InputAction.CallbackContext context)
    {
        isShiftPressed = true;
    }

    private void OnShiftCanceled(InputAction.CallbackContext context)
    {
        isShiftPressed = false;
    }

    private bool isPlayerHit()
    {
        Ray mouseCameraRay = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseCameraRay, out RaycastHit hit, Mathf.Infinity, unitLayer))
        {
            if (!isShiftPressed)
            {
                UnitCommandManager.Instance.DeselectAllUnits();
            }

            Unit unit = hit.collider.GetComponent<Unit>();
            if (unit != null)
            {
                UnitDataManager.Instance.isSelected[unit.dataIndex] = true;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    private Vector3 GetPosition()
    {
        Ray mouseCameraRay = mainCam.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(mouseCameraRay, out float distance))
        {
            return mouseCameraRay.GetPoint(distance);
        }
        else
        {
            return Vector3.zero;
        }
    }
}
