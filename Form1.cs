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

        // ================= UI 事件部分 =================

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

        // 【新增】：选择文件夹按钮事件
        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "请选择包含 PRT 文件的文件夹进行批量处理";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = fbd.SelectedPath;
                    Log($"已选中文件夹，准备批量处理: {fbd.SelectedPath}");
                }
            }
        }

        // 【重写】：入口方法，负责调度
        private void btnRun_Click(object sender, EventArgs e)
        {
            string inputPath = txtFilePath.Text;

            if (string.IsNullOrEmpty(inputPath))
            {
                Log("请先选择有效的文件或文件夹路径！");
                return;
            }

            btnRun.Enabled = false;

            try
            {
                // 1. 初始化 NX (提出来，只初始化一次)
                Log("正在连接 NX Session...");
                if (theSession == null)
                {
                    theSession = Session.GetSession();
                    theUFSession = UFSession.GetUFSession();
                }

                // 2. 智能分发
                if (File.Exists(inputPath) && inputPath.ToLower().EndsWith(".prt"))
                {
                    // 单文件处理
                    Log($"\r\n====== 开始单文件处理: {Path.GetFileName(inputPath)} ======");
                    ProcessSinglePart(inputPath);
                    Log("====== 处理完成 ======");
                }
                else if (Directory.Exists(inputPath))
                {
                    // 批量文件夹处理
                    string[] prtFiles = Directory.GetFiles(inputPath, "*.prt");
                    if (prtFiles.Length == 0)
                    {
                        Log("该文件夹下没有找到 PRT 文件。");
                        return;
                    }

                    Log($"\r\n====== 启动批量任务，共发现 {prtFiles.Length} 个文件 ======");
                    int successCount = 0;

                    for (int i = 0; i < prtFiles.Length; i++)
                    {
                        Log($"\r\n>>> 进度 [{i + 1}/{prtFiles.Length}] 正在处理: {Path.GetFileName(prtFiles[i])}");
                        try
                        {
                            ProcessSinglePart(prtFiles[i]);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            Log($"[跳过] 文件处理失败: {ex.Message}");
                        }
                    }
                    Log($"\r\n====== 批量任务结束！成功: {successCount}/{prtFiles.Length} ======");
                }
                else
                {
                    Log("无效的路径，请检查。");
                }
            }
            catch (Exception ex)
            {
                Log($"系统错误: {ex.Message}");
            }
            finally
            {
                btnRun.Enabled = true;
            }
        }

        // ================= 核心工作流 (你的原始代码，原封不动) =================

        // 【提取】：把你之前写在 btnRun_Click 里面的代码原封不动地搬过来
        private void ProcessSinglePart(string filePath)
        {
            Part workPart = null;
            string tempPrtPath = "";
            string tempStlPath = "";

            try
            {
                // 2. 打开原始部件
                Log($"正在打开文件: {Path.GetFileName(filePath)}");

                PartLoadStatus status = null;
                workPart = (Part)theSession.Parts.OpenBaseDisplay(filePath, out status);
                
                // 3. 提取结构化 CAM 信息
                Log("正在解析并构建结构化数据集...");
                PartDataset dataModel = CAMExtractor.ExtractDataModel(workPart);
                Log(CAMExtractor.GenerateLogText(dataModel));

                // 4. 几何隔离
                Log("正在进行几何隔离分析...");
                string isolationLog = ""; 
                // 【核心理解】：执行完这句后，内存里的 workPart 已经“变身”成了那个干净的临时文件
                tempPrtPath = GeometryIsolator.IsolateDesignPart(workPart, ref isolationLog);
                rtbLog.AppendText(isolationLog);
                Log($"提取完成，临时文件: {Path.GetFileName(tempPrtPath)}");

                // 5. 导出临时 STL (专门用于 Python 截图)
                Log("正在生成渲染网格 (STL)...");
                tempStlPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(tempPrtPath) + ".stl");
                
                // 【注意】：为了防止批量时上一轮的STL没删干净，加一句保护
                if (File.Exists(tempStlPath)) File.Delete(tempStlPath);

                ModelExporter.ExportToSTL(workPart, tempStlPath);

                // 6. 导出 STP (用于数据集存储)
                Log("正在导出高精模型 (STP)...");
                string outputDir = Path.Combine(Path.GetDirectoryName(filePath), "output_data");
                
                // 外部转换器是读磁盘上的文件，所以传 tempPrtPath 是没问题的
                ModelExporter.ExportToSTP(tempPrtPath, outputDir);
                
                // ---------------------------------------------------------
                // 【补全】STP 文件重命名逻辑
                // ---------------------------------------------------------
                string generatedStpName = Path.GetFileNameWithoutExtension(tempPrtPath) + ".stp";
                string generatedStpPath = Path.Combine(outputDir, generatedStpName);
                
                string finalStpName = Path.GetFileNameWithoutExtension(filePath) + ".stp";
                string targetStpPath = Path.Combine(outputDir, finalStpName);

                if (File.Exists(generatedStpPath))
                {
                    // 【注意】：防报错保护
                    if (File.Exists(targetStpPath)) File.Delete(targetStpPath);
                    File.Move(generatedStpPath, targetStpPath);
                    Log($"[成功] STP 已重命名为: {finalStpName}");
                }
                else
                {
                    Log("[警告] 找不到刚生成的 STP 文件，可能转换失败。");
                }
                
                // 记录 STP 的相对路径到 JSON 中
                dataModel.ModelFile_STP = $"output_data/{finalStpName}";

                // 7. 调用 Python 渲染引擎 (传入 STL 路径)
                Log("启动 Python 渲染引擎生成六视图...");
                string imageFolder = Path.Combine(Path.GetDirectoryName(filePath), "output_images");
                string baseName = Path.GetFileNameWithoutExtension(filePath);
                RunPythonScreenshot(tempStlPath, imageFolder, baseName);
                string[] views = { "TOP", "BOTTOM", "FRONT", "BACK", "LEFT", "RIGHT" };
                foreach(string v in views)
                {
                    dataModel.ViewImages.Add($"output_images/{baseName}_{v}.png");
                }
                
                // 8. 保存最终的 JSON 数据集清单
                string jsonFolder = Path.Combine(Path.GetDirectoryName(filePath), "output_json");
                CAMExtractor.SaveToJsonFile(dataModel, jsonFolder);
                Log($"[成功] 结构化数据集文件已保存至 output_json 文件夹。");
                
            }
            finally
            {
                // 9. 清理工作 (放入 finally 确保一定会执行，这极大地降低了 file already exists 的概率)
                if (workPart != null)
                {
                    try { workPart.Close(BasePart.CloseWholeTree.True, BasePart.CloseModified.UseResponses, null); } catch { }
                }
                try 
                {
                    if (File.Exists(tempPrtPath)) File.Delete(tempPrtPath);
                    if (File.Exists(tempStlPath)) File.Delete(tempStlPath); 
                } 
                catch {}
            }
        }

        // ================= 辅助方法 =================

        // 核心方法：跨语言调用 Python 脚本
        private void RunPythonScreenshot(string modelPath, string outputFolder, string baseName)
        {
            try
            {
                string scriptPath = Path.Combine(Application.StartupPath, "screenshot.py");
                if (!File.Exists(scriptPath)) scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "screenshot.py");

                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = "python.exe",
                    // 【修改】：在参数最后，加上带引号的 baseName
                    Arguments = $"\"{scriptPath}\" \"{modelPath}\" \"{outputFolder}\" \"{baseName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

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
            catch (Exception ex) { Log("Python 渲染失败: " + ex.Message); }
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