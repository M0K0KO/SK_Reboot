using UnityEditor;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    private Unit unit;
    private Animator animator;
    
    private UnitState previousState;

    [Range(0f, 0.5f)]
    [SerializeField] private float fixedTransitionDuration = 0.1f;

    void Awake()
    {
        unit = GetComponent<Unit>();
        animator = GetComponent<Animator>();
    }

    void LateUpdate()
    {
        if (unit.dataIndex < 0) return;

        UnitState currentState = UnitDataManager.Instance.unitState[unit.dataIndex];

        if (currentState != previousState)
        {
            if (animator)
                UpdateAnimation(currentState);
            previousState = currentState;
        }
    }

    private void UpdateAnimation(UnitState state)
    {
        string animStateName;
        
        switch (state)
        {
            case UnitState.Idle:
                animStateName = "UnitIdle";
                break;
            case UnitState.Move:
                animStateName = "UnitMove";
                break;
            default:
                animStateName = "UnitIdle";
                break;
        }
        
        animator.CrossFadeInFixedTime(animStateName, fixedTransitionDuration);
    }
}
