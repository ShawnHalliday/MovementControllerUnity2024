using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private readonly float playerViewYOffset = 0.5f;
    public Transform playerView;
    public float xMouseSensitivity = 50.0f;
    public float yMouseSensitivity = 50.0f;
    private readonly float gravity = 20.0f;
    private readonly float friction = 6.0f;
    private float moveSpeed = 7.0f;
    private readonly float runAcceleration = 14.0f;
    private readonly float runDeacceleration = 10.0f;
    private readonly float airAcceleration = 2.0f;
    private readonly float airDecceleration = 2.0f;
    private readonly float airControl = 0.3f;
    private readonly float sideStrafeAcceleration = 100.0f;
    private readonly float sideStrafeSpeed = 1.0f;
    private readonly float jumpSpeed = 8.0f;
    private readonly float grappleForce = 2.0f;
    public LayerMask layerMask;
    private float forwardMove;
    private readonly Vector3 crouchModifier = new Vector3(1f, 0.5f, 1f);
    private float rightMove;
    private float rotX = 0.0f;
    private float rotY = 0.0f;
    public bool isGrappled = false;
    public Vector3 grapplePos;
    private Vector3 crouchScale = Vector3.one;
    public GUIStyle style;
    private Vector3 CollisionNormal = Vector3.zero;
    private Vector3 playerVelocity = Vector3.zero;
    private bool isColliding = false;

    private bool wishJump = false;

    private CharacterController _controller;
    public enum CollisionType
    {
        Floor,
        Roof,
        Wall,
        Ramp,
        NULL
    }
    private CollisionType collisionType;
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
        playerView.position = new Vector3(transform.position.x, transform.position.y + playerViewYOffset*crouchScale.y, transform.position.z);
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
        isColliding = false;
        QueueJump();
        Grapple();
        crouch();
        if (_controller.isGrounded)
            GroundMove();
        else if (!_controller.isGrounded)
            AirMove();
        HandleCollisionTypes();
        _controller.Move(playerVelocity * Time.deltaTime);
        playerView.position = new Vector3(transform.position.x, transform.position.y + playerViewYOffset + (crouchScale.y-1), transform.position.z);
        Debug.Log(isColliding);
        if (!isColliding)
        {
            collisionType = CollisionType.NULL;
        }

    }
    private void SetMovementDir()
    {
        forwardMove = Input.GetAxisRaw("Vertical");
        rightMove = Input.GetAxisRaw("Horizontal");
    }
    private void QueueJump()
    {
        wishJump = Input.GetButton("Jump");
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
        if (collisionType == CollisionType.Ramp)
        {
            playerVelocity.y = -4f;
        }
        else
        {
            playerVelocity.y = -gravity * Time.deltaTime;
        }
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
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        isColliding = true;
        CollisionNormal = hit.normal;
        float dot = Vector3.Dot(CollisionNormal, Vector3.up);
        if (hit.gameObject.layer == 3 || dot == 1)
        {
            collisionType = CollisionType.Floor;
            return;
        }
        else if (dot < -0.01f)
        {
            collisionType = CollisionType.Roof;
            return;
        }
        else if (dot < 0.01f)
        {
            collisionType = CollisionType.Wall;
            return;
        }
        else if (dot >= 0.01f && dot < 1f)
        {
            collisionType = CollisionType.Ramp;
            return;
        }
        else
        {
            collisionType = CollisionType.NULL;
            return;
        }
    }
    private void HandleCollisionTypes()
    {
        float dot = Vector3.Dot(CollisionNormal, Vector3.up);
        if (collisionType == CollisionType.Roof)
        {
            playerVelocity = Vector3.ProjectOnPlane(playerVelocity, CollisionNormal);
            playerVelocity.y -= 1f;
        }
        else if (collisionType == CollisionType.Wall)
        {
            playerVelocity = Vector3.ProjectOnPlane(playerVelocity, CollisionNormal);
        }
        else if (collisionType == CollisionType.Ramp)
        {
            if (!_controller.isGrounded)
            {
                playerVelocity = Vector3.ProjectOnPlane(playerVelocity, CollisionNormal);
            }
        }
    }
    private void crouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            crouchScale = crouchModifier;
            moveSpeed = 4.5f;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            crouchScale = Vector3.one;
            moveSpeed = 7f;
        }
        transform.localScale = crouchScale;
    }

    private void OnGUI()
    {
        var ups = _controller.velocity;
        ups.y = 0;
        GUI.Label(new Rect(0, 15, 400, 100), "Speed: " + Mathf.Round(ups.magnitude * 100) / 100 + "ups", style);
        GUI.Label(new Rect(0, 30, 400, 100), "Grapple Pos: " + grapplePos.ToString(), style);
    }
}
