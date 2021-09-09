using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = nameof(Biome), menuName = "GridWorld/Biome")]
    public class Biome : ScriptableObject
    {
        [SerializeField] private float _startsFromHeightNorm;
        public float StartsFromHeightNorm
        {
            get => this._startsFromHeightNorm;
        }

        [Tooltip("Where does texture for this biome starts in the texture of biomes.")]
        [SerializeField] private Vector2 _uvs;

        public Vector2 uvs
        {
            get => this._uvs;
        }
        
        [SerializeField] private float _timeToDestroy;
        public float TimeToDestroy
        {
            get => this._timeToDestroy;
        }
    }
}