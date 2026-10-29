using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

public class WorldSpaceUIRaycaster : MonoBehaviour
{
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private Transform xrOrigin;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private readonly List<UnityEngine.XR.InputDevice> controllerDevices = new List<UnityEngine.XR.InputDevice>();
    private readonly HashSet<string> pressedDevices = new HashSet<string>();
    private Camera sceneCamera;
    private LineRenderer pointerLine;

    public void Configure(Canvas canvas)
    {
        worldCanvas = canvas;
        graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        XROrigin origin = FindAnyObjectByType<XROrigin>();
        xrOrigin = origin != null ? origin.transform : null;
    }

    private void Awake()
    {
        if (worldCanvas == null) { worldCanvas = GetComponent<Canvas>(); }
        if (graphicRaycaster == null && worldCanvas != null) { graphicRaycaster = worldCanvas.GetComponent<GraphicRaycaster>(); }
        if (xrOrigin == null)
        {
            XROrigin origin = FindAnyObjectByType<XROrigin>();
            xrOrigin = origin != null ? origin.transform : null;
        }
        sceneCamera = Camera.main;
        CreatePointerLine();
    }

    private void Update()
    {
        if (worldCanvas == null || graphicRaycaster == null) { return; }
        if (sceneCamera == null) { sceneCamera = Camera.main; }
        if (sceneCamera == null) { return; }

        if (Mouse.current != null)
        {
            Ray mouseRay = sceneCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            DrawPointer(mouseRay, false);
            if (Mouse.current.leftButton.wasPressedThisFrame) { ClickButton(mouseRay); }
        }

        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice, controllerDevices);
        foreach (UnityEngine.XR.InputDevice device in controllerDevices)
        {
            if (!device.isValid
                || !device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 localPosition)
                || !device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion localRotation))
            {
                continue;
            }

            Vector3 position = xrOrigin != null ? xrOrigin.TransformPoint(localPosition) : localPosition;
            Quaternion rotation = xrOrigin != null ? xrOrigin.rotation * localRotation : localRotation;
            Ray controllerRay = new Ray(position, rotation * Vector3.forward);
            DrawPointer(controllerRay, true);

            string deviceKey = device.name;
            bool isPressed = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed;
            if (isPressed && pressedDevices.Add(deviceKey)) { ClickButton(controllerRay); }
            if (!isPressed) { pressedDevices.Remove(deviceKey); }
        }
    }

    private void ClickButton(Ray ray)
    {
        if (!TryGetButton(ray, out Button button)) { return; }
        if (button.interactable) { button.onClick.Invoke(); }
    }

    private bool TryGetButton(Ray ray, out Button button)
    {
        button = null;
        Plane canvasPlane = new Plane(worldCanvas.transform.forward, worldCanvas.transform.position);
        if (!canvasPlane.Raycast(ray, out float distance) || distance < 0f) { return false; }

        Vector3 hitPoint = ray.GetPoint(distance);
        Vector3 screenPoint = sceneCamera.WorldToScreenPoint(hitPoint);
        if (screenPoint.z <= 0f) { return false; }

        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPoint };
        raycastResults.Clear();
        graphicRaycaster.Raycast(eventData, raycastResults);
        if (raycastResults.Count == 0) { return false; }

        button = raycastResults[0].gameObject.GetComponentInParent<Button>();
        return button != null;
    }

    private void CreatePointerLine()
    {
        GameObject lineObject = new GameObject("Controller Pointer");
        lineObject.transform.SetParent(transform, false);
        pointerLine = lineObject.AddComponent<LineRenderer>();
        pointerLine.positionCount = 2;
        pointerLine.startWidth = 0.006f;
        pointerLine.endWidth = 0.002f;
        pointerLine.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        pointerLine.startColor = new Color(0.25f, 0.9f, 0.8f, 0.9f);
        pointerLine.endColor = new Color(0.25f, 0.9f, 0.8f, 0.1f);
    }

    private void DrawPointer(Ray ray, bool visible)
    {
        if (pointerLine == null) { return; }
        pointerLine.enabled = visible;
        if (!visible) { return; }
        pointerLine.SetPosition(0, ray.origin);
        pointerLine.SetPosition(1, ray.origin + ray.direction * 3f);
    }
}