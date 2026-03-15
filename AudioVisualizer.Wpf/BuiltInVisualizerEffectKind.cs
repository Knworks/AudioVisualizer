namespace AudioVisualizer.Wpf
{
    /// <summary>
    /// AudioVisualizer.Wpf が提供する組込エフェクト種別を表します。
    /// </summary>
    public enum BuiltInVisualizerEffectKind
    {
        #region 定数

        /// <summary>
        /// バー型スペクトラムを描画します。
        /// </summary>
        SpectrumBar,

        /// <summary>
        /// 波形を折れ線で描画します。
        /// </summary>
        WaveformLine,

        #endregion
    }
}
