using System;

namespace Ink_Canvas.Services.Logging
{
    public sealed class LogOptions
    {
        public bool Enabled { get; set; } = true;

        public string DirectoryPath { get; set; } = string.Empty;

        public string ActiveFileName { get; set; } = "Log.txt";

        public long MaxFileSizeBytes { get; set; } = 512 * 1024;

        public int RetainedArchiveCount { get; set; } = 5;

        public LogOptions Clone()
        {
            return new LogOptions
            {
                Enabled = Enabled,
                DirectoryPath = DirectoryPath,
                ActiveFileName = ActiveFileName,
                MaxFileSizeBytes = MaxFileSizeBytes,
                RetainedArchiveCount = RetainedArchiveCount
            };
        }
    }
}
