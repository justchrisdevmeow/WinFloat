using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace WinFloat;

public class WindowCapture
{
    // Windows API imports
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
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hwnd);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public struct WindowInfo
    {
        public IntPtr Handle;
        public string Title;
        public bool IsVisible;
    }

    private int _textureId = -1;
    private int _width = 0;
    private int _height = 0;

    public WindowCapture()
    {
        // Create OpenGL texture
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
                // Skip empty titles (system windows)
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

    public int CaptureWindow(IntPtr hwnd)
    {
        // Get window dimensions
        RECT rect = new RECT();
        GetWindowRect(hwnd, ref rect);
        
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        
        if (width <= 0 || height <= 0)
            return _textureId;
        
        _width = width;
        _height = height;
        
        // Get window DC
        IntPtr hdcSrc = GetWindowDC(hwnd);
        if (hdcSrc == IntPtr.Zero)
            return _textureId;
        
        // Create compatible DC and bitmap
        using (var bitmap = new Bitmap(width, height))
        {
            using (var g = Graphics.FromImage(bitmap))
            {
                IntPtr hdcDest = g.GetHdc();
                // Copy window content to bitmap
                NativeMethods.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, NativeMethods.SRCCOPY);
                g.ReleaseHdc(hdcDest);
            }
            
            // Convert bitmap to byte array
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bytes = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);
            
            // Update OpenGL texture
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bytes);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        
        ReleaseDC(hwnd, hdcSrc);
        return _textureId;
    }

    public int CaptureDesktop()
    {
        return CaptureWindow(GetDesktopWindow());
    }

    public int GetTextureId() => _textureId;
    
    public (int width, int height) GetTextureSize() => (_width, _height);
}

// Native methods wrapper
internal static class NativeMethods
{
    public const int SRCCOPY = 0x00CC0020;
    
    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
}
