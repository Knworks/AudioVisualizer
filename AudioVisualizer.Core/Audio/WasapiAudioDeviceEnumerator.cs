using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AudioVisualizer.Core.Models;
using NAudio.CoreAudioApi;


namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// NAudio を使用して Windows の音声デバイスを列挙する内部実装です。
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ExcludeFromCodeCoverage]
    internal sealed class WasapiAudioDeviceEnumerator : IAudioPlatformDeviceEnumerator, IDisposable
    {
        #region フィールド

        /// <summary>
        /// WASAPI のデバイス列挙と通知購読を担当する列挙子です。
        /// </summary>
        private readonly MMDeviceEnumerator m_DeviceEnumerator = new();

        /// <summary>
        /// 既定デバイス変更通知を受け取る内部コールバックです。
        /// </summary>
        private readonly MMDeviceNotificationClient m_NotificationClient;

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// OS の既定音声デバイスが切り替わったときに発生します。
        /// </summary>
        public event EventHandler<DefaultAudioDeviceChangedEventArgs>? DefaultDeviceChanged;

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="WasapiAudioDeviceEnumerator"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public WasapiAudioDeviceEnumerator()
        {
            m_NotificationClient = m_DeviceEnumerator.CreateNotificationClient(useSynchronizationContext: false);
            m_NotificationClient.DefaultDeviceChanged += HandleDefaultDeviceChanged;
        }

        /// <summary>
        /// 通知購読と WASAPI 列挙子を解放します。
        /// </summary>
        public void Dispose()
        {
            m_NotificationClient.DefaultDeviceChanged -= HandleDefaultDeviceChanged;
            m_NotificationClient.Dispose();
            m_DeviceEnumerator.Dispose();
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 指定した入力種別に対応する利用可能デバイス一覧を取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>OS 依存情報を含むデバイス一覧です。</returns>
        public IReadOnlyList<AudioDeviceSnapshot> GetDevices(InputSource inputSource)
        {
            var dataFlow = ResolveDataFlow(inputSource);
            var devices = m_DeviceEnumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active);
            var snapshots = new List<AudioDeviceSnapshot>(devices.Count);

            for (var index = 0; index < devices.Count; index++)
            {
                using var device = devices[index];
                snapshots.Add(new AudioDeviceSnapshot(device.ID, device.FriendlyName));
            }

            return snapshots;
        }

        /// <summary>
        /// 指定した入力種別に対応する既定デバイスを取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>既定デバイス。存在しない場合は <see langword="null"/>。</returns>
        public AudioDeviceSnapshot? GetDefaultDevice(InputSource inputSource)
        {
            try
            {
                var dataFlow = ResolveDataFlow(inputSource);
                using var device = m_DeviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
                return new AudioDeviceSnapshot(device.ID, device.FriendlyName);
            }
            catch (COMException)
            {
                return null;
            }
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// WASAPI の既定デバイス変更通知を Core 向けのイベントへ変換します。
        /// </summary>
        /// <param name="sender">イベント送信元です。</param>
        /// <param name="e">既定デバイスの変更情報です。</param>
        private void HandleDefaultDeviceChanged(object? sender, DefaultDeviceChangedEventArgs e)
        {
            if (e.Role != Role.Multimedia)
            {
                return;
            }

            var inputSource = e.Flow switch
            {
                DataFlow.Render => InputSource.SystemOutput,
                DataFlow.Capture => InputSource.Microphone,
                _ => (InputSource?)null,
            };
            if (inputSource is null)
            {
                return;
            }

            DefaultDeviceChanged?.Invoke(this, new DefaultAudioDeviceChangedEventArgs(inputSource.Value, string.IsNullOrWhiteSpace(e.DeviceId) ? null : e.DeviceId));
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 入力種別から NAudio の DataFlow を解決します。
        /// </summary>
        /// <param name="inputSource">解決対象の入力種別です。</param>
        /// <returns>対応する DataFlow です。</returns>
        /// <exception cref="ArgumentOutOfRangeException">未対応の入力種別が指定された場合にスローされます。</exception>
        private static DataFlow ResolveDataFlow(InputSource inputSource)
        {
            return inputSource switch
            {
                InputSource.SystemOutput => DataFlow.Render,
                InputSource.Microphone => DataFlow.Capture,
                _ => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。"),
            };
        }

        #endregion
    }
}
