using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class WireCube : MonoBehaviour
    {
        public bool Enabled
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public Vector3 Center => new Vector3(transform.position.x + _voxelSize * 0.5f,
            transform.position.y + _voxelSize * 0.5f,
            transform.position.z + _voxelSize * 0.5f);

        private float _voxelSize;
        
        private void Start()
        {
            _voxelSize = Locator.Instance.GameSettings.VoxelSize;

            transform.localScale = new Vector3(_voxelSize, _voxelSize, _voxelSize);
        }

        public void SetPosition(Vector3 pos)
        {
            transform.position = pos;
        }
    }
}