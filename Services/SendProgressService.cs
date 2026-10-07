namespace MailSender.Services;

public class SendProgressService
{
    private readonly object _lock = new();

    private bool _isRunning;
    private bool _isPaused;
    private int _total;
    private int _processed;
    private int _success;
    private int _failed;

    private int _currentRow;
    private string _currentName = string.Empty;
    private string _currentEmail = string.Empty;
    private string _currentStatus = string.Empty;
    private string _message = string.Empty;

    public void Start(int total)
    {
        lock (_lock)
        {
            _isRunning = true;
            _isPaused = false;

            _total = total;
            _processed = 0;
            _success = 0;
            _failed = 0;

            _currentRow = 0;
            _currentName = string.Empty;
            _currentEmail = string.Empty;
            _currentStatus = "Starting...";
            _message = "Preparing to send certificates...";
        }
    }

    public void SetCurrentStudent(
        int rowNumber,
        string name,
        string email)
    {
        lock (_lock)
        {
            _currentRow = rowNumber;
            _currentName = name;
            _currentEmail = email;
            _currentStatus = "Sending...";
            _message = $"Sending certificate to {name}...";
        }
    }

    public void SetSuccess()
    {
        lock (_lock)
        {
            _processed++;
            _success++;

            _currentStatus = "Success";
            _message = $"Certificate sent successfully to {_currentName}.";
        }
    }

    public void SetFailed(string error)
    {
        lock (_lock)
        {
            _processed++;
            _failed++;

            _currentStatus = "Failed";
            _message = $"Failed to send certificate to {_currentName}.";

            if (!string.IsNullOrWhiteSpace(error))
            {
                _message += $" {error}";
            }
        }
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _isPaused = true;
                _message = "Sending is paused.";
            }
        }
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _isPaused = false;
                _message = "Sending resumed.";
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (_lock)
            {
                return _isPaused;
            }
        }
    }

    public void Finish()
    {
        lock (_lock)
        {
            _isRunning = false;
            _isPaused = false;

            _currentStatus = "Completed";
            _message =
                $"Completed. {_success} sent, {_failed} failed.";
        }
    }

    public object GetStatus()
    {
        lock (_lock)
        {
            return new
            {
                isRunning = _isRunning,
                isPaused = _isPaused,

                total = _total,
                processed = _processed,
                success = _success,
                failed = _failed,

                currentRow = _currentRow,
                currentName = _currentName,
                currentEmail = _currentEmail,
                currentStatus = _currentStatus,

                message = _message,

                percentage = _total > 0
                    ? (int)Math.Round(
                        (_processed * 100.0) / _total)
                    : 0
            };
        }
    }
}
