﻿using HMUI;
using TMPro;
using System;
using Zenject;
using UnityEngine;
using IPA.Utilities;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Components.Settings;

namespace DiSounds.UI
{
    [ViewDefinition("DiSounds.Views.info-view.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\info-view.bsml")]
    internal class DisoInfoView : BSMLAutomaticViewController
    {
        public event Action<DisoFlowCoordinator.Action>? ActionClicked;
        private static readonly FieldAccessor<DropdownWithTableView, ModalView>.Accessor ModalView = FieldAccessor<DropdownWithTableView, ModalView>.GetAccessor("_modalView");

        [Inject]
        protected readonly Config _config = null!;

        [Inject]
        protected readonly FadeInOutController fadeInOutController = null!;

        [Inject]
        protected readonly MenuTransitionsHelper menuTransitionsHelper = null!;

        [UIValue("version")]
        protected string Version => $"Mod Version: {_config.Version}";

        #region BSML

        [UIParams]
        protected BSMLParserParams parserParams = null!;

        [UIComponent("info-window")]
        protected Backgroundable infoWindowBackground = null!;

        [UIAction("#post-parse")]
        protected void Parsed()
        {
            infoWindowBackground.background.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            FixModalPos();
        }

        #endregion

        #region Help Window

        [UIAction("faq")]
        protected void FAQ()
        {
            SetModal("" +
                "<b><u>How do I add new sounds?</u></b>\n" +
                "- Add the files into Beat Saber/UserData/Di/Sounds/ and place them into the respective folder\n" +
                "\n" +
                "<b><u>What file types are supported?</u></b>\n" +
                "- Only .ogg files are supported for all audio files.\n"
                , delegate () { parserParams.EmitEvent("hide-yn"); } , TextAlignmentOptions.TopLeft);
            parserParams.EmitEvent("show-yn");
        }

        [UIAction("tutorial")]
        public void Tutorial()
        {
            SetModal("Would you like to start the tutorial?", delegate ()
            {
                ActionClicked?.Invoke(DisoFlowCoordinator.Action.Tutorial);
                parserParams.EmitEvent("hide-yn");
            });
            parserParams.EmitEvent("show-yn");
        }

        [UIAction("reset")]
        protected void Reset()
        {
            SetModal("Are you sure you want to reset your config?", delegate ()
            {
                var config = new Config { Version = _config.Version, FirstTime = _config.FirstTime };
                _config.CopyFrom(config);
                parserParams.EmitEvent("hide-yn");
                fadeInOutController.FadeOut(0.5f, delegate ()
                {
                    menuTransitionsHelper.RestartGame();
                });
            }, TextAlignmentOptions.Center);
            parserParams.EmitEvent("show-yn");
        }

        #endregion

        #region Extras Window

        [UIAction("github")]
        protected void GitHub() => URL("https://github.com/Auros/DiSounds");

        [UIAction("donate")]
        protected void Donate() => URL("https://ko-fi.com/aurosnex");

        protected void URL(string url)
        {
            SetModal($"This will open <color=#4e68de>{url}</color> in your default browser.", delegate ()
            {
                Application.OpenURL(url); 
                parserParams.EmitEvent("hide-yn"); 
            }, TextAlignmentOptions.Center);
            parserParams.EmitEvent("show-yn");
        }

        #endregion

        #region Navigation BSML

        [UIComponent("menu-list")]
        protected readonly DropDownListSetting menuListDropdown = null!;

        private DisoFlowCoordinator.Action _menuValue;
        [UIValue("menu-value")]
        public DisoFlowCoordinator.Action MenuValue
        {
            get => _menuValue;
            set
            {
                _menuValue = value;
                NotifyPropertyChanged();
                ActionClicked?.Invoke(_menuValue);
            }
        }

        [UIValue("menu-options")]
        protected List<object> menuOptions = new List<object>
        {
            DisoFlowCoordinator.Action.None,
            DisoFlowCoordinator.Action.MusicPlayer,
            DisoFlowCoordinator.Action.MenuClicks,
            DisoFlowCoordinator.Action.Intro,
            DisoFlowCoordinator.Action.Outro,
            DisoFlowCoordinator.Action.Results,
            DisoFlowCoordinator.Action.ResultsFC,
            DisoFlowCoordinator.Action.ResultsFailed,
        };

        [UIAction("format-menu-options")]
        protected string FormatMenuOptions(DisoFlowCoordinator.Action action)
        {
            return action switch
            {
                DisoFlowCoordinator.Action.None => "Select Menu",
                DisoFlowCoordinator.Action.MusicPlayer => "Music Player",
                DisoFlowCoordinator.Action.MenuClicks => "Menu Clicks",
                DisoFlowCoordinator.Action.Intro => "Intro",
                DisoFlowCoordinator.Action.Outro => "Outro",
                DisoFlowCoordinator.Action.Results => "Results",
                DisoFlowCoordinator.Action.ResultsFC => "Results (FC)",
                DisoFlowCoordinator.Action.ResultsFailed => "Results (Failed)",
                _ => "UNKNOWN"
            };
        }

        private void FixModalPos()
        {
            var dropdown = menuListDropdown.dropdown as DropdownWithTableView;
            var modal = ModalView(ref dropdown);
            modal.transform.localPosition = new Vector3(modal.transform.localPosition.x, 14.1f, modal.transform.localPosition.z);
        }

        #endregion

        #region Dashboard Window

        [UIValue("music-player")]
        protected bool MusicPlayerEnabled
        {
            get => _config.MusicPlayerEnabled;
            set => _config.MusicPlayerEnabled = value;
        }

        [UIValue("menu-clicks")]
        protected bool MenuClicksEnabled
        {
            get => _config.MenuClicksEnabled;
            set => _config.MenuClicksEnabled = value;
        }

        [UIValue("intro")]
        protected bool Intro
        {
            get => _config.IntroSoundsEnabled;
            set => _config.IntroSoundsEnabled = value;
        }

        [UIValue("outro")]
        protected bool Outro
        {
            get => _config.OutroSoundsEnabled;
            set => _config.OutroSoundsEnabled = value;
        }

        [UIValue("results")]
        protected bool Results
        {
            get => _config.ResultSoundsEnabled;
            set => _config.ResultSoundsEnabled = value;
        }

        [UIValue("results-failed")]
        protected bool ResultsFailed
        {
            get => _config.ResultFailedSoundsEnabled;
            set => _config.ResultFailedSoundsEnabled = value;
        }

        #endregion

        #region YN Modal

        private string _text = "No Content Set";
        [UIValue("yn-text")]
        protected string Text
        {
            get => _text;
            set
            {
                _text = value;
                NotifyPropertyChanged();
            }
        }

        private TextAlignmentOptions _alignment;
        [UIValue("alignment")]
        protected TextAlignmentOptions Alignment
        {
            get => _alignment;
            set
            {
                _alignment = value;
                NotifyPropertyChanged();
            }
        }

        protected Action? didOK;

        [UIAction("did-ok")]
        protected void DidOK()
        {
            didOK?.Invoke();
        }

        public void SetModal(string text, Action? onOK = null, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            Text = text;
            didOK = onOK;
            Alignment = alignment;
        }

        #endregion
    }
}