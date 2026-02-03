using System;
using NXOpen;
using NXOpen.Assemblies;

namespace DataProcessSystem
{
    public class AssemblyAnalyzer
    {
        // 返回设计模型的文件路径
        public static string FindDesignPart(Part rootPart)
        {
            try
            {
                ComponentAssembly assembly = rootPart.ComponentAssembly;
                if (assembly == null || assembly.RootComponent == null)
                {
                    // 如果不是装配体，那它自己可能就是零件，直接返回
                    return rootPart.FullPath;
                }

                // 遍历第一层组件
                // 在 CAM 文件中，通常名字里不带 "fixture" 或 "vise" 的，
                // 或者名字和 CAM 文件名高度相似的，就是设计模型。
                
                // 更好的策略：
                // NX CAM 内部有一个 "Geometry View"，里面明确定义了哪个是 Part，哪个是 Blank。
                // 但读取那个比较复杂。我们先用简单的“名称过滤法”。
                
                foreach (Component child in assembly.RootComponent.GetChildren())
                {
                    string childName = child.Name.ToUpper();
                    string childPath = child.Prototype.OwningPart.FullPath;

                    // 这里的逻辑需要根据你们实验室的文件命名规范来调整
                    // 假设：夹具通常叫 "FIXTURE", "VISE", "CLAMP"
                    // 假设：毛坯通常叫 "BLANK"
                    
                    if (childPath.Contains("model") || !IsFixture(childName))
                    {
                        // 这是一个极其简单的猜测，实际情况可能需要人工确认或更复杂的逻辑
                        Console.WriteLine($"[分析] 疑似设计模型: {childName} -> {childPath}");
                        return childPath;
                    }
                }

                // 如果没找到明显的，就返回根文件
                return rootPart.FullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[分析失败] {ex.Message}");
                return rootPart.FullPath;
            }
        }

        private static bool IsFixture(string name)
        {
            name = name.ToUpper();
            return name.Contains("FIX") || name.Contains("VISE") || name.Contains("CLAMP") || name.Contains("JIG");
        }
    }
}