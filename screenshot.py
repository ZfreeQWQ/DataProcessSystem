import pyvista as pv
import sys
import os

def generate_screenshots(input_file, output_folder):
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    try:
        # 1. 加载模型
        mesh = pv.read(input_file)
        
        # 【关键：清理网格】STL 文件通常是散落的三角形，必须先清理并合并重合顶点
        mesh = mesh.clean()
        
        # 重新计算法线，保证面片平整
        mesh = mesh.compute_normals(split_vertices=True)
        
        # 【美化核心：提取特征边缘】
        # 提取夹角大于 30 度的边界线 (孔的边缘、零件外轮廓等)
        edges = mesh.extract_feature_edges(boundary_edges=True, 
                                           feature_edges=True, 
                                           manifold_edges=False, 
                                           feature_angle=30)
    except Exception as e:
        print(f"Error loading model: {e}")
        return

    # 2. 设置绘图窗口
    # multi_samples=4 开启抗锯齿，让黑线更平滑
    plotter = pv.Plotter(off_screen=True, window_size=[1024, 1024], line_smoothing=True)
    
    # 添加主模型 (使用类似 NX/SolidWorks 默认的高级蓝灰色)
    plotter.add_mesh(mesh, color='#D0D5DB', smooth_shading=False, 
                     ambient=0.3, diffuse=0.8, specular=0.1)
                     
    # 【美化核心：绘制黑线】把刚才提取的特征边盖在模型上面
    plotter.add_mesh(edges, color='black', line_width=2.5)

    plotter.set_background("white")

    # 开启正交投影（工业制图标准）
    plotter.enable_parallel_projection()

    if len(sys.argv) > 3:
            base_name = sys.argv[3]
    else:
        base_name = os.path.splitext(os.path.basename(input_file))[0]

    # 定义 6 个视角
    view_funcs = {
        "TOP":    lambda: plotter.view_xy(),
        "BOTTOM": lambda: plotter.view_xy(negative=True),
        "FRONT":  lambda: plotter.view_xz(),
        "BACK":   lambda: plotter.view_xz(negative=True),
        "RIGHT":  lambda: plotter.view_yz(),
        "LEFT":   lambda: plotter.view_yz(negative=True)
    }

    # 3. 循环截图
    for v_name, func in view_funcs.items():
        func()
        plotter.reset_camera()
        
        output_path = os.path.join(output_folder, f"{base_name}_{v_name}.png")
        plotter.screenshot(output_path)
        print(f"Saved: {v_name}")

    plotter.close()

if __name__ == "__main__":
    if len(sys.argv) > 3:
        generate_screenshots(sys.argv[1], sys.argv[2])
    elif len(sys.argv) > 2:
        generate_screenshots(sys.argv[1], sys.argv[2])