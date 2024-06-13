using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float playerViewYOffset = 0.5f;
    public float xMouseSensitivity = 30.0f;
    public float yMouseSensitivity = 30.0f;
    public float gravity = 20.0f;
    public float friction = 6;
    public float moveSpeed = 7.0f;
    public float runAcceleration = 14.0f;
    public float runDeacceleration = 10.0f;
    public float airAcceleration = 2.0f;
    public float airDecceleration = 2.0f;
    public float airControl = 0.3f;
    public float sideStrafeAcceleration = 50.0f;
    public float sideStrafeSpeed = 1.0f;
    public float jumpSpeed = 8.0f;
    public float speedClamp = 20.0f;
    public float grappleForce = 1f;
    public bool holdJumpToBhop = false;
    public LayerMask layerMask;
    private float forwardMove;
    private float rightMove;
    private float rotX = 0.0f;
    private float rotY = 0.0f;
    public Vector3 grapplePos;
    public GUIStyle style;
    public bool isGrappled = false;


    private Vector3 playerVelocity = Vector3.zero;

    public Transform playerView;

    private bool wishJump = false;

    private CharacterController _controller;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        _controller = GetComponent<CharacterController>();
        if (playerView == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                playerView = mainCamera.gameObject.transform;
            }
        }
        playerView.position = new Vector3(transform.position.x, transform.position.y + playerViewYOffset, transform.position.z);
    }
    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetButtonDown("Fire1"))
                Cursor.lockState = CursorLockMode.Locked;
        }
        rotX -= Input.GetAxisRaw("Mouse Y") * xMouseSensitivity * 0.02f;
        rotY += Input.GetAxisRaw("Mouse X") * yMouseSensitivity * 0.02f;
        rotX = Mathf.Clamp(rotX, -85f, 85f);
        transform.rotation = Quaternion.Euler(0, rotY, 0);
        playerView.rotation = Quaternion.Euler(rotX, rotY, 0);
        QueueJump();
        Grapple();
        if (_controller.isGrounded)
            GroundMove();
        else if (!_controller.isGrounded)
            AirMove();
        
        _controller.Move(playerVelocity * Time.deltaTime);
        playerView.position = new Vector3(transform.position.x, transform.position.y + playerViewYOffset, transform.position.z);
    }
    private void SetMovementDir()
    {
        forwardMove = Input.GetAxisRaw("Vertical");
        rightMove = Input.GetAxisRaw("Horizontal");
    }
    private void QueueJump()
    {
        if (holdJumpToBhop)
        {
            wishJump = Input.GetButton("Jump");
            return;
        }

        if (Input.GetButtonDown("Jump") && !wishJump)
        {
            wishJump = true;
        }
        if (Input.GetButtonUp("Jump"))
        {
            wishJump = false;
        }
    }
    private void AirMove()
    {
        Vector3 wishdir;
        float accel;
        SetMovementDir();
        wishdir = new Vector3(rightMove, 0, forwardMove);
        wishdir = transform.TransformDirection(wishdir);
        float wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;
        wishdir.Normalize();
        float wishspeed2 = wishspeed;
        if (Vector3.Dot(playerVelocity, wishdir) < 0)
        {
            accel = airDecceleration;
        }
        else
        {
            accel = airAcceleration;
        }
        
        if (wishspeed > sideStrafeSpeed)
        {
            wishspeed = sideStrafeSpeed;
        }
        accel = sideStrafeAcceleration;
        
        Accelerate(wishdir, wishspeed, accel);
        if (wishspeed2 > moveSpeed)
        {
            wishspeed2 = moveSpeed;
        }
        if (airControl > 0)
        {
            AirControl(wishdir, wishspeed2);
        }
        playerVelocity.y -= gravity * Time.deltaTime;
    }
    private void AirControl(Vector3 wishdir, float wishspeed)
    {
        float zspeed;
        float speed;
        float dot;
        float k;
        
        if (Mathf.Abs(forwardMove) < 0.001 || Mathf.Abs(wishspeed) < 0.001)
        {
            return;
        }
        zspeed = playerVelocity.y;
        playerVelocity.y = 0;
        speed = playerVelocity.magnitude;
        playerVelocity.Normalize();
        dot = Vector3.Dot(playerVelocity, wishdir);
        k = 32;
        k *= airControl * dot * dot * Time.deltaTime;
        if (dot > 0)
        {
            playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
            playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
            playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;
            playerVelocity.Normalize();
        }
        playerVelocity.x *= speed;
        playerVelocity.y = zspeed;
        playerVelocity.z *= speed;
    }
    private void GroundMove()
    {
        Vector3 wishdir;
        if (!wishJump)
        {
            ApplyFriction(1.0f);
        }
        else
        {
            ApplyFriction(1f);
        }
        SetMovementDir();
        wishdir = new Vector3(rightMove, 0, forwardMove);
        wishdir = transform.TransformDirection(wishdir);
        wishdir.Normalize();
        var wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;
        Accelerate(wishdir, wishspeed, runAcceleration);
        playerVelocity.y = -gravity * Time.deltaTime;
        if (wishJump)
        {
            playerVelocity.y = jumpSpeed;
            wishJump = false;
        }
    }
    private void ApplyFriction(float t)
    {
        Vector3 vec = playerVelocity;
        float speed;
        float newspeed;
        float control;
        float drop;
        vec.y = 0.0f;
        speed = vec.magnitude;
        drop = 0.0f;
        if (_controller.isGrounded)
        {
            control = speed < runDeacceleration ? runDeacceleration : speed;
            drop = control * friction * Time.deltaTime * t;
        }
        newspeed = speed - drop;
        if (newspeed < 0)
        {
            newspeed = 0;
        }
        if (speed > 0)
        {
            newspeed /= speed;
        }
        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }
    private void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float addspeed;
        float accelspeed;
        float currentspeed;
        
        currentspeed = Vector3.Dot(playerVelocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
        {
            return;
        }
        accelspeed = accel * Time.deltaTime * wishspeed;
        if (accelspeed > addspeed)
        {
            accelspeed = addspeed;
        }
        playerVelocity.x += accelspeed * wishdir.x;
        playerVelocity.z += accelspeed * wishdir.z;
    }
    private void Grapple()
    {
        RaycastHit hit;
        Vector3 x;
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(playerView.position, Camera.main.transform.forward, out hit, 200f, layerMask))
            {
                grapplePos = hit.point;
                isGrappled = true;
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isGrappled = false;
        }
        if (isGrappled)
        {
            x = grapplePos - transform.position;
            playerVelocity += x * grappleForce * Time.deltaTime;
        }
        Debug.DrawLine(playerView.position, playerView.position + Camera.main.transform.forward);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Vector3 normal = hit.normal;
        if (hit.gameObject.layer == 3)
        {
            return;
        }
        else if(!(Vector3.Dot(normal, Vector3.up) > 0.1f))
        {
            playerVelocity = Vector3.ProjectOnPlane(new Vector3(playerVelocity.x, 0, playerVelocity.z), normal);
        }
        else
        {
            if (!_controller.isGrounded)
            {
                playerVelocity = Vector3.ProjectOnPlane(playerVelocity, normal);
            }
            Debug.DrawLine(transform.position, transform.position + Vector3.ProjectOnPlane(playerVelocity, normal).normalized*3f);
        }
    }
        
    private void OnGUI()
    {
        var ups = _controller.velocity;
        ups.y = 0;
        GUI.Label(new Rect(0, 15, 400, 100), "Speed: " + Mathf.Round(ups.magnitude * 100) / 100 + "ups", style);
        GUI.Label(new Rect(0, 30, 400, 100), "Grapple Pos: " + grapplePos.ToString(), style);
    }
}
