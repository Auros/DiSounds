using System;
using Zenject;
using Tweening;
using UnityEngine;
using VRUIControls;
using IPA.Utilities;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;

namespace DiSounds.UI
{
    [ViewDefinition("DiSounds.Views.highway-tutorial.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\highway-tutorial.bsml")]
    internal class HighwayTutorialSystem : BSMLAutomaticViewController
    {
        public bool Active { get; private set; }

        private FloatingScreen _floatingScreen = null!;
        private TweeningManager _tweeningManager = null!;

        private int _selectedIndex = 0;
        private readonly List<Blossom> _blossomSet = new List<Blossom>();

        public Action? DidFinish;
        public Action<Blossom>? BlossomHappened;

        private string _text = "";
        [UIValue("text")]
        protected string Text
        {
            get => _text;
            set
            {
                _text = value;
                NotifyPropertyChanged();
            }
        }

        private string _posText = "0 / 0";
        [UIValue("pos")]
        protected string PosText
        {
            get => _posText;
            set
            {
                _posText = value;
                NotifyPropertyChanged();
            }
        }

        private bool _showArrow = false;
        [UIValue("show-arrow")]
        protected bool ShowArrow
        {
            get => _showArrow;
            set
            {
                _showArrow = value;
                NotifyPropertyChanged();
            }
        }

        [Inject]
        protected void Construct(PhysicsRaycasterWithCache raycaster, TweeningManager tweeningManager)
        {
            _tweeningManager = tweeningManager;
            _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(55f, 35f), false, Vector3.zero, Quaternion.identity);
            _floatingScreen.GetComponent<VRGraphicRaycaster>().SetField("_physicsRaycaster", raycaster);
            _floatingScreen.name = $"{nameof(HighwayTutorialSystem)}Screen";
        }

        public void Add(Blossom instruction)
        {
            _blossomSet.Add(instruction);
            instruction.RunEvent = BlossomReceived;
        }

        public void Remove(Blossom instruction)
        {
            _blossomSet.Remove(instruction);
        }

        public void Enable()
        {
            _tweeningManager.KillAllTweens(this);
            if (_blossomSet.Count > 0)
            {
                Active = true;
                _selectedIndex = 0;
                var instr = _blossomSet[0];
                _floatingScreen.ScreenPosition = instr.Pos;
                _floatingScreen.ScreenRotation = instr.Rot;
                _floatingScreen.SetRootViewController(this, AnimationType.In);
                SetPanelData(instr.Text, 1, _blossomSet.Count);
            }
        }

        [UIAction("disable")]
        public void Disable()
        {
            Active = false;
            DidFinish?.Invoke();
            _floatingScreen.SetRootViewController(null, AnimationType.Out);
            _tweeningManager.KillAllTweens(this);
            _blossomSet.Clear();
        }

        public void BlossomReceived(Blossom instruction)
        {
            _tweeningManager.KillAllTweens(this);
            BlossomHappened?.Invoke(instruction);
            ShowArrow = instruction.ShowArrow;

            var currentPos = _floatingScreen.ScreenPosition;
            var currentRot = _floatingScreen.ScreenRotation;

            const float time = 1.5f;
            const EaseType ease = EaseType.OutCubic;

            _tweeningManager.AddTween(new Vector3Tween(currentPos, instruction.Pos, val =>
            {
                _floatingScreen.ScreenPosition = val;
            }, time, ease), this);
            _tweeningManager.AddTween(new Vector3Tween(currentRot.eulerAngles, instruction.Rot.eulerAngles, val =>
            {
                _floatingScreen.ScreenRotation = Quaternion.Euler(val);
            }, time, ease), this);
        }

        [UIAction("move-next")]
        protected void MoveToNextBlossom()
        {
            if (_selectedIndex < _blossomSet.Count - 1)
            {
                var instr = _blossomSet[++_selectedIndex];
                SetPanelData(instr.Text, _selectedIndex + 1, _blossomSet.Count);
                instr.Run();
            }
            else
            {
                Disable();
            }
        }

        [UIAction("move-prev")]
        protected void MoveToPreviousBlossom()
        {
            if (_selectedIndex > 0)
            {
                var instr = _blossomSet[--_selectedIndex];
                SetPanelData(instr.Text, _selectedIndex + 1, _blossomSet.Count);
                instr.Run();
            }
        }

        public void PresentBlossom(Blossom instr)
        {
            SetPanelData(instr.Text, _selectedIndex + 1, _blossomSet.Count);
            _selectedIndex = _blossomSet.IndexOf(instr);
            instr.Run();
        }

        private void SetPanelData(string text, int current, int total)
        {
            Text = text;
            PosText = $"{current} / {total}";
        }

        internal class Blossom
        {
            public string Text { get; }
            public Vector3 Pos { get; set; }
            public Quaternion Rot { get; set; }
            public bool ShowArrow { get; set; }
            protected internal Action<Blossom>? RunEvent { get; set; }
            public Blossom(string text, Vector3 pos, Quaternion rot, bool showArrow = false)
            {
                Text = text;
                Pos = pos;
                Rot = rot;
                ShowArrow = showArrow;
            }

            protected internal void Run()
            {
                RunEvent?.Invoke(this);
            }
        }
    }
}