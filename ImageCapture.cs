// using System;
// using System.IO;
// using NXOpen;
// using NXOpen.UF;
//
// namespace DataProcessSystem
// {
//     public class ImageCapture
//     {
//         public static void CaptureSixViews(Part workPart, string outputFolder)
//         {
//             if (workPart == null) return;
//
//             if (!Directory.Exists(outputFolder))
//             {
//                 Directory.CreateDirectory(outputFolder);
//             }
//
//             string[] viewNames = { "TOP", "BOTTOM", "FRONT", "BACK", "LEFT", "RIGHT" };
//
//             Session theSession = Session.GetSession();
//             UFSession theUFSession = UFSession.GetUFSession();
//
//             // 获取当前布局和视图
//             Layout layout = workPart.Layouts.Current;
//             ModelingView currentView = workPart.ModelingViews.WorkView;
//
//             foreach (string viewName in viewNames)
//             {
//                 try
//                 {
//                     ModelingView targetView = workPart.ModelingViews.FindObject(viewName);
//                     if (targetView != null)
//                     {
//                         // 1. 切换视图
//                         layout.ReplaceView(currentView, targetView, true);
//                         currentView = targetView;
//                         
//                         // 2. 适应窗口
//                         targetView.Fit();
//                         
//                         // 3. 必须更新显示，否则打印出来是空的
//                         theUFSession.Disp.RegenerateDisplay();
//
//                         // 4. 构建文件路径
//                         string fileName = $"{Path.GetFileNameWithoutExtension(workPart.FullPath)}_{viewName}.jpg";
//                         string fullPath = Path.Combine(outputFolder, fileName);
//
//                         // 5. 使用 PrintBuilder 导出图片
//                         ExportUsingPrintBuilder(workPart, fullPath);
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     Console.WriteLine($"视图 {viewName} 处理失败: {ex.Message}");
//                 }
//             }
//         }
//
//         private static void ExportUsingPrintBuilder(Part workPart, string outputFile)
//         {
//             Session theSession = Session.GetSession();
//             PrintBuilder printBuilder = null;
//
//             try
//             {
//                 // 创建打印构建器
//                 printBuilder = workPart.PlotManager.CreatePrintBuilder();
//
//                 // 配置输出为 JPG 文件
//                 // 注意：NX 打印出的文件名通常会自动加后缀，或者需要我们处理
//                 printBuilder.OutputText = outputFile; 
//                 printBuilder.RasterImages = true; // 强制光栅化
//                 
//                 // 设置分辨率和格式
//                 // 这里的参数比较敏感，不同版本 NX 行为不同
//                 // 3 = JPEG 格式 (在某些版本API枚举中)
//                 // 如果找不到枚举，我们尝试用默认配置，依赖 OutputText 的扩展名
//                 
//                 // 设置纸张大小 (如果不设置可能会很小)
//                 printBuilder.Paper = PrintBuilder.PaperSize.A4;
//                 printBuilder.Orientation = PrintBuilder.OrientationTypes.Landscape;
//
//                 // 关键：设置为输出到文件
//                 printBuilder.Output = PrintBuilder.OutputTypes.File;
//                 
//                 // 设置打印源为当前布局/视图
//                 printBuilder.SourceBuilder.Type = PrintSourceBuilder.Types.Layout;
//
//                 // 提交打印
//                 NXObject nXObject = printBuilder.Commit();
//             }
//             catch (Exception ex)
//             {
//                 // 如果打印失败，不要让整个程序崩掉
//                 Console.WriteLine("PrintBuilder 导出失败: " + ex.Message);
//             }
//             finally
//             {
//                 if (printBuilder != null)
//                 {
//                     printBuilder.Destroy();
//                 }
//             }
//         }
//     }
// }