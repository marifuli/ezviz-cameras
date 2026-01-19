using Hik.Api;
using Hik.Api.Abstraction;

namespace LiveStreamPlayer;

public partial class Form1 : Form
{
    private PictureBox pictureBox;
    private Button startButton;
    private Button stopButton;
    private IHikApi? hikApi;
    private int playbackId = -1;

    public Form1()
    {
        InitializeComponent();
        InitializeControls();
        HikApi.Initialize();
    }

    private void InitializeControls()
    {
        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        this.Controls.Add(pictureBox);

        var panel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        this.Controls.Add(panel);

        startButton = new Button { Text = "Start Live Stream", Left = 10, Top = 10 };
        startButton.Click += StartButton_Click;
        panel.Controls.Add(startButton);

        stopButton = new Button { Text = "Stop Live Stream", Left = 150, Top = 10, Enabled = false };
        stopButton.Click += StopButton_Click;
        panel.Controls.Add(stopButton);
    }

    private void StartButton_Click(object? sender, EventArgs e)
    {
        try
        {
            // Update with your device credentials
            hikApi = HikApi.Login("138.252.14.100", 8002, "admin", "stex##2025");
            // Assuming channel 1, adjust as needed
            playbackId = hikApi.PlaybackService.StartPlayBack(1, pictureBox.Handle);
            startButton.Enabled = false;
            stopButton.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting stream: {ex.Message}");
        }
    }

    private void StopButton_Click(object? sender, EventArgs e)
    {
        try
        {
            if (playbackId != -1)
            {
                hikApi?.PlaybackService.StopPlayBack(playbackId);
                playbackId = -1;
            }
            hikApi?.Logout();
            hikApi = null;
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error stopping stream: {ex.Message}");
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopButton_Click(null, EventArgs.Empty);
        HikApi.Cleanup();
        base.OnFormClosing(e);
    }
}
