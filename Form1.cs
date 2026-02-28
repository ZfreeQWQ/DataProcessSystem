using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NXOpen;
using NXOpen.CAM;
using NXOpen.UF;
using Path = System.IO.Path;
using System.Diagnostics; // 用于启动进程

namespace DataProcessSystem
{
    public partial class Form1 : Form
    {
        private Session theSession;
        private UFSession theUFSession;

        public Form1()
        {
            // 在界面初始化前，先配置 NX 的环境路径
            try
            {
                string nxBaseDir = Environment.GetEnvironmentVariable("UGII_BASE_DIR");
                if (string.IsNullOrEmpty(nxBaseDir))
                {
                    MessageBox.Show("错误：未检测到环境变量 UGII_BASE_DIR。\n请确认已安装 Siemens NX 并重启了电脑。");
                }
                else
                {
                    string nxBinDir = Path.Combine(nxBaseDir, "NXBIN");
                    string nxUgiiDir = Path.Combine(nxBaseDir, "UGII");
                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    string newPath = $"{nxBinDir};{nxUgiiDir};{currentPath}";
                    Environment.SetEnvironmentVariable("PATH", newPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("配置 NX 环境失败: " + ex.Message);
            }
            InitializeComponent();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "NX Part Files (*.prt)|*.prt";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = dlg.FileName;
                ShowPreviewImmediately(dlg.FileName);
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFilePath.Text) || !File.Exists(txtFilePath.Text))
            {
                Log("请先选择有效的 PRT 文件！");
                return;
            }

            btnRun.Enabled = false;

            try
            {
                // 1. 初始化 NX
                Log("正在连接 NX Session...");
                if (theSession == null)
                {
                    theSession = Session.GetSession();
                    theUFSession = UFSession.GetUFSession();
                }

                // 2. 打开原始部件
                string filePath = txtFilePath.Text;
                Log($"正在打开文件: {Path.GetFileName(filePath)}");

                Part workPart = null; 
                PartLoadStatus status = null;
                workPart = (Part)theSession.Parts.OpenBaseDisplay(filePath, out status);
                
                // 3. 提取 CAM 信息
                Log("正在解析工艺参数...");
                string camInfo = CAMExtractor.ExtractProcessInfo(workPart);
                rtbLog.AppendText(camInfo);

                // 4. 几何隔离 (生成纯净临时文件)
                Log("正在进行几何隔离分析...");
                string isolationLog = ""; 
                string tempPrtPath = GeometryIsolator.IsolateDesignPart(workPart, ref isolationLog);
                // 把类内部的详细过程打印出来！
                rtbLog.AppendText(isolationLog);
                Log($"提取完成，临时文件: {Path.GetFileName(tempPrtPath)}");

                // 5. 导出临时 STL (专门用于 Python 截图)
                Log("正在生成渲染网格 (STL)...");
                string tempStlPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(tempPrtPath) + ".stl");
                ModelExporter.ExportToSTL(workPart, tempStlPath);
                
                // 6. 导出 STP (用于数据集存储)
                Log("正在导出高精模型 (STP)...");
                string outputDir = Path.Combine(Path.GetDirectoryName(filePath), "output_data");
                ModelExporter.ExportToSTP(tempPrtPath, outputDir);

                // 7. 调用 Python 渲染引擎 (传入 STL 路径)
                Log("正在启动 Python 渲染引擎...");
                string imageFolder = Path.Combine(Path.GetDirectoryName(filePath), "output_images");
                RunPythonScreenshot(tempStlPath, imageFolder);

                // 8. 清理工作
                try 
                {
                    if (File.Exists(tempPrtPath)) File.Delete(tempPrtPath);
                    if (File.Exists(tempStlPath)) File.Delete(tempStlPath); // 删掉临时STL
                } 
                catch {}

                workPart.Close(BasePart.CloseWholeTree.True, BasePart.CloseModified.UseResponses, null);
                Log("流程处理完成。");
            }
            catch (Exception ex)
            {
                Log($"错误: {ex.Message}");
            }
            finally
            {
                btnRun.Enabled = true;
            }
        }

        // 核心方法：跨语言调用 Python 脚本
        private void RunPythonScreenshot(string modelPath, string outputFolder)
        {
            try
            {
                // 1. 设置 Python 脚本路径 (假设放在 exe 同目录下)
                string scriptPath = Path.Combine(Application.StartupPath, "screenshot.py");
                
                if (!File.Exists(scriptPath))
                {
                    // 如果在开发环境下，尝试去项目源码目录找
                    scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "screenshot.py");
                }

                // 2. 配置启动进程
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "python.exe"; // 确保 python 已加入环境变量
                start.Arguments = $"\"{scriptPath}\" \"{modelPath}\" \"{outputFolder}\"";
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;

                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Log("Python 引擎输出:\r\n" + result);
                    }
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Log("Python 渲染启动失败: " + ex.Message);
            }
        }
        
        private void Log(string msg)
        {
            rtbLog.AppendText($"[{DateTime.Now.ToLongTimeString()}] {msg}\r\n");
            rtbLog.ScrollToCaret();
            Application.DoEvents();
        }
        
        private void ShowPreviewImmediately(string filePath)
        {
            // 预览图功能暂时留空，避免干扰核心功能
            pbPreview.Image = null; 
            Log("已选中文件，准备就绪。");
        }
    }
}