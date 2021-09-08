using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class GameUI : MonoBehaviour
    {
        [Header("Modes")]
        public CanvasGroup buildIcon;
        public CanvasGroup destroyIcon;
        public float disabledAlpha = 0.3f;

        [Header("Biomes")] 
        public List<CanvasGroup> biomeGraphics;
        
        private CharacterController _characterController;
        
        private void Start()
        {
            _characterController = Locator.Instance.CharacterController;
            _characterController.ModeChangedEvent += CharacterControllerOnModeChangedEvent;
            _characterController.ActiveBiomeIdxChangedEvent += CharacterControllerOnActiveBiomeIdxChangedEvent;

            var userBiomes = Locator.Instance.GameSettings.UserBiomes;
            var biomeUvSize = Locator.Instance.GameSettings.BiomeUVSize;

            for (int i = 0; i < userBiomes.Count; i++)
            {
                var biome = userBiomes[i];

                var rawimg = biomeGraphics[i].GetComponentInChildren<RawImage>();
                rawimg.uvRect = new Rect(biome.uvs.x, biome.uvs.y, biomeUvSize, biomeUvSize);
            }
        }

        private void CharacterControllerOnActiveBiomeIdxChangedEvent(int activeBiomeIdx)
        {
            foreach (var bg in biomeGraphics) bg.alpha = disabledAlpha;
            
            biomeGraphics[activeBiomeIdx].alpha = 1.0f;
        }

        private void CharacterControllerOnModeChangedEvent(CharacterController.EMode mode)
        {
            var buildIconAlpha = disabledAlpha;
            var destroyIconAlpha = disabledAlpha;
            
            switch (mode)
            {
                case CharacterController.EMode.Build:
                    buildIconAlpha = 1.0f;
                    break;
                case CharacterController.EMode.Destroy:
                    destroyIconAlpha = 1.0f;
                    break;
            }

            buildIcon.alpha = buildIconAlpha;
            destroyIcon.alpha = destroyIconAlpha;
        }
    }
}