# 🔍 AudioVisualizer コードレビュー

## 📋 結果

公開コントロールの音声取得ライフサイクル、デバイス切替時の停止・復旧、スペクトラムの周波数欠落を優先して修正する必要がある。通常の自動テストは158件すべて成功した。追加の検証では、既存テストを通過する状態でも音声や表示の問題が起きることを確認した。

| 重要度 | 件数 | 判定基準 |
| --- | ---: | --- |
| 致命的 | 0 | 一般的な利用で広範囲に重大な損失が発生し、回避手段も乏しい |
| 高 | 9 | 対応対象の操作でアプリの停止、意図しない取得継続、機能の停止、主要な表示の欠落につながる |
| 中 | 16 | 特定の入力、設定、描画条件、または利用方法で誤動作や品質低下が起きる |
| 低 | 11 | 局所的な表示、APIの使い勝手、保守・検証手順に影響する |
| 合計 | 36 | 下記に詳細を記載 |

## 🧭 対象と確認方法

- 対象はコミット `fc10f09` と、レビュー開始時の未コミット変更2ファイルを含む作業ツリー。波形の12ミリ秒表示、ゼロ交差への位置合わせ、チャンネル平均は未コミット差分に含まれる。 [解析実装](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L98) [解析テスト](../AudioVisualizer.Core.Tests/AudioFrameAnalyzerTests.cs#L156)
- トラッキング対象のCore、WPF、SampleApp、テスト3プロジェクト、ビルド・依存設定、設計書とREADMEを確認した。Windows音声APIと依存ライブラリの挙動は、Microsoftの仕様と使用中のNAudio 2.3.0のソースで照合した。 [ソリューション](../AudioVisualizer.slnx) [要件](./01_requirements.md) [設計](./02_architect.md)
- **実行確認**は、既存テスト、検証用の一時テスト、またはコマンドで結果を観測した指摘。**コード確認**は、実装や一次資料から成立条件を確認した指摘。デバイスの実機切断、長時間運転、異なるDPIのモニター間移動の実測は今回の範囲では未確認。
- セキュリティ面では、デバイス由来のデータ、公開API、依存関係を確認した。依存関係の脆弱性スキャン結果は下記に記載。外部エフェクトパッケージの読込は現行要件で対象外のため、ロード機構の指摘は計上していない。 [要件](./01_requirements.md#L96)

## 🔴 高

### 🎙️ H01 コントロールを画面から外しても音声取得が続く

**コード確認。** コントロールのコンストラクターは取得サービスを作り、`FrameProduced` を購読する。取得の停止は `IsActive` の変更時に限られ、`Unloaded`、ウィンドウ終了、所有するサービスの破棄に対応する経路がない。ホストが `IsActive = true` のまま別画面へ遷移すると、描画先が外れてもマイクやループバックの取得が継続し、音声ハンドルと通知購読が残り得る。SampleAppも自ら生成したデバイスサービスをウィンドウ終了時に破棄していない。

**対応案。** コントロールが所有する取得サービスの停止・解放を画面のライフサイクルに結び付け、再配置時だけ必要に応じて取得を再開する。画面取り外しと再配置、画面終了をテストする。 [コントロールの生成と購読](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L371) [取得の開始・停止](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L514) [SampleAppのサービス生成](../AudioVisualizer.SampleApp/MainWindow.xaml.cs#L18)

**修正済み。** WPFの `Loaded` / `Unloaded` に合わせて取得を開始・解放し、再配置時に `IsActive` が有効なら新しいプロバイダーで再開する。SampleAppは終了時に所有するデバイスサービスを破棄する。配置・取り外し・再配置の回帰テストを追加し、全159テストが成功した。

### 🔁 H02 OSのデバイス通知内で同期再接続し、停止操作とも競合する

**コード確認。** Windowsの通知コールバックから同期的に `Start → Stop → Dispose` を呼んでおり、NAudio 2.3.0の `Dispose` は取得スレッドの終了を `Join` で待つ。Windowsは通知コールバックを非ブロッキングにするよう求めている。さらに開始・停止・既定変更が同じフィールドを別スレッドから更新するため、OS通知が停止や入力元切替をまたいだ場合、古い設定で再開したり、古いフレームが新しい表示へ混入したりする経路がある。実機でのデッドロック発生は未確認。

**対応案。** OS通知では処理をキューへ入れてすぐ戻る。取得の開始・停止・再接続を直列化し、セッションを識別して古い通知やフレームを破棄する。同時に停止・既定変更した場合を検証する。 [通知の起点](../AudioVisualizer.Core/Audio/WasapiAudioDeviceEnumerator.cs#L113) [再接続とフレーム通知](../AudioVisualizer.Core/Audio/SystemOutputAudioInputProvider.cs#L197) [WPFの保留フレーム](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L626) [Microsoftのコールバック要件](https://learn.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nn-mmdeviceapi-immnotificationclient) [NAudio 2.3.0の破棄処理](https://github.com/naudio/NAudio/blob/v2.3.0/NAudio.Wasapi/WasapiCapture.cs#L303-L315)

**修正済み。** 既定デバイス通知は再接続をスレッドプールへ予約する。取得操作を直列化し、停止や切替をまたぐ通知は要求の識別子で除外する。WPFは処理待ちのフレームを描画する前に取得セッションの有効性を確認する。再接続前のフレームと停止・再開をまたぐ通知を回帰テストし、全160テストが成功した。

### 💥 H03 デバイス切断時にNAudioがプロセスを終了させ得る

**コードと上流資料で確認。** このリポジトリはNAudio 2.3.0の `WasapiCapture` と `WasapiLoopbackCapture` を使う。2.3.0は、取得スレッドの `finally` で呼ぶ `AudioClient.Stop` の例外を処理しない。デバイス無効化などでこの呼び出しが失敗すると、それ以前の例外を捕捉していても、取得スレッドの未処理例外としてホストが終了し得る。上流でもデバイス切替時に報告され、修正されている。今回の環境で実機切断によるプロセス終了は未検証。

**対応案。** 修正を含むバージョンか同等の修正を取り込んだ実装を選び、ループバックとマイクの切断、停止通知、再接続を確認する。依存関係の更新と、H04のアプリ側の停止通知処理を合わせて検証する。 [依存設定](../AudioVisualizer.Core/AudioVisualizer.Core.csproj#L10) [キャプチャ生成](../AudioVisualizer.Core/Audio/WasapiAudioCaptureFactory.cs#L24) [NAudio 2.3.0の取得スレッド](https://github.com/naudio/NAudio/blob/v2.3.0/NAudio.Wasapi/WasapiCapture.cs#L201-L219) [上流の修正PR](https://github.com/naudio/NAudio/pull/1369)

**修正済み。** NAudio.Wasapi 3.3.0に更新し、マイクとループバックを `WasapiRecorder` に移行した。タグ付きソースで切断後の停止・リセット処理が例外を捕捉し、停止通知を発行することを確認した。ビルドは警告0件、全160テスト成功、NuGetの脆弱性スキャンは検出0件。実機抜線は未確認。 [修正版の停止処理](https://github.com/naudio/NAudio/blob/v3.3.0/src/NAudio.Wasapi/WasapiRecorder.cs)

### 🛑 H04 取得失敗を画面に通知せず、自動復旧も途切れる

**実行確認とコード確認。** WPFは `Start` の結果を破棄し、SampleAppの開始ボタンは `IsActive` を設定する。開始に失敗すると、画面ではもう一度開始できず、利用者に理由も伝わらない。また、NAudioの `RecordingStopped` を購読していないため、取得スレッドが停止しても `IsCapturing` と最後の表示が残り得る。既定デバイスが不在になった通知は列挙アダプターで破棄される。既定デバイス変更で再開始に失敗する場合は、設定を消すため、後続の既定変更でも再試行しない。一時テストで、再開始失敗後に有効な既定デバイスへ変更しても取得要求が発行されないことを確認した。デバイス解決自体も `try` の外にあり、列挙例外は失敗結果に変換されない。

**対応案。** 取得の希望状態と実状態を分ける。開始・停止・故障をUIに伝え、既定デバイスの消失・復帰を扱う。取得希望が続く間は次の既定変更で復旧できるようにする。 [開始結果の破棄](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L607) [SampleAppのコマンド状態](../AudioVisualizer.SampleApp/ViewModels/MainWindowViewModel.cs#L434) [開始と再接続](../AudioVisualizer.Core/Audio/SystemOutputAudioInputProvider.cs#L132) [通知のフィルター](../AudioVisualizer.Core/Audio/WasapiAudioDeviceEnumerator.cs#L113) [取得イベントの購読](../AudioVisualizer.Core/Audio/WasapiAudioCaptureFactory.cs#L66) [Microsoftの既定デバイス不在通知](https://learn.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nf-mmdeviceapi-immnotificationclient-ondefaultdevicechanged)

**修正済み。** Coreが取得要求・実際の取得状態・エラーを管理し、既定デバイスの消失や再接続失敗後も次の既定変更で再試行する。WPFは取得状態とエラーを公開し、SampleAppでエラー表示と再試行を可能にした。開始失敗、取得終了、デバイス消失後の復旧を回帰テストし、全163テストが成功した。

### 🧹 H05 開始途中で失敗した取得セッションが解放されない

**実行確認。** `Start` は新規セッションにサンプル通知を登録してから開始し、成功後に初めてフィールドへ代入する。開始処理で例外が出ると、失敗処理が参照するフィールドに新規セッションが入っていない。一時テストで、失敗セッションの `Dispose` が呼ばれず、通知購読が1件残ることを確認した。実機で失敗を繰り返すと、部分的に取得した資源が残り得る。

**対応案。** 開始中のセッションを失敗時にも解除・破棄できるようにする。作成後の開始失敗をテストし、イベント購読解除と破棄まで検証する。 [開始時の所有権移譲](../AudioVisualizer.Core/Audio/SystemOutputAudioInputProvider.cs#L142) [解放処理](../AudioVisualizer.Core/Audio/SystemOutputAudioInputProvider.cs#L241) [既存のファクトリー失敗テスト](../AudioVisualizer.Core.Tests/SystemOutputAudioInputProviderTests.cs#L265)

**修正済み。** セッション生成直後に取得側へ所有権を移し、開始が例外で失敗した場合もサンプルと停止の両方の購読を解除してセッションを破棄する。購読解除と破棄回数の回帰テストを追加し、全164テストが成功した。

### 📉 H06 スペクトラムのバー間にある実音がほぼ消える

**実行確認。** 解析は各バーにつき離散周波数の1点だけを計算する。既定の32点では、1024サンプルの周波数点の大半を飛ばす。48 kHz・モノラルで、振幅0.2の468.75 Hzの最大出力は約0.00000762、同じ振幅の796.875 Hzは約0.400だった。後段のプロファイルで補間しても、未計算の周波数成分は復元できない。サンプルが2点の場合にHann窓の係数が両方ゼロになり、スペクトラムが常にゼロになる境界条件もある。

**対応案。** 完全なスペクトラムから各バーの周波数範囲を集計し、データが短い場合の窓も定義する。固定振幅の周波数掃引で検証する。 [解析する周波数点の選択](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L185) [窓関数](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L220) [WPFでの再配置](../AudioVisualizer.Wpf/Effects/SpectrumBarEffect.cs#L166)

**修正済み。** 正の周波数成分をFFTで一括計算し、各バーに対応する範囲内の最大振幅を採用する。2サンプルの場合は矩形窓を使う。48 kHzの固定振幅8周波数と2サンプル入力で確認し、全173テストが成功した。

### 🎚️ H07 ステレオのスペクトラムを誤った時間軸で解析する

**コード確認。** スペクトラム解析へ左右交互のPCM配列を渡す。左右同一の48 kHz音声でも、各時刻の値が2回ずつ並ぶ列として解析される。例えば1024要素はモノラルの1024時刻に対してステレオでは512時刻となり、同じ周波数でも表示位置がずれる。左右の内容が異なる場合、チャンネル間の差も時間方向の変化として解析される。通常のシステム再生音でバー型の全エフェクトに影響する。

**対応案。** チャンネルを時刻ごとに処理し、チャンネル別のスペクトラムを合成するか、混合する場合の仕様を明記する。同じ音についてモノラル、同相ステレオ、片チャンネルだけのステレオを比較する。 [入力PCMフォーマットの引き渡し](../AudioVisualizer.Core/Audio/WasapiAudioCaptureFactory.cs#L116) [波形とスペクトラムの入力](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L44) [スペクトラム計算](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L185)

**修正済み。** 左右が交互に並ぶPCMをチャンネル単位でFFTし、各周波数ビンの最大振幅を採用する。モノラルに対する同相ステレオ・片側のみ・逆相の周波数と振幅の一致を確認し、全176テストが成功した。

### 🧩 H08 独自エフェクトの例外がUIに伝播し、以降の描画が止まり得る

**コード確認。** 公開エフェクトの `BuildRenderData` をWPF側で直接呼ぶ。設定変更やDispatcher経由のフレーム処理で例外が出たときの回復処理がない。保留キューは空になった場合だけフラグを戻すため、フレーム処理が途中で例外終了すると次のフレームを受けてもDispatcherへの登録が再開しない。設計書は描画データ生成失敗時の空描画を想定している。

**対応案。** エフェクト呼び出しの境界で例外を扱い、失敗内容を記録して空描画または既定エフェクトへ切り替える。Dispatcher処理が中断した場合も、次のフレームを受付可能にする。 [エフェクト呼び出し](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L689) [Dispatcher処理](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L706) [設計上の失敗処理](./02_architect.md#L173)

**修正済み。** エフェクトの描画生成・状態初期化の例外を記録し、描画生成の失敗時は空描画へ戻す。Dispatcherの受付状態は終了時に再設定する。設定変更とバックグラウンドのフレーム通知で例外を発生させ、後続の描画が復旧することを確認した。全177テストが成功した。

### ⏳ H09 フレーム通知のハンドラーで取得を停止すると処理が戻らない

**コードと上流資料で確認。** Coreの `FrameProduced` はNAudioの取得スレッド上で同期的に発行される。利用側が稼働中に通知ハンドラーの中で `Stop` または `Dispose` を呼ぶと、NAudioの破棄処理が現在の取得スレッド自体を `Join` し、終了を無期限に待つ。待機中は取得の停止・解放も完了しない。

**対応案。** フレーム通知と取得の停止・解放を異なる実行経路にし、ハンドラーから停止を要求しても処理が完了することを検証する。 [フレーム通知](../AudioVisualizer.Core/Audio/SystemOutputAudioInputProvider.cs#L222) [停止と解放](../AudioVisualizer.Core/Audio/SystemOutputAudioInputProvider.cs#L168) [WASAPIデータ通知](../AudioVisualizer.Core/Audio/WasapiAudioCaptureFactory.cs#L116) [NAudio 2.3.0のデータ通知と破棄](https://github.com/naudio/NAudio/blob/v2.3.0/NAudio.Wasapi/WasapiCapture.cs#L266-L315) [MicrosoftのThread.Join仕様](https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.join?view=net-10.0#system-threading-thread-join)

**修正済み。** 解析済みフレームは取得スレッドとは別のスレッドから通知し、通知待ちは最新1件に集約する。停止・破棄がサンプル通知の終了を待つ条件で、フレームハンドラーから両操作が完了することを確認した。全177テストが成功した。

## 🟠 中

### 🎧 M01 取得処理が扱えるPCM形式が限定され、未対応の形式は取得中に失敗する

**コード確認。** 32ビットIEEE Floatと16ビット幅の変換しかない。符号付き24・32ビットPCMなどが渡ると初回のデータ通知で例外が出る。16ビットはエンコーディングを確認せずPCMとして解釈する。実機で未対応形式を返すデバイスは未確認。対応フォーマットへの変換、または開始前の明示的な拒否を実装し、PCM変換を実機に依存せず検証できる形にする。 [PCMの変換](../AudioVisualizer.Core/Audio/WasapiAudioCaptureFactory.cs#L139) [NAudio 2.3.0のMixFormat](https://github.com/naudio/NAudio/blob/v2.3.0/NAudio.Wasapi/WasapiCapture.cs#L68-L99)

**修正済み。** 8・16・24・32ビット整数PCMと32ビットIEEE Floatに対応し、エンコードとビット幅を取得開始前に確認する。整数の符号・振幅、Float、未対応形式の拒否を実機に依存しないテストで検証した。全183テストが成功した。

### 📐 M02 波形では感度スライダーが効かない

**実行確認。** WPFは取得時の感度を常に `1.0` で渡し、波形エフェクトは描画時に `context.Sensitivity` を使用しない。検証で感度を1から5へ変えても同じ入力のY座標が0.45のままだった。波形描画の前に感度を一度適用し、符号とクリップを含めてWPFの公開プロパティ経由で確認する。 [キャプチャ設定](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L653) [波形の座標変換](../AudioVisualizer.Wpf/Effects/WaveformLineEffect.cs#L35)

### 🔢 M03 バー数の変更で解析解像度が更新されない

**実行確認。** 解析点数は開始時に `max(32, BarCount)` で固定される。稼働中に52から64へ増やしても、検証では取得設定が52のままだった。波形は実際に渡された点数で固定され、スペクトラムは既存の解析結果を補間して描画本数を増やす。同じ設定でも開始時から指定した場合と開始後に変更した場合で、表示に使えるデータが異なる。解析解像度と描画点数を独立に定義し、一貫した精度で描画する。 [設定変更](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L545) [解析点数の決定](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L653) [設計上の反映時期](./02_architect.md#L77)

### 🌀 M04 波形を間引くと高い音が低い波に見える

**実行確認・未コミットの波形差分。** 細かなPCMを直線補間で間引き、間引く前に表示可能な帯域へ制限する処理がない。既定の32点・12ミリ秒では表示点の間隔から表現できる上限は約1.29 kHzで、2 kHzの入力を描画点に変換すると12ミリ秒に約24周期ある波形が、上向きの交差7回を含む別の形に見えた。描画密度に合わせて帯域を制限するか、振幅の包絡を描いて高域の存在が伝わるようにする。 [表示窓](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L23) [間引き処理](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L139)

### ↔️ M05 逆相のステレオ音が波形で無音になる

**実行確認・未コミットの波形差分。** 同時刻の左右を平均するため、左右に振幅0.2の逆相波を与えると全表示点がゼロになる。ピーク値は0.2だった。ステレオの音が存在することを波形で表すなら、表示するチャンネルの選択、両チャンネル表示、または振幅の範囲表示を検討する。モノラル合成を仕様にする場合は、相殺が起きることを公開契約に記載する。 [チャンネル平均](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L120)

### ⏱️ M06 取得ブロックの長さと単発音の位置で波形の表示時間が変わる

**コード確認・未コミットの波形差分。** 12ミリ秒の表示窓を取得ブロック単体から切り出す。短いブロックでは利用可能なデータを横幅全体に伸ばし、長いブロックでは選択窓から外れた単発のピークを表示しない。ゼロ交差に合わせる場合、直近の最大20ミリ秒分を後ろ側から外す場合もある。連続したモノラルの履歴から時刻基準で切り出し、短いブロックが連続する場合と単発音の前後を検証する。 [ブロックごとの解析](../AudioVisualizer.Core/Audio/SystemOutputAudioInputProvider.cs#L222) [窓と探索範囲](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L110)

### 📻 M07 独自エフェクトの対応入力種別が尊重されない

**コード確認。** 設計書は、現在の入力に対応しないエフェクトを既定のスペクトラムへ置き換えると定めている。実装は `Effect` が `null` の場合だけ既定へ切り替え、`Metadata.SupportedInputSources` を参照しない。例えばシステム再生音専用のエフェクトを選択したままマイクへ変更すると、マイクのフレームがそのエフェクトに渡る。入力種別とエフェクトを変更したときに適用可否を再判定する。 [エフェクトの選択](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L689) [対応エフェクトの設計](./02_architect.md#L170)

### 🔀 M08 エフェクト切替時に旧エフェクトの描画値が混ざる

**コード確認。** 切替処理はエフェクト内部の状態をリセットするが、直前の描画データを残す。平滑化は型が同じかどうかだけで判定するため、Spectrum BarとMirror Barを切り替えると旧エフェクトのバーの高さが新エフェクトへ混ざる。既定の平滑化設定では切替直後の混入が大きい。描画の対応関係が変わる場合は平滑化の履歴を破棄する。 [切替処理](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L531) [平滑化の対象判定](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L801)

### 🖥️ M09 高負荷時にフレーム処理がUIスレッドを占有する

**コード確認。** Dispatcherは保留フレームを1件に集約するが、コールバック内で空になるまで処理を続ける。エフェクト処理中に毎回新フレームが届くとDispatcherへ処理を返せず、入力や画面描画が遅れる。開始・停止の同期処理もUIスレッドで行われ、停止時は取得スレッドの終了待ちが入る。Dispatcherの実行1回あたりの仕事量を制限し、取得の状態遷移をUIスレッドから外す。 [フレーム処理](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L730) [停止処理](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L626) [NAudio 2.3.0の終了待ち](https://github.com/naudio/NAudio/blob/v2.3.0/NAudio.Wasapi/WasapiCapture.cs#L303-L315)

### 🎛️ M10 SampleAppの入力切替や一覧更新で中間状態が取得処理へ伝わる

**コード確認。** 入力種別を先に通知し、その後で新しいデバイス一覧を読み込む。明示指定中は入力種別と旧デバイスIDの組み合わせで一度再接続を試みる。稼働中の一覧再構築でも、選択値が一時的に外れれば停止・再接続に至る。指定デバイスが消えた場合に別の既定・先頭デバイスを選ぶため、明示指定で使用するマイクも変わり得る。入力種別とデバイスIDを確定してから一度だけ反映し、指定機器が消えた場合は状態を表示する。 [入力変更](../AudioVisualizer.SampleApp/ViewModels/MainWindowViewModel.cs#L136) [一覧と選択の更新](../AudioVisualizer.SampleApp/ViewModels/MainWindowViewModel.cs#L485) [接続変更の処理](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L493)

### 🪟 M11 SampleAppのエラー表示が可視化領域に覆われる

**コード確認。** `StatusMessage` 用のテキストと不透明背景の可視化領域が同じGrid行とほぼ同じ上端にある。後から記述された可視化領域が前面に描かれるため、デバイスがないなどのメッセージを隠す。別のGrid行へ置き、エラーがある状態の画面を確認する。 [メッセージと可視化領域の配置](../AudioVisualizer.SampleApp/MainWindow.xaml#L192)

### 🔣 M12 数値・列挙・点列の検証が公開API間でそろっていない

**コード確認。** Coreの大小比較を使った検証は `NaN` を通し、感度では正の無限大も拒否しない。描画データのリストには `null` 要素も渡せる。WPFの入力種別とCoreの設定は未定義の `InputSource` を構築時点で拒否せず、後段のデバイス列挙で例外になる。通常の設定ミスや独自エフェクトの計算異常が、分析結果の汚染や描画エラーとして遅れて現れる。境界で有限値、列挙、要素の妥当性を統一的に検証する。 [設定の検証](../AudioVisualizer.Core/Models/VisualizerSettings.cs#L77) [公開描画値](../AudioVisualizer.Core/Effects/NormalizedPoint.cs#L32) [描画点列](../AudioVisualizer.Core/Effects/PolylineRenderData.cs#L26) [WPF設定登録](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L245)

### 📈 M13 バー数に上限がなく、分析と描画の負荷が急増する

**コード確認。** 公開APIは正の整数をすべて受け付け、その値で配列を確保する。解析ではフレームごとに各出力点について最大1024サンプルの三角関数計算を繰り返し、1024サンプルの半分を超える出力点は同じ周波数を重複計算する。設定値次第でUI応答性を失い、メモリ不足にもなり得る。描画点数の上限と解析負荷の予算を定め、周波数点を一度求めて再利用する。 [バー数の検証](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L589) [周波数計算とループ](../AudioVisualizer.Core/Analysis/AudioFrameAnalyzer.cs#L185)

### 🧊 M14 波形の描画が呼び出し元のブラシを凍結する

**実行確認。** `PolylineRenderer` は受け取ったブラシを `Pen` に直接渡し、その `Pen` を凍結する。WPFでは子のブラシも凍結される。書き換え可能な `SolidColorBrush` が描画後に `IsFrozen = true` となることを確認した。同じブラシを使う画面や後から色を更新するホストに影響する。共有ブラシを直接凍結せず、必要な場合は描画側の複製を使用する。 [Pen生成と凍結](../AudioVisualizer.Wpf/Rendering/PolylineRenderer.cs#L82)

### 🎚️ M15 帯域メーターは固定の周波数帯域を使っていない

**コード確認。** `Low` から `High` の4区分は、スペクトラムの要素数を等分した区間の平均で決まる。フレームに周波数の情報がないため、異なるサンプルレートでも固定帯域を示すことができない。仮に48 kHzモノラルのナイキスト周波数までを等分した場合、最初の区分だけで概ね6 kHzまでを含む。メーター用途の帯域境界をHzで定義し、周波数情報に基づき集計する。 [帯域の計算](../AudioVisualizer.Wpf/Effects/BandLevelMeterEffect.cs#L81) [フレームの公開契約](../AudioVisualizer.Core/Models/VisualizerFrame.cs#L10) [固定帯域の設計](./02_architect.md#L168)

### 🎙️ M16 Coreを直接利用すると取得無効の設定でもキャプチャが開始する

**コード確認。** `VisualizerSettings.IsActive` は音声取得を有効化するかどうかを示すが、公開の `SystemOutputAudioInputProvider.Start` はこの値を参照しない。WPFは独自に開始を制御するため通常経路ではガードされる。Coreの直接利用では `IsActive = false` の設定でも指定入力を取得する。開始要求と有効フラグの優先関係を契約で明示し、無効時にデバイスを開かない挙動を検証する。 [有効フラグの説明](../AudioVisualizer.Core/Models/VisualizerSettings.cs#L26) [開始処理](../AudioVisualizer.Core/Audio/SystemOutputAudioInputProvider.cs#L132)

## 🟡 低

### 📍 L01 平滑化とピーク減衰の速度がフレーム間隔に依存する

**コード確認。** ピークは描画の評価1回ごとに一定量減衰し、平滑化は経過時間を使わない。同じ時刻のフレームも設定を変更するたびに再評価される。フレーム落ちや設定操作で減衰速度が変わり、`Smoothing = 1.0` では既存点が更新されず表示が固定される。経過時間に基づく設定にするか、端点を含め現在の契約を明文化する。 [ピーク減衰](../AudioVisualizer.Wpf/Effects/PeakHoldBarEffect.cs#L121) [設定変更による再描画](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L545) [平滑化式](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L841)

### 🎨 L02 継承されたForegroundの変更がキャッシュへ反映されない

**実行確認。** `Foreground` は `PrimaryBrush` を指定しない場合の描画色だが、描画用ブラシを更新するコールバックは `PrimaryBrush` と `SecondaryBrush` にしか付いていない。検証で `Foreground` を変更しても描画ブラシがその値へ変わらなかった。テーマ由来の色も含め、`Foreground` の変更で描画ブラシを更新する。 [ブラシ用プロパティ](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L339) [描画用ブラシのキャッシュ](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L763) [既定テーマ](../AudioVisualizer.Wpf/Themes/Generic.xaml#L6)

### 🌈 L03 セカンダリ色を指定すると任意のBrushが失われる

**コード確認。** 公開プロパティは `Brush` 型を受け付けるが、`SecondaryBrush` を指定すると両者を単色に変換する。入力がグラデーションなら最初の色だけを使用し、画像ブラシなどは既定色になる。ブラシの種類次第で利用側指定が反映されない。合成可能な型と挙動を契約に明記するか、ブラシを保持して描画する。 [グラデーション生成](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L763) [単色の抽出](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L972)

### 🔠 L04 帯域ラベルが狭い領域と高DPIに適応しない

**コード確認。** ラベルは各帯域の幅に合わせた省略や非表示がなく、狭い領域では幅の長いラベルが重なる。高さがフォントより小さい場合、描画のY座標が負になる。また、`FormattedText` の `pixelsPerDip` が常に1.0で、実際のDPIを反映しない。ラベルの省略・非表示条件とDPIの取得を実装する。 [ラベルの位置計算](../AudioVisualizer.Wpf/Rendering/BandLevelMeterRenderer.cs#L61) [DPI指定](../AudioVisualizer.Wpf/Rendering/BandLevelMeterRenderer.cs#L132)

### 📦 L05 読み取り専用として公開する配列の内容を書き換えられる

**コード確認。** コンストラクターでコピーするが、配列をそのまま `IReadOnlyList<T>` として返す。配列型へキャストできるため、公開データを受け取った後も内容を変更できる。フレームを別スレッドで受け渡す場合や、レンダラーがデータ参照で座標をキャッシュする場合は、表示と公開値が一致しなくなり得る。公開するなら読み取り専用ラッパーか不変コレクションを使う。 [フレームの配列](../AudioVisualizer.Core/Models/VisualizerFrame.cs#L65) [描画点の配列](../AudioVisualizer.Core/Effects/PolylineRenderData.cs#L34) [参照ベースのキャッシュ](../AudioVisualizer.Wpf/Rendering/PolylineRenderer.cs#L106)

### 🗂️ L06 Coreディレクトリにソリューション外のテストプロジェクトが残っている

**コード確認。** `AudioVisualizer.Core/AudioVisualizer.Tests.csproj` はソリューションに含まれず、Core本体と同じフォルダーにある。プロジェクト指定を省いて同フォルダーで操作する場合や、全 `csproj` を探す自動化でビルド対象や中間出力が混乱する。正式な `AudioVisualizer.Core.Tests` への移行を確認し、残存プロジェクトを整理する。 [残存プロジェクト](../AudioVisualizer.Core/AudioVisualizer.Tests.csproj) [ソリューション](../AudioVisualizer.slnx)

### 🧹 L07 リポジトリ指定のフォーマット検査が失敗する

**実行確認。** `dotnet format AudioVisualizer.slnx --verify-no-changes --no-restore` が失敗した。Coreのデバイスサービスおよびインターフェースのインデントと、WPFのアセンブリ属性に空白診断が出ている。既存の差分と混ぜずに整形し、文書化されている確認コマンドを通す。 [Coreデバイスサービス](../AudioVisualizer.Core/Audio/WindowsAudioDeviceService.cs#L5) [WPFアセンブリ属性](../AudioVisualizer.Wpf/AssemblyInfo.cs#L5) [リポジトリの確認手順](../AGENTS.md#L9)

### 📚 L08 公開資料と実際のプロジェクト・テスト構成が一致しない

**コード確認。** 日英READMEのツリーは公開対象の `docs/06_inplementation_plan.md` を掲げているが、追跡されているファイルはない。AGENTS.mdは検出されるテストがないと記載し、要件書と設計書のプロジェクト一覧にはSampleApp.Testsが載っていない。実際のソリューションを基準に記載を合わせる。 [README日本語版](../README.md#L174) [README英語版](../README.en.md#L174) [AGENTS.md](../AGENTS.md#L20) [要件の構成](./01_requirements.md#L113) [ソリューション](../AudioVisualizer.slnx)

### 🧪 L09 Windows固有処理と品質ゲートの自動検証が不足している

**コード確認。** リポジトリ上にGitHub Actionsなどの追跡されたCI定義が見当たらない。WASAPIの取得アダプターと列挙アダプターはカバレッジから除外され、既存の再接続テストは主に管理コード内の代替オブジェクトを使う。全テストが通過しても、OS通知と停止の順序、実デバイス切断、PCM変換を確認できない。Windowsの自動ビルド・テストと、ハードウェアに依存しない音声変換のテスト、実機の確認項目を設ける。 [取得アダプター](../AudioVisualizer.Core/Audio/WasapiAudioCaptureFactory.cs#L13) [列挙アダプター](../AudioVisualizer.Core/Audio/WasapiAudioDeviceEnumerator.cs#L14) [既存の再接続テスト](../AudioVisualizer.Core.Tests/SystemOutputAudioInputProviderTests.cs#L108)

### 🧬 L10 公開EffectプロパティがWPFの同名プロパティを隠す

**コード確認。** `AudioVisualizerControl` は画像効果用の `UIElement.Effect` と異なる型で `new Effect` を公開する。派生コントロール上で一般的なWPFの `Effect` の指定が音声エフェクトの指定として解決されるため、汎用スタイルとの併用や利用者の型推論が紛らわしい。音声表示であることを明示する名前への移行と、既存XAMLの互換性を検討する。 [同名プロパティ](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L151) [依存プロパティ登録](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L285)

### 🧰 L11 点数や間隔が異なる折れ線が同じ添字で補間される

**コード確認。** 同じエフェクトでも点数や間隔が変わった場合、旧点と新点を同じ添字で混ぜる。例えば2点から3点へ変わると、旧点の右端と新点の中央が補間される。新旧のX座標が一致しない場合に補間をやめるか、位置に合わせて再サンプリングする。 [折れ線の補間](../AudioVisualizer.Wpf/AudioVisualizerControl.cs#L870) [現行の点数変更テスト](../AudioVisualizer.Wpf.Tests/WaveformLineEffectTests.cs#L241)

## ✅ 検証結果

| 確認 | 結果 |
| --- | --- |
| `dotnet test AudioVisualizer.slnx` | Core 63件、WPF 76件、SampleApp 19件が成功 |
| 一時NUnitテスト3件 | 取得セッションの所有権と既定変更後の復旧、スペクトラムと波形、描画ブラシとWPF設定の実際の挙動を確認。テストファイルは検証後に削除 |
| `dotnet list AudioVisualizer.slnx package --vulnerable --include-transitive` | 使用したNuGetソースではソリューション6プロジェクトの直接・推移依存に既知の脆弱性の報告なし |
| `dotnet format AudioVisualizer.slnx --verify-no-changes --no-restore` | 空白の診断により失敗。L07に記載 |

実機切断時のプロセス終了、長時間取得時の資源消費、複数DPIでの外観、実デバイスごとのPCM対応状況は未実測。これらの指摘は、発生条件をコードと一次資料に基づいて記載した。
