using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Point = System.Drawing.Point;

public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public struct MatchResult
{
    public double ResultValue;
    public Mat Src;
    public Mat Dst;
    public Mat ResultMat;
    public Point DstPoint;
    public double MaxVal, MinVal;
    public Point MaxLoc, MinLoc;
}

public class CaptureUtil
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // P/Invoke: SetForegroundWindow
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // P/Invoke: ShowWindow
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // P/Invoke: BitBlt
    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hObjSource, int nXSrc, int nYSrc, uint dwRop);

    // P/Invoke: GetDC
    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    // P/Invoke: ReleaseDC
    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    // 定义常量
    public const int SW_RESTORE = 9; // 恢复窗口（如果最小化）
    public const int SW_SHOW = 5; // 显示窗口

    // 获取窗口句柄
    public static IntPtr GetWindowHandle(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
        {
            throw new Exception($"没有找到名为 {processName} 的进程");
        }

        return processes[0].MainWindowHandle;
    }

    // 捕获指定窗口的画面
    public static Bitmap CaptureWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            throw new ArgumentException("无效的窗口句柄", nameof(hWnd));
        }

        // 获取窗口的位置和大小
        if (!GetWindowRect(hWnd, out RECT rect))
        {
            throw new Exception("无法获取窗口矩形");
        }

        // 创建一个 Bitmap 来保存截图
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        Bitmap bitmap = new Bitmap(width, height);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            IntPtr hdc = g.GetHdc();
            try
            {
                // 获取桌面设备上下文
                IntPtr desktopHdc = GetDC(IntPtr.Zero);

                // 使用 BitBlt 复制图像
                if (!BitBlt(hdc, 0, 0, width, height, desktopHdc, rect.Left, rect.Top, 0x00CC0020 /* SRCCOPY */))
                {
                    Console.Error.WriteLine("spnapshot failed!");
                }

                // 释放桌面设备上下文
                ReleaseDC(IntPtr.Zero, desktopHdc);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }

        return bitmap;
    }

    public static void DrawRectangle(Mat image, Mat template, Point topLeft)
    {
        // 获取模板的宽度和高度
        int width = template.Cols;
        int height = template.Rows;

        // 定义矩形框的颜色和厚度
        MCvScalar color = new MCvScalar(0, 255, 0); // 绿色
        int thickness = 2;

        // 计算矩形框的右下角位置
        Point bottomRight = new Point(topLeft.X + width, topLeft.Y + height);

        // 在图像上绘制矩形框
        CvInvoke.Rectangle(image, new Rectangle(topLeft, template.Size), color, thickness);
    }

    public static Mat BitmapToEmguMat(Bitmap bitmap)
    {
        // 将 Bitmap 编码为 PNG 格式的字节数组
        byte[] buffer;
        using (MemoryStream ms = new MemoryStream())
        {
            bitmap.Save(ms, ImageFormat.Png);
            buffer = ms.ToArray();
        }

        // 使用 Imdecode 将字节数组解码为 Mat
        Mat dst = new Mat();
        CvInvoke.Imdecode(buffer, ImreadModes.AnyColor, dst);

        return dst;
    }

    public static MatchResult Match(Mat src, String iconFileName)
    {
        return Match(src, CvInvoke.Imread(iconFileName, ImreadModes.AnyColor));
    }

    public static MatchResult Match(Mat src, Mat dst)
    {
        MatchResult matchResult = new MatchResult
        {
            Src = src,
            Dst = dst
        };

        // 转换为灰度图像（可选，提高匹配速度）
        Mat screenshotGray = new Mat();
        Mat iconGray = new Mat();
        CvInvoke.CvtColor(src, screenshotGray, ColorConversion.Bgr2Gray);
        CvInvoke.CvtColor(dst, iconGray, ColorConversion.Bgr2Gray);

        // 使用模板匹配
        Mat result = new Mat();
        CvInvoke.MatchTemplate(screenshotGray, iconGray, result, TemplateMatchingType.CcoeffNormed);

        // 找到最佳匹配位置
        double minVal = 0, maxVal = 0;
        Point minLoc = new(), maxLoc = new();
        CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

        matchResult.MinVal = minVal;
        matchResult.MaxVal = maxVal;
        matchResult.MinLoc = minLoc;
        matchResult.MaxLoc = maxLoc;
        matchResult.ResultMat = result;
        matchResult.ResultValue = maxVal;

        return matchResult;
    }

    public static Point GetAbsPoint(RECT rect, MatchResult result)
    {
        int x = rect.Left + result.MaxLoc.X + 10;
        int y = rect.Top + result.MaxLoc.Y + 10;

        return new Point(x, y);
    }
}