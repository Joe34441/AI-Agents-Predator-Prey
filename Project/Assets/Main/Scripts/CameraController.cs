using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CameraController : MonoBehaviour
{
    [SerializeField] private new Camera camera;
    [SerializeField] private UIInfo uiinfo;

    private float mouseSensitivity = 1.0f;
    private float movementSpeed = 2.5f;

    private float shiftMultiplier = 5.0f;
    private float shiftSpeed = 1.0f;

    Vector3 rotationSmoothVelocity;
    float rotationSmoothTime = 0.01f;

    Vector3 currentRotation;

    private float xRot;
    private float yRot;

    private void Awake()
    {
        if (!camera) camera = GetComponentInChildren<Camera>();
        if (!uiinfo) uiinfo = GetComponent<UIInfo>();
    }

    private void Start()
    {
        Application.targetFrameRate = 144;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Select();
        Movement();
        LookRotation();
    }

    private void Select()
    {
        if (!Input.GetKeyDown(KeyCode.Mouse0)) return;

        RaycastHit[] hits;
        hits = Physics.RaycastAll(transform.position, transform.forward, 100.0f);

        Animal animal = null;
        bool foundTarget = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform != null)
            {
                if (hit.collider.gameObject.GetComponent<NavMeshAgent>())
                {
                    foundTarget = true;
                    animal = hit.collider.gameObject.GetComponent<Animal>();
                }
            }

            if (foundTarget) break;
        }

        if (!foundTarget) return;

        FindObjectOfType<UIInfo>().SelectAnimal(animal.GetAnimalIndex());
    }

    private void Movement()
    {
        //shift speed
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftSpeed = shiftMultiplier;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            shiftSpeed = 1.0f;
        }

        //WASD QE movement
        if (Input.GetKey(KeyCode.W))
        {
            Vector3 moveVelocity = transform.forward * movementSpeed * shiftSpeed;
            moveVelocity.y = 0.0f;
            transform.position += moveVelocity * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            Vector3 moveVelocity = transform.right * -movementSpeed * shiftSpeed;
            moveVelocity.y = 0.0f;
            transform.position += moveVelocity * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            Vector3 moveVelocity = transform.forward * -movementSpeed * shiftSpeed;
            moveVelocity.y = 0.0f;
            transform.position += moveVelocity * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Vector3 moveVelocity = transform.right * movementSpeed * shiftSpeed;
            moveVelocity.y = 0.0f;
            transform.position += moveVelocity * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            Vector3 newPos = transform.position;
            newPos.y -= movementSpeed * Time.deltaTime * (shiftSpeed / 2);
            transform.position = newPos;
        }
        if (Input.GetKey(KeyCode.E))
        {
            Vector3 newPos = transform.position;
            newPos.y += movementSpeed * Time.deltaTime * (shiftSpeed / 2);
            transform.position = newPos;
        }
    }

    private void LookRotation()
    {
        //camera rotation
        yRot += Input.GetAxis("Mouse X") * mouseSensitivity;
        xRot -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        xRot = Mathf.Clamp(xRot, -70, 70);

        currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(xRot, yRot), ref rotationSmoothVelocity, rotationSmoothTime);
        transform.eulerAngles = currentRotation;
    }
}
