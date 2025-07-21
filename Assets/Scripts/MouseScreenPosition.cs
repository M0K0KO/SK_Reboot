using System;
using UnityEngine;

public class MouseScreenPosition : MonoBehaviour
{
    public static MouseScreenPosition Instance;

    public Vector3 currentMousePosition { get; private set; }
    public bool mouseButtonClicked { get; private set; }

    private Camera mainCam;

    private bool isMouseButtonDown = false;

    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
        currentMousePosition = Vector3.zero;
    }

    private void Update()
    {

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

    public void ClearMouseButtonInput()
    {
        mouseButtonClicked = false;
    }
}