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

            try { /* 获取材质代码保持不变... */ } catch { }

            UFSession theUF = UFSession.GetUFSession();

            try
            {
                bool isInit = false;
                theUF.Cam.IsSessionInitialized(out isInit);
                if (!isInit) theUF.Cam.InitSession();

                if (workPart.CAMSetup == null) return dataset;

                CAMSetup camSetup = workPart.CAMSetup;

                // ==========================================
                // 1. 提取刀具 (使用官方 UF_PARAM 常量)
                // ==========================================
                foreach (NCGroup group in camSetup.CAMGroupCollection)
                {
                    if (group is Tool)
                    {
                        ToolInfo tInfo = new ToolInfo();
                        tInfo.ToolName = group.Name;
                        tInfo.ToolType = group.GetType().Name;
                        
                        int tNum = 0;
                        double dia = 0;

                        // 使用官方常量读取刀具号和直径
                        // 注意：不同NX版本 UFConstants 中的大小写可能不同，
                        // 如果报错，请在 VS 中敲下 NXOpen.UF.UFConstants. 后利用代码提示(IntelliSense)寻找包含 TL_NUMBER 的常量名
                        try { theUF.Param.AskIntValue(group.Tag, NXOpen.UF.UFConstants.UF_PARAM_TL_NUMBER, out tNum); } catch { } 
                        try { theUF.Param.AskDoubleValue(group.Tag, NXOpen.UF.UFConstants.UF_PARAM_TL_DIAMETER, out dia); } catch { }

                        if (dia <= 0.01)
                        {
                            Match m = Regex.Match(group.Name, @"(?:-|^)(\d+(?:\.\d+)?)[A-Za-z]*");
                            if (m.Success) double.TryParse(m.Groups[1].Value, out dia);
                        }

                        tInfo.ToolNumber = tNum;
                        tInfo.Diameter = Math.Round(dia, 2);
                        dataset.Tools.Add(tInfo);
                    }
                }

                // ==========================================
                // 2. 提取工序 (使用官方 UF_PARAM 常量解决 0.0 问题)
                // ==========================================
                int stepIdx = 1;
                foreach (CAMObject obj in camSetup.CAMOperationCollection)
                {
                    if (obj is Operation op)
                    {
                        OperationInfo opInfo = new OperationInfo();
                        opInfo.StepIndex = stepIdx;
                        opInfo.OperationName = op.Name;
                        opInfo.OperationType = op.GetType().Name;

                        double rpm = 0.0;
                        double feed = 0.0;

                        // 【核心修复】使用 UF_PARAM_SPINDLE_RPM 和 UF_PARAM_FEED_CUT 常量！
                        // 这样 UFSession 会自动去底层数据结构中找到正确的内存地址取值
                        try { theUF.Param.AskDoubleValue(op.Tag, NXOpen.UF.UFConstants.UF_PARAM_SPINDLE_RPM, out rpm); } catch { }
                        try { theUF.Param.AskDoubleValue(op.Tag, NXOpen.UF.UFConstants.UF_PARAM_FEED_CUT, out feed); } catch { }

                        opInfo.SpindleSpeed_RPM = Math.Round(rpm, 2);
                        opInfo.FeedRate_MMPM = Math.Round(feed, 2);
                        
                        double totalTimeMin = 0.0;
                        try
                        {
                            // 调用正确的 UF_OPER 接口获取机床加工时间
                            totalTimeMin = op.GetToolpathTime();
                        }
                        catch (NXException)
                        {
                            // 如果报 NXException，说明源文件里的这道工序【没有生成刀轨】
                            // 此时底层直接抛出异常，我们在这里安全拦截，让时间保持为默认的 0.0 即可
                        }
                        catch (Exception)
                        {
                            // 拦截其他未知错误
                        }

                        // 保留两位小数，存入数据模型
                        opInfo.MachiningTime_MIN = Math.Round(totalTimeMin, 2);


                        // 关联刀具
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