using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using NXOpen;
using NXOpen.UF;

namespace DataProcessSystem
{
    public class ModelExporter
    {
        public static void ExportToSTL(Part workPart, string outputFile)
        {
            if (workPart == null) return;
            
            string folder = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            UFSession theUF = UFSession.GetUFSession();
            IntPtr fileHandle = IntPtr.Zero; 

            try
            {
                // 1. 打开二进制 STL 文件
                theUF.Std.OpenBinaryStlFile(outputFile, false, "Dataset_Export", out fileHandle);

                if (fileHandle == IntPtr.Zero) throw new Exception("无法创建 STL 文件句柄。");

                // 2. 遍历并存入实体
                Body[] bodies = workPart.Bodies.ToArray();
                foreach (Body b in bodies)
                {
                    if (b.IsSolidBody)
                    {
                        int numErrors;
                        // 【修正点】：使用 .NET 包装后的数组类型
                        UFStd.StlError[] errorInfo;

                        // 参数: 句柄, 坐标系, 实体, min_len, max_len, 精度, out 错误数, out 错误数组
                        theUF.Std.PutSolidInStlFile(
                            fileHandle, 
                            Tag.Null, 
                            b.Tag, 
                            0.0, 
                            0.0, 
                            0.01, 
                            out numErrors, 
                            out errorInfo // 这里不再使用 IntPtr，而是托管数组
                        );
                        
                        // 注意：在 .NET 包装层中，这种 out 数组不需要手动调用 theUF.Free()
                    }
                }
                Console.WriteLine($"[STL 写入成功] {outputFile}");
            }
            catch (Exception ex)
            {
                throw new Exception("STL 导出失败: " + ex.Message);
            }
            finally
            {
                if (fileHandle != IntPtr.Zero)
                {
                    theUF.Std.CloseStlFile(fileHandle);
                }
            }
        }

        // STP 导出 (保持之前那个稳健的外部 EXE 版本)
        public static void ExportToSTP(string inputFilePath, string outputFolder)
        {
            if (!File.Exists(inputFilePath)) return;
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            string stpFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}.stp";
            string outputFile = Path.Combine(Path.GetFullPath(outputFolder), stpFileName);
            string translatorPath = @"E:\Program Files\Siemens\NX12.0\STEP214UG\step214ug.exe";

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = translatorPath;
                process.StartInfo.Arguments = $"O=\"{outputFile}\" \"{inputFilePath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                string baseDir = Path.GetDirectoryName(Path.GetDirectoryName(translatorPath));
                string rootDir = Path.Combine(baseDir, "UGII");
                string stepDir = Path.Combine(baseDir, "STEP214UG");
                if (!stepDir.EndsWith("\\")) stepDir += "\\";

                process.StartInfo.EnvironmentVariables["UGII_BASE_DIR"] = baseDir;
                process.StartInfo.EnvironmentVariables["UGII_ROOT_DIR"] = rootDir;
                process.StartInfo.EnvironmentVariables["STEP214UG_DIR"] = stepDir;
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                process.StartInfo.EnvironmentVariables["PATH"] = $"{rootDir};{currentPath}";

                process.Start();
                // 【核心修复】：必须在 WaitForExit 之前读取数据，清空缓冲区，防止死锁
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                // 可选：将转换器的输出打印到控制台，方便排查潜在错误
                if (!string.IsNullOrEmpty(error) || output.Contains("Error"))
                {
                    Console.WriteLine($"[STP 转换信息]:\n{output}\n{error}");
                }
                else
                {
                    Console.WriteLine($"[STP 转换成功] {stpFileName}");
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine("STP 异常: " + ex.Message); 
            }
        }
    }
}