using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerLook))]
public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;

    Rigidbody rb;

    [Header("Components")]
    [SerializeField] Transform orientation;
    [SerializeField] Camera cam;

    [Header("Speed")]
    [SerializeField] float walkSpeed = 7f;
    [SerializeField] float sprintSpeed = 10f;
    [SerializeField] float acceleration = 10f;
    float speed = 10.0f;
    bool sprint = false;

    [Header("Jump")]
    [SerializeField] public float jumpForce = 5f;

    [Header("Fov")]
    [SerializeField] float defaultFov = 70f;
    [SerializeField] float sprintFov = 72.5f;
    [SerializeField] float fovTime = 20f;

    [Header("Velocity")]
    [SerializeField] float groundVelocityChange = 1.5f;
    [SerializeField] float airVelocityChange = 0.3f;
    [SerializeField] float airDrag = 0.99f;

    [Header("Ground Detection")]
    [SerializeField, Range(0f, 90f)] float maxGroundAngle = 35f;
    [SerializeField] float playerHeight = 1.7f;
    float minGroundDotProduct;
    bool isGrounded;
    Vector3 contactNormal;
    int stepsSinceLastGrounded, stepsSinceLastJump;

    Vector3 targetVelocity;

    private void OnValidate()
    {
        //initialize inputs
        playerInput = GetComponent<PlayerInput>();

        //initialize other variables
        rb = GetComponent<Rigidbody>();

        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        OnValidate();
    }

    private void Update()
    {
        Inputs();
        ChangeSpeed();
        Jump();
    }

    private void Inputs()
    {
        //calculate velocity
        targetVelocity = (orientation.forward * playerInput.actions["Move"].ReadValue<Vector2>().y + orientation.right * playerInput.actions["Move"].ReadValue<Vector2>().x) * speed;
    }

    private void ChangeSpeed()
    {
        //detect sprint
        sprint = playerInput.actions["Sprint"].IsPressed();

        //change speed && fov
        if (sprint && targetVelocity != Vector3.zero && isGrounded)
        {
            //sprinting
            speed = Mathf.Lerp(speed, sprintSpeed, acceleration * Time.deltaTime);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, sprintFov, fovTime * Time.deltaTime);
        }
        else
        {
            //walking
            speed = Mathf.Lerp(speed, walkSpeed, acceleration * Time.deltaTime);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, defaultFov, fovTime * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        stepsSinceLastGrounded++;
        stepsSinceLastJump++;
        if (isGrounded || SnapToGround())
        {
            stepsSinceLastGrounded = 0;
            contactNormal.Normalize();
        }
        else
        {
            contactNormal = Vector3.up;
        }

        Move();
        Drag();

        isGrounded = false;
        contactNormal = Vector3.zero;
    }

    private void Move()
    {
        //calculate force
        Vector3 velocity = rb.velocity;
        Vector3 velocityChange = (targetVelocity - velocity);
        velocityChange.y = 0f;

        velocityChange = Vector3.ProjectOnPlane(velocityChange, contactNormal);

        //clamp velocity
        velocityChange = Vector3.ClampMagnitude(velocityChange, isGrounded ? groundVelocityChange : airVelocityChange);

        //apply force
        //total control on ground
        if (isGrounded)
        {
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
        //keep partial air control
        else if (!isGrounded && targetVelocity != Vector3.zero)
        {
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }

    private void Drag()
    {
        //slowdown velocity when not moving in air
        if (!isGrounded && targetVelocity == Vector3.zero)
        {
            rb.velocity = new Vector3(rb.velocity.x * airDrag, rb.velocity.y, rb.velocity.z * airDrag);
        }
    }

    private void Jump()
    {
        if (playerInput.actions["Jump"].WasPerformedThisFrame() && isGrounded)
        {
            //reset & apply vertical force
            stepsSinceLastJump = 0;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                isGrounded = true;
                contactNormal += normal;
            }
        }
    }

    bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 3)
        {
            return false;
        }
        if (!Physics.Raycast(rb.position, Vector3.down * (playerHeight / 2 + .5f), out RaycastHit hit))
        {
            return false;
        }
        if (hit.normal.y < minGroundDotProduct)
        {
            return false;
        }
        contactNormal = hit.normal;
        float speed = rb.velocity.magnitude;
        float dot = Vector3.Dot(rb.velocity, hit.normal);
        if (dot > 0f)
        {
            rb.velocity = (rb.velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }
}
