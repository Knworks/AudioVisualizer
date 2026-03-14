using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;
using AudioVisualizer.Wpf.Effects;
using AudioVisualizer.Wpf.Rendering;

namespace AudioVisualizer.Wpf
{
    /// <summary>
    /// 音声入力から生成した解析結果をバー型で表示する最小の可視化コントロールです。
    /// </summary>
    public class AudioVisualizerControl : Control
    {
        #region 定数

        /// <summary>
        /// 最小実装で使用する既定感度です。
        /// </summary>
        private const double DefaultSensitivity = 1.0;

        /// <summary>
        /// 最小実装で使用する既定平滑化係数です。
        /// </summary>
        private const double DefaultSmoothing = 0.5;

        /// <summary>
        /// 最小実装で使用する既定バー本数です。
        /// </summary>
        private const int DefaultBarCount = 32;

        #endregion

        #region フィールド

        /// <summary>
        /// 音声入力の開始と停止を担当する入力プロバイダーです。
        /// </summary>
        private readonly IAudioInputProvider m_AudioInputProvider;

        /// <summary>
        /// 既定で使用するバー型エフェクトです。
        /// </summary>
        private readonly IVisualizerEffect m_DefaultEffect;

        /// <summary>
        /// レンダーデータを WPF 描画へ変換するレンダラーです。
        /// </summary>
        private readonly BarSpectrumRenderer m_Renderer;

        /// <summary>
        /// 直近で受信した解析済みフレームです。
        /// </summary>
        private VisualizerFrame? m_CurrentFrame;

        /// <summary>
        /// 直近で生成したレンダーデータです。
        /// </summary>
        private VisualizerRenderData? m_CurrentRenderData;

        #endregion

        #region プロパティ

        /// <summary>
        /// 音声入力元を取得または設定します。
        /// </summary>
        public InputSource InputSource
        {
            get => (InputSource)GetValue(InputSourceProperty);
            set => SetValue(InputSourceProperty, value);
        }

        /// <summary>
        /// 既定デバイスを使用するかどうかを取得または設定します。
        /// </summary>
        public bool UseDefaultDevice
        {
            get => (bool)GetValue(UseDefaultDeviceProperty);
            set => SetValue(UseDefaultDeviceProperty, value);
        }

        /// <summary>
        /// 音声取得と可視化を有効化するかどうかを取得または設定します。
        /// </summary>
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// 利用する可視化エフェクトを取得または設定します。
        /// </summary>
        public new IVisualizerEffect? Effect
        {
            get => (IVisualizerEffect?)GetValue(EffectProperty);
            set => SetValue(EffectProperty, value);
        }

        /// <summary>
        /// テスト確認用の現在レンダーデータを取得します。
        /// </summary>
        internal VisualizerRenderData? CurrentRenderData => m_CurrentRenderData;

        #endregion

        #region 依存関係プロパティ

        /// <summary>
        /// 音声入力元の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty InputSourceProperty =
            DependencyProperty.Register(
                nameof(InputSource),
                typeof(InputSource),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(InputSource.SystemOutput, OnConnectionSettingChanged));

        /// <summary>
        /// 既定デバイス利用可否の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty UseDefaultDeviceProperty =
            DependencyProperty.Register(
                nameof(UseDefaultDevice),
                typeof(bool),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(true, OnConnectionSettingChanged));

        /// <summary>
        /// 開始・停止状態の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(false, OnIsActiveChanged));

        /// <summary>
        /// エフェクト指定の依存関係プロパティです。
        /// </summary>
        public static new readonly DependencyProperty EffectProperty =
            DependencyProperty.Register(
                nameof(Effect),
                typeof(IVisualizerEffect),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(null, OnEffectChanged));

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="AudioVisualizerControl"/> クラスの静的メンバーを初期化します。
        /// </summary>
        static AudioVisualizerControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AudioVisualizerControl), new FrameworkPropertyMetadata(typeof(AudioVisualizerControl)));
        }

        /// <summary>
        /// <see cref="AudioVisualizerControl"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public AudioVisualizerControl()
            : this(new SystemOutputAudioInputProvider(), new SpectrumBarEffect(), new BarSpectrumRenderer())
        {
        }

        /// <summary>
        /// <see cref="AudioVisualizerControl"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="audioInputProvider">音声入力の開始と停止を担当する入力プロバイダーです。</param>
        /// <param name="defaultEffect">既定で使用するバー型エフェクトです。</param>
        /// <param name="renderer">レンダーデータを WPF 描画へ変換するレンダラーです。</param>
        internal AudioVisualizerControl(IAudioInputProvider audioInputProvider, IVisualizerEffect defaultEffect, BarSpectrumRenderer renderer)
        {
            m_AudioInputProvider = audioInputProvider ?? throw new ArgumentNullException(nameof(audioInputProvider));
            m_DefaultEffect = defaultEffect ?? throw new ArgumentNullException(nameof(defaultEffect));
            m_Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            m_AudioInputProvider.FrameProduced += OnFrameProduced;
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 現在保持しているレンダーデータを描画します。
        /// </summary>
        /// <param name="drawingContext">描画先の DrawingContext です。</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            m_Renderer.Render(drawingContext, m_CurrentRenderData, new Size(ActualWidth, ActualHeight), Foreground ?? Brushes.DeepSkyBlue);
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 音声入力プロバイダーからフレーム通知を受け取ったときの処理です。
        /// </summary>
        /// <param name="sender">イベント送信元です。</param>
        /// <param name="e">解析済みフレームを含むイベントデータです。</param>
        private void OnFrameProduced(object? sender, VisualizerFrameEventArgs e)
        {
            if (Dispatcher.CheckAccess())
            {
                ApplyFrame(e.Frame);
                return;
            }

            _ = Dispatcher.InvokeAsync(() => ApplyFrame(e.Frame));
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 接続条件に関わる依存関係プロパティ変更を処理します。
        /// </summary>
        /// <param name="dependencyObject">変更対象のコントロールです。</param>
        /// <param name="e">変更イベントデータです。</param>
        private static void OnConnectionSettingChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = (AudioVisualizerControl)dependencyObject;
            if (!control.IsActive)
            {
                return;
            }

            control.RestartCapture();
        }

        /// <summary>
        /// 開始・停止状態の変更を処理します。
        /// </summary>
        /// <param name="dependencyObject">変更対象のコントロールです。</param>
        /// <param name="e">変更イベントデータです。</param>
        private static void OnIsActiveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = (AudioVisualizerControl)dependencyObject;
            if ((bool)e.NewValue)
            {
                control.StartCapture();
                return;
            }

            control.StopCapture();
        }

        /// <summary>
        /// エフェクト変更時に現在フレームからレンダーデータを再生成します。
        /// </summary>
        /// <param name="dependencyObject">変更対象のコントロールです。</param>
        /// <param name="e">変更イベントデータです。</param>
        private static void OnEffectChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = (AudioVisualizerControl)dependencyObject;
            control.UpdateRenderData();
            control.InvalidateVisual();
        }

        /// <summary>
        /// 音声入力開始を試行します。
        /// </summary>
        private void StartCapture()
        {
            var settings = CreateSettings();
            if (settings is null)
            {
                return;
            }

            if (m_AudioInputProvider.IsCapturing)
            {
                return;
            }

            _ = m_AudioInputProvider.Start(settings);
        }

        /// <summary>
        /// 音声入力を停止します。
        /// </summary>
        private void StopCapture()
        {
            m_AudioInputProvider.Stop();
        }

        /// <summary>
        /// 現在設定で音声入力を再接続します。
        /// </summary>
        private void RestartCapture()
        {
            StopCapture();
            StartCapture();
        }

        /// <summary>
        /// 現在の依存関係プロパティから Core の設定を生成します。
        /// </summary>
        /// <returns>開始可能な場合の設定。開始不可なら <see langword="null"/>。</returns>
        private VisualizerSettings? CreateSettings()
        {
            if (!UseDefaultDevice)
            {
                return null;
            }

            return new VisualizerSettings(InputSource, null, true, IsActive, DefaultSensitivity, DefaultSmoothing, DefaultBarCount);
        }

        /// <summary>
        /// 受信したフレームを保持し、レンダーデータへ変換します。
        /// </summary>
        /// <param name="frame">受信した解析済みフレームです。</param>
        private void ApplyFrame(VisualizerFrame frame)
        {
            m_CurrentFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            UpdateRenderData();
            InvalidateVisual();
        }

        /// <summary>
        /// 現在のフレームとエフェクトからレンダーデータを再生成します。
        /// </summary>
        private void UpdateRenderData()
        {
            if (m_CurrentFrame is null)
            {
                m_CurrentRenderData = null;
                return;
            }

            var effect = Effect ?? m_DefaultEffect;
            var context = new VisualizerEffectContext(InputSource, DefaultSensitivity, DefaultSmoothing, DefaultBarCount);
            m_CurrentRenderData = effect.BuildRenderData(m_CurrentFrame, context);
        }

        #endregion
    }
}
