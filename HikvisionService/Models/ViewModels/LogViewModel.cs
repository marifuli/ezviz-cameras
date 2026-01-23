namespace HikvisionService.Models.ViewModels;

public class LogViewModel
{
    public List<string> LogLines { get; set; } = new List<string>();
    public string LogFilePath { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public int MaxLines { get; set; } = 200;
    public string LogLevel { get; set; } = "All"; // All, Error, Warning, Info, Debug
}