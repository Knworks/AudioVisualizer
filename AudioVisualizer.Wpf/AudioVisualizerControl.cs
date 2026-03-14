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
        /// 公開プロパティの既定感度です。
        /// </summary>
        private const double DefaultSensitivity = 1.0;

        /// <summary>
        /// 公開プロパティの既定平滑化係数です。
        /// </summary>
        private const double DefaultSmoothing = 0.82;

        /// <summary>
        /// 公開プロパティの既定バー本数です。
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
        /// 明示的に使用する音声デバイス識別子を取得または設定します。
        /// </summary>
        public string? DeviceId
        {
            get => (string?)GetValue(DeviceIdProperty);
            set => SetValue(DeviceIdProperty, value);
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
        /// 可視化の感度補正値を取得または設定します。
        /// </summary>
        public double Sensitivity
        {
            get => (double)GetValue(SensitivityProperty);
            set => SetValue(SensitivityProperty, value);
        }

        /// <summary>
        /// 描画平滑化係数を取得または設定します。
        /// </summary>
        public double Smoothing
        {
            get => (double)GetValue(SmoothingProperty);
            set => SetValue(SmoothingProperty, value);
        }

        /// <summary>
        /// バー描画本数を取得または設定します。
        /// </summary>
        public int BarCount
        {
            get => (int)GetValue(BarCountProperty);
            set => SetValue(BarCountProperty, value);
        }

        /// <summary>
        /// 主描画色を取得または設定します。
        /// </summary>
        public Brush? PrimaryBrush
        {
            get => (Brush?)GetValue(PrimaryBrushProperty);
            set => SetValue(PrimaryBrushProperty, value);
        }

        /// <summary>
        /// 補助描画色を取得または設定します。
        /// </summary>
        public Brush? SecondaryBrush
        {
            get => (Brush?)GetValue(SecondaryBrushProperty);
            set => SetValue(SecondaryBrushProperty, value);
        }

        /// <summary>
        /// テスト確認用の現在レンダーデータを取得します。
        /// </summary>
        internal VisualizerRenderData? CurrentRenderData => m_CurrentRenderData;

        /// <summary>
        /// テスト確認用の現在描画ブラシを取得します。
        /// </summary>
        internal Brush CurrentRenderBrush => CreateRenderBrush();

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
        /// 明示デバイス識別子の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty DeviceIdProperty =
            DependencyProperty.Register(
                nameof(DeviceId),
                typeof(string),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(null, OnConnectionSettingChanged));

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

        /// <summary>
        /// 感度補正値の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty SensitivityProperty =
            DependencyProperty.Register(
                nameof(Sensitivity),
                typeof(double),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(DefaultSensitivity, OnVisualizationSettingChanged),
                IsValidSensitivity);

        /// <summary>
        /// 平滑化係数の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty SmoothingProperty =
            DependencyProperty.Register(
                nameof(Smoothing),
                typeof(double),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(DefaultSmoothing, OnVisualizationSettingChanged),
                IsValidSmoothing);

        /// <summary>
        /// バー本数の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty BarCountProperty =
            DependencyProperty.Register(
                nameof(BarCount),
                typeof(int),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(DefaultBarCount, OnVisualizationSettingChanged),
                IsValidBarCount);

        /// <summary>
        /// 主描画色の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty PrimaryBrushProperty =
            DependencyProperty.Register(
                nameof(PrimaryBrush),
                typeof(Brush),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// 補助描画色の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty SecondaryBrushProperty =
            DependencyProperty.Register(
                nameof(SecondaryBrush),
                typeof(Brush),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(null, OnAppearanceChanged));

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

        #region オーバーライド

        /// <summary>
        /// 現在保持しているレンダーデータを描画します。
        /// </summary>
        /// <param name="drawingContext">描画先の DrawingContext です。</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            m_Renderer.Render(drawingContext, m_CurrentRenderData, new Size(ActualWidth, ActualHeight), CreateRenderBrush());
        }

        /// <summary>
        /// 描画領域サイズ変更時に再描画を要求します。
        /// </summary>
        /// <param name="sizeInfo">サイズ変更情報です。</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
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

            if (e.Property == DeviceIdProperty && control.UseDefaultDevice)
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
        /// 可視化設定変更時に現在フレームのレンダーデータを再生成します。
        /// </summary>
        /// <param name="dependencyObject">変更対象のコントロールです。</param>
        /// <param name="e">変更イベントデータです。</param>
        private static void OnVisualizationSettingChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = (AudioVisualizerControl)dependencyObject;
            control.UpdateRenderData();
            control.InvalidateVisual();
        }

        /// <summary>
        /// 描画色変更時に再描画だけを要求します。
        /// </summary>
        /// <param name="dependencyObject">変更対象のコントロールです。</param>
        /// <param name="e">変更イベントデータです。</param>
        private static void OnAppearanceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = (AudioVisualizerControl)dependencyObject;
            control.InvalidateVisual();
        }

        /// <summary>
        /// Sensitivity の入力値妥当性を検証します。
        /// </summary>
        /// <param name="value">検証対象値です。</param>
        /// <returns>有効値の場合は <see langword="true"/>。</returns>
        private static bool IsValidSensitivity(object value)
        {
            return value is double sensitivity && sensitivity > 0;
        }

        /// <summary>
        /// Smoothing の入力値妥当性を検証します。
        /// </summary>
        /// <param name="value">検証対象値です。</param>
        /// <returns>有効値の場合は <see langword="true"/>。</returns>
        private static bool IsValidSmoothing(object value)
        {
            return value is double smoothing && smoothing >= 0 && smoothing <= 1;
        }

        /// <summary>
        /// BarCount の入力値妥当性を検証します。
        /// </summary>
        /// <param name="value">検証対象値です。</param>
        /// <returns>有効値の場合は <see langword="true"/>。</returns>
        private static bool IsValidBarCount(object value)
        {
            return value is int barCount && barCount > 0;
        }

        /// <summary>
        /// 音声入力開始を試行します。
        /// </summary>
        private void StartCapture()
        {
            var settings = CreateCaptureSettings();
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
        private VisualizerSettings? CreateCaptureSettings()
        {
            if (!UseDefaultDevice && string.IsNullOrWhiteSpace(DeviceId))
            {
                return null;
            }

            return new VisualizerSettings(
                InputSource,
                DeviceId,
                UseDefaultDevice,
                IsActive,
                DefaultSensitivity,
                0.0,
                Math.Max(DefaultBarCount, BarCount));
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
            var rawRenderData = effect.BuildRenderData(m_CurrentFrame, CreateEffectContext());
            m_CurrentRenderData = ApplySmoothing(m_CurrentRenderData, rawRenderData);
        }

        /// <summary>
        /// 現在の公開プロパティからエフェクト用コンテキストを生成します。
        /// </summary>
        /// <returns>現在設定を反映したエフェクト実行コンテキストです。</returns>
        private VisualizerEffectContext CreateEffectContext()
        {
            return new VisualizerEffectContext(InputSource, Sensitivity, Smoothing, BarCount);
        }

        /// <summary>
        /// 現在の描画設定から実際に使用するブラシを生成します。
        /// </summary>
        /// <returns>描画に使用するブラシです。</returns>
        private Brush CreateRenderBrush()
        {
            var primaryBrush = PrimaryBrush ?? Foreground ?? Brushes.DeepSkyBlue;
            if (SecondaryBrush is null)
            {
                return primaryBrush;
            }

            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 1.0),
                EndPoint = new Point(0.5, 0.0),
            };
            gradientBrush.GradientStops.Add(new GradientStop(GetBrushColor(primaryBrush, Colors.DeepSkyBlue), 1.0));
            gradientBrush.GradientStops.Add(new GradientStop(GetBrushColor(SecondaryBrush, Colors.White), 0.0));

            if (gradientBrush.CanFreeze)
            {
                gradientBrush.Freeze();
            }

            return gradientBrush;
        }

        /// <summary>
        /// バー描画データ同士を平滑化係数で補間します。
        /// </summary>
        /// <param name="currentRenderData">現在描画中のレンダーデータです。</param>
        /// <param name="nextRenderData">次に描画するレンダーデータです。</param>
        /// <returns>平滑化適用後のレンダーデータです。</returns>
        private VisualizerRenderData ApplySmoothing(VisualizerRenderData? currentRenderData, VisualizerRenderData nextRenderData)
        {
            if (Smoothing <= 0 ||
                currentRenderData is not BarSpectrumRenderData currentBarRenderData ||
                nextRenderData is not BarSpectrumRenderData nextBarRenderData)
            {
                return nextRenderData;
            }

            var smoothedBars = new BarRenderItem[nextBarRenderData.Bars.Count];
            for (var index = 0; index < nextBarRenderData.Bars.Count; index++)
            {
                var nextBar = nextBarRenderData.Bars[index];
                if (index >= currentBarRenderData.Bars.Count)
                {
                    smoothedBars[index] = nextBar;
                    continue;
                }

                var currentBar = currentBarRenderData.Bars[index];
                var smoothedHeight = (currentBar.Height * Smoothing) + (nextBar.Height * (1.0 - Smoothing));
                var smoothedLevel = (currentBar.Level * Smoothing) + (nextBar.Level * (1.0 - Smoothing));
                smoothedBars[index] = new BarRenderItem(nextBar.X, nextBar.Width, smoothedHeight, smoothedLevel);
            }

            return new BarSpectrumRenderData(nextBarRenderData.EffectId, nextBarRenderData.Timestamp, smoothedBars);
        }

        /// <summary>
        /// ブラシから代表色を抽出します。
        /// </summary>
        /// <param name="brush">色抽出対象のブラシです。</param>
        /// <param name="fallbackColor">色抽出に失敗した場合の代替色です。</param>
        /// <returns>抽出した色です。</returns>
        private static Color GetBrushColor(Brush brush, Color fallbackColor)
        {
            return brush switch
            {
                SolidColorBrush solidColorBrush => solidColorBrush.Color,
                GradientBrush gradientBrush when gradientBrush.GradientStops.Count > 0 => gradientBrush.GradientStops[0].Color,
                _ => fallbackColor,
            };
        }

        #endregion
    }
}
