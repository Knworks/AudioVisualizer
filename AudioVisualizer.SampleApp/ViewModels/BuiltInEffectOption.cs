using AudioVisualizer.Wpf;

namespace AudioVisualizer.SampleApp.ViewModels
{
    /// <summary>
    /// SampleApp で表示する組込エフェクト選択肢を表します。
    /// </summary>
    public sealed class BuiltInEffectOption
    {
        #region プロパティ

        /// <summary>
        /// 選択肢に対応する組込エフェクト種別を取得します。
        /// </summary>
        public BuiltInVisualizerEffectKind Value { get; }

        /// <summary>
        /// 画面へ表示する選択肢名を取得します。
        /// </summary>
        public string DisplayName { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="BuiltInEffectOption"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="value">選択肢に対応する組込エフェクト種別です。</param>
        /// <param name="displayName">画面へ表示する選択肢名です。</param>
        public BuiltInEffectOption(BuiltInVisualizerEffectKind value, string displayName)
        {
            Value = value;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("DisplayName は空白にできません。", nameof(displayName))
                : displayName;
        }

        #endregion
    }
}
