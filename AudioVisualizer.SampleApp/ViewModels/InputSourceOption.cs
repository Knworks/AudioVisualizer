using AudioVisualizer.Core.Models;

namespace AudioVisualizer.SampleApp.ViewModels
{
    /// <summary>
    /// 入力種別選択コンボボックスに表示する選択肢を表します。
    /// </summary>
    public sealed class InputSourceOption
    {
        #region プロパティ

        /// <summary>
        /// 選択肢が表す入力種別を取得します。
        /// </summary>
        public InputSource Value { get; }

        /// <summary>
        /// 画面表示用の名称を取得します。
        /// </summary>
        public string DisplayName { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="InputSourceOption"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="value">入力種別です。</param>
        /// <param name="displayName">表示名称です。</param>
        public InputSourceOption(InputSource value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        #endregion
    }
}
