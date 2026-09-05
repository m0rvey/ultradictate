using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UltraDictate.Windows.UI;

public class RecordingHUD : Form
{
    private float _audioLevel = 0f;
    private float _smoothedLevel = 0f;
    private float _pulsePhase = 0f;
    private readonly System.Windows.Forms.Timer _animationTimer;

    public RecordingHUD()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(216, 46);
        BackColor = Color.FromArgb(13, 17, 23);
        DoubleBuffered = true;

        SetStyle(ControlStyles.AllPaintingInWmPaint | 
                 ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | 
                 ControlStyles.SupportsTransparentBackColor, true);

        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
        _animationTimer.Tick += (s, e) =>
        {
            _pulsePhase += 0.08f;
            if (_pulsePhase > MathF.PI * 2) _pulsePhase -= MathF.PI * 2;

            // Smooth audio level damping
            _smoothedLevel = (_smoothedLevel * 0.72f) + (_audioLevel * 0.28f);
            Invalidate();
        };

        UpdateRegion();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0) return;
        using var path = GetCapsulePath(new Rectangle(0, 0, Width, Height));
        Region = new Region(path);
    }

    public void ShowAtCursor()
    {
        UpdateRegion();
        var screen = Screen.FromPoint(Cursor.Position);
        int x = Cursor.Position.X - Width / 2;
        int y = Cursor.Position.Y - Height - 28;

        if (x < screen.WorkingArea.Left + 12) x = screen.WorkingArea.Left + 12;
        if (x + Width > screen.WorkingArea.Right - 12) x = screen.WorkingArea.Right - Width - 12;
        if (y < screen.WorkingArea.Top + 12) y = Cursor.Position.Y + 32;

        Location = new Point(x, y);
        _audioLevel = 0.05f;
        _smoothedLevel = 0.05f;
        Show();
        _animationTimer.Start();
    }

    public void HideHUD()
    {
        _animationTimer.Stop();
        _audioLevel = 0f;
        _smoothedLevel = 0f;
        Hide();
    }

    public void UpdateAudioLevel(float level)
    {
        _audioLevel = Math.Clamp(level, 0f, 1f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var bounds = new Rectangle(0, 0, Width, Height);
        var innerRect = new Rectangle(1, 1, Width - 2, Height - 2);

        // 1. Dark Acrylic Glassmorphic Background Gradient
        using (var bgPath = GetCapsulePath(bounds))
        using (var bgBrush = new LinearGradientBrush(
            bounds,
            Color.FromArgb(28, 33, 44),
            Color.FromArgb(13, 17, 23),
            LinearGradientMode.Vertical))
        {
            g.FillPath(bgBrush, bgPath);
        }

        // 2. Subtle glass highlight sheen along top hemisphere
        using (var sheenPath = GetTopSheenPath(bounds))
        using (var sheenBrush = new LinearGradientBrush(
            new Rectangle(0, 0, Width, Height / 2),
            Color.FromArgb(35, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255),
            LinearGradientMode.Vertical))
        {
            g.FillPath(sheenBrush, sheenPath);
        }

        // 3. Ultra-fine metallic border outline
        using (var borderPath = GetCapsulePath(innerRect))
        using (var borderPen = new Pen(Color.FromArgb(70, 255, 255, 255), 1.2f))
        {
            g.DrawPath(borderPen, borderPath);
        }

        // 4. Pulsing recording beacon (Left)
        float pulse = (MathF.Sin(_pulsePhase) + 1f) * 0.5f;
        int haloSize = (int)(16 + pulse * 6);
        int haloAlpha = (int)(30 + pulse * 50);

        int dotCenterX = 24;
        int dotCenterY = Height / 2;

        // Outer neon glow halo
        using (var haloBrush = new SolidBrush(Color.FromArgb(haloAlpha, 255, 69, 58)))
        {
            g.FillEllipse(haloBrush, dotCenterX - haloSize / 2, dotCenterY - haloSize / 2, haloSize, haloSize);
        }

        // Crisp inner red dot
        using (var dotBrush = new SolidBrush(Color.FromArgb(255, 59, 48)))
        {
            g.FillEllipse(dotBrush, dotCenterX - 5, dotCenterY - 5, 10, 10);
        }

        // 5. Typography: "UltraDictate"
        using (var textBrush = new SolidBrush(Color.FromArgb(245, 247, 250)))
        using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
        {
            g.DrawString("UltraDictate", font, textBrush, 42, 13);
        }

        // 6. Dynamic Equalizer Waveform Bars (5 rounded pill bars)
        int startX = 148;
        int centerY = Height / 2;
        int barWidth = 4;
        int barSpacing = 8;

        for (int i = 0; i < 5; i++)
        {
            // Harmonic wave offset for each bar
            float wave = MathF.Sin(_pulsePhase * 2.6f + (i * 0.85f)) * 0.35f + 0.65f;
            float barVal = MathF.Max(0.12f, _smoothedLevel * wave * 1.3f);
            int barHeight = Math.Clamp((int)(barVal * 26), 4, 26);

            int x = startX + (i * barSpacing);
            int y = centerY - (barHeight / 2);

            var barRect = new Rectangle(x, y, barWidth, barHeight);
            using var barPath = GetRoundedRectPath(barRect, 2);

            // Vibrant Electric Cyan to Neon Azure gradient
            using var barBrush = new LinearGradientBrush(
                barRect,
                Color.FromArgb(90, 200, 250),
                Color.FromArgb(0, 122, 255),
                LinearGradientMode.Vertical);

            g.FillPath(barBrush, barPath);
        }
    }

    private static GraphicsPath GetCapsulePath(Rectangle rect)
    {
        int diameter = rect.Height;
        var path = new GraphicsPath();
        // Left semicircle
        path.AddArc(rect.X, rect.Y, diameter, diameter, 90, 180);
        // Right semicircle
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 180);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath GetTopSheenPath(Rectangle rect)
    {
        int diameter = rect.Height;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddLine(rect.X + diameter / 2, rect.Y, rect.Right - diameter / 2, rect.Y);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddLine(rect.Right, rect.Y + diameter / 2, rect.X, rect.Y + diameter / 2);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
    {
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

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
