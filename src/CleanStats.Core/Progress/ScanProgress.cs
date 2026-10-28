using System;
using System.Collections.Generic;
using System.Text;

namespace CleanStats.Core.Progress;

public abstract class ScanProgress
{
    public DateTime Timestamp { get => _timestamp; private set => _timestamp = value; }
    private DateTime _timestamp;

    public ScanProgress()
    {
        Timestamp = DateTime.UtcNow;
    }
}
