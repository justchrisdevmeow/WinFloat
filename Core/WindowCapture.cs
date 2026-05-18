using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace WinFloat;

public class WindowCapture
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowRect(IntPtr hwnd, ref RECT rect);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hwnd);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    private const int SRCCOPY = 0x00CC0020;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public struct WindowInfo
    {
        public IntPtr Handle;
        public string Title;
        public bool IsVisible;
    }

    private int _textureId = -1;
    private int _width = 0, _height = 0;

    public WindowCapture()
    {
        _textureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _textureId);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        EnumWindows((hwnd, lParam) =>
        {
            if (IsWindowVisible(hwnd))
            {
                var title = GetWindowTitle(hwnd);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    windows.Add(new WindowInfo
                    {
                        Handle = hwnd,
                        Title = title,
                        IsVisible = true
                    });
                }
            }
            return true;
        }, IntPtr.Zero);
        return windows;
    }

    public string GetWindowTitle(IntPtr hwnd)
    {
        int length = GetWindowTextLength(hwnd);
        if (length == 0) return "";
        var sb = new System.Text.StringBuilder(length + 1);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public IntPtr GetDesktopWindowHandle()
    {
        return GetDesktopWindow();
    }

    public int CaptureWindow(IntPtr hwnd)
    {
        RECT rect = new RECT();
        GetWindowRect(hwnd, ref rect);
        
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        
        if (width <= 0 || height <= 0)
            return _textureId;
        
        _width = width;
        _height = height;
        
        IntPtr hdcSrc = GetWindowDC(hwnd);
        if (hdcSrc == IntPtr.Zero)
            return _textureId;
        
        using (var bitmap = new Bitmap(width, height))
        {
            using (var g = Graphics.FromImage(bitmap))
            {
                IntPtr hdcDest = g.GetHdc();
                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
                g.ReleaseHdc(hdcDest);
            }
            
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bytes = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);
            
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bytes);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        
        ReleaseDC(hwnd, hdcSrc);
        return _textureId;
    }

    public int GetTextureId() => _textureId;
    public (int width, int height) GetTextureSize() => (_width, _height);
}
