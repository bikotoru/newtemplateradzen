using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;
using Frontend.Services;

namespace Frontend.Tests.Services
{
    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Created { get; set; }
        public bool Modified { get; set; }
    }
    public class APIServiceTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<APIService>> _loggerMock;
        private readonly APIService _apiService;
        private readonly JsonSerializerOptions _jsonOptions;

        public APIServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://api.test.com/")
            };
            _loggerMock = new Mock<ILogger<APIService>>();
            _apiService = new APIService(_httpClient, _loggerMock.Object);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region GET Method Tests

        [Fact]
        public async Task GetAsync_Should_Return_Deserialized_Object_When_Successful()
        {
            // Arrange
            var expectedData = new TestItem { Id = 1, Name = "Test Item" };
            var jsonResponse = JsonSerializer.Serialize(expectedData, _jsonOptions);
            
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Get && 
                        req.RequestUri!.ToString().EndsWith("/test")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _apiService.GetAsync<TestItem>("/test");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Test Item");
            // Verify logging
            VerifyLogCalled(LogLevel.Information, "GET request to: /test");
        }

        [Fact]
        public async Task GetAsync_Should_Return_Default_When_Not_Successful()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("Not found")
                });

            // Act
            var result = await _apiService.GetAsync<string>("/notfound");

            // Assert
            result.Should().BeNull();
            VerifyLogCalled(LogLevel.Warning, "GET request failed:");
        }

        [Fact]
        public async Task GetListAsync_Should_Return_List_When_Successful()
        {
            // Arrange
            var expectedData = new List<TestItem> 
            { 
                new TestItem { Id = 1, Name = "Item 1" },
                new TestItem { Id = 2, Name = "Item 2" }
            };
            var jsonResponse = JsonSerializer.Serialize(expectedData, _jsonOptions);
            
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _apiService.GetListAsync<TestItem>("/test-list");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Item 1");
        }

        [Fact]
        public async Task GetListAsync_Should_Return_Empty_List_When_Failed()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            // Act
            var result = await _apiService.GetListAsync<TestItem>("/test-list");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region POST Method Tests

        [Fact]
        public async Task PostAsync_Generic_Should_Return_Result_When_Successful()
        {
            // Arrange
            var requestData = new TestItem { Name = "Test Post" };
            var responseData = new TestItem { Id = 1, Name = "Test Post", Created = true };
            var jsonResponse = JsonSerializer.Serialize(responseData, _jsonOptions);
            
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _apiService.PostAsync<TestItem>("/test", requestData);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Created.Should().BeTrue();
            VerifyLogCalled(LogLevel.Information, "POST request to: /test");
        }

        [Fact]
        public async Task PostAsync_Boolean_Should_Return_True_When_Successful()
        {
            // Arrange
            var requestData = new { Name = "Test Post" };
            
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            // Act
            var result = await _apiService.PostAsync("/test", requestData);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task PostAsync_Boolean_Should_Return_False_When_Failed()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Bad request")
                });

            // Act
            var result = await _apiService.PostAsync("/test", null);

            // Assert
            result.Should().BeFalse();
            VerifyLogCalled(LogLevel.Warning, "POST request failed:");
        }

        #endregion

        #region PUT Method Tests

        [Fact]
        public async Task PutAsync_Generic_Should_Return_Result_When_Successful()
        {
            // Arrange
            var requestData = new TestItem { Id = 1, Name = "Updated Item" };
            var responseData = new TestItem { Id = 1, Name = "Updated Item", Modified = true };
            var jsonResponse = JsonSerializer.Serialize(responseData, _jsonOptions);
            
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Put),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _apiService.PutAsync<TestItem>("/test/1", requestData);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Modified.Should().BeTrue();
            VerifyLogCalled(LogLevel.Information, "PUT request to: /test/1");
        }

        [Fact]
        public async Task PutAsync_Boolean_Should_Return_True_When_Successful()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            // Act
            var result = await _apiService.PutAsync("/test/1", null);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region DELETE Method Tests

        [Fact]
        public async Task DeleteAsync_Should_Return_True_When_Successful()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Delete),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NoContent
                });

            // Act
            var result = await _apiService.DeleteAsync("/test/1");

            // Assert
            result.Should().BeTrue();
            VerifyLogCalled(LogLevel.Information, "DELETE request to: /test/1");
        }

        [Fact]
        public async Task DeleteAsync_Should_Return_False_When_Failed()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            // Act
            var result = await _apiService.DeleteAsync("/test/1");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Utility Method Tests

        [Fact]
        public async Task IsHealthyAsync_Should_Return_True_When_Health_Endpoint_Returns_Success()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.RequestUri!.ToString().EndsWith("/health")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            // Act
            var result = await _apiService.IsHealthyAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsHealthyAsync_Should_Return_False_When_Health_Endpoint_Fails()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _apiService.IsHealthyAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetErrorMessageAsync_Should_Parse_JSON_Error_Message()
        {
            // Arrange
            var errorResponse = new { message = "Validation failed", error = "Bad request" };
            var jsonError = JsonSerializer.Serialize(errorResponse);
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(jsonError, Encoding.UTF8, "application/json")
            };

            // Act
            var result = await _apiService.GetErrorMessageAsync(httpResponse);

            // Assert
            result.Should().Be("Validation failed");
        }

        [Fact]
        public async Task GetErrorMessageAsync_Should_Return_Raw_Content_For_Non_JSON()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Plain text error", Encoding.UTF8, "text/plain")
            };

            // Act
            var result = await _apiService.GetErrorMessageAsync(httpResponse);

            // Assert
            result.Should().Be("Plain text error");
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetAsync_Should_Throw_When_HttpClient_Throws()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _apiService.GetAsync<string>("/test"));
            VerifyLogCalled(LogLevel.Error, "Error in GET request to: /test");
        }

        [Fact]
        public async Task PostAsync_Should_Throw_When_HttpClient_Throws()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Timeout"));

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => _apiService.PostAsync<string>("/test", null));
        }

        #endregion

        #region Helper Methods

        private void VerifyLogCalled(LogLevel level, string message)
        {
            _loggerMock.Verify(
                logger => logger.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}