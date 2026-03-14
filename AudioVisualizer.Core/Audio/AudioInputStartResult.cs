namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 音声入力開始処理の結果を表します。
    /// </summary>
    public sealed class AudioInputStartResult
    {
        #region プロパティ

        /// <summary>
        /// 開始処理が成功したかどうかを示す値を取得します。
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// 失敗時のエラーメッセージを取得します。
        /// </summary>
        public string? ErrorMessage { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="AudioInputStartResult"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="succeeded">開始処理が成功したかどうかを示す値です。</param>
        /// <param name="errorMessage">失敗時のエラーメッセージです。</param>
        private AudioInputStartResult(bool succeeded, string? errorMessage)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 成功結果を生成します。
        /// </summary>
        /// <returns>成功を表す開始結果です。</returns>
        public static AudioInputStartResult Success()
        {
            return new AudioInputStartResult(true, null);
        }

        /// <summary>
        /// 失敗結果を生成します。
        /// </summary>
        /// <param name="errorMessage">利用者向けのエラーメッセージです。</param>
        /// <returns>失敗を表す開始結果です。</returns>
        public static AudioInputStartResult Failed(string errorMessage)
        {
            return new AudioInputStartResult(false, errorMessage);
        }

        #endregion
    }
}
