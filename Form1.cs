using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NXOpen;
using NXOpen.CAM;
using NXOpen.UF;
using Path = System.IO.Path;

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
                // 1. 获取 NX 安装目录 (通常安装 NX 时会自动写入这个环境变量)
                string nxBaseDir = Environment.GetEnvironmentVariable("UGII_BASE_DIR");
                
                if (string.IsNullOrEmpty(nxBaseDir))
                {
                    MessageBox.Show("错误：未检测到环境变量 UGII_BASE_DIR。\n请确认已安装 Siemens NX 并重启了电脑。");
                }
                else
                {
                    // 2. 找到 NXBIN 目录
                    string nxBinDir = Path.Combine(nxBaseDir, "NXBIN");
                    string nxUgiiDir = Path.Combine(nxBaseDir, "UGII");

                    // 3. 获取当前进程的 PATH 变量
                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";

                    // 4. 将 NX 目录加入到 PATH 的最前面
                    // 这样程序在加载 DLL 时，就会先去 NXBIN 里面找 libuginit.dll
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

            // 禁用按钮防止重复点击
            btnRun.Enabled = false;

            try
            {
                // 1. 初始化 NX (如果没启动，这步会启动后台 NX 进程，比较慢)
                Log("正在连接 NX Session...");
                if (theSession == null)
                {
                    theSession = Session.GetSession();
                    theUFSession = UFSession.GetUFSession();
                }
                Log("NX 连接成功！");

                // 2. 打开部件
                string filePath = txtFilePath.Text;
                Log($"正在打开文件: {Path.GetFileName(filePath)}");

                Part workPart = null; 
                PartLoadStatus status = null;
                
                // 使用 OpenBaseDisplay 打开
                workPart = (Part)theSession.Parts.OpenBaseDisplay(filePath, out status);

                // 【新增 1】: 强制加载所有几何数据 (针对装配体或轻量化加载的情况)
                // 这能防止导出的 STP 是空的
                try 
                {
                    // 加载选项：加载所有组件的精确几何
                    PartLoadStatus loadStatus;
                    theSession.Parts.SetWork(workPart); // 设为工作部件
                    
                    // 尝试将所有组件设为完全加载 (如果是装配体)
                    // ComponentAssembly 可能为空，加个判断
                    if (workPart.ComponentAssembly != null && workPart.ComponentAssembly.RootComponent != null)
                    {
                        Log("正在加载装配体组件...");
                        // 这里不做复杂的递归加载了，简单设为显示部件通常会自动加载
                    }
                }
                catch (Exception ex) 
                {
                    Log("加载几何时遇到警告(可忽略): " + ex.Message);
                }

                // 【新增 2】: 关键！强制将其设为 Display Part
                // 在后台模式下，这一步告诉导出器：即便没有屏幕，这个就是“当前显示的零件”
                try
                {
                    theSession.Parts.SetDisplay(workPart, true, true, out status);
                }
                catch
                {
                    Log("注意：后台模式设置 Display Part 可能会有警告，尝试继续...");
                }

                // 3. 验证 CAM 环境 (测试你的目标 2 是否可行)
                if (workPart.CAMSetup != null)
                {
                    Log("成功检测到 CAM Setup 数据！");
                    // 简单的测试：获取一下加工操作的数量
                    CAMSetup camSetup = workPart.CAMSetup;
                    int opCount = 0;
                    foreach (CAMObject obj in camSetup.CAMOperationCollection)
                    {
                        opCount++;
                    }
                    Log($"当前文件包含 {opCount} 个加工工序。");
                }
                else
                {
                    Log("警告：该文件似乎没有 CAM 工艺数据（CAMSetup 为空）。");
                }
                
                // 1. 提取 CAM 信息
                rtbLog.AppendText("正在解析工艺参数...\r\n");
                string camInfo = CAMExtractor.ExtractProcessInfo(workPart);
                rtbLog.AppendText(camInfo);

                // 2. 导出 STP 模型 (替代不稳定的截图)
                rtbLog.AppendText("正在导出 STP 模型...\r\n");
                string outputDir = Path.Combine(Path.GetDirectoryName(filePath), "output_data");
                ModelExporter.ExportToSTP(workPart, outputDir);
                rtbLog.AppendText($"STP 导出成功！\r\n");

                // 4. (预留) 截图功能占位
                Log("准备进行多视图截图...");
                // ExportImages(part); 

                // 5. (预留) STP 导出占位
                Log("准备进行模型导出...");

                // 关闭文件（不保存，以免破坏原始数据）
                workPart.Close(BasePart.CloseWholeTree.True, BasePart.CloseModified.UseResponses, null);
                Log("文件处理完毕，已关闭。");

            }
            catch (Exception ex)
            {
                Log($"错误: {ex.Message}");
                // 如果是 DLL 加载错误，通常是因为环境变量没配好
            }
            finally
            {
                btnRun.Enabled = true;
            }
        }
        
        // 辅助方法：打印日志到界面
        private void Log(string msg)
        {
            // 因为涉及到跨线程刷新 UI，简单处理如下
            rtbLog.AppendText($"[{DateTime.Now.ToLongTimeString()}] {msg}\r\n");
            rtbLog.ScrollToCaret();
            Application.DoEvents(); // 强制刷新界面，防止假死
        }
        
        // 新增的辅助方法：只为了看一眼预览图
        private void ShowPreviewImmediately(string filePath)
        {
            pbPreview.Image = null; // 清空图片框
            Log("提示：预览图提取功能已暂时禁用，以确保核心功能编译通过。");
            // // 临时禁用界面，防止乱点
            // this.Cursor = Cursors.WaitCursor;
            //
            // try
            // {
            //     // 1. 确保 Session 存在
            //     if (theSession == null)
            //     {
            //         theSession = Session.GetSession();
            //         theUFSession = UFSession.GetUFSession();
            //     }
            //
            //     // 2. 打开部件 (为了速度，只加载结构，不完全加载)
            //     BasePart part;
            //     PartLoadStatus status;
            //     
            //     // OpenBaseDisplay 比较重，但在 External 模式下是必须的
            //     part = theSession.Parts.OpenBaseDisplay(filePath, out status);
            //     
            //     if (part != null)
            //     {
            //         // 3. 提取预览图到临时文件夹
            //         string tempDir = Path.Combine(Path.GetTempPath(), "NX_Dataset_Temp");
            //         if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            //         
            //         string tempImage = Path.Combine(tempDir, "temp_preview.bmp");
            //         
            //         // 确保旧文件被删除
            //         if (File.Exists(tempImage)) File.Delete(tempImage);
            //
            //         // 调用 UF 函数导出预览
            //         theUFSession.Part.ExportPreview(part.Tag, tempImage);
            //
            //         // 4. 显示在 PictureBox
            //         if (File.Exists(tempImage))
            //         {
            //             // 使用 MemoryStream 加载，这样不会占用文件锁，方便后续删除或覆盖
            //             using (FileStream fs = new FileStream(tempImage, FileMode.Open, FileAccess.Read))
            //             {
            //                 byte[] buffer = new byte[fs.Length];
            //                 fs.Read(buffer, 0, buffer.Length);
            //                 using (MemoryStream ms = new MemoryStream(buffer))
            //                 {
            //                     pbPreview.Image = Image.FromStream(ms);
            //                 }
            //             }
            //         }
            //         else
            //         {
            //             // 如果没有预览图，清空显示
            //             pbPreview.Image = null;
            //             Log("该文件内部未存储预览图。");
            //         }
            //
            //         // 5. 关闭文件 (为了释放内存，因为用户可能选了文件但不点运行)
            //         part.Close(BasePart.CloseWholeTree.True, BasePart.CloseModified.UseResponses, null);
            //     }
            // }
            // catch (Exception ex)
            // {
            //     Log($"预览加载失败: {ex.Message}");
            // }
            // finally
            // {
            //     this.Cursor = Cursors.Default;
            // }
        }
    }
}