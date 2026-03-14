using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using AudioVisualizer.Core.Models;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// NAudio を使用して Windows の音声デバイスを列挙する内部実装です。
    /// </summary>
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
        private readonly DefaultDeviceNotificationClient m_NotificationClient;

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
            m_NotificationClient = new DefaultDeviceNotificationClient(HandleDefaultDeviceChanged);
            m_DeviceEnumerator.RegisterEndpointNotificationCallback(m_NotificationClient);
        }

        /// <summary>
        /// 通知購読と WASAPI 列挙子を解放します。
        /// </summary>
        public void Dispose()
        {
            m_DeviceEnumerator.UnregisterEndpointNotificationCallback(m_NotificationClient);
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
                var device = devices[index];
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
                var device = m_DeviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
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
        /// <param name="dataFlow">変更対象のデータフローです。</param>
        /// <param name="role">変更対象のロールです。</param>
        /// <param name="defaultDeviceId">新しい既定デバイス識別子です。</param>
        private void HandleDefaultDeviceChanged(DataFlow dataFlow, Role role, string defaultDeviceId)
        {
            if (role != Role.Multimedia)
            {
                return;
            }

            var inputSource = dataFlow switch
            {
                DataFlow.Render => InputSource.SystemOutput,
                DataFlow.Capture => InputSource.Microphone,
                _ => (InputSource?)null,
            };
            if (inputSource is null || string.IsNullOrWhiteSpace(defaultDeviceId))
            {
                return;
            }

            DefaultDeviceChanged?.Invoke(this, new DefaultAudioDeviceChangedEventArgs(inputSource.Value, defaultDeviceId));
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

        #region 内部クラス

        /// <summary>
        /// WASAPI の既定デバイス変更通知だけを中継する内部コールバックです。
        /// </summary>
        private sealed class DefaultDeviceNotificationClient : IMMNotificationClient
        {
            #region フィールド

            /// <summary>
            /// 既定デバイス変更時に呼び出す処理です。
            /// </summary>
            private readonly Action<DataFlow, Role, string> m_OnDefaultDeviceChanged;

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="DefaultDeviceNotificationClient"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="onDefaultDeviceChanged">既定デバイス変更時に呼び出す処理です。</param>
            public DefaultDeviceNotificationClient(Action<DataFlow, Role, string> onDefaultDeviceChanged)
            {
                m_OnDefaultDeviceChanged = onDefaultDeviceChanged ?? throw new ArgumentNullException(nameof(onDefaultDeviceChanged));
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 既定デバイス変更通知を呼び出し元へ中継します。
            /// </summary>
            /// <param name="flow">変更対象のデータフローです。</param>
            /// <param name="role">変更対象のロールです。</param>
            /// <param name="defaultDeviceId">新しい既定デバイス識別子です。</param>
            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                m_OnDefaultDeviceChanged(flow, role, defaultDeviceId);
            }

            /// <summary>
            /// デバイス追加通知は使用しません。
            /// </summary>
            /// <param name="pwstrDeviceId">追加されたデバイス識別子です。</param>
            public void OnDeviceAdded(string pwstrDeviceId)
            {
            }

            /// <summary>
            /// デバイス削除通知は使用しません。
            /// </summary>
            /// <param name="pwstrDeviceId">削除されたデバイス識別子です。</param>
            public void OnDeviceRemoved(string pwstrDeviceId)
            {
            }

            /// <summary>
            /// デバイス状態変更通知は使用しません。
            /// </summary>
            /// <param name="pwstrDeviceId">変更されたデバイス識別子です。</param>
            /// <param name="dwNewState">新しい状態です。</param>
            public void OnDeviceStateChanged(string pwstrDeviceId, DeviceState dwNewState)
            {
            }

            /// <summary>
            /// プロパティ変更通知は使用しません。
            /// </summary>
            /// <param name="pwstrDeviceId">変更されたデバイス識別子です。</param>
            /// <param name="key">変更されたプロパティキーです。</param>
            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
            }

            #endregion
        }

        #endregion
    }
}
