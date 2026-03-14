using AudioVisualizer.Core.Models;

namespace AudioVisualizer.SampleApp.ViewModels
{
    /// <summary>
    /// スペクトラム計算プロファイル選択コンボボックスに表示する選択肢を表します。
    /// </summary>
    public sealed class SpectrumProfileOption
    {
        #region プロパティ

        /// <summary>
        /// 選択肢が表すスペクトラム計算プロファイルを取得します。
        /// </summary>
        public SpectrumProfile Value { get; }

        /// <summary>
        /// 画面表示用の名称を取得します。
        /// </summary>
        public string DisplayName { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="SpectrumProfileOption"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="value">スペクトラム計算プロファイルです。</param>
        /// <param name="displayName">表示名称です。</param>
        public SpectrumProfileOption(SpectrumProfile value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        #endregion
    }
}
