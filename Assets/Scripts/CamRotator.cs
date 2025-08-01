using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
public class CamRotator : MonoBehaviour
{
    public Transform target;
    public Camera mainCamera;

    [Range(0.1f, 15f)]
    public float mouseRotateSpeed = 2.5f;

    [Range(0.001f, 10)]
    public float touchRotateSpeed = 0.1f;

    public float slerpValue = 0.25f;

    public enum RotateMethod { Mouse, Touch };
    public RotateMethod rotateMethod = RotateMethod.Touch;

    private Quaternion cameraRot;
    private float offset;

    private float minXRotAngle = 0f;
    private float maxXRotAngle = 90f;

    private float rotX;
    private float rotY;

    private float touchRotX;
    private float touchRotY;
    private Vector2 lastTouchPosition;
    private bool isRotating = false;

    [SerializeField]
    private float zoomSpeed = 10f;
    private Camera zoomCamera;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        EnhancedTouchSupport.Enable(); // Enable Enhanced Touch for Input System
    }

    void Start()
    {
        offset = Vector3.Distance(mainCamera.transform.position, target.position);
        zoomCamera = mainCamera;

        cameraRot = mainCamera.transform.rotation;
        rotX = NormalizeAngle(mainCamera.transform.eulerAngles.x);
        rotY = NormalizeAngle(mainCamera.transform.eulerAngles.y);

        touchRotX = rotX;
        touchRotY = rotY;
    }

    void Update()
    {
        if (rotateMethod == RotateMethod.Mouse)
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    cameraRot = mainCamera.transform.rotation;
                    rotX = NormalizeAngle(mainCamera.transform.eulerAngles.x);
                    rotY = NormalizeAngle(mainCamera.transform.eulerAngles.y);
                }

                if (Mouse.current.leftButton.isPressed)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    rotX += -mouseDelta.y * mouseRotateSpeed * Time.deltaTime;
                    rotY += mouseDelta.x * mouseRotateSpeed * Time.deltaTime;

                    rotX = Mathf.Clamp(rotX, minXRotAngle, maxXRotAngle);
                }

                float scroll = Mouse.current.scroll.ReadValue().y * 0.01f;
                if (scroll != 0)
                {
                    ApplyZoom(scroll * zoomSpeed);
                }
            }
        }
        else if (rotateMethod == RotateMethod.Touch)
        {
            var activeTouches = Touch.activeTouches;

            if (activeTouches.Count == 1)
            {
                var touch = activeTouches[0];

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    cameraRot = mainCamera.transform.rotation;
                    touchRotX = NormalizeAngle(mainCamera.transform.eulerAngles.x);
                    touchRotY = NormalizeAngle(mainCamera.transform.eulerAngles.y);
                    lastTouchPosition = touch.screenPosition;
                    isRotating = true;
                }

                if (isRotating && touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector2 delta = touch.screenPosition - lastTouchPosition;
                    touchRotX += -delta.y * touchRotateSpeed;
                    touchRotY += delta.x * touchRotateSpeed;
                    lastTouchPosition = touch.screenPosition;
                }

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    isRotating = false;
                }

                touchRotX = Mathf.Clamp(touchRotX, minXRotAngle, maxXRotAngle);
            }
            else if (activeTouches.Count >= 2)
            {
                var touch0 = activeTouches[0];
                var touch1 = activeTouches[1];

                isRotating = false; // disable rotation during zoom

                Vector2 prevPos0 = touch0.screenPosition - touch0.delta;
                Vector2 prevPos1 = touch1.screenPosition - touch1.delta;

                float prevDist = Vector2.Distance(prevPos0, prevPos1);
                float currDist = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);

                float zoomAmount = (currDist - prevDist) * 0.01f * zoomSpeed;
                ApplyZoom(zoomAmount);
            }
        }
    }

    private void LateUpdate()
    {
        Vector3 dir = new Vector3(0, 0, -offset);
        Quaternion newQ;

        if (rotateMethod == RotateMethod.Mouse)
        {
            newQ = Quaternion.Euler(rotX, rotY, 0);
        }
        else
        {
            newQ = Quaternion.Euler(touchRotX, touchRotY, 0);
        }

        cameraRot = Quaternion.Slerp(cameraRot, newQ, slerpValue);
        mainCamera.transform.position = target.position + cameraRot * dir;
        mainCamera.transform.LookAt(target.position);
    }

    private void ApplyZoom(float zoomAmount)
    {
        if (zoomCamera.orthographic)
        {
            zoomCamera.orthographicSize = Mathf.Clamp(zoomCamera.orthographicSize - zoomAmount, 1f, 20f);
        }
        else
        {
            zoomCamera.fieldOfView = Mathf.Clamp(zoomCamera.fieldOfView - zoomAmount, 20f, 80f);
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360;
        if (angle > 180) angle -= 360;
        return angle;
    }
}
