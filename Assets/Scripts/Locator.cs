using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class Locator : MonoBehaviour
    {
        [SerializeField] private GameSettings gameSettings;

        public GameSettings GameSettings
        {
            get => this.gameSettings;
        }

        private WorldManager _worldManager;
        public WorldManager WorldManager => _worldManager;

        private BiomeManager _biomeManager;
        public BiomeManager BiomeManager => _biomeManager;

        private CharacterController _characterController;
        public CharacterController CharacterController => _characterController;

        [SerializeField]
        private WireCube _buildCube;
        public WireCube BuildCube => _buildCube;
        
        [SerializeField]
        private WireCube _destroyCube;
        public WireCube DestroyCube => _destroyCube;

        [SerializeField] private GameObject _debugToken;

        public GameObject DebugToken
        {
            get => this._debugToken;
        }

        private void Awake()
        {
            _worldManager = FindObjectOfType<WorldManager>();
            _characterController = FindObjectOfType<CharacterController>();
            _biomeManager = new BiomeManager(GameSettings.GeneratedBiomes);
        }

        #region SINGLETON

        private static Locator _instance;
        public static Locator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<Locator>();

                    if (_instance == null)
                    {
                        Debug.LogError("No Locator in the scene.");
                    }
                }

                return _instance;
            }
        }



        #endregion
    }
}