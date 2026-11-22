using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
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
        /// 描画するバー数から独立した解析点数です。
        /// </summary>
        private const int CaptureAnalysisPointCount = VisualizerSettings.MaximumBarCount;

        /// <summary>
        /// 公開プロパティの既定スペクトラム計算プロファイルです。
        /// </summary>
        private const SpectrumProfile DefaultSpectrumProfile = SpectrumProfile.Balanced;

        #endregion

        #region フィールド

        /// <summary>
        /// 音声入力の開始と停止を担当する入力プロバイダーです。
        /// </summary>
        private IAudioInputProvider? m_AudioInputProvider;

        /// <summary>
        /// 画面に配置されるたびに所有する入力プロバイダーを生成します。
        /// </summary>
        private readonly Func<IAudioInputProvider>? m_CreateAudioInputProvider;

        /// <summary>
        /// コントロールが画面に配置されているかどうかを示します。
        /// </summary>
        private bool m_IsCaptureHostLoaded;

        /// <summary>
        /// 所有する入力プロバイダーへの操作を順番に実行します。
        /// </summary>
        private DispatcherOperation? m_PendingConnectionChange;
        private (InputSource Source, bool UseDefault, string? DeviceId)? m_CaptureConnection;

        private Task m_CaptureOperations = Task.CompletedTask;

        /// <summary>
        /// UI が最後に指示した取得操作の世代です。
        /// </summary>
        private long m_CaptureRequestVersion;

        /// <summary>
        /// 入力プロバイダーへ反映された最後の取得開始の世代です。
        /// </summary>
        private long m_StartedCaptureVersion;

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

        /// <summary>
        /// バックグラウンド到着フレームを集約するための排他制御です。
        /// </summary>
        private readonly object m_PendingFrameSync = new();

        /// <summary>
        /// UI スレッドで未反映の最新フレームです。
        /// </summary>
        private VisualizerFrameEventArgs? m_PendingFrame;

        /// <summary>
        /// UI スレッドへのフレーム反映が予約済みかどうかを示します。
        /// </summary>
        private bool m_IsPendingFrameDispatchQueued;

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
        /// 音声入力を実際に取得しているかどうかを取得します。
        /// </summary>
        public bool IsCapturing => (bool)GetValue(IsCapturingProperty);

        /// <summary>
        /// 最新の音声取得エラーを取得します。
        /// </summary>
        public string? CaptureError => (string?)GetValue(CaptureErrorProperty);

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
        /// 16 ミリ秒あたりに残す割合です。0.0 は補間なし、1.0 は 0.99 として扱います。
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
        /// 現在までに予約された取得操作の完了を取得します。
        /// </summary>
        internal Task CaptureOperations => m_CaptureOperations;

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
                new FrameworkPropertyMetadata(InputSource.SystemOutput, OnConnectionSettingChanged),
                value => value is InputSource source && Enum.IsDefined(source));

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

        private static readonly DependencyPropertyKey IsCapturingPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsCapturing), typeof(bool), typeof(AudioVisualizerControl), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// 実際の音声取得状態の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty IsCapturingProperty = IsCapturingPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey CaptureErrorPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(CaptureError), typeof(string), typeof(AudioVisualizerControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// 音声取得エラーの依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty CaptureErrorProperty = CaptureErrorPropertyKey.DependencyProperty;

        /// <summary>
        /// 音声取得エラーの内容が変化したときに発生します。
        /// </summary>
        public event EventHandler? CaptureErrorChanged;

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
            var defaultForeground = new SolidColorBrush(Color.FromRgb(0x3B, 0xA7, 0xFF));
            defaultForeground.Freeze();
            ForegroundProperty.OverrideMetadata(typeof(AudioVisualizerControl), new FrameworkPropertyMetadata(defaultForeground));
        }

        /// <summary>
        /// <see cref="AudioVisualizerControl"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public AudioVisualizerControl()
            : this(static () => new SystemOutputAudioInputProvider())
        {
        }

        /// <summary>
        /// 画面のライフサイクルに合わせて入力プロバイダーを生成するコントロールを初期化します。
        /// </summary>
        /// <param name="createAudioInputProvider">音声入力プロバイダーの生成処理です。</param>
        internal AudioVisualizerControl(Func<IAudioInputProvider> createAudioInputProvider)
            : this(null, new SpectrumBarEffect(), new BarSpectrumRenderer(), new PolylineRenderer(), new BandLevelMeterRenderer(), createAudioInputProvider)
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
            : this(audioInputProvider, defaultEffect, renderer, polylineRenderer, bandLevelMeterRenderer, null)
        {
        }

        /// <summary>
        /// 描画処理と音声入力の依存関係を設定します。
        /// </summary>
        /// <param name="audioInputProvider">呼び出し元が管理する音声入力プロバイダーです。</param>
        /// <param name="defaultEffect">既定エフェクトです。</param>
        /// <param name="renderer">バー型描画レンダラーです。</param>
        /// <param name="polylineRenderer">折れ線描画レンダラーです。</param>
        /// <param name="bandLevelMeterRenderer">固定帯域メーター描画レンダラーです。</param>
        /// <param name="createAudioInputProvider">コントロールが所有する入力プロバイダーの生成処理です。</param>
        private AudioVisualizerControl(
            IAudioInputProvider? audioInputProvider,
            IVisualizerEffect defaultEffect,
            BarSpectrumRenderer renderer,
            PolylineRenderer polylineRenderer,
            BandLevelMeterRenderer bandLevelMeterRenderer,
            Func<IAudioInputProvider>? createAudioInputProvider)
        {
            m_AudioInputProvider = audioInputProvider;
            m_CreateAudioInputProvider = createAudioInputProvider;
            m_DefaultEffect = defaultEffect ?? throw new ArgumentNullException(nameof(defaultEffect));
            m_BarRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            m_PolylineRenderer = polylineRenderer ?? throw new ArgumentNullException(nameof(polylineRenderer));
            m_BandLevelMeterRenderer = bandLevelMeterRenderer ?? throw new ArgumentNullException(nameof(bandLevelMeterRenderer));
            if (audioInputProvider is not null)
            {
                audioInputProvider.FrameProduced += OnFrameProduced;
            }

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            RefreshRenderBrush();
        }

        #endregion

        #region オーバーライド

        /// <summary>継承やテーマによる Foreground の変更を描画ブラシへ反映します。</summary>
        /// <param name="e">依存関係プロパティの変更内容です。</param>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ForegroundProperty && PrimaryBrush is null)
            {
                RefreshRenderBrush();
                InvalidateVisual();
            }
        }

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
                if (ReferenceEquals(sender, m_AudioInputProvider) && e.IsCurrent && CanApplyCaptureFrame())
                {
                    ApplyFrame(e.Frame);
                }

                return;
            }

            QueuePendingFrame(sender, e);
        }

        /// <summary>
        /// 画面への配置時に音声入力を準備し、有効な設定で取得を開始します。
        /// </summary>
        /// <param name="sender">イベント送信元です。</param>
        /// <param name="e">画面への配置イベントです。</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            m_IsCaptureHostLoaded = true;
            if (IsActive)
            {
                StartCapture();
            }
        }

        /// <summary>
        /// 画面から外されたときに音声取得を停止し、所有する資源を解放します。
        /// </summary>
        /// <param name="sender">イベント送信元です。</param>
        /// <param name="e">画面からの取り外しイベントです。</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            m_IsCaptureHostLoaded = false;
            var provider = m_AudioInputProvider;
            if (provider is null || m_CreateAudioInputProvider is null)
            {
                StopCapture();
                return;
            }

            lock (m_PendingFrameSync)
            {
                m_AudioInputProvider = null;
            }

            provider.FrameProduced -= OnFrameProduced;
            if (provider is SystemOutputAudioInputProvider captureProvider)
            {
                captureProvider.CaptureStatusChanged -= OnCaptureStatusChanged;
            }

            QueueCaptureOperation(() =>
            {
                if (provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else
                {
                    provider.Stop();
                }
            });
            StopCapture();
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

            if (control.m_CreateAudioInputProvider is null)
            {
                control.RestartCapture();
                return;
            }

            control.m_PendingConnectionChange ??= control.Dispatcher.InvokeAsync(() =>
            {
                control.m_PendingConnectionChange = null;
                if (control.IsActive && control.m_IsCaptureHostLoaded &&
                    control.m_CaptureConnection != control.GetConnection())
                {
                    control.RestartCapture();
                }
            }, DispatcherPriority.Background);
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
            control.m_CurrentRenderData = null;
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
            return value is double sensitivity && double.IsFinite(sensitivity) && sensitivity > 0;
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
            return value is int barCount && barCount >= 1 && barCount <= VisualizerSettings.MaximumBarCount;
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
            m_CaptureConnection = GetConnection();
            var settings = CreateCaptureSettings();
            if (settings is null)
            {
                return;
            }

            try
            {
                EnsureAudioInputProvider();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError("{0}", exception);
                UpdateCaptureStatus(false, "音声入力の初期化に失敗しました。");
                return;
            }

            if (m_AudioInputProvider is not { } provider)
            {
                return;
            }

            if (m_CreateAudioInputProvider is null)
            {
                if (!provider.IsCapturing)
                {
                    var result = provider.Start(settings);
                    UpdateCaptureStatus(provider.IsCapturing, result.ErrorMessage);
                }

                return;
            }

            var version = Interlocked.Increment(ref m_CaptureRequestVersion);
            QueueCaptureOperation(() =>
            {
                if (version != Interlocked.Read(ref m_CaptureRequestVersion))
                {
                    return;
                }

                string? error;
                try
                {
                    error = provider.Start(settings).ErrorMessage;
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError("{0}", exception);
                    error = "音声入力の開始に失敗しました。";
                }

                Interlocked.Exchange(ref m_StartedCaptureVersion, version);
                _ = Dispatcher.InvokeAsync(() =>
                {
                    if (ReferenceEquals(m_AudioInputProvider, provider) && IsActive &&
                        version == Interlocked.Read(ref m_CaptureRequestVersion))
                    {
                        if (provider is SystemOutputAudioInputProvider captureProvider)
                        {
                            UpdateCaptureStatus(captureProvider.IsCapturing, captureProvider.CaptureError ?? (captureProvider.IsCapturing ? null : error));
                        }
                        else
                        {
                            UpdateCaptureStatus(provider.IsCapturing, error);
                        }
                    }
                });
            });
        }

        /// <summary>
        /// 配置済みのコントロールが必要とする音声入力プロバイダーを準備します。
        /// </summary>
        private void EnsureAudioInputProvider()
        {
            if (m_AudioInputProvider is not null || !m_IsCaptureHostLoaded || m_CreateAudioInputProvider is null)
            {
                return;
            }

            var provider = m_CreateAudioInputProvider();
            lock (m_PendingFrameSync)
            {
                m_AudioInputProvider = provider;
            }

            provider.FrameProduced += OnFrameProduced;
            if (provider is SystemOutputAudioInputProvider captureProvider)
            {
                captureProvider.CaptureStatusChanged += OnCaptureStatusChanged;
            }
        }

        /// <summary>
        /// 音声入力を停止します。
        /// </summary>
        private void StopCapture()
        {
            m_CaptureConnection = null;
            try
            {
                Interlocked.Increment(ref m_CaptureRequestVersion);
                if (m_AudioInputProvider is { } provider)
                {
                    if (m_CreateAudioInputProvider is not null)
                    {
                        QueueCaptureOperation(provider.Stop);
                    }
                    else
                    {
                        provider.Stop();
                    }
                }
            }
            finally
            {
                UpdateCaptureStatus(false, null);
                ClearCaptureFrame();
            }
        }

        /// <summary>
        /// 所有する入力プロバイダーへの操作を UI スレッドの外で順番に実行します。
        /// </summary>
        /// <param name="operation">取得に対する操作です。</param>
        private void QueueCaptureOperation(Action operation)
        {
            m_CaptureOperations = m_CaptureOperations.ContinueWith(
                _ =>
                {
                    try
                    {
                        operation();
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Trace.TraceError("{0}", exception);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        /// <summary>
        /// 通知時の取得が現在の指示に対応しているかを判定します。
        /// </summary>
        /// <returns>描画や稼働状態を反映できる場合は true です。</returns>
        private bool CanApplyCaptureFrame()
        {
            return m_CreateAudioInputProvider is null ||
                Interlocked.Read(ref m_CaptureRequestVersion) == Interlocked.Read(ref m_StartedCaptureVersion);
        }

        /// <summary>
        /// 取得状態の変更を、現在の入力プロバイダーから UI へ反映します。
        /// </summary>
        /// <param name="sender">状態が変更された入力プロバイダーです。</param>
        /// <param name="e">変更イベントです。</param>
        private void OnCaptureStatusChanged(object? sender, EventArgs e)
        {
            if (sender is not SystemOutputAudioInputProvider provider)
            {
                return;
            }

            _ = Dispatcher.InvokeAsync(() =>
            {
                if (ReferenceEquals(m_AudioInputProvider, provider) && IsActive && CanApplyCaptureFrame())
                {
                    UpdateCaptureStatus(provider.IsCapturing, provider.CaptureError);
                }
            });
        }

        /// <summary>
        /// 実際の取得状態と取得エラーを依存関係プロパティへ反映します。
        /// </summary>
        /// <param name="isCapturing">音声取得中かどうかを示します。</param>
        /// <param name="error">音声取得エラーです。</param>
        private void UpdateCaptureStatus(bool isCapturing, string? error)
        {
            SetValue(IsCapturingPropertyKey, isCapturing);
            if (error is not null)
            {
                ClearCaptureFrame();
            }

            if (CaptureError != error)
            {
                SetValue(CaptureErrorPropertyKey, error);
                CaptureErrorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 保留中のフレームと画面の描画状態を消去します。
        /// </summary>
        private void ClearCaptureFrame()
        {
            lock (m_PendingFrameSync)
            {
                m_PendingFrame = null;
            }

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

        private (InputSource Source, bool UseDefault, string? DeviceId) GetConnection()
        {
            return (InputSource, UseDefaultDevice, UseDefaultDevice ? null : DeviceId);
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
                CaptureAnalysisPointCount);
        }

        /// <summary>
        /// 受信したフレームを保持し、レンダーデータへ変換します。
        /// </summary>
        /// <param name="frame">受信した解析済みフレームです。</param>
        private void ApplyFrame(VisualizerFrame frame)
        {
            if (!IsActive)
            {
                return;
            }

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

            try
            {
                var effect = Effect is { } selected && selected.Metadata.SupportedInputSources.Contains(InputSource)
                    ? selected
                    : m_DefaultEffect;
                var rawRenderData = effect.BuildRenderData(m_CurrentFrame, CreateEffectContext());
                m_CurrentRenderData = ApplySmoothing(m_CurrentRenderData, rawRenderData);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError("{0}", exception);
                m_CurrentRenderData = null;
            }
        }

        /// <summary>
        /// バックグラウンドから到着した最新フレームを UI スレッドへ集約して通知します。
        /// </summary>
        /// <param name="sender">フレームを生成した音声入力プロバイダーです。</param>
        /// <param name="frame">反映対象の最新フレームです。</param>
        private void QueuePendingFrame(object? sender, VisualizerFrameEventArgs frame)
        {
            ArgumentNullException.ThrowIfNull(frame);

            var shouldSchedule = false;
            lock (m_PendingFrameSync)
            {
                if (!ReferenceEquals(sender, m_AudioInputProvider) || !frame.IsCurrent || !CanApplyCaptureFrame())
                {
                    return;
                }

                m_PendingFrame = frame;
                if (!m_IsPendingFrameDispatchQueued)
                {
                    m_IsPendingFrameDispatchQueued = true;
                    shouldSchedule = true;
                }
            }

            if (shouldSchedule)
            {
                _ = Dispatcher.InvokeAsync(ProcessPendingFrames, DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 予約済みの最新フレームを UI スレッドで順次反映します。
        /// </summary>
        private void ProcessPendingFrames()
        {
            try
            {
                VisualizerFrameEventArgs? frame;
                lock (m_PendingFrameSync)
                {
                    frame = m_PendingFrame;
                    m_PendingFrame = null;
                }

                if (frame is { IsCurrent: true } && CanApplyCaptureFrame())
                {
                    ApplyFrame(frame.Frame);
                }
            }
            finally
            {
                bool shouldSchedule;
                lock (m_PendingFrameSync)
                {
                    shouldSchedule = m_PendingFrame is not null;
                    m_IsPendingFrameDispatchQueued = shouldSchedule;
                }

                if (shouldSchedule)
                {
                    _ = Dispatcher.InvokeAsync(ProcessPendingFrames, DispatcherPriority.Background);
                }
            }
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
            if (Smoothing <= 0 || currentRenderData is null || nextRenderData.Timestamp <= currentRenderData.Timestamp)
            {
                return nextRenderData;
            }

            var elapsed = (nextRenderData.Timestamp - currentRenderData.Timestamp).TotalMilliseconds;
            var previousWeight = Math.Pow(Math.Min(Smoothing, 0.99), elapsed / 16.0);

            if (currentRenderData is BarSpectrumRenderData currentBarRenderData &&
                nextRenderData is BarSpectrumRenderData nextBarRenderData)
            {
                return ApplyBarSmoothing(currentBarRenderData, nextBarRenderData, previousWeight);
            }

            if (currentRenderData is PolylineRenderData currentPolylineRenderData &&
                nextRenderData is PolylineRenderData nextPolylineRenderData)
            {
                return ApplyPolylineSmoothing(currentPolylineRenderData, nextPolylineRenderData, previousWeight);
            }

            if (currentRenderData is PeakHoldBarRenderData currentPeakHoldBarRenderData &&
                nextRenderData is PeakHoldBarRenderData nextPeakHoldBarRenderData)
            {
                return ApplyPeakHoldBarSmoothing(currentPeakHoldBarRenderData, nextPeakHoldBarRenderData, previousWeight);
            }

            if (currentRenderData is BandLevelMeterRenderData currentBandLevelMeterRenderData &&
                nextRenderData is BandLevelMeterRenderData nextBandLevelMeterRenderData)
            {
                return ApplyBandLevelMeterSmoothing(currentBandLevelMeterRenderData, nextBandLevelMeterRenderData, previousWeight);
            }

            return nextRenderData;
        }

        /// <summary>
        /// バー描画データへ平滑化を適用します。
        /// </summary>
        /// <param name="currentBarRenderData">現在描画中のバー描画データです。</param>
        /// <param name="previousWeight">前の描画値を残す割合です。</param>
        /// <param name="nextBarRenderData">次に描画するバー描画データです。</param>
        /// <returns>平滑化後のバー描画データです。</returns>
        private static VisualizerRenderData ApplyBarSmoothing(
            BarSpectrumRenderData currentBarRenderData,
            BarSpectrumRenderData nextBarRenderData,
            double previousWeight)
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
                var smoothedHeight = (currentBar.Height * previousWeight) + (nextBar.Height * (1.0 - previousWeight));
                var smoothedLevel = (currentBar.Level * previousWeight) + (nextBar.Level * (1.0 - previousWeight));
                smoothedBars[index] = new BarRenderItem(nextBar.X, nextBar.Width, smoothedHeight, smoothedLevel);
            }

            return new BarSpectrumRenderData(nextBarRenderData.EffectId, nextBarRenderData.Timestamp, smoothedBars);
        }

        /// <summary>
        /// 折れ線描画データへ平滑化を適用します。
        /// </summary>
        /// <param name="currentPolylineRenderData">現在描画中の折れ線描画データです。</param>
        /// <param name="previousWeight">前の描画値を残す割合です。</param>
        /// <param name="nextPolylineRenderData">次に描画する折れ線描画データです。</param>
        /// <returns>平滑化後の折れ線描画データです。</returns>
        private static VisualizerRenderData ApplyPolylineSmoothing(
            PolylineRenderData currentPolylineRenderData,
            PolylineRenderData nextPolylineRenderData,
            double previousWeight)
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
                var smoothedY = (currentPoint.Y * previousWeight) + (nextPoint.Y * (1.0 - previousWeight));
                smoothedPoints[index] = new NormalizedPoint(nextPoint.X, smoothedY);
            }

            return new PolylineRenderData(nextPolylineRenderData.EffectId, nextPolylineRenderData.Timestamp, smoothedPoints);
        }

        /// <summary>
        /// ピーク保持バー描画データへ平滑化を適用します。
        /// バー本体のみを補間し、ピーク保持線はエフェクト側の保持と減衰結果をそのまま使用します。
        /// </summary>
        /// <param name="currentPeakHoldBarRenderData">現在描画中のピーク保持バー描画データです。</param>
        /// <param name="previousWeight">前の描画値を残す割合です。</param>
        /// <param name="nextPeakHoldBarRenderData">次に描画するピーク保持バー描画データです。</param>
        /// <returns>平滑化後のピーク保持バー描画データです。</returns>
        private static VisualizerRenderData ApplyPeakHoldBarSmoothing(
            PeakHoldBarRenderData currentPeakHoldBarRenderData,
            PeakHoldBarRenderData nextPeakHoldBarRenderData,
            double previousWeight)
        {
            var smoothedBars = new BarRenderItem[nextPeakHoldBarRenderData.Bars.Count];
            for (var index = 0; index < nextPeakHoldBarRenderData.Bars.Count; index++)
            {
                var nextBar = nextPeakHoldBarRenderData.Bars[index];
                if (index >= currentPeakHoldBarRenderData.Bars.Count)
                {
                    smoothedBars[index] = nextBar;
                    continue;
                }

                var currentBar = currentPeakHoldBarRenderData.Bars[index];
                var smoothedHeight = (currentBar.Height * previousWeight) + (nextBar.Height * (1.0 - previousWeight));
                var smoothedLevel = (currentBar.Level * previousWeight) + (nextBar.Level * (1.0 - previousWeight));
                smoothedBars[index] = new BarRenderItem(nextBar.X, nextBar.Width, smoothedHeight, smoothedLevel);
            }

            return new PeakHoldBarRenderData(
                nextPeakHoldBarRenderData.EffectId,
                nextPeakHoldBarRenderData.Timestamp,
                smoothedBars,
                nextPeakHoldBarRenderData.PeakMarkers);
        }

        /// <summary>
        /// 固定帯域メーター描画データへ平滑化を適用します。
        /// </summary>
        /// <param name="currentBandLevelMeterRenderData">現在描画中の固定帯域メーター描画データです。</param>
        /// <param name="previousWeight">前の描画値を残す割合です。</param>
        /// <param name="nextBandLevelMeterRenderData">次に描画する固定帯域メーター描画データです。</param>
        /// <returns>平滑化後の固定帯域メーター描画データです。</returns>
        private static VisualizerRenderData ApplyBandLevelMeterSmoothing(
            BandLevelMeterRenderData currentBandLevelMeterRenderData,
            BandLevelMeterRenderData nextBandLevelMeterRenderData,
            double previousWeight)
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
                var smoothedLevel = (currentBand.Level * previousWeight) + (nextBand.Level * (1.0 - previousWeight));
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
                try
                {
                    resettableVisualizerEffect.Reset();
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError("{0}", exception);
                }
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
