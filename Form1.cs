using System;
using System.Collections.Generic;
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
        private List<PartDataset> batchDatasets = new List<PartDataset>();
        private int currentDataIndex = -1;
        private string currentBaseDir = "";

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
                    
                    string expectedJson = Path.Combine(Path.GetDirectoryName(inputPath), "output_json", Path.GetFileNameWithoutExtension(inputPath) + ".json");
                    currentBaseDir = Path.GetDirectoryName(inputPath);
                    
                    // 把这个单独的路径放到 List 里传过去
                    LoadResultsToDashboard(new List<string> { expectedJson });
                }
                else if (Directory.Exists(inputPath))
                {
                    // 批量文件夹处理
                    string[] prtFiles = Directory.GetFiles(inputPath, "*.prt");
                    if (prtFiles.Length == 0) return;

                    Log($"\r\n====== 启动批量任务，共发现 {prtFiles.Length} 个文件 ======");
                    int successCount = 0;
                    
                    // [新增] 用于记录本次成功处理了哪些 JSON 文件
                    List<string> successfulJsons = new List<string>();

                    for (int i = 0; i < prtFiles.Length; i++)
                    {
                        Log($"\r\n>>> 进度 [{i + 1}/{prtFiles.Length}] 正在处理: {Path.GetFileName(prtFiles[i])}");
                        try
                        {
                            ProcessSinglePart(prtFiles[i]);
                            successCount++;
                            
                            // [新增] 处理成功一个，就把它的期望 JSON 路径记录下来
                            string expectedJson = Path.Combine(inputPath, "output_json", Path.GetFileNameWithoutExtension(prtFiles[i]) + ".json");
                            successfulJsons.Add(expectedJson);
                        }
                        catch (Exception ex)
                        {
                            Log($"[跳过] 文件处理失败: {ex.Message}");
                        }
                    }
                    Log($"\r\n====== 批量任务结束！成功: {successCount}/{prtFiles.Length} ======");

                    // [修改这里] 只加载本次成功生成的那些 JSON
                    currentBaseDir = inputPath;
                    LoadResultsToDashboard(successfulJsons);
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
        
        // 展示一维结构化工艺数据
        private void Display1DData(PartDataset dataModel)
        {
            // 清空上次的数据
            treeViewData.Nodes.Clear();
    
            // 根节点：零件名称
            TreeNode rootNode = treeViewData.Nodes.Add($"零件: {dataModel.PartName}");
            rootNode.NodeFont = new Font(treeViewData.Font, FontStyle.Bold); // 加粗突出显示
    
            // 节点 1：刀具列表
            TreeNode toolsNode = rootNode.Nodes.Add($"所用刀具 ({dataModel.Tools.Count}把)");
            foreach (var tool in dataModel.Tools)
            {
                toolsNode.Nodes.Add($"[{tool.ToolType}] {tool.ToolName} (Φ{tool.Diameter}mm)");
            }

            // 节点 2：工艺路线 (工序)
            TreeNode opsNode = rootNode.Nodes.Add($"工艺路线 ({dataModel.Operations.Count}道工序)");
            foreach (var op in dataModel.Operations)
            {
                TreeNode stepNode = opsNode.Nodes.Add($"Step {op.StepIndex}: {op.OperationName}");
                stepNode.Nodes.Add($"加工类型: {op.OperationType}");
                stepNode.Nodes.Add($"主轴转速: {op.SpindleSpeed_RPM} RPM");
                stepNode.Nodes.Add($"进给速率: {op.FeedRate_MMPM} mm/min");
        
                if (!string.IsNullOrEmpty(op.UsedToolName))
                {
                    stepNode.Nodes.Add($"绑定刀具: {op.UsedToolName}");
                }
            }
    
            // 展开所有节点以便直接查看
            treeViewData.ExpandAll();
            // 让滚动条回到最顶端
            treeViewData.SelectedNode = rootNode;
        }
        
        // 展示二维多视角渲染图
        private void Display2DImages(PartDataset dataModel, string workDirectory)
        {
            // 1. 安全清空旧图片，释放内存（重要：不仅释放Image，还要释放PictureBox控件本身）
            foreach (Control ctrl in flowLayoutPanelImages.Controls)
            {
                if (ctrl is PictureBox oldPb)
                {
                    if (oldPb.Image != null) oldPb.Image.Dispose();
                    oldPb.Dispose();
                }
            }
            flowLayoutPanelImages.Controls.Clear();

            // 2. 遍历加载新图片
            foreach (string relativePath in dataModel.ViewImages)
            {
                string fullPath = Path.Combine(workDirectory, relativePath.Replace("/", "\\"));
        
                if (File.Exists(fullPath))
                {
                    PictureBox pb = new PictureBox
                    {
                        Width = 220,
                        Height = 220,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BorderStyle = BorderStyle.FixedSingle,
                        Margin = new Padding(10)
                    };

                    try
                    {
                        // 【终极修复】：将文件直接读取为纯字节数组，存入内存流
                        // 这样生成的 Image 完全独立存在于内存，永远不会去读磁盘，完美解决“参数无效”报错
                        byte[] imageBytes = File.ReadAllBytes(fullPath);
                        MemoryStream ms = new MemoryStream(imageBytes);
                        pb.Image = Image.FromStream(ms);
                
                        flowLayoutPanelImages.Controls.Add(pb);
                    }
                    catch (Exception ex)
                    {
                        Log($"[警告] 图片加载失败 {fullPath}: {ex.Message}");
                    }
                }
            }
        }
        
        // 核心创新点：自然语言工艺提示词生成器 (NLG)
        private string GenerateNaturalLanguagePrompt(PartDataset dataModel)
        {
            System.Text.StringBuilder prompt = new System.Text.StringBuilder();

            prompt.AppendLine("【系统提示】你是一个资深的航空航天非标零件机械加工工艺专家。请根据以下结构化工艺数据，分析该零件的加工难点并评估加工策略。\n");
            prompt.AppendLine("【零件基本信息】");
            prompt.AppendLine($"当前待加工零件名称为“{dataModel.PartName}”。经过系统自动解析，该零件的制造材质为“{dataModel.Material}”。");
            prompt.AppendLine($"为完成该零件的加工，制造端共配置了 {dataModel.Tools.Count} 把刀具，工艺路线共包含 {dataModel.Operations.Count} 道加工工序。\n");

            prompt.AppendLine("【刀具库清单】");
            foreach (var tool in dataModel.Tools)
            {
                prompt.AppendLine($"- T{tool.ToolNumber}号刀具：属于 {tool.ToolType} 类型，名称为 {tool.ToolName}，其公称直径为 {tool.Diameter} mm。");
            }
            prompt.AppendLine();

            prompt.AppendLine("【详细工艺执行路线】");
            foreach (var op in dataModel.Operations)
            {
                prompt.Append($"第 {op.StepIndex} 步，执行 {op.OperationName} 工序（操作类型：{op.OperationType}）。");
                if (!string.IsNullOrEmpty(op.UsedToolName))
                {
                    prompt.Append($"本工序调用了 {op.UsedToolName} 进行切削。");
                }
                prompt.AppendLine($"切削参数设定为：主轴转速 {op.SpindleSpeed_RPM} RPM，进给速率 {op.FeedRate_MMPM} mm/min。");
            }

            prompt.AppendLine("\n【用户指令】请基于上述参数，评估这些切削参数对于该材质是否合理，并输出一份工艺优化建议报告。");

            return prompt.ToString();
        }

        private void btnToggleView_Click(object sender, EventArgs e)
        {
            // 如果当前显示的是树状图 (结构化数据)
            if (treeViewData.Visible)
            {
                treeViewData.Visible = false;
                rtbPrompt.Visible = true;
                btnToggleView.Text = "切换至结构化数据 (JSON)";
                btnToggleView.BackColor = Color.LightGreen; // 给个颜色提示
            }
            else
            {
                // 如果当前显示的是文本 (自然语言)
                rtbPrompt.Visible = false;
                treeViewData.Visible = true;
                btnToggleView.Text = "切换至大模型提示词 (Prompt)";
                btnToggleView.BackColor = SystemColors.Control;
            }
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (currentDataIndex > 0)
            {
                currentDataIndex--;
                UpdateDashboard();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (currentDataIndex < batchDatasets.Count - 1)
            {
                currentDataIndex++;
                UpdateDashboard();
            }
        }
        private void UpdateDashboard()
        {
            if (batchDatasets == null || batchDatasets.Count == 0 || currentDataIndex < 0) return;

            PartDataset currentData = batchDatasets[currentDataIndex];
    
            // 刷新页码
            lblPage.Text = $"{currentDataIndex + 1} / {batchDatasets.Count}";
    
            // 控制按钮是否可用
            btnPrev.Enabled = (currentDataIndex > 0);
            btnNext.Enabled = (currentDataIndex < batchDatasets.Count - 1);

            // 渲染一维和二维数据
            Display1DData(currentData);
            rtbPrompt.Text = GenerateNaturalLanguagePrompt(currentData); // 如果你加了 Prompt 功能
            Display2DImages(currentData, currentBaseDir);

            Log($"正在查看: {currentData.PartName}");
        }
        
        private void LoadResultsToDashboard(List<string> jsonFilesList)
        {
            batchDatasets.Clear();
            currentDataIndex = -1;

            foreach (string file in jsonFilesList)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        // 读取 JSON 结构化数据
                        string jsonContent = File.ReadAllText(file, System.Text.Encoding.UTF8);
                        PartDataset ds = Newtonsoft.Json.JsonConvert.DeserializeObject<PartDataset>(jsonContent);
                        if (ds != null)
                        {
                            batchDatasets.Add(ds);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"[警告] 读取数据集文件失败: {file}, 错误: {ex.Message}");
                }
            }

            if (batchDatasets.Count > 0)
            {
                currentDataIndex = 0;
                Log($"\r\n>>> 成功加载 {batchDatasets.Count} 个零件数据集，可使用【上一个/下一个】浏览。");
                UpdateDashboard(); // 刷新界面
            }
        }
    }
}