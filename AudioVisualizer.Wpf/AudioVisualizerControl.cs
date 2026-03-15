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
    /// 音声入力から生成した解析結果をエフェクトごとに描画する可視化コントロールです。
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

        /// <summary>
        /// 公開プロパティの既定スペクトラム計算プロファイルです。
        /// </summary>
        private const SpectrumProfile DefaultSpectrumProfile = SpectrumProfile.Balanced;

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
        /// バー型レンダーデータを WPF 描画へ変換するレンダラーです。
        /// </summary>
        private readonly BarSpectrumRenderer m_BarRenderer;

        /// <summary>
        /// 折れ線レンダーデータを WPF 描画へ変換するレンダラーです。
        /// </summary>
        private readonly PolylineRenderer m_PolylineRenderer;

        /// <summary>
        /// 固定帯域メーターレンダーデータを WPF 描画へ変換するレンダラーです。
        /// </summary>
        private readonly BandLevelMeterRenderer m_BandLevelMeterRenderer;

        /// <summary>
        /// 直近で受信した解析済みフレームです。
        /// </summary>
        private VisualizerFrame? m_CurrentFrame;

        /// <summary>
        /// 直近で生成したレンダーデータです。
        /// </summary>
        private VisualizerRenderData? m_CurrentRenderData;

        /// <summary>
        /// 直近の描画に使用するブラシです。
        /// </summary>
        private Brush m_CurrentRenderBrush = Brushes.DeepSkyBlue;

        #endregion

        #region プロパティ

        /// <summary>
        /// 音声入力元を取得または設定します。
        /// `SystemOutput` は再生中のシステム音声、`Microphone` は録音入力を表します。
        /// `IsActive = true` の間に変更すると、必要な場合だけ入力を再接続します。
        /// </summary>
        public InputSource InputSource
        {
            get => (InputSource)GetValue(InputSourceProperty);
            set => SetValue(InputSourceProperty, value);
        }

        /// <summary>
        /// 既定デバイスを使用するかどうかを取得または設定します。
        /// `true` の場合は OS の既定デバイスを利用し、`DeviceId` は UI 上の保持値として扱います。
        /// `false` の場合は `DeviceId` に指定した明示デバイスを使用します。
        /// </summary>
        public bool UseDefaultDevice
        {
            get => (bool)GetValue(UseDefaultDeviceProperty);
            set => SetValue(UseDefaultDeviceProperty, value);
        }

        /// <summary>
        /// 明示的に使用する音声デバイス識別子を取得または設定します。
        /// `UseDefaultDevice = false` のときに実際の接続先として使用されます。
        /// 空文字または <see langword="null"/> の場合は明示デバイスが未指定とみなし、開始は行いません。
        /// </summary>
        public string? DeviceId
        {
            get => (string?)GetValue(DeviceIdProperty);
            set => SetValue(DeviceIdProperty, value);
        }

        /// <summary>
        /// 音声取得と可視化を有効化するかどうかを取得または設定します。
        /// `true` にすると現在の入力設定で開始し、`false` にすると停止します。
        /// 設定変更だけを保持したい場合は `false` のまま各プロパティを更新します。
        /// </summary>
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// 利用する可視化エフェクトを取得または設定します。
        /// <see langword="null"/> の場合は既定のバー表示エフェクトを使用します。
        /// エフェクト変更は再接続を行わず、次回描画から反映されます。
        /// </summary>
        public new IVisualizerEffect? Effect
        {
            get => (IVisualizerEffect?)GetValue(EffectProperty);
            set => SetValue(EffectProperty, value);
        }

        /// <summary>
        /// 可視化の感度補正値を取得または設定します。
        /// `1.0` が基準で、`1.0` より大きいほど小さな音でもバーが高く反応しやすくなります。
        /// 例えば `1.6` 前後は動きを強めたい場合、`0.8` 前後は落ち着かせたい場合の目安です。
        /// `0` 以下は無効です。
        /// </summary>
        public double Sensitivity
        {
            get => (double)GetValue(SensitivityProperty);
            set => SetValue(SensitivityProperty, value);
        }

        /// <summary>
        /// 描画平滑化係数を取得または設定します。
        /// `0.0` はほぼ生の反応で、値を大きくするほど前フレームを残してなめらかに見せます。
        /// 例えば `0.2` から `0.4` は俊敏、`0.7` 以上はゆったりした追従感になります。
        /// `0.0` から `1.0` の範囲だけを受け付けます。
        /// </summary>
        public double Smoothing
        {
            get => (double)GetValue(SmoothingProperty);
            set => SetValue(SmoothingProperty, value);
        }

        /// <summary>
        /// バー描画本数を取得または設定します。
        /// 値を増やすほど細かい表示になり、値を減らすほど 1 本ごとの動きが大きく見えやすくなります。
        /// 例えば `24` から `32` は見やすさ重視、`48` 以上は密度重視の設定です。
        /// `1` 以上の整数だけを受け付けます。
        /// </summary>
        public int BarCount
        {
            get => (int)GetValue(BarCountProperty);
            set => SetValue(BarCountProperty, value);
        }

        /// <summary>
        /// スペクトラム値をバーへ割り当てる計算プロファイルを取得または設定します。
        /// `Balanced` は低域の偏りを緩和しつつ中央の反応も見やすくする推奨設定、`Raw` は元の分布、`HighBoost` は右側の動きを強調したい場合の設定です。
        /// 例えば左端だけが大きく動く場合や中央が薄い場合は `Balanced`、さらに派手に動かしたい場合は `HighBoost` が目安です。
        /// 変更時は再接続せず、次回描画から反映します。
        /// </summary>
        public SpectrumProfile SpectrumProfile
        {
            get => (SpectrumProfile)GetValue(SpectrumProfileProperty);
            set => SetValue(SpectrumProfileProperty, value);
        }

        /// <summary>
        /// 主描画色を取得または設定します。
        /// 単色ブラシならバーの基調色として使用し、`SecondaryBrush` を指定した場合はグラデーションの下側色になります。
        /// 未指定時は `Foreground`、さらに未指定なら既定色を使用します。
        /// </summary>
        public Brush? PrimaryBrush
        {
            get => (Brush?)GetValue(PrimaryBrushProperty);
            set => SetValue(PrimaryBrushProperty, value);
        }

        /// <summary>
        /// 補助描画色を取得または設定します。
        /// 指定するとバー上部に向かうグラデーション色として使われ、未指定時は単色表示になります。
        /// 描画色だけを変え、音声入力の再接続は発生しません。
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
        internal Brush CurrentRenderBrush => m_CurrentRenderBrush;

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
        /// スペクトラム計算プロファイルの依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty SpectrumProfileProperty =
            DependencyProperty.Register(
                nameof(SpectrumProfile),
                typeof(SpectrumProfile),
                typeof(AudioVisualizerControl),
                new FrameworkPropertyMetadata(DefaultSpectrumProfile, OnVisualizationSettingChanged),
                IsValidSpectrumProfile);

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
            : this(new SystemOutputAudioInputProvider(), new SpectrumBarEffect(), new BarSpectrumRenderer(), new PolylineRenderer(), new BandLevelMeterRenderer())
        {
        }

        /// <summary>
        /// <see cref="AudioVisualizerControl"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="audioInputProvider">音声入力の開始と停止を担当する入力プロバイダーです。</param>
        /// <param name="defaultEffect">既定で使用するバー型エフェクトです。</param>
        /// <param name="renderer">バー型レンダーデータを WPF 描画へ変換するレンダラーです。</param>
        internal AudioVisualizerControl(IAudioInputProvider audioInputProvider, IVisualizerEffect defaultEffect, BarSpectrumRenderer renderer)
            : this(audioInputProvider, defaultEffect, renderer, new PolylineRenderer(), new BandLevelMeterRenderer())
        {
        }

        /// <summary>
        /// <see cref="AudioVisualizerControl"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="audioInputProvider">音声入力の開始と停止を担当する入力プロバイダーです。</param>
        /// <param name="defaultEffect">既定で使用するバー型エフェクトです。</param>
        /// <param name="renderer">バー型レンダーデータを WPF 描画へ変換するレンダラーです。</param>
        /// <param name="polylineRenderer">折れ線レンダーデータを WPF 描画へ変換するレンダラーです。</param>
        internal AudioVisualizerControl(
            IAudioInputProvider audioInputProvider,
            IVisualizerEffect defaultEffect,
            BarSpectrumRenderer renderer,
            PolylineRenderer polylineRenderer)
            : this(audioInputProvider, defaultEffect, renderer, polylineRenderer, new BandLevelMeterRenderer())
        {
        }

        /// <summary>
        /// テストや差し替え用途向けの依存注入コンストラクタです。
        /// </summary>
        /// <param name="audioInputProvider">音声入力プロバイダーです。</param>
        /// <param name="defaultEffect">既定エフェクトです。</param>
        /// <param name="renderer">バー型描画レンダラーです。</param>
        /// <param name="polylineRenderer">折れ線描画レンダラーです。</param>
        /// <param name="bandLevelMeterRenderer">固定帯域メーター描画レンダラーです。</param>
        internal AudioVisualizerControl(
            IAudioInputProvider audioInputProvider,
            IVisualizerEffect defaultEffect,
            BarSpectrumRenderer renderer,
            PolylineRenderer polylineRenderer,
            BandLevelMeterRenderer bandLevelMeterRenderer)
        {
            m_AudioInputProvider = audioInputProvider ?? throw new ArgumentNullException(nameof(audioInputProvider));
            m_DefaultEffect = defaultEffect ?? throw new ArgumentNullException(nameof(defaultEffect));
            m_BarRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            m_PolylineRenderer = polylineRenderer ?? throw new ArgumentNullException(nameof(polylineRenderer));
            m_BandLevelMeterRenderer = bandLevelMeterRenderer ?? throw new ArgumentNullException(nameof(bandLevelMeterRenderer));
            m_AudioInputProvider.FrameProduced += OnFrameProduced;
            RefreshRenderBrush();
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
            var renderSize = new Size(ActualWidth, ActualHeight);
            if (m_CurrentRenderData is BandLevelMeterRenderData)
            {
                m_BandLevelMeterRenderer.Render(drawingContext, m_CurrentRenderData, renderSize, m_CurrentRenderBrush);
                return;
            }

            if (m_CurrentRenderData is PolylineRenderData)
            {
                m_PolylineRenderer.Render(drawingContext, m_CurrentRenderData, renderSize, m_CurrentRenderBrush);
                return;
            }

            m_BarRenderer.Render(drawingContext, m_CurrentRenderData, renderSize, m_CurrentRenderBrush);
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
            ResetEffectState(e.OldValue as IVisualizerEffect);
            ResetEffectState(e.NewValue as IVisualizerEffect);
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
            control.RefreshRenderBrush();
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
        /// SpectrumProfile の入力値妥当性を検証します。
        /// </summary>
        /// <param name="value">検証対象値です。</param>
        /// <returns>有効値の場合は <see langword="true"/>。</returns>
        private static bool IsValidSpectrumProfile(object value)
        {
            return value is SpectrumProfile spectrumProfile && Enum.IsDefined(spectrumProfile);
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
            ResetEffectState(Effect ?? m_DefaultEffect);
            m_CurrentFrame = null;
            m_CurrentRenderData = null;
            InvalidateVisual();
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
            return new VisualizerEffectContext(InputSource, Sensitivity, Smoothing, BarCount, SpectrumProfile);
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
        /// 現在のブラシ設定から描画用ブラシを再生成して保持します。
        /// </summary>
        private void RefreshRenderBrush()
        {
            m_CurrentRenderBrush = CreateRenderBrush();
        }

        /// <summary>
        /// 同種の描画データ同士を平滑化係数で補間します。
        /// </summary>
        /// <param name="currentRenderData">現在描画中のレンダーデータです。</param>
        /// <param name="nextRenderData">次に描画するレンダーデータです。</param>
        /// <returns>平滑化適用後のレンダーデータです。</returns>
        private VisualizerRenderData ApplySmoothing(VisualizerRenderData? currentRenderData, VisualizerRenderData nextRenderData)
        {
            if (Smoothing <= 0)
            {
                return nextRenderData;
            }

            if (currentRenderData is BarSpectrumRenderData currentBarRenderData &&
                nextRenderData is BarSpectrumRenderData nextBarRenderData)
            {
                return ApplyBarSmoothing(currentBarRenderData, nextBarRenderData);
            }

            if (currentRenderData is PolylineRenderData currentPolylineRenderData &&
                nextRenderData is PolylineRenderData nextPolylineRenderData)
            {
                return ApplyPolylineSmoothing(currentPolylineRenderData, nextPolylineRenderData);
            }

            if (currentRenderData is BandLevelMeterRenderData currentBandLevelMeterRenderData &&
                nextRenderData is BandLevelMeterRenderData nextBandLevelMeterRenderData)
            {
                return ApplyBandLevelMeterSmoothing(currentBandLevelMeterRenderData, nextBandLevelMeterRenderData);
            }

            return nextRenderData;
        }

        /// <summary>
        /// バー描画データへ平滑化を適用します。
        /// </summary>
        /// <param name="currentBarRenderData">現在描画中のバー描画データです。</param>
        /// <param name="nextBarRenderData">次に描画するバー描画データです。</param>
        /// <returns>平滑化後のバー描画データです。</returns>
        private VisualizerRenderData ApplyBarSmoothing(
            BarSpectrumRenderData currentBarRenderData,
            BarSpectrumRenderData nextBarRenderData)
        {
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
        /// 折れ線描画データへ平滑化を適用します。
        /// </summary>
        /// <param name="currentPolylineRenderData">現在描画中の折れ線描画データです。</param>
        /// <param name="nextPolylineRenderData">次に描画する折れ線描画データです。</param>
        /// <returns>平滑化後の折れ線描画データです。</returns>
        private VisualizerRenderData ApplyPolylineSmoothing(
            PolylineRenderData currentPolylineRenderData,
            PolylineRenderData nextPolylineRenderData)
        {
            var smoothedPoints = new NormalizedPoint[nextPolylineRenderData.Points.Count];
            for (var index = 0; index < nextPolylineRenderData.Points.Count; index++)
            {
                var nextPoint = nextPolylineRenderData.Points[index];
                if (index >= currentPolylineRenderData.Points.Count)
                {
                    smoothedPoints[index] = nextPoint;
                    continue;
                }

                var currentPoint = currentPolylineRenderData.Points[index];
                var smoothedY = (currentPoint.Y * Smoothing) + (nextPoint.Y * (1.0 - Smoothing));
                smoothedPoints[index] = new NormalizedPoint(nextPoint.X, smoothedY);
            }

            return new PolylineRenderData(nextPolylineRenderData.EffectId, nextPolylineRenderData.Timestamp, smoothedPoints);
        }

        /// <summary>
        /// 固定帯域メーター描画データへ平滑化を適用します。
        /// </summary>
        /// <param name="currentBandLevelMeterRenderData">現在描画中の固定帯域メーター描画データです。</param>
        /// <param name="nextBandLevelMeterRenderData">次に描画する固定帯域メーター描画データです。</param>
        /// <returns>平滑化後の固定帯域メーター描画データです。</returns>
        private VisualizerRenderData ApplyBandLevelMeterSmoothing(
            BandLevelMeterRenderData currentBandLevelMeterRenderData,
            BandLevelMeterRenderData nextBandLevelMeterRenderData)
        {
            var smoothedBands = new BandLevelMeterItem[nextBandLevelMeterRenderData.Bands.Count];
            for (var index = 0; index < nextBandLevelMeterRenderData.Bands.Count; index++)
            {
                var nextBand = nextBandLevelMeterRenderData.Bands[index];
                if (index >= currentBandLevelMeterRenderData.Bands.Count)
                {
                    smoothedBands[index] = nextBand;
                    continue;
                }

                var currentBand = currentBandLevelMeterRenderData.Bands[index];
                var smoothedLevel = (currentBand.Level * Smoothing) + (nextBand.Level * (1.0 - Smoothing));
                smoothedBands[index] = new BandLevelMeterItem(nextBand.X, nextBand.Width, smoothedLevel, nextBand.Label);
            }

            return new BandLevelMeterRenderData(nextBandLevelMeterRenderData.EffectId, nextBandLevelMeterRenderData.Timestamp, smoothedBands);
        }

        /// <summary>
        /// 状態保持型エフェクトの内部状態を初期化します。
        /// </summary>
        /// <param name="effect">初期化対象のエフェクトです。</param>
        private static void ResetEffectState(IVisualizerEffect? effect)
        {
            if (effect is IResettableVisualizerEffect resettableVisualizerEffect)
            {
                resettableVisualizerEffect.Reset();
            }
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
