using System;
using System.Diagnostics;
using System.IO;
using NXOpen;

namespace DataProcessSystem
{
    public class ModelExporter
    {
        public static void ExportToSTP(Part workPart, string outputFolder)
        {
            if (workPart == null) return;
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            string stpFileName = $"{Path.GetFileNameWithoutExtension(workPart.FullPath)}.stp";
            string outputFile = Path.Combine(Path.GetFullPath(outputFolder), stpFileName);

            // 1. 设置转换器路径 (填入你找到的绝对路径)
            string translatorPath = @"E:\Program Files\Siemens\NX12.0\STEP214UG\step214ug.exe";

            if (!File.Exists(translatorPath))
            {
                Console.WriteLine($"[错误] 找不到转换器，请检查路径: {translatorPath}");
                return;
            }

            Console.WriteLine($"[DEBUG] 使用转换器: {translatorPath}");
            Console.WriteLine($"[DEBUG] 正在导出: {workPart.FullPath}");

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = translatorPath;
                
                // 【关键修改】参数格式改为：O="输出路径" "输入路径"
                process.StartInfo.Arguments = $"O=\"{outputFile}\" \"{workPart.FullPath}\"";
                
                // 关键配置：隐藏窗口，重定向输出
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                // 【关键修复】手动补全所有必要的环境变量
                // 1. 推算 UGII_BASE_DIR (E:\Program Files\Siemens\NX12.0)
                string baseDir = Path.GetDirectoryName(Path.GetDirectoryName(translatorPath));
                
                // 2. 推算 UGII_ROOT_DIR (E:\Program Files\Siemens\NX12.0\UGII) - 核心DLL都在这
                string rootDir = Path.Combine(baseDir, "UGII");
                
                // 3. 推算 STEP214UG_DIR (E:\Program Files\Siemens\NX12.0\STEP214UG) - 报错里缺的那个
                string stepDir = Path.Combine(baseDir, "STEP214UG");
                
                // 【关键修复】确保路径末尾有斜杠
                if (!stepDir.EndsWith("\\"))
                {
                    stepDir += "\\";
                }

                // 设置变量
                process.StartInfo.EnvironmentVariables["UGII_BASE_DIR"] = baseDir;
                process.StartInfo.EnvironmentVariables["UGII_ROOT_DIR"] = rootDir;
                process.StartInfo.EnvironmentVariables["STEP214UG_DIR"] = stepDir;
                
                // 还要确保 PATH 包含 UGII 目录，否则可能会报找不到 DLL
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                process.StartInfo.EnvironmentVariables["PATH"] = $"{rootDir};{currentPath}";

                // 调试信息：打印一下我们设置了什么，方便排查
                Console.WriteLine($"[DEBUG] 设置环境变量 STEP214UG_DIR = {stepDir}");

                // 启动进程
                process.Start();
                
                // 等待结束
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // 检查结果
                if (File.Exists(outputFile) && new FileInfo(outputFile).Length > 0)
                {
                    Console.WriteLine("[成功] STP 导出完成！");
                }
                else
                {
                    Console.WriteLine("[失败] STP 文件未生成。");
                    Console.WriteLine($"转换器输出:\n{output}");
                    Console.WriteLine($"错误信息:\n{error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[异常] 调用转换器出错: {ex.Message}");
            }
        }
    }
}