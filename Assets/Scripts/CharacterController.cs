using System;
using System.Collections.Generic;
using DefaultNamespace.Utils;
using UnityEngine;

namespace DefaultNamespace
{
    public class CharacterController : MonoBehaviour
    {
        public event Action<EMode> ModeChangedEvent;
        public event Action<int> ActiveBiomeIdxChangedEvent;
        
        public enum EMode
        {
            Build,
            Destroy
        }
        
        public EMode Mode
        {
            get => _mode;
            set
            {
                if (value != _mode)
                {
                    _mode = value;
                    ModeChangedEvent?.Invoke(_mode);
                }
            }
        }

        public int ActiveBiomeIdx
        {
            get => _activeBiomeIdx;
            set
            {
                var newIdx = Mathf.Clamp(value, 0, _userBiomes.Count);

                if (newIdx != _activeBiomeIdx)
                {
                    _activeBiomeIdx = newIdx;
                    ActiveBiomeIdxChangedEvent?.Invoke(_activeBiomeIdx);
                }
            }
        }

        [Header("Movement")]
        public float speed = 5;
        public float gravityScale = 1;

        [Header("Jump")] 
        public float jumpSpeedDuration;
        public float jumpSpeed = 1f;
        public AnimationCurve jumpCurve;

        [Header("Camera")] 
        public float lookSensitivity;

        [Header("Controls")] 
        public KeyCode switchModeKey;
        public KeyCode switchBlockKey;
        public KeyCode jumpKey;
        
        [Space]
        public float maxBuildDistance;
        public float maxDestroyDistance;

        [Header("References")] 
        public Transform lookParent;

        private Vector3 _lastMousePos;
        private float _lookPitch; // x rot
        private float _lookYaw; // y rot

        private Vector3 _velocity;
        private Vector3 _gravityVelocity;
        private Vector3 _moveVelocity;

        private float _jumpTime;
        private bool _jumping;
        private Vector3 _jumpVelocity;

        private EMode _mode;
        private int _activeBiomeIdx;
        private IReadOnlyList<Biome> _userBiomes;
        
        private bool _grounded;
        private float _currentVoxelHeight;
        private VoxelHitInfo _voxelHitInfo;
        private VoxelInfo _standingVoxelInfo;

        // references
        private WorldManager _worldManager;
        private WireCube _buildCube;
        
        private WireCube _destroyCube;

        // constants
        private float _voxelSize;

        private void Start()
        {
            _lastMousePos = Input.mousePosition;

            _worldManager = Locator.Instance.WorldManager;

            _voxelSize = Locator.Instance.GameSettings.VoxelSize;

            _buildCube = Locator.Instance.BuildCube;
            _destroyCube = Locator.Instance.DestroyCube;

            _userBiomes = Locator.Instance.GameSettings.UserBiomes;
            
            ModeChangedEvent?.Invoke(_mode);
            ActiveBiomeIdxChangedEvent?.Invoke(_activeBiomeIdx);
            
            RefreshVoxelHeight();
        }
        
        private void Update()
        {
            #region CONTROLS

            if (Input.GetKeyDown(switchModeKey))
            {
                Mode = (Mode == EMode.Build ? EMode.Destroy : EMode.Build);
            }

            if (Input.GetKeyDown(switchBlockKey))
            {
                ActiveBiomeIdx = (ActiveBiomeIdx + 1) % _userBiomes.Count;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (_voxelHitInfo.voxelInfo != null)
                {
                    switch (Mode)
                    {
                        case EMode.Build:
                            _worldManager.AddVoxelToWorldPos(_buildCube.Center, _userBiomes[ActiveBiomeIdx]);
                            break;
                        case EMode.Destroy:
                            _worldManager.RemoveVoxelOnWorldPos(_destroyCube.Center);
                            break;
                    }
                }
            }
 
            if (Input.GetKeyDown(jumpKey) && _grounded)
            {
                _jumping = true;
                _jumpTime = 0f;
                _grounded = false;
                _gravityVelocity = Vector3.zero;
            }

            #endregion            
            
            #region LOOk
            
            var mousePos = Input.mousePosition;
            var mouseLookDelta = (mousePos - _lastMousePos) * lookSensitivity;
            _lastMousePos = mousePos;

            _lookPitch += mouseLookDelta.y * -1;
            _lookYaw += mouseLookDelta.x;
            _lookPitch = Mathf.Clamp(_lookPitch, -90f, 90f);
            
            lookParent.transform.rotation = Quaternion.Euler(_lookPitch, _lookYaw, 0f);
            
            #endregion
            
            #region INPUT MOVE

            Vector2 inputMovement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            var correctedForward = Vector3.ProjectOnPlane(lookParent.forward, Vector3.up).normalized;
            _moveVelocity = lookParent.right * (inputMovement.x * speed) +
                            correctedForward * (inputMovement.y * speed);

            if (Mathf.Abs(inputMovement.x) > 0 || Mathf.Abs(inputMovement.y) > 0)
                RefreshVoxelHeight();
            
            #endregion
            
            #region JUMP
            
            if (_jumping)
            {
                VoxelInfo voxelAtHeadPos = null;
                if (lookParent.transform.position.y < Locator.Instance.GameSettings.WorldHeight * _voxelSize)
                {
                    voxelAtHeadPos = _worldManager.GetVoxelAtWorldPos(lookParent.transform.position);    
                }
                
                if (voxelAtHeadPos != null)
                {
                    _jumping = false;
                    _jumpVelocity = Vector3.zero;
                }
                else
                {
                    _jumpVelocity = Vector3.up * (jumpCurve.Evaluate(_jumpTime / jumpSpeedDuration) * jumpSpeed);
                    _jumpTime += Time.deltaTime;

                    if (_jumpTime > jumpSpeedDuration)
                    {
                        _jumping = false;
                        _jumpVelocity = Vector3.zero;
                    }
                }
            }

            #endregion

            #region GRAVITY
            
            if (!_grounded)
            {
                _gravityVelocity += Physics.gravity * (Time.deltaTime * gravityScale);
            }
            else
            {
                _gravityVelocity = Vector3.zero;
            }
            
            #endregion

            #region WORLD MOVE & COLLISION

            // move with velocity
            var nextPos = transform.position 
                          + _gravityVelocity * Time.deltaTime 
                          + _moveVelocity * Time.deltaTime 
                          + _jumpVelocity * Time.deltaTime;

            transform.position = ResolveMovePosition(transform.position, nextPos);
            
            // adjust for falling
            if (transform.position.y < _currentVoxelHeight)
            {
                transform.position = transform.position.WithY(_currentVoxelHeight); 
                _grounded = true;
            }

            #endregion

            #region AIM

            var aimRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.5f));
            
            Debug.DrawRay(aimRay.origin, aimRay.direction * 10);

            _buildCube.Enabled = false;
            _destroyCube.Enabled = false;
            
            if (Mode == EMode.Build)
            {
                DetectAndDrawBuildVertex(aimRay);
            }
            else if (Mode == EMode.Destroy)
            {
                DetectAndDrawDestroyVertex(aimRay);
            }

            #endregion


        }

        /// <summary>
        /// Tries to move into target direction.
        /// Can climb steps one voxel high.
        /// Can slide on walls.
        /// Can go through holes.
        /// </summary>
        /// <param name="originalPosition"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        private Vector3 ResolveMovePosition(Vector3 originalPosition, Vector3 targetPosition)
        {
            var targetVoxelHeight = _worldManager.GeVoxelHeightAtWorldPos(targetPosition);
            if (Mathf.Approximately(targetVoxelHeight, _currentVoxelHeight))
                return targetPosition;
            else if (_jumping)
            {
                var voxelAtTargetPos = _worldManager.GetVoxelAtWorldPos(targetPosition);
                if (voxelAtTargetPos != null)
                    return originalPosition.WithY(targetPosition.y);
                else
                    return targetPosition;
            }
            else if (targetVoxelHeight <= _currentVoxelHeight + _voxelSize && targetVoxelHeight > _currentVoxelHeight)
            { // one voxel high step, we can climb that
                _currentVoxelHeight = targetVoxelHeight;
                return targetPosition.WithY(targetVoxelHeight);
            }
            else if (targetVoxelHeight < _currentVoxelHeight)
            { // we're falling
                _currentVoxelHeight = targetVoxelHeight;
                _grounded = false;
                return targetPosition;
            }
            else
            { // even higher step, we can't climb that but it's possible that there is a hole, so we need to check for it
                var potentialMovePos = targetPosition + Vector3.up * (_voxelSize 
                                                                      * 1.01f); // we want to leave room for one voxel that we can actually climb
                Vector3 checkPosition = potentialMovePos; 
                VoxelInfo voxelInfo = null;
                while (checkPosition.y < lookParent.transform.position.y)
                {
                    voxelInfo = _worldManager.GetVoxelAtWorldPos(checkPosition);
                    if (voxelInfo != null)
                        break;
                    
                    checkPosition += Vector3.up *_voxelSize;
                }

                if (voxelInfo == null)
                { // there is indeed a hole
                    return potentialMovePos;
                }
                else
                {
                    return originalPosition.WithY(targetPosition.y);
                    
                    // there is two or more voxel high wall and we can't climb that but want to slide on walls
                    Vector3 slidePosition;
                    Vector3 planeNormal = Vector3.right; // aiming into voxel

                    if (targetPosition.x < _standingVoxelInfo.worldPos.x)
                    {
                        planeNormal = Vector3.right;
                    }
                    else if (targetPosition.x > _standingVoxelInfo.worldPos.x + _voxelSize)
                    {
                        planeNormal = Vector3.left;
                    }
                    else if (targetPosition.z < _standingVoxelInfo.worldPos.z)
                    {
                        planeNormal = Vector3.forward;
                    }
                    else if (targetPosition.z > _standingVoxelInfo.worldPos.z + _voxelSize)
                    {
                        planeNormal = Vector3.back;
                    }
                    
                    slidePosition = Vector3.ProjectOnPlane((targetPosition - originalPosition).WithY(0), planeNormal) + targetPosition;
                    slidePosition += planeNormal * 0.01f;

                    //DebugDraw.Sphere(slidePosition, 0.1f, Color.red);
                    Debug.DrawLine(targetPosition, slidePosition, Color.red);
                    
                    return ResolveMovePosition(originalPosition, slidePosition);
                }
            }
        }
        
        private void RefreshVoxelHeight()
        {
            _currentVoxelHeight = _worldManager.GeVoxelHeightAtWorldPos(transform.position);
            _standingVoxelInfo = _worldManager.GetVoxelAtWorldPos(transform.position + Vector3.down * 0.01f);
            _grounded = false;
        }

        private void DetectAndDrawBuildVertex(Ray ray)
        {
            _voxelHitInfo.voxelInfo = null;
            var hit = _worldManager.GetVoxelRayIntersection(ray, out _voxelHitInfo, maxBuildDistance);

            if (!hit)
            {
                return;
            }
            
            _buildCube.Enabled = true;
            _buildCube.SetPosition(_voxelHitInfo.voxelInfo.worldPos + _voxelHitInfo.normal * _voxelSize);
        }
        
        private void DetectAndDrawDestroyVertex(Ray ray)
        {
            _voxelHitInfo.voxelInfo = null;
            var hit = _worldManager.GetVoxelRayIntersection(ray, out _voxelHitInfo, maxDestroyDistance);

            if (!hit)
            {
                return;
            }
            
            _destroyCube.Enabled = true;
            _destroyCube.SetPosition(_voxelHitInfo.voxelInfo.worldPos);
        }

        private void LateUpdate()
        {
            
        }
    }
}