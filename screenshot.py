import pyvista as pv
import sys
import os

def generate_screenshots(input_file, output_folder):
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    try:
        # 1. 加载模型
        mesh = pv.read(input_file)
        
        # 【关键修复1】重新计算法线，并在锐角处分离顶点，消除机械零件的黑色渐变阴影
        mesh = mesh.compute_normals(split_vertices=True)
    except Exception as e:
        print(f"Error loading model: {e}")
        return

    # 2. 设置绘图窗口
    plotter = pv.Plotter(off_screen=True)
    
    # 【关键修复2】关闭平滑着色 (smooth_shading=False)，加入合适的环境光
    plotter.add_mesh(mesh, color='#B0B0B0', smooth_shading=False, 
                     ambient=0.3, diffuse=0.7, specular=0.2)
    plotter.set_background("white")

    # 【关键修复3】开启正交投影（工业制图标准，消除近大远小的透视变形）
    plotter.enable_parallel_projection()

    base_name = os.path.splitext(os.path.basename(input_file))[0]

    # 【关键修复4】使用 PyVista 官方的标准视图设置方法，自动处理 Up-Vector，消除警告
    view_funcs = {
        "TOP":    lambda: plotter.view_xy(),               # 俯视图 (看 XY 平面)
        "BOTTOM": lambda: plotter.view_xy(negative=True),  # 仰视图
        "FRONT":  lambda: plotter.view_xz(),               # 主视图 (看 XZ 平面)
        "BACK":   lambda: plotter.view_xz(negative=True),  # 后视图
        "RIGHT":  lambda: plotter.view_yz(),               # 右视图 (看 YZ 平面)
        "LEFT":   lambda: plotter.view_yz(negative=True)   # 左视图
    }

    # 3. 循环截图
    for v_name, func in view_funcs.items():
        func() # 调用对应的视角函数
        plotter.reset_camera() # 自动缩放，让零件填满画面
        
        output_path = os.path.join(output_folder, f"{base_name}_{v_name}.png")
        plotter.screenshot(output_path)
        print(f"Saved: {v_name}")

    plotter.close()

if __name__ == "__main__":
    if len(sys.argv) > 2:
        generate_screenshots(sys.argv[1], sys.argv[2])