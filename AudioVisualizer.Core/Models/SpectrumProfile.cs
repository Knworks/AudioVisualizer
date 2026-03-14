namespace AudioVisualizer.Core.Models
{
    /// <summary>
    /// スペクトラム値をバーへ割り当てる際の計算プロファイルを表します。
    /// </summary>
    public enum SpectrumProfile
    {
        #region 列挙型

        /// <summary>
        /// 元のスペクトラム分布をそのまま線形にバーへ割り当てます。
        /// 低域の成分が強い音源では、左側のバーが大きく動きやすい設定です。
        /// </summary>
        Raw = 0,

        /// <summary>
        /// 低域の偏りを抑えつつ、高域側も見やすくなるよう平均化と補正を行います。
        /// 既定表示として推奨する、見た目のバランス重視設定です。
        /// 中域も見やすくしつつ、HighBoost ほど高域を強調しません。
        /// </summary>
        Balanced = 1,

        /// <summary>
        /// 高域側の成分をより強く持ち上げて、右側のバーも大きく動くように補正します。
        /// 派手な動きを優先したい場合の設定です。
        /// </summary>
        HighBoost = 2,

        #endregion
    }
}
