using System;
using System.Collections.Generic;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Tests
{
    /// <summary>
    /// WindowsAudioDeviceService のデバイス一覧取得と既定デバイス解決を検証します。
    /// </summary>
    [TestFixture]
    public class WindowsAudioDeviceServiceTests
    {
        #region 公開メソッド

        /// <summary>
        /// WindowsAudioDeviceService の公開コンストラクタでインスタンス生成できることを確認します。
        /// ■入力
        /// ・1. 引数なしコンストラクタ
        /// ■確認内容
        /// ・1. WindowsAudioDeviceService を生成できる
        /// </summary>
        [Test]
        public void Given_PublicConstructor_When_CreatingService_Then_InstanceIsCreated()
        {
            // 準備と実行
            var sut = new WindowsAudioDeviceService();

            // 検証
            Assert.That(sut, Is.Not.Null);
        }

        /// <summary>
        /// WindowsAudioDeviceService が SystemOutput のデバイス一覧を取得できることを確認します。
        /// ■入力
        /// ・1. SystemOutput 用の 2 件のスナップショット
        /// ・2. 1 件目を既定デバイスとする FakeAudioPlatformDeviceEnumerator
        /// ■確認内容
        /// ・1. 2 件の AudioDeviceInfo が返る
        /// ・2. 既定デバイスだけ IsDefault=true になる
        /// </summary>
        [Test]
        public void Given_SystemOutputDevices_When_GetDevices_Then_DeviceInfosAreReturned()
        {
            // 準備
            var enumerator = new FakeAudioPlatformDeviceEnumerator(
                renderDevices: new[]
                {
                    new AudioDeviceSnapshot("render-1", "Speakers"),
                    new AudioDeviceSnapshot("render-2", "HDMI"),
                },
                captureDevices: Array.Empty<AudioDeviceSnapshot>(),
                defaultRenderDevice: new AudioDeviceSnapshot("render-1", "Speakers"),
                defaultCaptureDevice: null);
            var sut = new WindowsAudioDeviceService(enumerator);

            // 実行
            var result = sut.GetDevices(InputSource.SystemOutput);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result[0].DeviceId, Is.EqualTo("render-1"));
                Assert.That(result[0].IsDefault, Is.True);
                Assert.That(result[1].IsDefault, Is.False);
            });
        }

        /// <summary>
        /// WindowsAudioDeviceService が Microphone の既定デバイスを取得できることを確認します。
        /// ■入力
        /// ・1. Microphone 用の既定デバイススナップショット
        /// ■確認内容
        /// ・1. AudioDeviceInfo が返る
        /// ・2. InputSource が Microphone になる
        /// ・3. IsDefault が true になる
        /// </summary>
        [Test]
        public void Given_MicrophoneDefaultDevice_When_GetDefaultDevice_Then_DefaultDeviceInfoIsReturned()
        {
            // 準備
            var enumerator = new FakeAudioPlatformDeviceEnumerator(
                renderDevices: Array.Empty<AudioDeviceSnapshot>(),
                captureDevices: new[] { new AudioDeviceSnapshot("capture-1", "Microphone") },
                defaultRenderDevice: null,
                defaultCaptureDevice: new AudioDeviceSnapshot("capture-1", "Microphone"));
            var sut = new WindowsAudioDeviceService(enumerator);

            // 実行
            var result = sut.GetDefaultDevice(InputSource.Microphone);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.InputSource, Is.EqualTo(InputSource.Microphone));
                Assert.That(result.IsDefault, Is.True);
                Assert.That(result.DeviceId, Is.EqualTo("capture-1"));
            });
        }

        /// <summary>
        /// WindowsAudioDeviceService がデバイス未検出時に空一覧と null を返すことを確認します。
        /// ■入力
        /// ・1. すべて空の FakeAudioPlatformDeviceEnumerator
        /// ■確認内容
        /// ・1. GetDevices が空一覧を返す
        /// ・2. GetDefaultDevice が null を返す
        /// </summary>
        [Test]
        public void Given_NoDevices_When_QueryingDevices_Then_EmptyAndNullAreReturned()
        {
            // 準備
            var enumerator = new FakeAudioPlatformDeviceEnumerator(
                renderDevices: Array.Empty<AudioDeviceSnapshot>(),
                captureDevices: Array.Empty<AudioDeviceSnapshot>(),
                defaultRenderDevice: null,
                defaultCaptureDevice: null);
            var sut = new WindowsAudioDeviceService(enumerator);

            // 実行
            var devices = sut.GetDevices(InputSource.SystemOutput);
            var defaultDevice = sut.GetDefaultDevice(InputSource.SystemOutput);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(devices, Is.Empty);
                Assert.That(defaultDevice, Is.Null);
            });
        }

        /// <summary>
        /// WindowsAudioDeviceService が内部列挙子の既定デバイス変更通知を公開イベントへ転送することを確認します。
        /// ■入力
        /// ・1. 既定デバイス変更通知を発行できる FakeAudioPlatformDeviceEnumerator
        /// ・2. Microphone の既定デバイス変更通知
        /// ■確認内容
        /// ・1. DefaultDeviceChanged が 1 回発火する
        /// ・2. InputSource と DeviceId が通知値と一致する
        /// </summary>
        [Test]
        public void Given_DefaultDeviceChangedByEnumerator_When_ServiceSubscribed_Then_EventIsForwarded()
        {
            // 準備
            var enumerator = new FakeAudioPlatformDeviceEnumerator(
                renderDevices: Array.Empty<AudioDeviceSnapshot>(),
                captureDevices: new[] { new AudioDeviceSnapshot("capture-2", "Headset Mic") },
                defaultRenderDevice: null,
                defaultCaptureDevice: new AudioDeviceSnapshot("capture-1", "Microphone"));
            using var sut = new WindowsAudioDeviceService(enumerator);
            DefaultAudioDeviceChangedEventArgs? raisedEventArgs = null;
            sut.DefaultDeviceChanged += (_, eventArgs) => raisedEventArgs = eventArgs;

            // 実行
            enumerator.RaiseDefaultDeviceChanged(InputSource.Microphone, "capture-2");

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(raisedEventArgs, Is.Not.Null);
                Assert.That(raisedEventArgs!.InputSource, Is.EqualTo(InputSource.Microphone));
                Assert.That(raisedEventArgs.DeviceId, Is.EqualTo("capture-2"));
            });
        }

        /// <summary>
        /// WindowsAudioDeviceService が null 列挙子を拒否することを確認します。
        /// ■入力
        /// ・1. deviceEnumerator = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullEnumerator_When_CreatingService_Then_ThrowsArgumentNullException()
        {
            // 準備と実行と検証
            Assert.That(() => new WindowsAudioDeviceService(null!), Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        /// AudioDeviceSnapshot が不正な入力を拒否することを確認します。
        /// ■入力
        /// ・1. deviceId または displayName に空文字を指定する
        /// ■確認内容
        /// ・1. ArgumentException が送出される
        /// </summary>
        [Test]
        public void Given_InvalidSnapshotInput_When_CreatingSnapshot_Then_ThrowsArgumentException()
        {
            // 準備と実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => new AudioDeviceSnapshot("", "Speakers"), Throws.TypeOf<ArgumentException>());
                Assert.That(() => new AudioDeviceSnapshot("device-1", ""), Throws.TypeOf<ArgumentException>());
            });
        }

        /// <summary>
        /// DefaultAudioDeviceChangedEventArgs が空のデバイス識別子を拒否することを確認します。
        /// ■入力
        /// ・1. deviceId に空文字を指定する
        /// ■確認内容
        /// ・1. ArgumentException が送出される
        /// </summary>
        [Test]
        public void Given_InvalidDefaultDeviceChangedArgsInput_When_Creating_Then_ThrowsArgumentException()
        {
            // 準備と実行と検証
            Assert.That(
                () => new DefaultAudioDeviceChangedEventArgs(InputSource.SystemOutput, string.Empty),
                Throws.TypeOf<ArgumentException>());
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// テスト用のデバイス列挙子です。
        /// </summary>
        private sealed class FakeAudioPlatformDeviceEnumerator : IAudioPlatformDeviceEnumerator
        {
            #region フィールド

            /// <summary>
            /// 再生デバイス一覧です。
            /// </summary>
            private readonly IReadOnlyList<AudioDeviceSnapshot> m_RenderDevices;

            /// <summary>
            /// キャプチャデバイス一覧です。
            /// </summary>
            private readonly IReadOnlyList<AudioDeviceSnapshot> m_CaptureDevices;

            /// <summary>
            /// 既定の再生デバイスです。
            /// </summary>
            private readonly AudioDeviceSnapshot? m_DefaultRenderDevice;

            /// <summary>
            /// 既定のキャプチャデバイスです。
            /// </summary>
            private readonly AudioDeviceSnapshot? m_DefaultCaptureDevice;

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeAudioPlatformDeviceEnumerator"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="renderDevices">再生デバイス一覧です。</param>
            /// <param name="captureDevices">キャプチャデバイス一覧です。</param>
            /// <param name="defaultRenderDevice">既定の再生デバイスです。</param>
            /// <param name="defaultCaptureDevice">既定のキャプチャデバイスです。</param>
            public FakeAudioPlatformDeviceEnumerator(
                IReadOnlyList<AudioDeviceSnapshot> renderDevices,
                IReadOnlyList<AudioDeviceSnapshot> captureDevices,
                AudioDeviceSnapshot? defaultRenderDevice,
                AudioDeviceSnapshot? defaultCaptureDevice)
            {
                m_RenderDevices = renderDevices;
                m_CaptureDevices = captureDevices;
                m_DefaultRenderDevice = defaultRenderDevice;
                m_DefaultCaptureDevice = defaultCaptureDevice;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 指定入力種別に対応するデバイス一覧を返します。
            /// </summary>
            /// <param name="inputSource">取得対象の入力種別です。</param>
            /// <returns>対応するデバイス一覧です。</returns>
            public IReadOnlyList<AudioDeviceSnapshot> GetDevices(InputSource inputSource)
            {
                return inputSource switch
                {
                    InputSource.SystemOutput => m_RenderDevices,
                    InputSource.Microphone => m_CaptureDevices,
                    _ => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。"),
                };
            }

            /// <summary>
            /// 指定入力種別に対応する既定デバイスを返します。
            /// </summary>
            /// <param name="inputSource">取得対象の入力種別です。</param>
            /// <returns>既定デバイス。存在しない場合は <see langword="null"/>。</returns>
            public AudioDeviceSnapshot? GetDefaultDevice(InputSource inputSource)
            {
                return inputSource switch
                {
                    InputSource.SystemOutput => m_DefaultRenderDevice,
                    InputSource.Microphone => m_DefaultCaptureDevice,
                    _ => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。"),
                };
            }

            #endregion

            #region イベントハンドラ

            /// <summary>
            /// 既定音声デバイスが切り替わったときに発生します。
            /// </summary>
            public event EventHandler<DefaultAudioDeviceChangedEventArgs>? DefaultDeviceChanged;

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 任意の既定デバイス変更通知を発生させます。
            /// </summary>
            /// <param name="inputSource">通知対象の入力種別です。</param>
            /// <param name="deviceId">新しい既定デバイス識別子です。</param>
            public void RaiseDefaultDeviceChanged(InputSource inputSource, string deviceId)
            {
                DefaultDeviceChanged?.Invoke(this, new DefaultAudioDeviceChangedEventArgs(inputSource, deviceId));
            }

            #endregion
        }

        #endregion
    }
}
