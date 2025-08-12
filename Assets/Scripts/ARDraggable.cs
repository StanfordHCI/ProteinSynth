using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARDraggable : MonoBehaviour
{
    [Header("Touch Settings")]
    [SerializeField] private float touchRadius = 2.0f; // Radius around object for touch detection
    [SerializeField] private bool showTouchArea = true; // Debug visualization
    
    private Camera camera;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private int activeTouchId = -1;
    
    void Start()
    {
        camera = Camera.main;
        
        // Enable enhanced touch support
        EnhancedTouchSupport.Enable();
    }
    
    void OnDestroy()
    {
        // Disable enhanced touch support when object is destroyed
        EnhancedTouchSupport.Disable();
    }
    
    void Update()
    {
        HandleDragging();
    }
    
    void HandleDragging()
    {
        // Handle touch input using Input System
        if (Touch.activeTouches.Count > 0)
        {
            foreach (var touch in Touch.activeTouches)
            {
                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        if (!isDragging)
                        {
                            TryStartDrag(touch.screenPosition, touch.touchId);
                        }
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        if (isDragging && touch.touchId == activeTouchId)
                        {
                            UpdateDrag(touch.screenPosition);
                        }
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        if (isDragging && touch.touchId == activeTouchId)
                        {
                            StopDragging();
                        }
                        break;
                }
            }
        }
        
        // Handle mouse input for editor testing
        #if UNITY_EDITOR
        HandleMouse();
        #endif
    }
    
    #if UNITY_EDITOR
    void HandleMouse()
    {
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryStartDrag(Mouse.current.position.ReadValue(), -1);
            }
            else if (Mouse.current.leftButton.isPressed && isDragging)
            {
                UpdateDrag(Mouse.current.position.ReadValue());
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
            {
                StopDragging();
            }
        }
    }
    #endif
    
    void TryStartDrag(Vector2 screenPos, int touchId)
    {
        Ray ray = camera.ScreenPointToRay(screenPos);
        
        // Check if touch is within radius of object
        if (IsTouchWithinRadius(ray))
        {
            isDragging = true;
            activeTouchId = touchId;
            
            Debug.Log($"Started dragging object: {gameObject.name}");
            
            // Calculate offset from touch point to object center
            Plane dragPlane = new Plane(camera.transform.forward, transform.position);
            float distance;
            
            if (dragPlane.Raycast(ray, out distance))
            {
                Vector3 touchPoint = ray.GetPoint(distance);
                dragOffset = transform.position - touchPoint;
            }
        }
    }
    
    bool IsTouchWithinRadius(Ray ray)
    {
        // First try standard raycast for direct hits
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) && hit.transform == transform)
        {
            return true;
        }
        
        // Then check if touch is within radius of object center
        Plane touchPlane = new Plane(camera.transform.forward, transform.position);
        float distance;
        
        if (touchPlane.Raycast(ray, out distance))
        {
            Vector3 touchPoint = ray.GetPoint(distance);
            float distanceToObject = Vector3.Distance(touchPoint, transform.position);
            return distanceToObject <= touchRadius;
        }
        
        return false;
    }
    
    void UpdateDrag(Vector2 screenPos)
    {
        if (!isDragging) return;
        
        Ray ray = camera.ScreenPointToRay(screenPos);
        Plane dragPlane = new Plane(camera.transform.forward, transform.position);
        
        float distance;
        if (dragPlane.Raycast(ray, out distance))
        {
            Vector3 newPosition = ray.GetPoint(distance) + dragOffset;
            transform.position = newPosition;
        }
    }
    
    void StopDragging()
    {
        isDragging = false;
        activeTouchId = -1;
        Debug.Log($"Stopped dragging object: {gameObject.name}");
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (showTouchArea)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, touchRadius);
        }
    }
}