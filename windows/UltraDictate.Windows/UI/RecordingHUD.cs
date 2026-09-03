using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UltraDictate.Windows.UI;

public class RecordingHUD : Form
{
    private float _audioLevel = 0f;
    private readonly System.Windows.Forms.Timer _animationTimer;

    public RecordingHUD()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(160, 50);
        BackColor = Color.FromArgb(15, 17, 23); // Dark Mode First (#0F1117)
        Opacity = 0.92;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animationTimer.Tick += (s, e) => Invalidate();
    }

    public void ShowAtCursor()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        int x = Cursor.Position.X - Width / 2;
        int y = Cursor.Position.Y - Height - 20;

        // Keep inside screen bounds
        if (x < screen.WorkingArea.Left) x = screen.WorkingArea.Left + 10;
        if (x + Width > screen.WorkingArea.Right) x = screen.WorkingArea.Right - Width - 10;
        if (y < screen.WorkingArea.Top) y = Cursor.Position.Y + 25;

        Location = new Point(x, y);
        Show();
        _animationTimer.Start();
    }

    public void HideHUD()
    {
        _animationTimer.Stop();
        Hide();
    }

    public void UpdateAudioLevel(float level)
    {
        _audioLevel = level;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Glassmorphic rounded rectangle
        using var path = GetRoundedRectPath(ClientRectangle, 12);
        using var brush = new SolidBrush(Color.FromArgb(20, 24, 36));
        using var borderPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1.5f);

        g.FillPath(brush, path);
        g.DrawPath(borderPen, path);

        // Recording indicator dot
        using var dotBrush = new SolidBrush(Color.FromArgb(255, 75, 75));
        g.FillEllipse(dotBrush, 16, 18, 14, 14);

        // Text status
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", 10f, FontStyle.Bold);
        g.DrawString("UltraDictate", font, textBrush, 38, 15);

        // Dynamic audio bar
        int barHeight = Math.Clamp((int)(_audioLevel * 25), 4, 20);
        using var waveBrush = new SolidBrush(Color.FromArgb(70, 150, 255));
        g.FillRectangle(waveBrush, 130, 25 - barHeight / 2, 8, barHeight);
    }

    private static GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
