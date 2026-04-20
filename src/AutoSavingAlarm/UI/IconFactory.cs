using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AutoSavingAlarm.UI;

internal static class IconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon CreateStatusIcon(Color fillColor)
    {
        using Bitmap bitmap = new(32, 32);
        using Graphics graphics = Graphics.FromImage(bitmap);
        using SolidBrush brush = new(fillColor);
        using Pen outlinePen = new(Color.FromArgb(60, 60, 60), 2f);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        graphics.FillEllipse(brush, 4, 4, 24, 24);
        graphics.DrawEllipse(outlinePen, 4, 4, 24, 24);

        IntPtr handle = bitmap.GetHicon();

        try
        {
            using Icon sourceIcon = Icon.FromHandle(handle);
            return (Icon)sourceIcon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
