using Zenject;
using UnityEngine;
using DiSounds.Models;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;

namespace DiSounds.UI
{
    [ViewDefinition("DiSounds.Views.audio-view.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\audio-view.bsml")]
    internal class DisoAudioView : BSMLAutomaticViewController
    {
        public bool Built { get; private set; }

        [Inject]
        private readonly DiContainer _container = null!;

        [UIComponent("audio-list")]
        protected CustomCellListTableData audioTable = null!;

        private string _forVal = "for <color=red>undefined</color>";
        [UIValue("for-val")]
        public string For
        {
            get => _forVal;
            set
            {
                _forVal = $"for {value}";
                NotifyPropertyChanged(nameof(For));
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            Built = true;
        }

        public void Present(IEnumerable<DisoAudioPacket> disoAudioPacket)
        {
            audioTable.data.Clear();
            foreach (Transform t in audioTable.tableView.contentTransform)
            {
                Destroy(t.gameObject);
            }
            foreach (var packet in disoAudioPacket)
            {
                _container.Inject(packet);
                audioTable.data.Add(packet);
            }
            audioTable.tableView.ReloadData();
        }
    }
}