using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public GameObject ShurikenSpawn;
    public GameObject Shuriken;
    public Transform playerBody;
    public Transform groundCheck;
    public LayerMask ground;
    public PlayerStats playerStats;
    public AudioSource throw1, throw2;

    public float movementSpeed = 15f;
    public float throwForce = 150f;
    public float rotationSpeed = 50f;
    public float groundDistance = 0.4f;
    public float jumpHeight = 3f;

    private bool isGrounded;
    private Rigidbody rb;
    private Animator anim;
    private RaycastHit hit;
    private bool canThrow = false;
    private float throwRange = 30f;
    private GameObject Target;
    private GameObject PreviousTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true; // Prevent physics from rotating the player
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, ground);
        anim.SetBool("Walk", false);

        HandleJump();
        HandleMovement();
        HandleThrowing();
        HandleTargeting();
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") )
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), rb.linearVelocity.z);
            anim.Play("Standing Jump");
        }
    }


    void HandleMovement()
    {
        Quaternion targetRotation = transform.rotation;
        bool isMoving = false;

        if (Input.GetKey(KeyCode.S))
        {
            targetRotation = Quaternion.Euler(0, 0f, 0); // Face North
            isMoving = true;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            targetRotation = Quaternion.Euler(0, 180f, 0); // Face South
            isMoving = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            targetRotation = Quaternion.Euler(0, 270f, 0); // Face West
            isMoving = true;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            targetRotation = Quaternion.Euler(0, 90f, 0); // Face East
            isMoving = true;
        }

        // Smoothly rotate the player
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (isMoving)
        {
            // Move in the direction the player is facing
            Vector3 moveDirection = transform.forward * movementSpeed;
            rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
            anim.SetBool("Walk", true);
        }
        else
        {
            // Stop horizontal movement
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void HandleThrowing()
    {
        if (playerStats == null)
        {
            Debug.LogError("playerStats is not assigned!");
            return;
        }
        if (Input.GetButtonDown("Fire1") && canThrow && playerStats.shuriken > 0)
        {
            // Null check for playerStats


            playerStats.UseShuriken();

            // Null check for anim
            if (anim == null)
            {
                Debug.LogError("Animator component is not assigned!");
                return;
            }
            anim.SetTrigger("Throw");

            // Null check for throw1
            if (throw1 == null)
            {
                Debug.LogError("AudioSource for throwing is not assigned!");
                return;
            }
            throw1.PlayOneShot(throw1.clip);

            // Null check for Shuriken prefab and spawn point
            if (Shuriken == null)
            {
                Debug.LogError("Shuriken prefab is not assigned!");
                return;
            }
            if (ShurikenSpawn == null)
            {
                Debug.LogError("Shuriken spawn point is not assigned!");
                return;
            }

            GameObject thrownShuriken = Instantiate(Shuriken, ShurikenSpawn.transform.position, ShurikenSpawn.transform.rotation);

            // Null check for Target
            if (Target == null)
            {
                Debug.LogError("Target is not assigned!");
                return;
            }

            Vector3 direction = Target.transform.position - thrownShuriken.transform.position;

            // Null check for Rigidbody
            Rigidbody shurikenRb = thrownShuriken.GetComponent<Rigidbody>();
            if (shurikenRb == null)
            {
                Debug.LogError("Shuriken prefab is missing Rigidbody component!");
                return;
            }
            shurikenRb.AddForce(direction * throwForce);

            canThrow = false;

            // Null check for Target's Canvas
            Canvas targetCanvas = Target.GetComponentInChildren<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogWarning("Target doesn't have a Canvas component in children");
            }
            else
            {
                targetCanvas.enabled = false;
            }
        }
    }

    void HandleTargeting()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Preseed");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, throwRange) && hit.collider.CompareTag("Target"))
            {
                TorchController torch = hit.collider.GetComponentInParent<TorchController>();
                if (torch != null && !torch.isExtinguished)
                {
                    PreviousTarget = Target;
                    Target = hit.collider.gameObject;
                    canThrow = true;

                    if (PreviousTarget != null)
                        PreviousTarget.GetComponentInChildren<Canvas>().enabled = false;
                    Target.GetComponentInChildren<Canvas>().enabled = true;
                }
            }
        }
    }
}