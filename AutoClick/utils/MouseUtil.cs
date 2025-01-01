using System.Drawing;
using System.Runtime.InteropServices;

namespace AutoClick.utils;

class MouseUtil
{
    // 定义 mouse_event 函数的参数
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int X, int Y);

    //移动鼠标 
    const int MOUSEEVENTF_MOVE = 0x0001;

    //模拟鼠标左键按下 
    const int MOUSEEVENTF_LEFTDOWN = 0x0002;

    //模拟鼠标左键抬起 
    const int MOUSEEVENTF_LEFTUP = 0x0004;

    //模拟鼠标右键按下 
    const int MOUSEEVENTF_RIGHTDOWN = 0x0008;

    //模拟鼠标右键抬起 
    const int MOUSEEVENTF_RIGHTUP = 0x0010;

    //模拟鼠标中键按下 
    const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;

    //模拟鼠标中键抬起 
    const int MOUSEEVENTF_MIDDLEUP = 0x0040;

    //标示是否采用绝对坐标 
    const int MOUSEEVENTF_ABSOLUTE = 0x8000;

    //模拟鼠标滚轮滚动操作，必须配合dwData参数
    const int MOUSEEVENTF_WHEEL = 0x0800;

    // 模拟鼠标左键点击
    public static void MoveAndClick(Point point)
    {
        SetCursorPos(point.X, point.Y);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    public static void Move(Point point)
    {
        SetCursorPos(point.X, point.Y);
    }

    public static void Click()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }
}