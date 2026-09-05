using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UltraDictate.Windows.UI;

public class RecordingHUD : Form
{
    private float _targetLevel = 0f;
    private float _smoothedLevel = 0f;
    private float _animTime = 0f;
    private string _statusText = "Listening...";
    private readonly Stopwatch _stopwatch = new();
    private readonly System.Windows.Forms.Timer _animationTimer;

    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int SW_SHOWNOACTIVATE = 4;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public RecordingHUD()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(236, 52);
        BackColor = Color.FromArgb(13, 17, 23);
        DoubleBuffered = true;

        SetStyle(ControlStyles.AllPaintingInWmPaint | 
                 ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | 
                 ControlStyles.SupportsTransparentBackColor, true);

        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
        _animationTimer.Tick += OnAnimationTick;

        UpdateRegion();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        float dt = (float)_stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();

        if (dt <= 0 || dt > 0.1f) dt = 0.016f;
        _animTime += dt;

        // Asymmetric attack & decay: snappy attack (26f) when speaking, silky smooth decay (7.5f) when quiet
        float lerpSpeed = _targetLevel > _smoothedLevel ? 26f : 7.5f;
        _smoothedLevel += (_targetLevel - _smoothedLevel) * Math.Clamp(dt * lerpSpeed, 0.02f, 0.7f);

        Invalidate();
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
        _targetLevel = 0.06f;
        _smoothedLevel = 0.06f;
        _statusText = "Listening...";
        _stopwatch.Restart();

        // Show window WITHOUT stealing focus from the active app
        ShowWindow(Handle, SW_SHOWNOACTIVATE);
        Visible = true;
        _animationTimer.Start();
    }

    public void SetTranscribing()
    {
        _statusText = "Processing...";
        _targetLevel = 0.4f;
        Invalidate();
    }

    public void HideHUD()
    {
        _animationTimer.Stop();
        _targetLevel = 0f;
        _smoothedLevel = 0f;
        Hide();
    }

    public void UpdateAudioLevel(float level)
    {
        _targetLevel = Math.Clamp(level, 0f, 1f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Clear canvas with base dark background to prevent any fringe artifacts
        g.Clear(Color.FromArgb(13, 17, 23));

        var bounds = new Rectangle(0, 0, Width, Height);
        var innerBorderRect = new Rectangle(1, 1, Width - 2, Height - 2);

        // 1. Dark Acrylic Glassmorphic Background Gradient
        using (var bgPath = GetCapsulePath(bounds))
        using (var bgBrush = new LinearGradientBrush(
            bounds,
            Color.FromArgb(26, 31, 42),
            Color.FromArgb(13, 17, 23),
            LinearGradientMode.Vertical))
        {
            g.FillPath(bgBrush, bgPath);
        }

        // 2. Soft inner glass sheen (strictly inset, never touching outer boundary)
        using (var sheenBrush = new LinearGradientBrush(
            new Rectangle(26, 2, Width - 52, 20),
            Color.FromArgb(16, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255),
            LinearGradientMode.Vertical))
        {
            g.FillRectangle(sheenBrush, 26, 2, Width - 52, 20);
        }

        // 3. Deep Slate Titanium Border (eliminates any white subpixel halo)
        using (var borderPath = GetCapsulePath(innerBorderRect))
        using (var borderPen = new Pen(Color.FromArgb(42, 54, 72), 1.0f))
        {
            g.DrawPath(borderPen, borderPath);
        }

        // 4. Pulsing recording beacon (Left)
        float pulse = (MathF.Sin(_animTime * 4f) + 1f) * 0.5f;
        int haloSize = (int)(16 + pulse * 8);
        int haloAlpha = (int)(25 + pulse * 55);

        int dotCenterX = 26;
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

        // 5. Typography: "UltraDictate" & Subtitle
        using (var titleBrush = new SolidBrush(Color.FromArgb(245, 247, 250)))
        using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
        {
            g.DrawString("UltraDictate", titleFont, titleBrush, 46, 10);
        }

        using (var subBrush = new SolidBrush(Color.FromArgb(139, 148, 158)))
        using (var subFont = new Font("Segoe UI", 7.8f, FontStyle.Regular))
        {
            g.DrawString(_statusText, subFont, subBrush, 46, 29);
        }

        // 6. Dynamic Equalizer Waveform Bars (6 rounded pill bars with fluid wave physics)
        int startX = 152;
        int centerY = Height / 2;
        int barWidth = 4;
        int barSpacing = 8;
        int barCount = 6;
        bool isProcessing = _statusText.Contains("Processing");

        for (int i = 0; i < barCount; i++)
        {
            float energy;
            if (isProcessing)
            {
                // Sleek sine traveling wave when transcribing
                energy = 0.3f + (MathF.Sin(_animTime * 11f + (i * 0.9f)) * 0.5f + 0.5f) * 0.45f;
            }
            else
            {
                // Multiphase organic fluid harmonic wave
                float wave1 = MathF.Sin(_animTime * 6.5f + (i * 0.95f)) * 0.35f + 0.65f;
                float wave2 = MathF.Cos(_animTime * 4.8f - (i * 0.75f)) * 0.15f;
                energy = MathF.Max(0.12f, _smoothedLevel * (wave1 + wave2) * 1.6f);

                // Subtle organic breathing motion when idle
                energy += (MathF.Sin(_animTime * 3.2f + (i * 0.8f)) * 0.5f + 0.5f) * 0.05f;
            }

            int barHeight = Math.Clamp((int)(energy * 28f), 4, 30);
            int x = startX + (i * barSpacing);
            int y = centerY - (barHeight / 2);

            var barRect = new Rectangle(x, y, barWidth, barHeight);
            using var barPath = GetRoundedRectPath(barRect, 2);

            // Vibrant Electric Cyan to Neon Azure or Amethyst gradient
            Color colorTop = isProcessing ? Color.FromArgb(218, 119, 242) : Color.FromArgb(90, 200, 250);
            Color colorBottom = isProcessing ? Color.FromArgb(130, 80, 223) : Color.FromArgb(0, 122, 255);

            using var barBrush = new LinearGradientBrush(
                barRect,
                colorTop,
                colorBottom,
                LinearGradientMode.Vertical);

            g.FillPath(barBrush, barPath);
        }
    }

    private static GraphicsPath GetCapsulePath(Rectangle rect)
    {
        int diameter = rect.Height;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 90, 180);
        path.AddLine(rect.X + diameter / 2, rect.Y, rect.Right - diameter / 2, rect.Y);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 180);
        path.AddLine(rect.Right - diameter / 2, rect.Bottom, rect.X + diameter / 2, rect.Bottom);
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
