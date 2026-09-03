using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UltraDictate.Windows.UI;

public class RecordingHUD : Form
{
    private float _audioLevel = 0f;
    private float _pulsePhase = 0f;
    private readonly System.Windows.Forms.Timer _animationTimer;

    public RecordingHUD()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(190, 52);
        BackColor = Color.FromArgb(13, 17, 23); // Dark theme #0D1117
        Opacity = 0.94;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animationTimer.Tick += (s, e) =>
        {
            _pulsePhase += 0.08f;
            if (_pulsePhase > MathF.PI * 2) _pulsePhase -= MathF.PI * 2;
            Invalidate();
        };
    }

    public void ShowAtCursor()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        int x = Cursor.Position.X - Width / 2;
        int y = Cursor.Position.Y - Height - 24;

        if (x < screen.WorkingArea.Left + 10) x = screen.WorkingArea.Left + 10;
        if (x + Width > screen.WorkingArea.Right - 10) x = screen.WorkingArea.Right - Width - 10;
        if (y < screen.WorkingArea.Top + 10) y = Cursor.Position.Y + 28;

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
        _audioLevel = Math.Clamp(level, 0f, 1f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Outer glow & glassmorphic body
        var rect = new Rectangle(1, 1, ClientRectangle.Width - 3, ClientRectangle.Height - 3);
        using var path = GetRoundedRectPath(rect, 14);

        using var bgBrush = new LinearGradientBrush(
            rect,
            Color.FromArgb(24, 28, 40),
            Color.FromArgb(16, 20, 28),
            LinearGradientMode.Vertical
        );
        g.FillPath(bgBrush, path);

        using var borderPen = new Pen(Color.FromArgb(70, 255, 255, 255), 1.2f);
        g.DrawPath(borderPen, path);

        // Pulsing red recording dot
        float pulse = (MathF.Sin(_pulsePhase) + 1f) * 0.5f;
        int glowAlpha = (int)(40 + pulse * 60);
        using var haloBrush = new SolidBrush(Color.FromArgb(glowAlpha, 255, 69, 58));
        g.FillEllipse(haloBrush, 12, 17, 18, 18);

        using var dotBrush = new SolidBrush(Color.FromArgb(255, 69, 58));
        g.FillEllipse(dotBrush, 15, 20, 12, 12);

        // App title & status
        using var textBrush = new SolidBrush(Color.FromArgb(240, 246, 252));
        using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        g.DrawString("UltraDictate", font, textBrush, 36, 17);

        // Multi-bar animated waveform visualizer (5 bars)
        int startX = 132;
        int baseY = 26;
        for (int i = 0; i < 5; i++)
        {
            float harmonic = MathF.Sin(_pulsePhase * 2f + i * 0.9f) * 0.3f + 0.7f;
            float barVal = MathF.Max(0.15f, _audioLevel * harmonic);
            int barHeight = Math.Clamp((int)(barVal * 24), 4, 22);

            int x = startX + (i * 8);
            int y = baseY - (barHeight / 2);

            using var waveBrush = new LinearGradientBrush(
                new Rectangle(x, y, 4, barHeight),
                Color.FromArgb(88, 166, 255),
                Color.FromArgb(56, 139, 253),
                LinearGradientMode.Vertical
            );
            g.FillRectangle(waveBrush, x, y, 4, barHeight);
        }
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
