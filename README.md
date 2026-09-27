# EasyDownload（无浏览器版 / Light Edition）

> ⚠️ **【中文】本软件界面仅支持简体中文，不提供其他语言界面。**
> ⚠️ **【日本語】本ソフトの UI は簡体字中国語のみ対応しています。他の言語の UI は提供していません。**
> ⚠️ **[English] The UI of this software is Simplified Chinese only. No other UI languages are available.**

![Version](https://img.shields.io/badge/version-1.0.0--light-orange)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-blue)
![WebView2](https://img.shields.io/badge/WebView2-not%20required-brightgreen)
![Python](https://img.shields.io/badge/python-none-brightgreen)
![Status](https://img.shields.io/badge/status-Beta-yellow)

---

## 中文

### 简介

EasyDownload（无浏览器版）是一个轻量、专注的 Windows 多线程下载器。

本版本移除了内置浏览器与 WebView2 依赖，只保留纯粹的下载能力：体积小、启动快、不需要安装 WebView2 运行库、不需要系统自带 Edge，在被精简过的系统或老旧机器上也能正常运行。

下载方式：复制文件直链 → 粘贴到软件中 → 开始下载。

### 功能特点

**下载核心**

- 32 线程多线程下载：单个文件最多切分为 32 段并发下载（1–32 可调）
- 无合并卡顿：采用预分配加定点写入技术，下载完成即改名，彻底消除卡在 99% 合并不动的问题
- 断点续传：暂停后可从断点继续，不需要重新下载
- 分块自动重试：网络抖动导致某一段失败时自动重试，不用手动干预
- 实时速度显示：采用 EMA 平滑算法，速度数字稳定不跳
- 进度条、百分比、剩余时间：剩余时间基于近期实测速度估算，不会全程乱跳
- 并发任务控制：同时下载的文件数可调（1–8）
- 自动限速：可限制到约 1 MB/s
- 下载前确认：可选下载前先问我，弹窗确认后再开始

**文件名智能识别**

- 优先读取服务器 Content-Disposition 中的真实文件名
- 支持重定向后的最终地址取名
- 支持 RFC 5987、UTF-8 与 GB18030 中文文件名，不再乱码
- 服务器不给名字时，根据 Content-Type 自动补扩展名
- 扩展名兜底还会读取文件头魔数，例如 MZ 对应 exe、PK 对应 zip、百分号 PDF 对应 pdf
- 提供改名按钮，万一名不对可手动修正

**界面与体验**

- WinUI 3 风格 Fluent Design：圆角卡片、圆角按钮、Fluent 图标、自定义标题栏
- 深浅主题切换：浅色与深色一键切换，标题栏跟随
- 首次运行引导，OOBE 风格，4 步配置，仅在第一次启动时显示
- 下载目录自定义，可自由选择保存位置，支持打开文件夹
- 下载列表操作：开始、暂停、继续、打开、改名、删除、清空
- 长文件名自动省略：文件名再长也不会把进度条和按钮挤出去，鼠标悬停显示全名

**技术特性**

- 纯 C# 实现，零 Python 依赖，不需要任何脚本运行环境
- 不依赖 WebView2，不依赖 Edge
- 基于 WPF，Windows 原生，无需额外运行时

### 与完整版（含浏览器版）的区别

| 功能 | 无浏览器版（本版） | 完整版 |
|---|---|---|
| 多线程下载与断点续传 | 支持 | 支持 |
| 多标签网页浏览 | 不支持 | 支持 |
| 网页内点击下载自动接管 | 不支持 | 支持 |
| 需要 WebView2 运行库 | 不需要 | 需要 |
| 需要系统 Edge | 不需要 | 需要 |
| 安装包体积 | 更小 | 较大 |
| 精简或老旧系统兼容性 | 更好 | 一般 |

### 运行要求

| 项目 | 要求 |
|---|---|
| 操作系统 | Windows 10 1809 及以上，或 Windows 11，仅 x64 |
| 不支持 | Windows 7 与 Windows 8.1 无法运行 |
| .NET | 无需手动安装，独立发布版已内置运行时 |
| WebView2 | 不需要 |
| 磁盘空间 | 约 120 MB |

### 安装与使用

**安装包方式（推荐）**

1. 下载 EasyDownload_Light_1.0.0_Setup.exe
2. 双击运行，按向导提示安装
3. 安装时需勾选我同意免责声明
4. 从开始菜单启动，可在控制面板正常卸载

**源码编译**

1. 用 Visual Studio 打开 EasyDownload.csproj
2. 切换到 Release，重新生成，然后 F5
3. 本版本无需还原 WebView2 的 NuGet 包

**使用方法**

1. 复制文件的下载直链，支持 HTTP 与 HTTPS
2. 打开软件，进入下载页，粘贴链接，新建下载
3. 观察进度条、速度与剩余时间，可随时暂停或继续

### 支持的下载类型

- 支持 HTTP 与 HTTPS 直链
- 不支持 BT、磁力链接、P2P
- 不支持 m3u8 视频流
- 不支持需要登录鉴权的链接，例如带 Cookie 或登录态的地址

### 免责声明

1. 本软件按原样（AS IS）提供，不附带任何明示或默示担保。
2. 用户自愿使用本软件，因使用或无法使用本软件导致的任何直接或间接损失，包括数据丢失、文件损坏、设备异常、业务中断等，开发者不承担任何责任。
3. 本软件仅供下载用户依法享有权利的内容之用；禁止用于下载受版权保护且未经授权的作品、涉密文件及任何违法违规内容。违法使用产生的一切责任由用户自行承担。
4. 多线程下载仅是对 HTTP 与 HTTPS 直链的分块并发请求，不具备任何加速承诺，实际速度取决于用户带宽与目标服务器策略。
5. 本软件运行依赖 Microsoft .NET 等第三方组件，其知识产权归各自权利人所有，本软件仅作调用，不主张任何权利。
6. 本软件不收集、不上传任何个人信息与下载记录，所有数据仅保存在用户本机。
7. 继续使用本软件即视为已完整阅读并同意本声明全部内容。

### 测试版说明

此版本为测试版（Beta）。

若遇到某些功能不可用、显示异常、下载失败或其他问题，请提交问题反馈。提交时请尽量附上系统版本、软件版本、问题截图与复现步骤。

感谢你的测试与反馈。

---

## 日本語

### 概要

EasyDownload（ブラウザなし版）は、軽量でダウンロードに特化した Windows 用マルチスレッドダウンローダーです。

本バージョンは内蔵ブラウザと WebView2 への依存を削除し、純粋なダウンロード機能のみを備えています。サイズが小さく起動が速く、WebView2 ランタイムのインストールもシステムの Edge も不要なため、機能が削減された環境や古いマシンでも正常に動作します。

ダウンロード方法：ファイルの直リンクをコピーし、ソフトに貼り付けてダウンロードを開始します。

### 主な特徴

**ダウンロード中核機能**

- 32スレッド対応：1ファイルを最大32分割して並列ダウンロード（1〜32で調整可能）
- 結合時の停止なし：事前割り当てと定点書き込みを採用し、完了後は即リネーム。99% で止まる問題を解消
- レジューム対応：一時停止後も続きから再開でき、再ダウンロードは不要
- チャンク自動リトライ：通信不良で一部が失敗しても自動的に再試行
- リアルタイム速度表示：EMA 平滑化アルゴリズムにより速度の数値が安定
- 進捗バー・パーセンテージ・残り時間：残り時間は直近の実測速度から算出し、終始乱れません
- 同時ダウンロード数：1〜8 で調整可能
- 帯域制限：約 1 MB/s に制限可能
- ダウンロード前の確認：確認ダイアログを表示する設定が可能

**ファイル名の自動判別**

- サーバーの Content-Disposition に含まれる正式なファイル名を最優先で取得
- リダイレクト後の最終 URL からの取得に対応
- RFC 5987、UTF-8、GB18030 の中国語ファイル名に対応し、文字化けを防止
- サーバーが名前を返さない場合は Content-Type から拡張子を自動付与
- 拡張子の補完にはファイル先頭のマジックナンバーも参照（MZ は exe、PK は zip、%PDF は pdf など）
- 名前変更ボタンを備え、万一名前が正しくない場合も手動で修正可能

**UI と操作性**

- WinUI 3 風 Fluent Design：角丸カード、角丸ボタン、Fluent アイコン、カスタムタイトルバー
- ライト / ダークテーマ切替：ワンクリックで切替、タイトルバーも追従
- 初回セットアップ（OOBE 風）：4ステップの構成で、初回起動時のみ表示
- 保存先の自由設定：任意のフォルダを指定でき、フォルダを開く機能も搭載
- ダウンロード一覧の操作：開始、一時停止、再開、開く、名前変更、削除、クリア
- 長いファイル名は自動で省略：進捗バーやボタンが押し出されることはなく、ホバーで全名を表示

**技術的特性**

- 純 C# 実装、Python 依存ゼロ、スクリプト実行環境は不要
- WebView2 にも Edge にも依存しません
- WPF ベースの Windows ネイティブ、追加ランタイム不要

### 完全版（ブラウザ付き）との違い

| 機能 | ブラウザなし版（本版） | 完全版 |
|---|---|---|
| マルチスレッドとレジューム | 対応 | 対応 |
| マルチタブブラウザ | 非対応 | 対応 |
| ページ内のダウンロード自動取込 | 非対応 | 対応 |
| WebView2 ランタイム | 不要 | 必要 |
| システムの Edge | 不要 | 必要 |
| インストーラ size | より小さい | 大きい |
| 削減環境・旧マシンとの互換性 | より良好 | 一般的 |

### 動作環境

| 項目 | 要件 |
|---|---|
| OS | Windows 10 1809 以降、または Windows 11（x64 のみ） |
| 非対応 | Windows 7 および Windows 8.1 では動作しません |
| .NET | 手動インストール不要（自己完結版に同梱） |
| WebView2 | 不要 |
| ディスク容量 | 約 120 MB |

### インストールと使い方

**インストーラ（推奨）**

1. EasyDownload_Light_1.0.0_Setup.exe をダウンロード
2. ダブルクリックで実行し、ウィザードに従ってインストール
3. インストール時に免責事項への同意が必要
4. スタートメニューから起動、コントロールパネルから通常通りアンインストール可能

**ソースからビルド**

1. Visual Studio で EasyDownload.csproj を開く
2. Release に切替え、リビルド後 F5
3. 本バージョンでは WebView2 の NuGet 復元は不要

**使い方**

1. ファイルの直リンク（HTTP または HTTPS）をコピー
2. ソフトを開き、ダウンロード画面でリンクを貼り付けて新規ダウンロード
3. 進捗・速度・残り時間を確認し、随時一時停止と再開が可能

### 対応ダウンロード種別

- HTTP および HTTPS の直リンクに対応
- BT、マグネットリンク、P2P は非対応
- m3u8 ストリーミングは非対応
- ログイン認証が必要なリンク（Cookie やセッションを要するもの）は非対応

### 免責事項

1. 本ソフトは現状のまま（AS IS）で提供され、いかなる明示または黙示の保証もありません。
2. 利用は自己責任です。本ソフトの使用または使用不能により生じたいかなる損害（データ損失、ファイル破損、機器異常、業務中断など）について、開発者は一切の責任を負いません。
3. 本ソフトは利用者が正当な権利を有するコンテンツのダウンロードのみに使用してください。著作権物の無断ダウンロード、機密ファイル、法令違反コンテンツへの使用を禁止します。違法利用による一切の責任は利用者が負います。
4. マルチスレッドダウンロードは HTTP および HTTPS 直リンクの分割並列リクエストにすぎず、いかなる高速化も保証しません。実際の速度は回線とサーバーに依存します。
5. 本ソフトは Microsoft .NET などのサードパーティ製コンポーネントに依存します。その知的財産権は各権利者に帰属し、本ソフトはそれらを呼び出すのみで権利を主張しません。
6. 本ソフトは個人情報やダウンロード履歴を一切収集・送信しません。データは端末内のみに保存されます。
7. 本ソフトの継続使用をもって、本免責事項の全文に同意したものとみなします。

### ベータ版について

本バージョンはベータ版（Beta）です。

一部の機能が利用できない、表示が異常、ダウンロードが失敗するなどの問題が発生した場合は、Issue を提出してください。その際、OS バージョン、ソフトのバージョン、問題のスクリーンショット、再現手順をなるべく添付してください。

テストとフィードバックに感謝します。

---

## English

### Overview

EasyDownload (Light Edition) is a lightweight, download-focused multi-threaded downloader for Windows.

This edition removes the built-in browser and the WebView2 dependency, keeping only pure downloading capability: smaller size, faster startup, no WebView2 Runtime installation required, no system Edge required. It runs correctly even on stripped-down systems or older machines.

How to download: copy a direct file link, paste it into the software, and start the download.

### Features

**Download Core**

- 32-thread downloading: a single file can be split into up to 32 segments downloaded concurrently (adjustable 1–32)
- No merge stall: uses pre-allocation and positional writes, then renames on completion, completely eliminating the "stuck at 99% merging" issue
- Resumable downloads: continue from the breakpoint after pausing, no need to restart
- Per-chunk automatic retry: if a segment fails due to network instability, it retries automatically with no manual intervention
- Live speed display: uses an EMA smoothing algorithm so the speed value stays stable
- Progress bar, percentage, and remaining time: remaining time is estimated from recent measured speed and does not fluctuate wildly
- Concurrent task control: number of files downloading at once is adjustable (1–8)
- Speed limit: can be throttled to approximately 1 MB/s
- Confirm before download: optional prompt asking for confirmation before starting

**Smart Filename Detection**

- Reads the real filename from the server's Content-Disposition header first
- Supports taking the name from the final URL after redirects
- Supports RFC 5987, UTF-8, and GB18030 Chinese filenames, eliminating garbled text
- Automatically appends an extension based on Content-Type when the server provides no name
- Fallback extension detection also reads file header magic numbers, such as MZ for exe, PK for zip, and %PDF for pdf
- Provides a rename button so the name can be corrected manually if needed

**Interface and Experience**

- WinUI 3-style Fluent Design: rounded cards, rounded buttons, Fluent icons, custom title bar
- Light and Dark theme switch: one-click switching, title bar follows
- First-run wizard (OOBE style): 4-step setup, shown only on the first launch
- Custom download directory: freely choose the save location, with an open folder option
- Download list actions: start, pause, resume, open, rename, delete, clear
- Long filenames are automatically truncated: the progress bar and buttons are never pushed out, hover to see the full name

**Technical Characteristics**

- Pure C# implementation, zero Python dependency, no scripting runtime required
- Does not depend on WebView2 or Edge
- Built on WPF, Windows native, no additional runtime required

### Differences from the Full Edition (with browser)

| Feature | Light Edition (this version) | Full Edition |
|---|---|---|
| Multi-threaded download and resume | Supported | Supported |
| Multi-tab web browsing | Not supported | Supported |
| Auto-capture of in-page downloads | Not supported | Supported |
| Requires WebView2 Runtime | No | Yes |
| Requires system Edge | No | Yes |
| Installer size | Smaller | Larger |
| Compatibility with stripped-down or older systems | Better | Average |

### Requirements

| Item | Requirement |
|---|---|
| Operating system | Windows 10 1809 or later, or Windows 11, x64 only |
| Not supported | Windows 7 and Windows 8.1 cannot run this software |
| .NET | No manual installation needed, included in the self-contained build |
| WebView2 | Not required |
| Disk space | Approximately 120 MB |

### Installation and Usage

**Installer (recommended)**

1. Download EasyDownload_Light_1.0.0_Setup.exe
2. Double-click to run and follow the wizard
3. You must accept the disclaimer during installation
4. Launch from the Start menu; can be uninstalled normally from Control Panel

**Building from source**

1. Open EasyDownload.csproj with Visual Studio
2. Switch to Release, rebuild, then press F5
3. This version does not require restoring the WebView2 NuGet package

**How to use**

1. Copy a direct download link, HTTP or HTTPS
2. Open the software, go to the download page, paste the link, and create a new download
3. Watch the progress bar, speed, and remaining time; pause or resume at any time

### Supported Download Types

- HTTP and HTTPS direct links are supported
- BT, magnet links, and P2P are not supported
- m3u8 video streams are not supported
- Links requiring authentication, such as those needing cookies or a login session, are not supported

### Disclaimer

1. This software is provided "AS IS", without warranty of any kind, express or implied.
2. Use at your own risk. The developer shall not be liable for any direct or indirect damages arising from the use or inability to use this software, including data loss, file corruption, device malfunction, or business interruption.
3. This software is intended only for downloading content you have the legal right to download. Do not use it for copyrighted material without authorization, confidential files, or any illegal content. All responsibility arising from illegal use lies with the user.
4. Multi-threaded downloading is merely segmented concurrent HTTP and HTTPS requests; no speed increase is guaranteed. Actual speed depends on your bandwidth and the target server.
5. This software relies on third-party components such as Microsoft .NET. Their intellectual property belongs to their respective owners; this software only invokes them and claims no rights.
6. This software does not collect or transmit any personal information or download history. All data is stored only on your device.
7. Continued use of this software constitutes full acceptance of this disclaimer.

### Beta Notice

This version is a Beta release.

If you encounter any unavailable features, display issues, download failures, or other problems, please submit an issue. When submitting, please include your system version, software version, a screenshot of the problem, and steps to reproduce.

Thank you for testing and for your feedback.
