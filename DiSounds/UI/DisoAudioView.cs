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
        [Inject]
        private readonly DiContainer _container = null!;

        [UIComponent("audio-list")]
        protected CustomCellListTableData audioTable = null!;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
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