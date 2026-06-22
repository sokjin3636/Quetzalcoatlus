using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    [Header("이동 설정")]
    public float walkSpeed = 10f;
    public float dashSpeed = 25f;
    public bool maintainHeight = true;

    [Header("마우스 감도")]
    public float sensitivity = 0.2f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 rot = transform.localRotation.eulerAngles;
        rotationY = rot.y;
        rotationX = rot.x;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
        }
    }

    void HandleRotation()
    {
        // 레거시 Input 호환 모드 기반 마우스 입력 처리
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * 10f;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * 10f;

        rotationY += mouseX;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

    void HandleMovement()
    {
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? dashSpeed : walkSpeed;

        float moveForward = Input.GetAxis("Vertical");
        float moveSide = Input.GetAxis("Horizontal");

        Vector3 moveDir = (transform.forward * moveForward) + (transform.right * moveSide);

        if (maintainHeight)
        {
            moveDir.y = 0;
        }

        transform.position += moveDir.normalized * currentSpeed * Time.deltaTime;

        // 수동 고도 조절 입력 (상승/하강)
        if (Input.GetKey(KeyCode.E)) transform.position += Vector3.up * walkSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) transform.position += Vector3.down * walkSpeed * Time.deltaTime;
    }
}