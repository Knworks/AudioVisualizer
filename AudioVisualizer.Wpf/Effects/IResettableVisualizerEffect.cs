namespace AudioVisualizer.Wpf.Effects
{
    /// <summary>
    /// 停止や切替時に内部状態をリセットできるエフェクト契約です。
    /// </summary>
    internal interface IResettableVisualizerEffect
    {
        #region 公開メソッド

        /// <summary>
        /// 内部状態を初期化します。
        /// </summary>
        void Reset();

        #endregion
    }
}
