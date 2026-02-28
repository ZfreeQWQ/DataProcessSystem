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
            UFSession theUF = UFSession.GetUFSession();

            try
            {
                // 【核心】在外部模式下必须检测并初始化 CAM Session
                bool isInit = false;
                theUF.Cam.IsSessionInitialized(out isInit);
                if (!isInit) theUF.Cam.InitSession();

                sb.AppendLine("=== 工艺信息提取报告 ===");
                sb.AppendLine($"文件: {workPart.Leaf}");
                sb.AppendLine("----------------------------");

                if (workPart.CAMSetup == null) return "未检测到 CAM 设置。";

                CAMSetup camSetup = workPart.CAMSetup;

                // 1. 刀具
                sb.AppendLine("[刀具列表]");
                foreach (NCGroup group in camSetup.CAMGroupCollection)
                {
                    if (group is Tool)
                    {
                        double dia = 0;
                        try { theUF.Param.AskDoubleValue(group.Tag, 1, out dia); } catch { }
                        sb.AppendLine($" - {group.Name} (D={dia}mm)");
                    }
                }

                // 2. 工序参数 (11=转速, 16=进给)
                sb.AppendLine("\n[工序流程]");
                int i = 1;
                foreach (CAMObject obj in camSetup.CAMOperationCollection)
                {
                    if (obj is Operation)
                    {
                        double rpm = 0, feed = 0;
                        // 这里尝试获取“生效值”
                        try { theUF.Param.AskDoubleValue(obj.Tag, 11, out rpm); } catch { }
                        try { theUF.Param.AskDoubleValue(obj.Tag, 16, out feed); } catch { }
                        
                        sb.AppendLine($"#{i} {obj.Name}: {rpm}RPM / {feed}MMPM");
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("提取异常: " + ex.Message);
            }
            return sb.ToString();
        }
    }
}