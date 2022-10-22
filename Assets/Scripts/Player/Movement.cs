using System;
using UnityEngine;

namespace Player
{
    public class Movement : MonoBehaviour
    {
        private CharacterController _characterController;
        
        [SerializeField] private Transform playerCam;
        [SerializeField] private Transform orientation;
        
        private Rigidbody _rigidbody;

        private float _xRotation;
        [SerializeField] private float sensitivity = 50f;
        [SerializeField] private float sensitivityMultiplier = 1f;
        
        [SerializeField] private float moveSpeed = 4500f;
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private bool grounded;
        [SerializeField] private LayerMask ground;
    
        [SerializeField] private float counterMovement = 0.175f;
        [SerializeField] private float maxSlopeAngle = 35f;

        [SerializeField] private Vector3 crouchScale = new(1, 0.5f, 1);
        private Vector3 _playerScale;

        private bool _readyToJump = true, _jumping;
        [SerializeField] private float jumpCooldown = 0.25f;
        [SerializeField] private float jumpForce = 150f;
        
        private Vector3 _normalVector = Vector3.up;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
    
        private void Start()
        {
            _playerScale = transform.localScale;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    
        private void FixedUpdate()
        {
            if (grounded) Move();
            if (_readyToJump && _jumping) Jump();
        }

        private void Update()
        {
            Look();
            _jumping = Input.GetKey(KeyCode.Space);
            if (Input.GetKeyDown(KeyCode.LeftControl)) StartCrouch();
            if (Input.GetKeyUp(KeyCode.LeftControl)) StopCrouch();

        }
        
        private void StartCrouch()
        {
            Vector3 transformPosition = transform.position;
            transform.localScale = crouchScale;
            transform.position = new Vector3(transformPosition.x, transformPosition.y - 0.5f, transformPosition.z);
            maxSpeed /= 2;
        }

        private void StopCrouch()
        {
            Vector3 transformPosition = transform.position;
            transform.localScale = _playerScale;
            transform.position = new Vector3(transformPosition.x, transformPosition.y + 0.5f, transformPosition.z);
            maxSpeed *= 2;
        }

        private void Move()
        {
            float x = Input.GetAxisRaw("Horizontal"), y = Input.GetAxisRaw("Vertical");

            _rigidbody.AddForce(Vector3.down * (Time.deltaTime * 10));
        
            Vector2 mag = FindVelRelativeToLook();

            CounterMovement(x, y, mag);
            
            if (x > 0 && mag.x > maxSpeed) x = 0;
            if (x < 0 && mag.x < -maxSpeed) x = 0;
            if (y > 0 && mag.y > maxSpeed) y = 0;
            if (y < 0 && mag.y < -maxSpeed) y = 0;

            _rigidbody.AddForce(orientation.transform.forward * (y * moveSpeed * Time.deltaTime * Mathf.Pow(grounded ? 1f : 0.5f, 2f)));
            _rigidbody.AddForce(orientation.transform.right * (x * moveSpeed * Time.deltaTime * (grounded ? 1f : 0.5f)));
        }

        private void Jump()
        {
            if (!grounded || !_readyToJump) return;
            _readyToJump = false;
    
            _rigidbody.AddForce(Vector2.up * (jumpForce * 1.5f));
            _rigidbody.AddForce(_normalVector * (jumpForce * 0.5f));
                
            Vector3 vel = _rigidbody.velocity;
            _rigidbody.velocity = vel.y switch
            {
                < 0.5f => new Vector3(vel.x, 0, vel.z),
                > 0 => new Vector3(vel.x, vel.y / 2, vel.z),
                _ => vel
            };

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    
        private void ResetJump()
        {
            _readyToJump = true;
        }
    
        private float _desiredX;
        private void Look() {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensitivityMultiplier;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensitivityMultiplier;
    
            Vector3 rot = playerCam.transform.localRotation.eulerAngles;
            _desiredX = rot.y + mouseX;
            
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
    
            playerCam.transform.localRotation = Quaternion.Euler(_xRotation, _desiredX, 0);
            orientation.transform.localRotation = Quaternion.Euler(0, _desiredX, 0);
        }

        private void CounterMovement(float x, float y, Vector2 mag)
        {
            if (!grounded || _jumping) return;
            
            const float threshold = 0.01f;
    
            if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0)) {
                _rigidbody.AddForce(orientation.transform.right * (moveSpeed * Time.deltaTime * -mag.x * counterMovement));
            }
            if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0)) {
                _rigidbody.AddForce(orientation.transform.forward * (moveSpeed * Time.deltaTime * -mag.y * counterMovement));
            }

            Vector3 velocity = _rigidbody.velocity;
            if (!(Mathf.Sqrt((Mathf.Pow(velocity.x, 2) + Mathf.Pow(velocity.z, 2))) >
                  maxSpeed)) return;
            float fallSpeed = velocity.y;
            Vector3 n = velocity.normalized * maxSpeed;
            _rigidbody.velocity = new Vector3(n.x, fallSpeed, n.z);
        }

        private Vector2 FindVelRelativeToLook()
        {
            Vector3 velocity = _rigidbody.velocity;
            float lookAngle = orientation.transform.eulerAngles.y;
            float moveAngle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
    
            float u = Mathf.DeltaAngle(lookAngle, moveAngle);
            float v = 90 - u;
    
            float magnitude = velocity.magnitude;
            float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
            float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);
            
            return new Vector2(xMag, yMag);
        }
    
        private bool IsFloor(Vector3 v)
        {
            float angle = Vector3.Angle(Vector3.up, v);
            return angle < maxSlopeAngle;
        }

        private bool _cancellingGrounded;
        
        private void OnCollisionStay(Collision other)
        {
            int layer = other.gameObject.layer;
            if (ground != (ground | (1 << layer))) return;
    
            for (int i = 0; i < other.contactCount; i++)
            {
                Vector3 normal = other.contacts[i].normal;
                if (!IsFloor(normal)) continue;
                grounded = true;
                _cancellingGrounded = false;
                _normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
    
            const float delay = 3f;
            if (_cancellingGrounded) return;
            _cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    
        private void StopGrounded()
        {
            grounded = false;
        }
    }
}
