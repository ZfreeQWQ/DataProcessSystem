using System;
using System.IO;
using System.Collections.Generic;
using NXOpen;
using NXOpen.Features;

namespace DataProcessSystem
{
    public class GeometryIsolator
    {
        public static string IsolateDesignPart(Part workPart, ref string logMsg)
        {
            Session theSession = Session.GetSession();
            Body designBody = null;
            int earliestTimestamp = int.MaxValue;

            logMsg += "=== 启动【特征历史溯源】算法 ===\r\n";

            try
            {
                Body[] allBodies = workPart.Bodies.ToArray();
                logMsg += $"检测到总实体数: {allBodies.Length}\r\n";

                // 1. 寻找“体 (1)”：即整个模型历史中最早生成的实体
                foreach (Body b in allBodies)
                {
                    // 获取产生该实体的特征
                    Feature[] features = b.GetFeatures();
                    if (features.Length > 0)
                    {
                        // 取该实体所有特征中最早的一个（Timestamp 越小越早）
                        foreach (Feature f in features)
                        {
                            if (f.Timestamp < earliestTimestamp)
                            {
                                earliestTimestamp = f.Timestamp;
                                designBody = b;
                            }
                        }
                    }
                }

                if (designBody == null)
                {
                    logMsg += "无法通过特征历史锁定零件，回退至首选模式。\r\n";
                    return workPart.FullPath;
                }

                logMsg += $"[锁定] 已定位到初始特征体: {(string.IsNullOrEmpty(designBody.Name) ? "体(1)" : designBody.Name)} (序号:{earliestTimestamp})\r\n";

                // 2. 准备剔除名单：除了 designBody，其他的全部删除
                List<NXObject> toDelete = new List<NXObject>();
                foreach (Body b in allBodies)
                {
                    if (b.Tag != designBody.Tag)
                    {
                        toDelete.Add(b);
                    }
                }

                // 3. 执行物理剔除 (为了保证 SaveAs 出来的 PRT 是纯净的)
                if (toDelete.Count > 0)
                {
                    logMsg += $"正在剔除后序生成的夹具与毛坯共 {toDelete.Count} 个实体...\r\n";
                    Session.UndoMarkId markId = theSession.SetUndoMark(Session.MarkVisibility.Invisible, "Isolate Body 1");
                    theSession.UpdateManager.AddToDeleteList(toDelete.ToArray());
                    theSession.UpdateManager.DoUpdate(markId);
                }

                // 4. 另存为临时文件
                string tempPath = Path.Combine(Path.GetTempPath(), $"Design_Root_{Guid.NewGuid().ToString().Substring(0, 8)}.prt");
                if (File.Exists(tempPath)) 
                {
                    File.Delete(tempPath);
                }
                workPart.SaveAs(tempPath);
                
                logMsg += $"[成功] 纯净模型已生成。\r\n";
                return tempPath;
            }
            catch (Exception ex)
            {
                logMsg += $"溯源提取失败: {ex.Message}\r\n";
                return workPart.FullPath;
            }
        }
    }
}