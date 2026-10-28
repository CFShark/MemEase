using System;
using System.Threading;
using System.Windows.Forms;

namespace MemEase
{
    internal static class Program
    {
        internal const string MutexName = @"Local\MemEase.Instance.v1";
        internal const string ReleaseEventName = @"Local\MemEase.Release.v1";

        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    SignalExistingInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += OnThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                TrayAppContext context = null;
                try
                {
                    context = new TrayAppContext();
                    Application.Run(context);
                }
                catch (Exception exception)
                {
                    Log.Error("Fatal error", exception);
                    try
                    {
                        MessageBox.Show(
                            "MemEase 启动失败。\r\n\r\n" + exception.Message,
                            "MemEase",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    catch
                    {
                    }
                }
                finally
                {
                    if (context != null) context.Dispose();
                    try { mutex.ReleaseMutex(); }
                    catch { }
                }
            }
        }

        private static void SignalExistingInstance()
        {
            try
            {
                using (EventWaitHandle waitHandle = EventWaitHandle.OpenExisting(ReleaseEventName))
                {
                    waitHandle.Set();
                }
            }
            catch
            {
                // The first instance is still starting up or is not listening.
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error("UI thread", e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error("Unhandled exception", e.ExceptionObject as Exception);
        }
    }
}
