using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using AutoClick.utils;
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
    private const uint SRCCOPY = 0x00CC0020;
    private const uint CAPTUREBLT = 0x40000000;
    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    private static volatile bool _notifiedWindowCaptureFailure;
    private static volatile bool _notifiedPrintWindowFailure;
    private static volatile bool _notifiedDesktopCaptureFailure;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hObjSource, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    public const int SW_RESTORE = 9;
    public const int SW_SHOW = 5;

    public static IntPtr GetWindowHandle(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
        {
            throw new Exception($"没有找到名为 {processName} 的进程");
        }

        var handle = processes[0].MainWindowHandle;
        if (handle == IntPtr.Zero)
        {
            throw new Exception($"进程 {processName} 的主窗口句柄为空");
        }

        return handle;
    }

    public static Bitmap CaptureWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            throw new ArgumentException("无效的窗口句柄", nameof(hWnd));
        }

        using var scope = PerformanceScope.Track("CaptureWindow", 30);

        if (!GetWindowRect(hWnd, out RECT rect))
        {
            throw new InvalidOperationException("无法获取窗口矩形");
        }

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException($"窗口尺寸异常: {width}x{height}");
        }

        Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
        bool success = false;

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            IntPtr destHdc = g.GetHdc();
            try
            {
                success = TryBitBltFromWindow(hWnd, destHdc, width, height);

                if (!success)
                {
                    if (!_notifiedWindowCaptureFailure)
                    {
                        _notifiedWindowCaptureFailure = true;
                        LogUtil.Warning($"BitBlt window capture失败，尝试PrintWindow重试。handle: {hWnd}");
                    }

                    success = TryPrintWindow(hWnd, destHdc);
                }

                if (!success)
                {
                    if (!_notifiedPrintWindowFailure)
                    {
                        _notifiedPrintWindowFailure = true;
                        LogUtil.Warning($"PrintWindow捕获失败，尝试桌面复制。handle: {hWnd}");
                    }

                    success = TryBitBltFromDesktop(rect, destHdc, width, height);
                }
            }
            finally
            {
                g.ReleaseHdc(destHdc);
            }
        }

        if (!success)
        {
            if (!_notifiedDesktopCaptureFailure)
            {
                _notifiedDesktopCaptureFailure = true;
                LogUtil.Warning($"桌面复制捕获失败 handle: {hWnd}");
            }

            LogUtil.Error($"窗口捕获失败 handle: {hWnd}");
        }

        return bitmap;
    }

    private static bool TryBitBltFromWindow(IntPtr hWnd, IntPtr destHdc, int width, int height)
    {
        IntPtr windowHdc = GetWindowDC(hWnd);
        if (windowHdc == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return BitBlt(destHdc, 0, 0, width, height, windowHdc, 0, 0, SRCCOPY | CAPTUREBLT);
        }
        finally
        {
            ReleaseDC(hWnd, windowHdc);
        }
    }

    private static bool TryPrintWindow(IntPtr hWnd, IntPtr destHdc)
    {
        return PrintWindow(hWnd, destHdc, PW_RENDERFULLCONTENT);
    }

    private static bool TryBitBltFromDesktop(RECT rect, IntPtr destHdc, int width, int height)
    {
        IntPtr desktopHdc = GetDC(IntPtr.Zero);
        if (desktopHdc == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return BitBlt(destHdc, 0, 0, width, height, desktopHdc, rect.Left, rect.Top, SRCCOPY | CAPTUREBLT);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, desktopHdc);
        }
    }

    public static void DrawRectangle(Mat image, Mat template, Point topLeft)
    {
        int width = template.Cols;
        int height = template.Rows;
        MCvScalar color = new(0, 255, 0);
        int thickness = 2;
        Point bottomRight = new(topLeft.X + width, topLeft.Y + height);
        CvInvoke.Rectangle(image, new Rectangle(topLeft, template.Size), color, thickness);
    }

    public static Mat BitmapToEmguMat(Bitmap bitmap)
    {
        using var scope = PerformanceScope.Track("BitmapToMat", 5);
        byte[] buffer;
        using (MemoryStream ms = new())
        {
            bitmap.Save(ms, ImageFormat.Png);
            buffer = ms.ToArray();
        }

        Mat dst = new();
        CvInvoke.Imdecode(buffer, ImreadModes.AnyColor, dst);
        return dst;
    }

    public static MatchResult Match(Mat src, string iconFileName)
    {
        Mat icon = CvInvoke.Imread(iconFileName, ImreadModes.AnyColor);
        if (icon.IsEmpty)
        {
            throw new FileNotFoundException($"匹配图标读取失败: {iconFileName}");
        }

        return Match(src, icon);
    }

    public static MatchResult Match(Mat src, Mat dst)
    {
        using var scope = PerformanceScope.Track("TemplateMatch", 10);

        if (dst.IsEmpty)
        {
            throw new ArgumentException("模板图像为空", nameof(dst));
        }

        MatchResult matchResult = new()
        {
            Src = src,
            Dst = dst
        };

        using Mat screenshotGray = new();
        using Mat iconGray = new();
        CvInvoke.CvtColor(src, screenshotGray, ColorConversion.Bgr2Gray);
        CvInvoke.CvtColor(dst, iconGray, ColorConversion.Bgr2Gray);

        using Mat result = new();
        CvInvoke.MatchTemplate(screenshotGray, iconGray, result, TemplateMatchingType.CcoeffNormed);

        double minVal = 0, maxVal = 0;
        Point minLoc = new(), maxLoc = new();
        CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

        matchResult.MinVal = minVal;
        matchResult.MaxVal = maxVal;
        matchResult.MinLoc = minLoc;
        matchResult.MaxLoc = maxLoc;
        matchResult.ResultValue = maxVal;

        return matchResult;
    }

    public static Point GetAbsPoint(RECT rect, MatchResult result)
    {
        int x = rect.Left + result.MaxLoc.X + 10;
        int y = rect.Top + result.MaxLoc.Y + 10;
        return new Point(x, y);
    }

    public static void ResetDiagnostics()
    {
        _notifiedWindowCaptureFailure = false;
        _notifiedPrintWindowFailure = false;
        _notifiedDesktopCaptureFailure = false;
    }
}
