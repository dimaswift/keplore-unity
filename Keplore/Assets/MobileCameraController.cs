using UnityEngine;


namespace ConsequenceCascade.Behaviours
{
    public class MobileCameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera targetCamera;
        
        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 0.1f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float zoomSmoothness = 10f;
        
        [Header("Pan Settings")]
        [SerializeField] private float panSpeed = 1f;
        [SerializeField] private bool invertPan = false;
        
        // Touch tracking variables
        private Vector2 touchStart;
        private float startingDistance = 0f;
        private float startingSize = 0f;
        private float targetSize;
        
        private void Awake()
        {
            // If no camera is assigned, use the camera attached to this GameObject
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
                
                // If still null, try to find the main camera
                if (targetCamera == null)
                {
                    targetCamera = Camera.main;
                    Debug.LogWarning("No camera assigned. Using Camera.main instead.");
                }
            }
            
            // Ensure the camera is orthographic
            if (targetCamera != null && !targetCamera.orthographic)
            {
                Debug.LogWarning("The assigned camera is not orthographic. Setting it to orthographic mode.");
                targetCamera.orthographic = true;
            }
            
            targetSize = targetCamera.orthographicSize;
        }
        
        public void SetSize(float size)
        {
            targetCamera.orthographicSize = size;
            targetSize = size;
        }
        
        private void Update()
        {
            // Check if we have at least 2 touches for our functionality
            if (Input.touchCount >= 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                
                // Handle initial touch phase
                if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                {
                    // Store the initial positions and camera settings
                    startingDistance = Vector2.Distance(touch0.position, touch1.position);
                    startingSize = targetCamera.orthographicSize;
                    
                    // For panning, use the middle point between the two touches
                    touchStart = (touch0.position + touch1.position) * 0.5f;
                }
                // Handle moving phase
                else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                {
                    // Calculate current distance between touches
                    float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                    
                    // Handle pinch zoom
                    if (startingDistance > 0f)
                    {
                        float zoomFactor = startingDistance / currentDistance;
                        
                        // Store target size for smooth interpolation
                        targetSize = Mathf.Clamp(
                            startingSize * zoomFactor, 
                            minZoom, 
                            maxZoom
                        );
                    }
                    
                    // Handle two-finger panning with zoom-adjusted speed
                    Vector2 touchDelta = (touch0.position + touch1.position) * 0.5f;
                    Vector2 panDelta = (touchStart - touchDelta);
                    
                    // Calculate zoom-adjusted pan speed
                    // This is the key improvement: pan speed is proportional to the current zoom level
                    float zoomAdjustedPanSpeed = panSpeed * (targetCamera.orthographicSize / maxZoom);
                    
                    // Apply pan movement (optionally inverted)
                    Vector3 panMovement = new Vector3(
                        invertPan ? -panDelta.x : panDelta.x,
                        invertPan ? -panDelta.y : panDelta.y,
                        0
                    ) * zoomAdjustedPanSpeed;
                    
                    transform.Translate(panMovement * Time.deltaTime);
                    
                    // Update touch start for the next frame
                    touchStart = touchDelta;
                }
            }
            
            // Smoothly interpolate the actual orthographic size to the target
            if (Mathf.Abs(targetCamera.orthographicSize - targetSize) > 0.01f)
            {
                // Save mouse position in world space before zoom
                Vector3 mousePositionBefore = Vector3.zero;
                if (Input.touchCount >= 1)
                {
                    mousePositionBefore = targetCamera.ScreenToWorldPoint(Input.GetTouch(0).position);
                }
                
                // Apply smooth zoom
                targetCamera.orthographicSize = Mathf.Lerp(
                    targetCamera.orthographicSize, 
                    targetSize, 
                    Time.deltaTime * zoomSmoothness
                );
                
                // Maintain mouse position in world space after zoom
                if (Input.touchCount >= 1)
                {
                    Vector3 mousePositionAfter = targetCamera.ScreenToWorldPoint(Input.GetTouch(0).position);
                    transform.position += (mousePositionBefore - mousePositionAfter);
                }
            }
        }
    }


}
