using MixVerse.Game;
using MixVerse.Game.Model;
using UnityEditor;
using UnityEngine;

namespace MixVerse.EditorTools.Chart
{
    internal interface IChartAssetRepository
    {
        NoteChartAsset CreateAtSavePath();
        void Save(NoteChartAsset asset, ChartGrid grid, float bpm, EditableNoteChart chart);
    }

    internal sealed class ChartAssetRepository : IChartAssetRepository
    {
        private const string DefaultDirectory = "Assets";
        private const string DefaultFileName = "NoteChart";

        public NoteChartAsset CreateAtSavePath()
        {
            var path = EditorUtility.SaveFilePanelInProject("譜面を作る", DefaultFileName, "asset",
                "譜面アセットの保存先を選んでください。", DefaultDirectory);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var asset = ScriptableObject.CreateInstance<NoteChartAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public void Save(NoteChartAsset asset, ChartGrid grid, float bpm, EditableNoteChart chart)
        {
            asset.Write(grid, bpm, chart);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
