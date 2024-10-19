using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;

namespace AzureFunctions.Api.Tests.Mocks
{
    /// <summary>
    /// A MOCK logger to be used in unit tests.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MockLogger<T> : ILogger<T>, IDisposable
    {
        private readonly ITestOutputHelper _testOutput;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MockLogger() { }

        /// <summary>
        /// Constructor for adding an xUnit ITestOutputHelper used when calling LogInformation().
        /// </summary>
        /// <param name="output">An xUnit ITestOutputHelper.</param>
        public MockLogger(ITestOutputHelper output)
        {
            _testOutput = output;
        }

        /// <summary>
        /// MOCK
        /// </summary>
        /// <typeparam name="TState">MOCK</typeparam>
        /// <param name="state">MOCK</param>
        /// <returns>MOCK</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }


        /// <summary>
        /// MOCK
        /// </summary>
        /// <param name="logLevel">MOCK</param>
        /// <returns>MOCK</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }


        /// <summary>
        /// Writes the text representation of the specified array of objects, followed by the current line terminator, to the standard output stream using the specified format information.
        /// </summary>
        /// <typeparam name="TState">MOCK</typeparam>
        /// <param name="logLevel">MOCK</param>
        /// <param name="eventId">MOCK</param>
        /// <param name="state">MOCK</param>
        /// <param name="exception">MOCK</param>
        /// <param name="formatter">MOCK</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (_testOutput != null)
            {
                _testOutput.WriteLine(formatter(state, exception));
            }

            Console.WriteLine(formatter(state, exception));
        }

        /// <summary>
        /// MOCK
        /// </summary>
        public void Dispose() { }

    }
}
