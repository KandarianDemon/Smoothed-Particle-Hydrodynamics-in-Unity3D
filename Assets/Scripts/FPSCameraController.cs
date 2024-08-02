using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    private Vector2 currentRotation;
    private bool isSprinting = false;

    [Range(1.0f,50.0f)]
    public float rotationSpeed;

    public Transform domain;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentRotation = new Vector2();
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleSprint();
        ResetViewOnKey();
        ChangeParticleViewMode();

        if(domain != null)
        {
             RotateObject();
        }

        if(Input.GetKeyDown(KeyCode.Space)){
            domain.GetComponent<ParticleSystem>().Run_Pause();
        }

        if(Input.GetKeyDown(KeyCode.Q))
        {
            ToggleSimulationObjects();
        }
       


    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        currentRotation.x -= mouseY; // Adjust pitch
        currentRotation.y += mouseX; // Adjust yaw

        currentRotation.x = Mathf.Clamp(currentRotation.x, -90f, 90f);

        gameObject.transform.localRotation = Quaternion.Euler(currentRotation.x,currentRotation.y, 0f);
       
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal") * moveSpeed;
        float moveZ = Input.GetAxis("Vertical") * moveSpeed;

        Vector3 moveDirection = transform.right * moveX + transform.forward * moveZ;
        if (isSprinting) moveDirection *= sprintMultiplier;

        transform.Translate(moveDirection * Time.deltaTime, Space.World);
    }

    void HandleSprint()
    {
        isSprinting = Input.GetKey(KeyCode.LeftShift);
    }

    void ResetViewOnKey()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if(transform.parent != null)
            {
                transform.parent.localPosition = Vector3.zero;
                transform.parent.localRotation = Quaternion.identity;
            }
        }
    }

    void RotateObject()
    {

        if (Input.GetKey(KeyCode.T)) // Rotate around X-axis
        {
            domain.Rotate(rotationSpeed * Time.deltaTime, 0, 0);
        }
        if (Input.GetKey(KeyCode.Y)) // Rotate around Y-axis
        {
            domain.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.U)) // Rotate around Z-axis
        {
            domain.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        

        
        // Apply rotation around each axis
        //domain.Rotate( rotationSpeed * Time.deltaTime, y * rotationSpeed * Time.deltaTime, u * rotationSpeed * Time.deltaTime);
    }

    void ChangeParticleViewMode()
    {
        ParticleSystem system = domain.GetComponent<ParticleSystem>();

        if(Input.GetKey(KeyCode.Alpha1))
        {
            system.ChangeViewMode(0);
        }

        if(Input.GetKey(KeyCode.Alpha2))
        {
            system.ChangeViewMode(1);
        }

        if(Input.GetKey(KeyCode.Alpha3))
        {
            system.ChangeViewMode(2);
        }
        if(Input.GetKey(KeyCode.Alpha4))
        {
            system.ChangeViewMode(3);
        }

    }

    void ToggleSimulationObjects()
    {
        GameObject[] simObjects = GameObject.FindGameObjectsWithTag("Simulation Object");

        foreach(var obj in simObjects)
        {
            MeshRenderer render = obj.GetComponent<MeshRenderer>();
            render.enabled = !render.enabled;
        }
    }
}
