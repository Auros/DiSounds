using System;
using Zenject;
using DiSounds.UI;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;

namespace DiSounds.Managers
{
    internal class DisoMusicPlayer : IInitializable, IDisposable
    {
        private readonly DisoPlayerPanel _disoPlayerPanel;
        private readonly GameplaySetupViewController _gameplaySetupViewController;
        private const string _content = "<clickable-text id=\"root\" on-click=\"toggle\" text=\"💿\" align=\"Center\" anchor-pos-x=\"40\" anchor-pos-y=\"0.25\" size-delta-x=\"8\" default-color=\"#ffd630\" />";

        public DisoMusicPlayer(DisoPlayerPanel disoPlayerPanel, GameplaySetupViewController gameplaySetupViewController)
        {
            _disoPlayerPanel = disoPlayerPanel;
            _gameplaySetupViewController = gameplaySetupViewController;
        }

        public void Initialize()
        {
            Setup();
        }

        public void Dispose()
        {

        }

        private void Setup()
        {
            BSMLParser.instance.Parse(_content, _gameplaySetupViewController.transform.Find("HeaderPanel").gameObject, this);
            root.name = "DisoPlayerToggle";
        }

        #region BSML

        [UIComponent("root")]
        protected RectTransform root = null!;

        [UIAction("toggle")]
        protected void Toggle()
        {
            if (_disoPlayerPanel.Enabled)
            {
                _disoPlayerPanel.Hide();
                return;
            }
            _disoPlayerPanel.Show();
        }

        #endregion
    }
}