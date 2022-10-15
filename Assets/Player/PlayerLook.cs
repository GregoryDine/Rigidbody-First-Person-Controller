using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerLook : MonoBehaviour
{
    PlayerInput playerInput;

    [SerializeField] float cameraSensitivity = 10f;

    [Header("Components")]
    [SerializeField] Transform cam;
    [SerializeField] Transform orientation;

    float mouseX;
    float mouseY;

    float multiplier = 0.01f;

    float xRotation;
    float yRotation;

    private void Awake()
    {
        //initialize inputs
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        //lock & hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Inputs();

        //update rotation
        cam.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    void Inputs()
    {
        //detect inputs
        mouseX = playerInput.actions["Look"].ReadValue<Vector2>().x;
        mouseY = playerInput.actions["Look"].ReadValue<Vector2>().y;

        //calculate rotation
        yRotation += mouseX * cameraSensitivity * multiplier;
        xRotation -= mouseY * cameraSensitivity * multiplier;

        //clamp vertical rotation
        xRotation = Mathf.Clamp(xRotation, -90, 90);
    }
}
