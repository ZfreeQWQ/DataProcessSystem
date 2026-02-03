using System;
using System.Text;
using NXOpen;
using NXOpen.CAM;
using NXOpen.UF;
using Operation = NXOpen.CAM.Operation;

namespace DataProcessSystem
{
    public class CAMExtractor
    {
        public static string ExtractProcessInfo(Part workPart)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== 工艺信息提取报告 ===");
            sb.AppendLine($"文件: {workPart.Leaf}");
            sb.AppendLine("----------------------------");

            try
            {
                if (workPart.CAMSetup == null)
                {
                    sb.AppendLine("提示: 未检测到 CAM Setup 环境。");
                    return sb.ToString();
                }

                UFSession theUF = UFSession.GetUFSession();
                CAMSetup camSetup = workPart.CAMSetup;

                // 1. 刀具列表 (新增：提取刀具直径)
                sb.AppendLine("[刀具列表]");
                foreach (CAMObject obj in camSetup.CAMGroupCollection)
                {
                    if (obj is Tool)
                    {
                        Tool tool = (Tool)obj;
                        string toolType = tool.GetType().Name;
                        double diameter = 0.0;
                        
                        // 尝试提取刀具直径 (UF_PARAM 1 = Diameter)
                        try 
                        { 
                            theUF.Param.AskDoubleValue(tool.Tag, 1, out diameter); 
                        } 
                        catch { diameter = -1; }

                        sb.AppendLine($" - 刀具: {tool.Name}");
                        sb.AppendLine($"   类型: {toolType}");
                        if (diameter > 0) sb.AppendLine($"   直径: {diameter} mm");
                    }
                }
                sb.AppendLine("");

                // 2. 工序流程
                sb.AppendLine("[工序流程]");
                int opIndex = 1;
                foreach (CAMObject obj in camSetup.CAMOperationCollection)
                {
                    if (obj is Operation)
                    {
                        Operation op = (Operation)obj;
                        sb.AppendLine($"#{opIndex} 工序: {op.Name}");

                        // 尝试提取参数
                        // 如果 UF_PARAM 失败，我们尝试从关联的刀具反推（有时工艺数据挂在刀具上）
                        double spindle = 0;
                        double feed = 0;
                        
                        try 
                        {
                            // 11 = Spindle RPM (主轴转速)
                            theUF.Param.AskDoubleValue(op.Tag, 11, out spindle);
                            
                            // 16 = Feed Cut (切削进给)
                            theUF.Param.AskDoubleValue(op.Tag, 16, out feed);
                        }
                        catch {}

                        if (spindle > 0.001) 
                            sb.AppendLine($"    -> 主轴: {spindle} RPM");
                        else 
                            sb.AppendLine($"    -> 主轴: (继承或未设置)");

                        if (feed > 0.001) 
                            sb.AppendLine($"    -> 进给: {feed} MMPM");
                        else 
                            sb.AppendLine($"    -> 进给: (继承或未设置)");
                        
                        opIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"提取错误: {ex.Message}");
            }
            return sb.ToString();
        }
    }
}