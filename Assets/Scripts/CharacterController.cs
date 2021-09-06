using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class CharacterController : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 5;
        public float gravityScale = 1;

        [Header("Camera")] 
        public float lookSensitivity;

        [Header("References")] 
        public Transform lookParent;

        private Vector3 _lastMousePos;
        private float _lookPitch; // x rot
        private float _lookYaw; // y rot

        private bool _grounded;
        private float _currentVoxelHeight;

        private void Start()
        {
            _lastMousePos = Input.mousePosition;
        }
        
        private void Update()
        {
            #region LOOk
            
            var mousePos = Input.mousePosition;
            var mouseLookDelta = (mousePos - _lastMousePos) * lookSensitivity;
            _lastMousePos = mousePos;

            _lookPitch += mouseLookDelta.y * -1;
            _lookYaw += mouseLookDelta.x;
            _lookPitch = Mathf.Clamp(_lookPitch, -90f, 90f);
            
            lookParent.transform.rotation = Quaternion.Euler(_lookPitch, _lookYaw, 0f);
            
            #endregion

            #region GRAVITY

            transform.position += Vector3.down * (Physics.gravity.y * gravityScale);

            #endregion
        }

        private void RefreshVoxelHeight()
        {
            
        }

        private void LateUpdate()
        {
            
        }
    }
}