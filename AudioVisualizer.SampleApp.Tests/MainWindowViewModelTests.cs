using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Models;
using AudioVisualizer.SampleApp.ViewModels;

namespace AudioVisualizer.SampleApp.Tests
{
    /// <summary>
    /// MainWindowViewModel の入力切替とデバイス選択ロジックを検証します。
    /// </summary>
    [TestFixture]
    public class MainWindowViewModelTests
    {
        #region 公開メソッド

        /// <summary>
        /// MainWindowViewModel が初期化時にシステム再生音デバイス一覧を読み込むことを確認します。
        /// ■入力
        /// ・1. SystemOutput と Microphone のデバイスを返す FakeAudioDeviceService
        /// ■確認内容
        /// ・1. 初期入力種別が SystemOutput になる
        /// ・2. デバイス一覧が SystemOutput 側で初期化される
        /// ・3. 既定デバイスの DeviceId が選択される
        /// ・4. 数値設定が既定値で初期化される
        /// </summary>
        [Test]
        public void Given_DeviceService_When_CreatingViewModel_Then_SystemOutputDevicesAreLoaded()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.SelectedInputSource, Is.EqualTo(InputSource.SystemOutput));
                Assert.That(sut.AvailableDevices.Select(device => device.DeviceId), Is.EqualTo(new[] { "render-1", "render-2" }));
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-1"));
                Assert.That(sut.BarCount, Is.EqualTo(32));
                Assert.That(sut.Sensitivity, Is.EqualTo(3.0).Within(1e-10));
                Assert.That(sut.Smoothing, Is.EqualTo(0.25).Within(1e-10));
                Assert.That(sut.StatusMessage, Is.Empty);
            });
        }

        /// <summary>
        /// MainWindowViewModel が数値設定の変更をそのまま保持できることを確認します。
        /// ■入力
        /// ・1. BarCount = 48
        /// ・2. Sensitivity = 2.4
        /// ・3. Smoothing = 0.55
        /// ■確認内容
        /// ・1. 変更後の数値設定が保持される
        /// ・2. 同じ値の再設定でも状態が破綻しない
        /// </summary>
        [Test]
        public void Given_RenderSettings_When_ChangingNumericProperties_Then_NewValuesAreRetained()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 実行
            sut.BarCount = 48;
            sut.Sensitivity = 2.4;
            sut.Smoothing = 0.55;
            sut.BarCount = 48;
            sut.Sensitivity = 2.4;
            sut.Smoothing = 0.55;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.BarCount, Is.EqualTo(48));
                Assert.That(sut.Sensitivity, Is.EqualTo(2.4).Within(1e-10));
                Assert.That(sut.Smoothing, Is.EqualTo(0.55).Within(1e-10));
            });
        }

        /// <summary>
        /// MainWindowViewModel が入力種別変更で対応デバイス一覧へ切り替わることを確認します。
        /// ■入力
        /// ・1. Microphone デバイスを返す FakeAudioDeviceService
        /// ・2. SelectedInputSource = Microphone
        /// ■確認内容
        /// ・1. デバイス一覧が Microphone 側へ更新される
        /// ・2. 選択デバイスが Microphone の既定デバイスになる
        /// </summary>
        [Test]
        public void Given_InputSourceChanges_When_LoadingDevices_Then_MatchingDeviceListIsDisplayed()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 実行
            sut.SelectedInputSource = InputSource.Microphone;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.AvailableDevices.Select(device => device.DeviceId), Is.EqualTo(new[] { "mic-1", "mic-2" }));
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("mic-1"));
            });
        }

        /// <summary>
        /// MainWindowViewModel が既定デバイス利用解除時に明示デバイス選択を有効化することを確認します。
        /// ■入力
        /// ・1. デバイス一覧を持つ MainWindowViewModel
        /// ・2. UseDefaultDevice = false
        /// ■確認内容
        /// ・1. CanSelectDevice が true になる
        /// ・2. SelectedDeviceId が明示選択可能な値を保持する
        /// </summary>
        [Test]
        public void Given_DefaultDeviceDisabled_When_ExplicitSelectionStarts_Then_DeviceSelectionIsEnabled()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 実行
            sut.UseDefaultDevice = false;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.CanSelectDevice, Is.True);
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-1"));
            });
        }

        /// <summary>
        /// MainWindowViewModel がデバイス未取得時に既定デバイス利用へ戻すことを確認します。
        /// ■入力
        /// ・1. 空一覧を返す FakeAudioDeviceService
        /// ・2. UseDefaultDevice = false
        /// ■確認内容
        /// ・1. UseDefaultDevice が true に戻る
        /// ・2. SelectedDeviceId が null になる
        /// ・3. 状態メッセージが表示される
        /// </summary>
        [Test]
        public void Given_NoDevices_When_DisablingDefaultDevice_Then_DefaultModeIsRestored()
        {
            // 準備
            var sut = new MainWindowViewModel(new FakeAudioDeviceService(Array.Empty<AudioDeviceInfo>(), Array.Empty<AudioDeviceInfo>()));

            // 実行
            sut.UseDefaultDevice = false;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.UseDefaultDevice, Is.True);
                Assert.That(sut.SelectedDeviceId, Is.Null);
                Assert.That(sut.StatusMessage, Is.Not.Empty);
            });
        }

        /// <summary>
        /// MainWindowViewModel がデバイス一覧取得失敗時も操作可能な状態を保つことを確認します。
        /// ■入力
        /// ・1. GetDevices で例外を送出する FakeAudioDeviceService
        /// ・2. StartCommand と StopCommand の実行
        /// ■確認内容
        /// ・1. 状態メッセージが表示される
        /// ・2. UseDefaultDevice が true になる
        /// ・3. Start と Stop の状態切替が継続できる
        /// </summary>
        [Test]
        public void Given_DeviceLoadFailure_When_RefreshingDevices_Then_ViewModelRemainsOperable()
        {
            // 準備
            var sut = new MainWindowViewModel(new FakeAudioDeviceService(Array.Empty<AudioDeviceInfo>(), Array.Empty<AudioDeviceInfo>(), throwOnGetDevices: true));

            // 実行
            sut.StartCommand.Execute(null);
            sut.StopCommand.Execute(null);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.StatusMessage, Is.Not.Empty);
                Assert.That(sut.UseDefaultDevice, Is.True);
                Assert.That(sut.IsActive, Is.False);
            });
        }

        /// <summary>
        /// MainWindowViewModel が再読込後も明示デバイス選択を維持できることを確認します。
        /// ■入力
        /// ・1. 明示デバイスを選択した MainWindowViewModel
        /// ・2. RefreshDevicesCommand 実行
        /// ■確認内容
        /// ・1. SelectedDeviceId が維持される
        /// ・2. RefreshDevicesCommand でデバイス一覧が再取得される
        /// </summary>
        [Test]
        public void Given_ExplicitDeviceSelection_When_RefreshingDevices_Then_SelectedDeviceIsPreserved()
        {
            // 準備
            var deviceService = CreateDeviceService();
            var sut = new MainWindowViewModel(deviceService)
            {
                UseDefaultDevice = false,
                SelectedDeviceId = "render-2",
            };
            deviceService.GetDevicesCallCount = 0;

            // 実行
            sut.RefreshDevicesCommand.Execute(null);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-2"));
                Assert.That(deviceService.GetDevicesCallCount, Is.EqualTo(1));
            });
        }

        /// <summary>
        /// MainWindowViewModel が開始停止コマンドの実行可否を状態に応じて切り替えることを確認します。
        /// ■入力
        /// ・1. StartCommand と StopCommand
        /// ■確認内容
        /// ・1. 初期状態では StartCommand のみ実行可能である
        /// ・2. 開始後は StopCommand のみ実行可能である
        /// </summary>
        [Test]
        public void Given_StartAndStopCommands_When_IsActiveChanges_Then_CanExecuteStateIsUpdated()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 実行
            var canStartBefore = sut.StartCommand.CanExecute(null);
            var canStopBefore = sut.StopCommand.CanExecute(null);
            var canRefresh = sut.RefreshDevicesCommand.CanExecute(null);
            sut.Start();
            var canStartAfter = sut.StartCommand.CanExecute(null);
            var canStopAfter = sut.StopCommand.CanExecute(null);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(canStartBefore, Is.True);
                Assert.That(canStopBefore, Is.False);
                Assert.That(canRefresh, Is.True);
                Assert.That(canStartAfter, Is.False);
                Assert.That(canStopAfter, Is.True);
            });
        }

        /// <summary>
        /// MainWindowViewModel が同じ値の再設定を無視することを確認します。
        /// ■入力
        /// ・1. 初期値と同じ SelectedInputSource
        /// ・2. Start 後に再度 Start する
        /// ■確認内容
        /// ・1. デバイス一覧の再取得が発生しない
        /// ・2. IsActive が維持される
        /// </summary>
        [Test]
        public void Given_SameValueAssignments_When_SettingProperties_Then_NoStateTransitionOccurs()
        {
            // 準備
            var deviceService = CreateDeviceService();
            var sut = new MainWindowViewModel(deviceService);
            deviceService.GetDevicesCallCount = 0;

            // 実行
            sut.SelectedInputSource = InputSource.SystemOutput;
            sut.Start();
            sut.Start();

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(deviceService.GetDevicesCallCount, Is.EqualTo(0));
                Assert.That(sut.IsActive, Is.True);
            });
        }

        /// <summary>
        /// MainWindowViewModel が一覧更新でデバイス消失時に既定デバイス利用へ戻すことを確認します。
        /// ■入力
        /// ・1. SystemOutput のみデバイスを持つ FakeAudioDeviceService
        /// ・2. UseDefaultDevice = false の状態で SelectedInputSource = Microphone
        /// ■確認内容
        /// ・1. UseDefaultDevice が true に戻る
        /// ・2. SelectedDeviceId が null になる
        /// </summary>
        [Test]
        public void Given_ExplicitMode_When_InputSourceChangesToEmptyDeviceList_Then_DefaultModeIsRestored()
        {
            // 準備
            var sut = new MainWindowViewModel(
                new FakeAudioDeviceService(
                    new[] { new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true) },
                    Array.Empty<AudioDeviceInfo>()))
            {
                UseDefaultDevice = false,
            };

            // 実行
            sut.SelectedInputSource = InputSource.Microphone;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.UseDefaultDevice, Is.True);
                Assert.That(sut.SelectedDeviceId, Is.Null);
            });
        }

        /// <summary>
        /// MainWindowViewModel が不正な明示デバイス指定を既定候補へ補正することを確認します。
        /// ■入力
        /// ・1. SelectedDeviceId = "missing-device"
        /// ・2. UseDefaultDevice = false
        /// ■確認内容
        /// ・1. SelectedDeviceId が既定デバイスへ補正される
        /// ・2. CanSelectDevice が true になる
        /// </summary>
        [Test]
        public void Given_InvalidExplicitDevice_When_DisablingDefaultDevice_Then_DefaultCandidateIsSelected()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService())
            {
                SelectedDeviceId = "missing-device",
            };

            // 実行
            sut.UseDefaultDevice = false;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-1"));
                Assert.That(sut.CanSelectDevice, Is.True);
            });
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 複数入力種別のデバイス一覧を返す偽サービスを生成します。
        /// </summary>
        /// <returns>テスト用の偽デバイスサービスです。</returns>
        private static FakeAudioDeviceService CreateDeviceService()
        {
            return new FakeAudioDeviceService(
                new[]
                {
                    new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true),
                    new AudioDeviceInfo("render-2", "Headphones", InputSource.SystemOutput, false),
                },
                new[]
                {
                    new AudioDeviceInfo("mic-1", "Microphone", InputSource.Microphone, true),
                    new AudioDeviceInfo("mic-2", "USB Microphone", InputSource.Microphone, false),
                });
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// テスト用の音声デバイスサービスです。
        /// </summary>
        private sealed class FakeAudioDeviceService : IAudioDeviceService
        {
            #region フィールド

            /// <summary>
            /// システム再生音デバイス一覧です。
            /// </summary>
            private readonly IReadOnlyList<AudioDeviceInfo> m_SystemOutputDevices;

            /// <summary>
            /// マイクデバイス一覧です。
            /// </summary>
            private readonly IReadOnlyList<AudioDeviceInfo> m_MicrophoneDevices;

            /// <summary>
            /// 一覧取得で例外を送出するかどうかを示します。
            /// </summary>
            private readonly bool m_ThrowOnGetDevices;

            #endregion

            #region プロパティ

            /// <summary>
            /// GetDevices の呼び出し回数を取得または設定します。
            /// </summary>
            public int GetDevicesCallCount { get; set; }

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeAudioDeviceService"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="systemOutputDevices">システム再生音デバイス一覧です。</param>
            /// <param name="microphoneDevices">マイクデバイス一覧です。</param>
            /// <param name="throwOnGetDevices">一覧取得で例外を送出する場合は <see langword="true"/> です。</param>
            public FakeAudioDeviceService(
                IReadOnlyList<AudioDeviceInfo> systemOutputDevices,
                IReadOnlyList<AudioDeviceInfo> microphoneDevices,
                bool throwOnGetDevices = false)
            {
                m_SystemOutputDevices = systemOutputDevices;
                m_MicrophoneDevices = microphoneDevices;
                m_ThrowOnGetDevices = throwOnGetDevices;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 指定入力種別のデバイス一覧を返します。
            /// </summary>
            /// <param name="inputSource">取得対象の入力種別です。</param>
            /// <returns>入力種別に対応するデバイス一覧です。</returns>
            public IReadOnlyList<AudioDeviceInfo> GetDevices(InputSource inputSource)
            {
                GetDevicesCallCount++;
                if (m_ThrowOnGetDevices)
                {
                    throw new InvalidOperationException("device load failure");
                }

                return inputSource switch
                {
                    InputSource.SystemOutput => m_SystemOutputDevices,
                    InputSource.Microphone => m_MicrophoneDevices,
                    _ => Array.Empty<AudioDeviceInfo>(),
                };
            }

            /// <summary>
            /// 指定入力種別の既定デバイスを返します。
            /// </summary>
            /// <param name="inputSource">取得対象の入力種別です。</param>
            /// <returns>既定デバイスです。存在しない場合は <see langword="null"/> です。</returns>
            public AudioDeviceInfo? GetDefaultDevice(InputSource inputSource)
            {
                return GetDevices(inputSource).FirstOrDefault(device => device.IsDefault);
            }

            #endregion
        }

        #endregion
    }
}
