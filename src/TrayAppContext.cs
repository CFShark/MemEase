using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MemEase
{
    internal sealed class TrayAppContext : ApplicationContext
    {
        private const int SampleIntervalMilliseconds = 2000;
        private const int RequiredHighSamples = 3;
        private const int AutoReleaseCooldownMinutes = 3;

        private readonly AppSettings _settings;
        private readonly Font _font;
        private readonly Font _fontBold;
        private readonly Font _fontSmall;
        private readonly MemEaseRenderer _renderer;
        private readonly Control _dispatcher;
        private readonly NotifyIcon _notify;
        private readonly ContextMenuStrip _menu;
        private ToolStripMenuItem _header;
        private ToolStripMenuItem _meta;
        private ToolStripMenuItem _releaseItem;
        private ToolStripMenuItem _autoItem;
        private ToolStripMenuItem _autoOffItem;
        private ToolStripMenuItem _startupItem;
        private ToolStripMenuItem _notifyItem;
        private ToolStripMenuItem _aboutItem;
        private ToolStripMenuItem _exitItem;
        private readonly Dictionary<int, ToolStripMenuItem> _thresholdItems =
            new Dictionary<int, ToolStripMenuItem>();

        private readonly System.Windows.Forms.Timer _sampleTimer;
        private System.Windows.Forms.Timer _introTimer;
        private readonly EventWaitHandle _releaseEvent;
        private readonly Thread _eventThread;

        private Icon _currentIcon;
        private MemoryStream _currentIconStream;
        private int _iconBand = -1;
        private DateTime _lastIconUpdateUtc = DateTime.MinValue;
        private string _lastTooltip;
        private ReleaseResult _lastResult;
        private DateTime _lastReleaseUtc = DateTime.MinValue;
        private int _highSamples;
        private int _releasing;
        private int _currentProcessId;
        private int _lastActiveProcessId;
        private volatile bool _disposed;

        public TrayAppContext()
        {
            _settings = AppSettings.Load();
            Theme.Refresh();
            using (Process current = Process.GetCurrentProcess())
            {
                _currentProcessId = current.Id;
            }
            _font = CreateFont(9.5f, FontStyle.Regular);
            _fontBold = CreateFont(9.5f, FontStyle.Bold);
            _fontSmall = CreateFont(8.5f, FontStyle.Regular);
            _renderer = new MemEaseRenderer();

            _dispatcher = new Control();
            _dispatcher.Tag = _dispatcher.Handle;

            _menu = BuildMenu();
            _notify = new NotifyIcon();
            _notify.ContextMenuStrip = _menu;
            _notify.Text = "MemEase";
            _notify.MouseClick += OnNotifyMouseClick;
            _notify.Visible = true;

            _sampleTimer = new System.Windows.Forms.Timer();
            _sampleTimer.Interval = SampleIntervalMilliseconds;
            _sampleTimer.Tick += OnSampleTick;
            _sampleTimer.Start();

            try
            {
                _releaseEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ReleaseEventName);
                _eventThread = new Thread(WaitForSecondInstance);
                _eventThread.IsBackground = true;
                _eventThread.Name = "MemEase.SingleInstance";
                _eventThread.Start();
            }
            catch (Exception exception)
            {
                Log.Error("Create single-instance event", exception);
            }

            RefreshMemory();
            UpdateChecks();

            if (!_settings.FirstRunDone)
            {
                _settings.FirstRunDone = true;
                _settings.Save();
                _introTimer = new System.Windows.Forms.Timer();
                _introTimer.Interval = 900;
                _introTimer.Tick += OnIntroTick;
                _introTimer.Start();
            }
        }

        private ContextMenuStrip BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = _font;
            menu.ShowImageMargin = false;
            menu.ShowCheckMargin = true;
            menu.Padding = new Padding(0, 4, 0, 4);
            menu.Renderer = _renderer;
            menu.BackColor = Theme.MenuBack;
            menu.ForeColor = Theme.Text;
            menu.Opening += OnMenuOpening;

            _header = new ToolStripMenuItem("正在读取内存...");
            _header.Name = "header";
            _header.Enabled = false;
            _header.Font = _fontBold;
            _header.Padding = new Padding(2, 5, 2, 2);
            menu.Items.Add(_header);

            _meta = new ToolStripMenuItem(string.Empty);
            _meta.Name = "meta";
            _meta.Enabled = false;
            _meta.Font = _fontSmall;
            _meta.Padding = new Padding(2, 0, 2, 5);
            menu.Items.Add(_meta);
            menu.Items.Add(new ToolStripSeparator());

            _releaseItem = CreateItem("立即释放内存");
            _releaseItem.Click += OnReleaseClick;
            menu.Items.Add(_releaseItem);

            _autoItem = CreateItem("自动释放");
            _autoItem.DropDown.Renderer = _renderer;
            ToolStripDropDownMenu autoMenu = _autoItem.DropDown as ToolStripDropDownMenu;
            if (autoMenu != null)
            {
                autoMenu.ShowImageMargin = false;
                autoMenu.ShowCheckMargin = true;
            }
            _autoItem.DropDown.BackColor = Theme.MenuBack;
            _autoItem.DropDown.ForeColor = Theme.Text;
            _autoItem.DropDown.Font = _font;
            menu.Items.Add(_autoItem);

            _autoOffItem = CreateItem("关闭自动释放");
            _autoOffItem.Click += OnAutoOffClick;
            _autoItem.DropDownItems.Add(_autoOffItem);
            _autoItem.DropDownItems.Add(new ToolStripSeparator());

            for (int i = 0; i < AppSettings.Thresholds.Length; i++)
            {
                int threshold = AppSettings.Thresholds[i];
                ToolStripMenuItem item = CreateItem(string.Format("内存达到 {0}% 时", threshold));
                item.Tag = threshold;
                item.Click += OnThresholdClick;
                _thresholdItems.Add(threshold, item);
                _autoItem.DropDownItems.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());
            _startupItem = CreateItem("开机自动启动");
            _startupItem.Click += OnStartupClick;
            menu.Items.Add(_startupItem);

            _notifyItem = CreateItem("释放后显示通知");
            _notifyItem.Click += OnNotifyClick;
            menu.Items.Add(_notifyItem);

            menu.Items.Add(new ToolStripSeparator());
            _aboutItem = CreateItem("关于 MemEase");
            _aboutItem.Click += OnAboutClick;
            menu.Items.Add(_aboutItem);

            _exitItem = CreateItem("退出");
            _exitItem.Click += OnExitClick;
            menu.Items.Add(_exitItem);

            return menu;
        }

        private ToolStripMenuItem CreateItem(string text)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Font = _font;
            item.Padding = new Padding(2, 4, 2, 4);
            item.Margin = Padding.Empty;
            return item;
        }

        private static Font CreateFont(float size, FontStyle style)
        {
            try
            {
                return new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
            }
            catch
            {
                try
                {
                    return new Font(SystemFonts.MenuFont.FontFamily, size, style, GraphicsUnit.Point);
                }
                catch
                {
                    return (Font)SystemFonts.MenuFont.Clone();
                }
            }
        }

        private void OnMenuOpening(object sender, CancelEventArgs e)
        {
            if (_disposed)
            {
                e.Cancel = true;
                return;
            }

            Theme.Refresh();
            ApplyTheme();
            RememberActiveProcess();
            try
            {
                _startupItem.Checked = StartupManager.IsEnabled();
            }
            catch
            {
                _startupItem.Checked = false;
            }
            _notifyItem.Checked = _settings.NotifyOnRelease;
            UpdateChecks();
            RefreshMemory();
        }

        private void ApplyTheme()
        {
            _menu.BackColor = Theme.MenuBack;
            _menu.ForeColor = Theme.Text;
            _autoItem.DropDown.BackColor = Theme.MenuBack;
            _autoItem.DropDown.ForeColor = Theme.Text;
            _menu.Invalidate();
            _autoItem.DropDown.Invalidate();
        }

        private void UpdateChecks()
        {
            _autoOffItem.Checked = !_settings.AutoRelease;
            foreach (KeyValuePair<int, ToolStripMenuItem> pair in _thresholdItems)
            {
                pair.Value.Checked = _settings.AutoRelease &&
                    pair.Key == _settings.ThresholdPercent;
            }
        }

        private void RefreshMemory()
        {
            RememberActiveProcess();
            MemorySnapshot snapshot;
            try
            {
                snapshot = MemorySnapshot.Capture();
            }
            catch (Exception exception)
            {
                Log.Error("Read memory status", exception);
                _header.Text = "无法读取内存信息";
                SetTooltip("MemEase · 无法读取内存信息");
                return;
            }

            UpdateHeader(snapshot);
            UpdateTrayIcon(snapshot);
        }

        private void UpdateHeader(MemorySnapshot snapshot)
        {
            string header = string.Format(
                "内存占用 {0}%   {1}",
                snapshot.LoadPercent,
                snapshot.UsedText);
            if (_header.Text != header) _header.Text = header;

            SetTooltip(string.Format(
                "MemEase · 内存 {0}% · {1}",
                snapshot.LoadPercent,
                snapshot.UsedText));
            UpdateMeta();
        }

        private void UpdateMeta()
        {
            string text = _settings.AutoRelease
                ? string.Format("自动释放 {0}%", _settings.ThresholdPercent)
                : "自动释放已关闭";

            if (_lastResult == null)
            {
                text += " · 尚未释放";
            }
            else if (_lastResult.FreedBytes >= 32L * 1024L * 1024L)
            {
                text += " · 上次释放 " + ByteFormat.Format(_lastResult.FreedBytes);
            }
            else if (_lastResult.TrimmedCount > 0)
            {
                text += " · 上次整理 " + _lastResult.TrimmedCount + " 个进程";
            }
            else
            {
                text += " · 上次无可用进程";
            }

            if (_meta.Text != text) _meta.Text = text;
        }

        private void SetTooltip(string text)
        {
            if (text != null && text.Length > 63) text = text.Substring(0, 63);
            if (_lastTooltip == text) return;
            try
            {
                _notify.Text = text;
                _lastTooltip = text;
            }
            catch
            {
                // The shell can transiently reject tooltip updates at logon/logoff.
            }
        }

        private void OnSampleTick(object sender, EventArgs e)
        {
            if (_disposed) return;

            RememberActiveProcess();
            MemorySnapshot snapshot;
            try
            {
                snapshot = MemorySnapshot.Capture();
            }
            catch (Exception exception)
            {
                Log.Error("Sample memory status", exception);
                return;
            }

            UpdateHeader(snapshot);
            UpdateTrayIcon(snapshot);

            if (!_settings.AutoRelease)
            {
                _highSamples = 0;
                return;
            }

            if (snapshot.LoadPercent < _settings.ThresholdPercent)
            {
                _highSamples = 0;
                return;
            }

            if (_highSamples < RequiredHighSamples) _highSamples++;
            if (_highSamples < RequiredHighSamples) return;
            if (_releasing != 0) return;
            if (DateTime.UtcNow - _lastReleaseUtc <
                TimeSpan.FromMinutes(AutoReleaseCooldownMinutes)) return;

            BeginRelease(true);
        }

        private void UpdateTrayIcon(MemorySnapshot snapshot)
        {
            int band = BandFor(snapshot.LoadPercent);
            if (band == _iconBand) return;
            if (_iconBand >= 0 &&
                (DateTime.UtcNow - _lastIconUpdateUtc).TotalSeconds < 15) return;

            MemoryStream stream = null;
            Icon icon = null;
            try
            {
                byte[] bytes = IconFactory.CreateTrayIcon(snapshot.LoadPercent);
                stream = new MemoryStream(bytes);
                icon = new Icon(stream);
                _notify.Icon = icon;

                Icon oldIcon = _currentIcon;
                MemoryStream oldStream = _currentIconStream;
                _currentIcon = icon;
                _currentIconStream = stream;
                _iconBand = band;
                _lastIconUpdateUtc = DateTime.UtcNow;
                icon = null;
                stream = null;

                if (oldIcon != null) oldIcon.Dispose();
                if (oldStream != null) oldStream.Dispose();
            }
            catch (Exception exception)
            {
                if (icon != null) icon.Dispose();
                if (stream != null) stream.Dispose();
                _iconBand = band;
                _lastIconUpdateUtc = DateTime.UtcNow;
                Log.Error("Create tray icon", exception);

                if (_currentIcon == null)
                {
                    try { _notify.Icon = SystemIcons.Application; }
                    catch { }
                }
            }
        }

        private static int BandFor(int loadPercent)
        {
            if (loadPercent >= 85) return 2;
            if (loadPercent >= 70) return 1;
            return 0;
        }

        private void OnNotifyMouseClick(object sender, MouseEventArgs e)
        {
            if (_disposed || e.Button != MouseButtons.Left) return;
            try
            {
                _menu.Show(Cursor.Position);
            }
            catch (Exception exception)
            {
                Log.Error("Show menu", exception);
            }
        }

        private void OnReleaseClick(object sender, EventArgs e)
        {
            BeginRelease(false);
        }

        private void OnAutoOffClick(object sender, EventArgs e)
        {
            SetAutoRelease(false, _settings.ThresholdPercent);
        }

        private void OnThresholdClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null) return;
            SetAutoRelease(true, (int)item.Tag);
        }

        private void SetAutoRelease(bool enabled, int threshold)
        {
            _settings.AutoRelease = enabled;
            _settings.ThresholdPercent = AppSettings.NormalizeThreshold(threshold);
            _settings.Save();
            _highSamples = 0;
            UpdateChecks();
            UpdateMeta();
        }

        private void OnStartupClick(object sender, EventArgs e)
        {
            bool enable = !_startupItem.Checked;
            try
            {
                StartupManager.SetEnabled(enable);
                _startupItem.Checked = StartupManager.IsEnabled();
                if (_startupItem.Checked != enable)
                {
                    ShowBalloon("设置未生效", "无法更新开机启动项。", 3000);
                }
            }
            catch (Exception exception)
            {
                Log.Error("Set startup", exception);
                ShowBalloon("设置失败", "无法更新开机启动项。", 3000);
                try { _startupItem.Checked = StartupManager.IsEnabled(); }
                catch { }
            }
        }

        private void OnNotifyClick(object sender, EventArgs e)
        {
            _settings.NotifyOnRelease = !_settings.NotifyOnRelease;
            _settings.Save();
            _notifyItem.Checked = _settings.NotifyOnRelease;
        }

        private void OnAboutClick(object sender, EventArgs e)
        {
            string text =
                "MemEase 1.0\r\n" +
                "轻量内存托盘工具\r\n\r\n" +
                "• 立即释放：回收闲置进程的工作集\r\n" +
                "• 自动释放：内存连续 6 秒达到阈值时触发\r\n" +
                "• 纯本地运行：不联网、无服务、无驱动\r\n\r\n" +
                "为避免卡顿，会跳过前台进程、系统关键进程、桌面外壳和安全软件。\r\n" +
                "Windows 的文件缓存不会被清空，任何进程都不会被结束。\r\n\r\n" +
                "设置保存在 HKCU\\Software\\MemEase";
            MessageBox.Show(
                text,
                "关于 MemEase",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void BeginRelease(bool automatic)
        {
            if (_disposed) return;
            if (Interlocked.CompareExchange(ref _releasing, 1, 0) != 0) return;

            RememberActiveProcess();
            int excludedProcessId = _lastActiveProcessId;
            _lastReleaseUtc = DateTime.UtcNow;
            _highSamples = 0;
            _releaseItem.Enabled = false;
            _releaseItem.Text = "正在释放内存...";

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                ReleaseResult result = null;
                Exception error = null;
                try
                {
                    result = MemoryReleaser.Release(excludedProcessId);
                }
                catch (Exception exception)
                {
                    error = exception;
                    Log.Error("Release memory", exception);
                }

                try
                {
                    if (!_disposed && _dispatcher != null && !_dispatcher.IsDisposed)
                    {
                        _dispatcher.BeginInvoke(new Action(delegate
                        {
                            CompleteRelease(automatic, result, error);
                        }));
                    }
                }
                catch
                {
                    // The application is shutting down.
                }
            });
        }

        private void CompleteRelease(bool automatic, ReleaseResult result, Exception error)
        {
            Interlocked.Exchange(ref _releasing, 0);
            if (_disposed) return;

            _releaseItem.Enabled = true;
            _releaseItem.Text = "立即释放内存";

            if (error != null)
            {
                if (_settings.NotifyOnRelease)
                {
                    ShowBalloon(
                        "内存释放失败",
                        "请稍后重试；详细信息已写入本地日志。",
                        3000);
                }
                return;
            }
            if (result == null) return;

            _lastResult = result;
            UpdateHeader(result.After);
            UpdateTrayIcon(result.After);

            if (_settings.NotifyOnRelease)
            {
                ShowBalloon(
                    automatic ? "已自动释放内存" : "释放完成",
                    BuildResultText(result),
                    3500);
            }
        }

        private static string BuildResultText(ReleaseResult result)
        {
            int before = result.Before.LoadPercent;
            int after = result.After.LoadPercent;
            string memory = before != after
                ? string.Format("内存 {0}% → {1}%", before, after)
                : string.Format("当前内存 {0}%", after);

            if (result.FreedBytes >= 32L * 1024L * 1024L)
            {
                return string.Format(
                    "{0}，释放 {1}。已整理 {2} 个进程。",
                    memory,
                    ByteFormat.Format(result.FreedBytes),
                    result.TrimmedCount);
            }
            if (result.TrimmedCount > 0)
            {
                return string.Format("{0}，已整理 {1} 个进程。", memory, result.TrimmedCount);
            }
            return string.Format("{0}，当前没有可整理的闲置进程。", memory);
        }

        private void ShowBalloon(string title, string text, int timeout)
        {
            if (_disposed) return;
            try
            {
                _notify.BalloonTipTitle = title;
                _notify.BalloonTipText = text;
                _notify.BalloonTipIcon = ToolTipIcon.None;
                _notify.ShowBalloonTip(timeout);
            }
            catch (Exception exception)
            {
                Log.Error("Show balloon", exception);
            }
        }

        private void RememberActiveProcess()
        {
            int processId = MemoryReleaser.GetForegroundProcessId();
            if (processId <= 4 || processId == _currentProcessId) return;
            _lastActiveProcessId = processId;
        }

        private void OnIntroTick(object sender, EventArgs e)
        {
            if (_introTimer != null)
            {
                _introTimer.Stop();
                _introTimer.Dispose();
                _introTimer = null;
            }
            if (_disposed) return;
            ShowBalloon(
                "MemEase 已在后台运行",
                "右键或左键单击托盘图标：立即释放内存，或设置自动释放阈值。",
                5000);
        }

        private void WaitForSecondInstance()
        {
            while (!_disposed)
            {
                bool signaled;
                try
                {
                    signaled = _releaseEvent.WaitOne(1000);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch
                {
                    Thread.Sleep(500);
                    continue;
                }

                if (!signaled || _disposed) continue;

                try
                {
                    if (!_disposed && _dispatcher != null && !_dispatcher.IsDisposed)
                    {
                        _dispatcher.BeginInvoke(new Action(delegate
                        {
                            BeginRelease(false);
                        }));
                    }
                }
                catch
                {
                    // The application is shutting down.
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Cleanup();
            base.Dispose(disposing);
        }

        private void Cleanup()
        {
            if (_disposed) return;
            _disposed = true;

            if (_sampleTimer != null)
            {
                _sampleTimer.Stop();
                _sampleTimer.Dispose();
            }
            if (_introTimer != null)
            {
                _introTimer.Stop();
                _introTimer.Dispose();
                _introTimer = null;
            }

            if (_releaseEvent != null)
            {
                try { _releaseEvent.Set(); }
                catch { }
            }
            if (_eventThread != null)
            {
                try { _eventThread.Join(500); }
                catch { }
            }

            if (_notify != null)
            {
                _notify.Visible = false;
                _notify.Dispose();
            }
            if (_menu != null) _menu.Dispose();
            if (_dispatcher != null) _dispatcher.Dispose();
            if (_currentIcon != null) _currentIcon.Dispose();
            if (_currentIconStream != null) _currentIconStream.Dispose();
            if (_releaseEvent != null) _releaseEvent.Close();

            _font.Dispose();
            _fontBold.Dispose();
            _fontSmall.Dispose();
        }
    }
}
