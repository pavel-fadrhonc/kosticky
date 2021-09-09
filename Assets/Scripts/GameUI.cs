using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace DefaultNamespace
{
    public class GameUI : MonoBehaviour
    {
        [Header("Modes")]
        public CanvasGroup buildIcon;
        public CanvasGroup destroyIcon;
        public float disabledAlpha = 0.3f;

        [Header("Saving")] 
        public GameObject SavingText;
        public CanvasGroup SavedText;

        [Header("Biomes")] 
        public List<CanvasGroup> biomeGraphics;

        [Header("Destroy")] 
        public Image DestroyProgressBar;
        public GameObject DestroyBarParent;

        private KeyCode _saveKey;
        private KeyCode _loadKey;

        private WorldManager _worldManager;
        private CharacterController _characterController;

        private readonly float SAVED_TEXT_FADE_TIME = 3.0f;
        
        private void Start()
        {
            _characterController = Locator.Instance.CharacterController;
            _worldManager = Locator.Instance.WorldManager;
            _characterController.ModeChangedEvent += CharacterControllerOnModeChangedEvent;
            _characterController.ActiveBiomeIdxChangedEvent += CharacterControllerOnActiveBiomeIdxChangedEvent;
            _characterController.RemoveTimerUpdateEvent += CharacterControllerOnRemoveTimerUpdateEvent;

            var userBiomes = Locator.Instance.GameSettings.UserBiomes;
            var biomeUvSize = Locator.Instance.GameSettings.BiomeUVSize;

            for (int i = 0; i < userBiomes.Count; i++)
            {
                var biome = userBiomes[i];

                var rawimg = biomeGraphics[i].GetComponentInChildren<RawImage>();
                rawimg.uvRect = new Rect(biome.uvs.x, biome.uvs.y, biomeUvSize, biomeUvSize);
            }

            _saveKey = Locator.Instance.GameSettings.SaveGameKey;
            _loadKey = Locator.Instance.GameSettings.LoadGameKey;
        }

        private void CharacterControllerOnRemoveTimerUpdateEvent(float removeProgress)
        {
            if (removeProgress == 0f)
            {
                DestroyBarParent.gameObject.SetActive(false);
                return;
            }
            
            DestroyBarParent.gameObject.SetActive(true);
            DestroyProgressBar.fillAmount = removeProgress;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_saveKey))
            {
                SavingText.gameObject.SetActive(true);
                _worldManager.SaveWorld();
                SavingText.gameObject.SetActive(false);
                SavedText.gameObject.SetActive(true);
                StartCoroutine(FadeSavedText());
            }
            if (Input.GetKeyDown(_loadKey))
            {
                _worldManager.LoadSavedWorld();
            }
        }

        private IEnumerator FadeSavedText()
        {
            float fadeTime = SAVED_TEXT_FADE_TIME;
            while (fadeTime > 0f)
            {
                SavedText.alpha = fadeTime / SAVED_TEXT_FADE_TIME;
                
                fadeTime -= Time.deltaTime;
                yield return null;
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