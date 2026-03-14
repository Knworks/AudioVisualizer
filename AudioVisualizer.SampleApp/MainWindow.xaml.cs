using System;
using System.Windows;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.SampleApp.ViewModels;

namespace AudioVisualizer.SampleApp
{
    /// <summary>
    /// SampleApp のメインウィンドウです。
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 構築 / 消滅

        /// <summary>
        /// <see cref="MainWindow"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public MainWindow()
            : this(new MainWindowViewModel(new WindowsAudioDeviceService()))
        {
        }

        /// <summary>
        /// <see cref="MainWindow"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="viewModel">画面状態を管理する ViewModel です。</param>
        internal MainWindow(MainWindowViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }

        #endregion
    }
}
