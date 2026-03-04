using System;
using System.Collections.Generic;
using Newtonsoft.Json; // 引入 Json 库
using NXOpen;
using NXOpen.CAM;
using NXOpen.UF;
using System.IO;
using Operation = NXOpen.CAM.Operation;
using Path = System.IO.Path;
using System.Text.RegularExpressions;


namespace DataProcessSystem
{
    public class CAMExtractor
    {
        // 核心方法：提取数据并返回数据模型对象
        public static PartDataset ExtractDataModel(Part workPart)
        {
            PartDataset dataset = new PartDataset();
            dataset.PartName = Path.GetFileNameWithoutExtension(workPart.FullPath);
            dataset.ExtractTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            UFSession theUF = UFSession.GetUFSession();

            try
            {
                // 初始化 CAM
                bool isInit = false;
                theUF.Cam.IsSessionInitialized(out isInit);
                if (!isInit) theUF.Cam.InitSession();

                if (workPart.CAMSetup == null) return dataset;

                CAMSetup camSetup = workPart.CAMSetup;

                // 1. 提取刀具
                foreach (NCGroup group in camSetup.CAMGroupCollection)
                {
                    if (group is Tool)
                    {
                        ToolInfo tInfo = new ToolInfo();
                        tInfo.ToolName = group.Name;
                        tInfo.ToolType = group.GetType().Name;
                        
                        double dia = 0;
                        try { theUF.Param.AskDoubleValue(group.Tag, 1, out dia); } catch { }
                        // 如果提取出来是 0，就尝试从名字里正则匹配
                        if (dia <= 0.01)
                        {
                            // 匹配如 EMC-10G 里的 10，或者 DR-6.6 里的 6.6
                            Match m = Regex.Match(group.Name, @"(?:-|^)(\d+(?:\.\d+)?)[A-Za-z]*");
                            if (m.Success)
                            {
                                double.TryParse(m.Groups[1].Value, out dia);
                            }
                        }
                        tInfo.Diameter = Math.Round(dia, 2);
                        
                        dataset.Tools.Add(tInfo);
                    }
                }

                // 2. 提取工序
                int stepIdx = 1;
                foreach (CAMObject obj in camSetup.CAMOperationCollection)
                {
                    if (obj is Operation)
                    {
                        Operation op = (Operation)obj;
                        OperationInfo opInfo = new OperationInfo();
                        
                        opInfo.StepIndex = stepIdx;
                        opInfo.OperationName = op.Name;
                        opInfo.OperationType = op.GetType().Name;

                        double rpm = 0, feed = 0;
                        try { theUF.Param.AskDoubleValue(op.Tag, 11, out rpm); } catch { }
                        try { theUF.Param.AskDoubleValue(op.Tag, 16, out feed); } catch { }
                        
                        opInfo.SpindleSpeed_RPM = Math.Round(rpm, 2);
                        opInfo.FeedRate_MMPM = Math.Round(feed, 2);

                        // 尝试获取使用的刀具
                        try
                        {
                            NCGroup toolGroup = op.GetParent(CAMSetup.View.MachineTool);
                            if (toolGroup != null && toolGroup is Tool)
                            {
                                opInfo.UsedToolName = toolGroup.Name;
                            }
                        }
                        catch {}

                        dataset.Operations.Add(opInfo);
                        stepIdx++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("数据提取异常: " + ex.Message);
            }

            return dataset;
        }

        // 辅助方法：将数据集模型保存为 JSON 文件
        public static void SaveToJsonFile(PartDataset dataset, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
            
            string jsonFilePath = Path.Combine(outputFolder, $"{dataset.PartName}.json");
            
            // 序列化为格式化的 JSON 字符串
            string jsonString = JsonConvert.SerializeObject(dataset, Formatting.Indented);
            
            // 写入文件 (使用 UTF8 编码防止乱码)
            File.WriteAllText(jsonFilePath, jsonString, System.Text.Encoding.UTF8);
        }

        // 辅助方法：用于在界面上显示的文本日志 (保留兼容性)
        public static string GenerateLogText(PartDataset data)
        {
            return $"成功提取结构化数据：刀具 {data.Tools.Count} 把，工序 {data.Operations.Count} 道。";
        }
    }
}