using UnityEngine;

namespace DefaultNamespace
{
    public class Locator : MonoBehaviour
    {
        [SerializeField] private GameConstants _gameConstants;

        public GameConstants GameConstants
        {
            get => this._gameConstants;
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