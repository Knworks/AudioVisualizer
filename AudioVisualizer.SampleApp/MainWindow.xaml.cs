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
        /// <summary>
        /// ウィンドウが生成したデバイスサービスです。
        /// </summary>
        private readonly WindowsAudioDeviceService? m_OwnedAudioDeviceService;

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="MainWindow"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public MainWindow()
            : this(new WindowsAudioDeviceService())
        {
        }

        /// <summary>
        /// ウィンドウが所有するデバイスサービスで画面を初期化します。
        /// </summary>
        /// <param name="audioDeviceService">ウィンドウ終了時に破棄するデバイスサービスです。</param>
        private MainWindow(WindowsAudioDeviceService audioDeviceService)
            : this(new MainWindowViewModel(audioDeviceService))
        {
            m_OwnedAudioDeviceService = audioDeviceService;
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

        /// <summary>
        /// ウィンドウが閉じたときに所有するデバイスサービスを解放します。
        /// </summary>
        /// <param name="e">ウィンドウの終了イベントです。</param>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                base.OnClosed(e);
            }
            finally
            {
                m_OwnedAudioDeviceService?.Dispose();
            }
        }
    }
}
