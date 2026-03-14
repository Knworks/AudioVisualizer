using System;
using System.Windows.Input;

namespace AudioVisualizer.SampleApp.Commands
{
    /// <summary>
    /// 引数なしアクションを実行する単純なコマンド実装です。
    /// </summary>
    public sealed class DelegateCommand : ICommand
    {
        #region フィールド

        /// <summary>
        /// 実行時に呼び出す処理です。
        /// </summary>
        private readonly Action m_Execute;

        /// <summary>
        /// 実行可否を判定する処理です。
        /// </summary>
        private readonly Func<bool> m_CanExecute;

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="DelegateCommand"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="execute">実行時に呼び出す処理です。</param>
        /// <param name="canExecute">実行可否を返す処理です。省略時は常に実行可能です。</param>
        public DelegateCommand(Action execute, Func<bool>? canExecute = null)
        {
            m_Execute = execute ?? throw new ArgumentNullException(nameof(execute));
            m_CanExecute = canExecute ?? (() => true);
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// コマンドが現在実行可能かどうかを返します。
        /// </summary>
        /// <param name="parameter">コマンド引数です。</param>
        /// <returns>実行可能な場合は <see langword="true"/> です。</returns>
        public bool CanExecute(object? parameter)
        {
            return m_CanExecute();
        }

        /// <summary>
        /// コマンド本体を実行します。
        /// </summary>
        /// <param name="parameter">コマンド引数です。</param>
        public void Execute(object? parameter)
        {
            m_Execute();
        }

        /// <summary>
        /// 実行可否の再評価を要求します。
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 実行可否が変化したときに発生します。
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        #endregion
    }
}
