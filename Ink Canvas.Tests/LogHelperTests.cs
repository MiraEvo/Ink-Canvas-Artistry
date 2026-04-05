using System;
using System.IO;
using System.Security;
using Xunit;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Tests
{
    public class LogHelperTests : IDisposable
    {
        private readonly Action<string> _originalWriteLineAction;

        public LogHelperTests()
        {
            _originalWriteLineAction = LogHelper.WriteLineAction;
        }

        public void Dispose()
        {
            LogHelper.WriteLineAction = _originalWriteLineAction;
        }

        [Fact]
        public void WriteLogToFile_IOException_Handled()
        {
            // Arrange
            LogHelper.WriteLineAction = _ => throw new IOException("Simulated IO Exception");

            // Act
            var exception = Record.Exception(() => LogHelper.WriteLogToFile("Test IO Exception"));

            // Assert
            Assert.Null(exception); // The exception should be caught and not bubble up
        }

        [Fact]
        public void WriteLogToFile_UnauthorizedAccessException_Handled()
        {
            // Arrange
            LogHelper.WriteLineAction = _ => throw new UnauthorizedAccessException("Simulated Unauthorized Access");

            // Act
            var exception = Record.Exception(() => LogHelper.WriteLogToFile("Test Unauthorized Access"));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void WriteLogToFile_SecurityException_Handled()
        {
            // Arrange
            LogHelper.WriteLineAction = _ => throw new SecurityException("Simulated Security Exception");

            // Act
            var exception = Record.Exception(() => LogHelper.WriteLogToFile("Test Security Exception"));

            // Assert
            Assert.Null(exception);
        }
    }
}
