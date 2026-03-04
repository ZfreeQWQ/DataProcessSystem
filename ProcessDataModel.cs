using System;
using System.Collections.Generic;

namespace DataProcessSystem
{
    // 根节点：一个零件的完整数据档案
    public class PartDataset
    {
        public string PartName { get; set; }
        public string ExtractTime { get; set; }
        
        // 关联的文件路径（相对路径，方便数据集移动）
        public string ModelFile_STP { get; set; }
        public List<string> ViewImages { get; set; } = new List<string>();

        // 工艺信息
        public List<ToolInfo> Tools { get; set; } = new List<ToolInfo>();
        public List<OperationInfo> Operations { get; set; } = new List<OperationInfo>();
    }

    // 刀具信息
    public class ToolInfo
    {
        public string ToolName { get; set; }
        public string ToolType { get; set; }
        public double Diameter { get; set; }
    }

    // 工序信息
    public class OperationInfo
    {
        public int StepIndex { get; set; }
        public string OperationName { get; set; }
        public string OperationType { get; set; }
        public double SpindleSpeed_RPM { get; set; }
        public double FeedRate_MMPM { get; set; }
        // 进阶：如果工序有使用的刀具，也可以存在这里
        public string UsedToolName { get; set; } 
    }
}