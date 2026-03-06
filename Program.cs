using System;
using System.Windows.Forms;
using System.Runtime.InteropServices; // 新增这一行

namespace DataProcessSystem
{
    static class Program
    {
        // 声明调用 Windows 底层 API，告诉系统本程序支持高 DPI
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            // 在这里强制开启高 DPI 支持 (解决启动时模糊、运行后抽搐缩小的问题)
            if (Environment.OSVersion.Version.Major >= 6) 
            {
                SetProcessDPIAware(); 
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}