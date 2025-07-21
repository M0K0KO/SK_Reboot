using Unity.Mathematics;
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

    public Vector2 moveInput { get; private set; }
    public bool unitCommandPressed { get; private set; }
    public Rect selectionRect { get; private set; }
    private Vector2 selectionStartpos, selectionEndpos;
    private bool isDragSelectionActive;
    [SerializeField] private Image selectionBoxImage; 

    private void Awake()
    {
        Instance = this;
        playerInput = new PlayerInput();
        Move = playerInput.Player.Move;
        UnitCommand = playerInput.Player.UnitCommand;
        mainCam = Camera.main;
    }

    void OnEnable()
    {
        playerInput.Enable();
        Move.performed += OnMove;
        UnitCommand.started += OnUnitCommandStarted;
        UnitCommand.performed += OnUnitCommandPerformed;
        UnitCommand.canceled += OnUnitCommandCanceled;
    }

    void OnDisable()
    {
        playerInput.Disable();
        Move.performed -= OnMove;
        UnitCommand.started -= OnUnitCommandStarted;
        UnitCommand.performed -= OnUnitCommandPerformed;
        UnitCommand.canceled -= OnUnitCommandCanceled;

    }

    void Update()
    {
        if (isDragSelectionActive)
        {
            if (!selectionBoxImage.enabled)
            {
                selectionBoxImage.enabled = true;
            }
            
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 size = currentMousePos - selectionStartpos;

            selectionRect  = new Rect(selectionStartpos, size);

            selectionBoxImage.rectTransform.position = selectionRect.center;
            selectionBoxImage.rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    
    
    private void OnUnitCommandStarted(InputAction.CallbackContext context)
    {
        selectionStartpos = Mouse.current.position.ReadValue();
    }    
    
    private void OnUnitCommandPerformed(InputAction.CallbackContext context)
    {
        if (context.interaction is UnityEngine.InputSystem.Interactions.TapInteraction)
        {
            float3 targetPos = GetPosition();
            UnitCommandManager.Instance.MoveUnits(targetPos);

            isDragSelectionActive = false;
            selectionBoxImage.enabled = false;
        }
        else if (context.interaction is UnityEngine.InputSystem.Interactions.HoldInteraction)
        {
            isDragSelectionActive = true;
        }
    }    
    
    private void OnUnitCommandCanceled(InputAction.CallbackContext context)
    {
        if (isDragSelectionActive)
        {
            UnitCommandManager.Instance.SelectUnitsInArea(selectionRect);
            
            isDragSelectionActive = false;
            selectionBoxImage.enabled = false;
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
