using System.Collections.Generic;
using System.Linq;

namespace DefaultNamespace
{
    public class BiomeManager
    {
        private List<Biome> _orderedBiomes = new List<Biome>();
        private List<float> _biomeNormHeights = new List<float>();
        
        public BiomeManager(IReadOnlyList<Biome> biomes)
        {
            _orderedBiomes.AddRange(biomes);
            _orderedBiomes = _orderedBiomes.OrderBy(b => b.StartsFromHeightNorm).ToList();
            _biomeNormHeights = _orderedBiomes.Select(b => b.StartsFromHeightNorm).ToList();
        }

        public Biome GetBiomeByNormHeight(float normHeight)
        {
            for (int i = 0; i < _biomeNormHeights.Count; i++)
            {
                if (i + 1 == _biomeNormHeights.Count ||
                    _biomeNormHeights[i + 1] > normHeight)
                    return _orderedBiomes[i];
            }

            return null;
        }
    }
}