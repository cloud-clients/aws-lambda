using Amazon.Lambda;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudClients.AWS.Lambda.Tests
{
    [TestClass]
    public class AWSLambdaClientTest
    {
        private readonly Mock<IAWSLambdaClientLogger> _mockAWSLambdaClientLogger;

        private AWSLambdaClientConfig? _configLocalLambda;

        public TestContext? TestContext { get; set; }

        private readonly List<string> _informationLogs;
        private readonly List<string> _warningsLogs;
        private readonly List<string> _errorLogs;
        private readonly List<string> _debugLogs;

        private bool _assertLogs = true;

        public AWSLambdaClientTest()
        {
            _informationLogs = new List<string>();
            _warningsLogs = new List<string>();
            _errorLogs = new List<string>();
            _debugLogs = new List<string>();

            _mockAWSLambdaClientLogger = new Mock<IAWSLambdaClientLogger>();
            _mockAWSLambdaClientLogger.Setup(m => m.LogInformation(It.IsAny<string>()))
                .Callback<string>(message =>
                {
                    PrintLog("INFO", message);
                    _informationLogs.Add(message);
                });

            _mockAWSLambdaClientLogger.Setup(m => m.LogWarning(It.IsAny<string>()))
                .Callback<string>(message =>
                {
                    PrintLog("WARN", message);
                    _warningsLogs.Add(message);
                });

            _mockAWSLambdaClientLogger.Setup(m => m.LogError(It.IsAny<string>()))
                .Callback<string>(message =>
                {
                    PrintLog("ERROR", message);
                    _errorLogs.Add(message);
                });

            _mockAWSLambdaClientLogger.Setup(m => m.LogDebug(It.IsAny<string>()))
                .Callback<string>(message =>
                {
                    PrintLog("DEBUG", message);
                    _debugLogs.Add(message);
                });
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _configLocalLambda = new AWSLambdaClientConfig
            {
                UseAnonymousCredentials = true,
                DebugMode = false,
                AmazonLambdaConfig = new AmazonLambdaConfig
                {
                    ServiceURL = "http://127.0.0.1:3001"
                },
                InvokeRequestConfig = new InvokeRequestConfig
                {
                    FunctionName = "HelloFromLambdaFunction",
                    InvocationType = InvocationType.RequestResponse,
                    ExpectedStatusCode = 200
                }
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_assertLogs)
            {
                Assert.AreEqual(0, _errorLogs.Count);
                Assert.AreEqual(0, _warningsLogs.Count);
                Assert.AreEqual(0, _debugLogs.Count);
            }
        }

        [TestMethod]
        public void Constructor_Given_invokeRequestConfig_is_null_Should_throw_an_exception()
        {
            Assert.IsNotNull(_configLocalLambda);
            _configLocalLambda.InvokeRequestConfig = null;

            var action = () => CreateAWSLambdaClient();

            var exceptionAssertions = action.Should().Throw<ArgumentNullException>();
            exceptionAssertions.WithMessage("Value cannot be null. (Parameter 'InvokeRequestConfig')");
        }

        [TestMethod]
        public async Task InvokeRequestResponse_Given_request_Should_return_a_response()
        {
            using var target = CreateAWSLambdaClient();

            var actual = await target.InvokeAsync<string, string>("abcde");
            Assert.AreEqual("ABCDE", actual);
        }

        [TestMethod]
        public async Task InvokeRequestResponse_Given_request_Should_log_response()
        {
            using var target = CreateAWSLambdaClient();

            await target.InvokeAsync<string, string>("abcde");

            var expected = @"Response from Lambda:
""ABCDE""";

            Assert.AreEqual(1, _informationLogs.Count);
            Assert.AreEqual(expected, _informationLogs[0]);
        }

        [TestMethod]
        public async Task InvokeRequestResponse_Given_debugMode_is_true_Should_log_debug_info()
        {
            Assert.IsNotNull(_configLocalLambda);
            _configLocalLambda.DebugMode = true;
            using var target = CreateAWSLambdaClient();

            await target.InvokeAsync<string, string>("abcde");

            Assert.AreEqual(1, _debugLogs.Count);

            _debugLogs[0].Should().Contain("Client config:");
            _debugLogs[0].Should().Contain(@"""ServiceURL"": ""http://127.0.0.1:3001""");
            
            _assertLogs = false;
            Assert.AreEqual(0, _errorLogs.Count);
            Assert.AreEqual(0, _warningsLogs.Count);
        }

        [TestMethod]
        public async Task InvokeRequestResponse_Given_invalid_status_code_Should_log_warning()
        {
            Assert.IsNotNull(_configLocalLambda);
            _configLocalLambda.InvokeRequestConfig.ExpectedStatusCode = 100;
            using var target = CreateAWSLambdaClient();

            await target.InvokeAsync<string, string>("abcde");

            Assert.AreEqual(1, _warningsLogs.Count);
            Assert.AreEqual("Invalid Status Code. Actual: 200, Expected: 100", _warningsLogs[0]);

            _assertLogs = false;
            Assert.AreEqual(0, _errorLogs.Count);
            Assert.AreEqual(0, _debugLogs.Count);
        }

        private AWSLambdaClient CreateAWSLambdaClient()
        {
            return new AWSLambdaClient(_configLocalLambda, _mockAWSLambdaClientLogger.Object);
        }

        private void Print(string message)
        {
            TestContext?.WriteLine(message);
        }

        private void PrintLog(string logType, string message)
        {
            Print($"LOG - {logType}:{Environment.NewLine}{message}");
        }
    }
}