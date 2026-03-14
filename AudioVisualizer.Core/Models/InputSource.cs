namespace AudioVisualizer.Core.Models
{
    #region 列挙型

    /// <summary>
    /// ビジュアライザーが使用する音声入力元を表します。
    /// </summary>
    public enum InputSource
    {
        /// <summary>
        /// システム再生デバイスから再生中の音声を取得します。
        /// </summary>
        SystemOutput = 0,

        /// <summary>
        /// マイク入力デバイスから音声を取得します。
        /// </summary>
        Microphone = 1,
    }

    #endregion
}
