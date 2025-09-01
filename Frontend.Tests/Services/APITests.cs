using System.Net;
using System.Text.Json;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Shared.Models.DTOs.Auth;
using Shared.Models.Responses;
using Xunit;

namespace Frontend.Tests.Services;

/// <summary>
/// Tests para el servicio API
/// </summary>
public class APITests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<AuthService> _authServiceMock;
    private readonly API _apiService;
    
    // Datos de prueba
    private readonly TestModel _testData = new() { Id = 1, Name = "Test", Email = "test@test.com" };
    private readonly ApiResponse<TestModel> _successApiResponse;
    private readonly ApiResponse<TestModel> _errorApiResponse;
    private const string TestEndpoint = "/api/test";
    private const string TestToken = "test-token-12345";

    public APITests()
    {
        // Configurar HttpMessageHandler mock
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:7124")
        };

        // Configurar AuthService mock
        _authServiceMock = new Mock<AuthService>(Mock.Of<Microsoft.JSInterop.IJSRuntime>(), _httpClient);
        _authServiceMock.Setup(x => x.EnsureInitializedAsync()).Returns(Task.CompletedTask);
        _authServiceMock.Setup(x => x.Session).Returns(new SessionDataDto
        {
            Token = TestToken,
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Nombre = "Test User"
        });

        // Crear servicio API
        _apiService = new API(_httpClient, _authServiceMock.Object);

        // Preparar respuestas de prueba
        _successApiResponse = ApiResponse<TestModel>.SuccessResponse(_testData);
        _errorApiResponse = ApiResponse<TestModel>.ErrorResponse("Error de prueba");
    }

    #region Test Models

    public class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    #endregion

    #region GET Tests

    [Fact]
    public async Task GetStringAsync_ShouldReturnString_WhenSuccessful()
    {
        // Arrange
        var expectedResponse = "Success response";
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiService.GetStringAsync(TestEndpoint);

        // Assert
        Assert.Equal(expectedResponse, result);
        VerifyAuthenticatedRequest(HttpMethod.Get, TestEndpoint);
    }

    [Fact]
    public async Task GetStringNoAuthAsync_ShouldReturnString_WithoutAuthentication()
    {
        // Arrange
        var expectedResponse = "Success response";
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiService.GetStringNoAuthAsync(TestEndpoint);

        // Assert
        Assert.Equal(expectedResponse, result);
        VerifyUnauthenticatedRequest(HttpMethod.Get, TestEndpoint);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnApiResponse_WhenSuccessful()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_successApiResponse, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.GetAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(_testData.Name, result.Data.Name);
        VerifyAuthenticatedRequest(HttpMethod.Get, TestEndpoint);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnErrorResponse_WhenHttpError()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_errorApiResponse, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.BadRequest, jsonResponse);

        // Act
        var result = await _apiService.GetAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Error de prueba", result.Message);
    }

    [Fact]
    public async Task GetNoAuthAsync_ShouldReturnApiResponse_WithoutAuthentication()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_successApiResponse, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.GetNoAuthAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(_testData.Name, result.Data.Name);
        VerifyUnauthenticatedRequest(HttpMethod.Get, TestEndpoint);
    }

    [Fact]
    public async Task GetDirectAsync_ShouldReturnObject_WhenSuccessful()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_testData, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.GetDirectAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testData.Name, result.Name);
        Assert.Equal(_testData.Email, result.Email);
        VerifyAuthenticatedRequest(HttpMethod.Get, TestEndpoint);
    }

    [Fact]
    public async Task GetDirectAsync_ShouldReturnNull_WhenHttpError()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, "Not found");

        // Act
        var result = await _apiService.GetDirectAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDirectNoAuthAsync_ShouldReturnObject_WithoutAuthentication()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_testData, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.GetDirectNoAuthAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testData.Name, result.Name);
        VerifyUnauthenticatedRequest(HttpMethod.Get, TestEndpoint);
    }

    #endregion

    #region POST Tests

    [Fact]
    public async Task PostStringAsync_ShouldReturnString_WhenSuccessful()
    {
        // Arrange
        var expectedResponse = "Post success";
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiService.PostStringAsync(TestEndpoint, _testData);

        // Assert
        Assert.Equal(expectedResponse, result);
        VerifyAuthenticatedRequest(HttpMethod.Post, TestEndpoint, _testData);
    }

    [Fact]
    public async Task PostStringNoAuthAsync_ShouldReturnString_WithoutAuthentication()
    {
        // Arrange
        var expectedResponse = "Post success";
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiService.PostStringNoAuthAsync(TestEndpoint, _testData);

        // Assert
        Assert.Equal(expectedResponse, result);
        VerifyUnauthenticatedRequest(HttpMethod.Post, TestEndpoint, _testData);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnApiResponse_WhenSuccessful()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_successApiResponse, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.PostAsync<TestModel>(TestEndpoint, _testData);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(_testData.Name, result.Data.Name);
        VerifyAuthenticatedRequest(HttpMethod.Post, TestEndpoint, _testData);
    }

    [Fact]
    public async Task PostNoAuthAsync_ShouldReturnApiResponse_WithoutAuthentication()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_successApiResponse, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.PostNoAuthAsync<TestModel>(TestEndpoint, _testData);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        VerifyUnauthenticatedRequest(HttpMethod.Post, TestEndpoint, _testData);
    }

    [Fact]
    public async Task PostDirectAsync_ShouldReturnObject_WhenSuccessful()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_testData, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.PostDirectAsync<TestModel>(TestEndpoint, _testData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testData.Name, result.Name);
        VerifyAuthenticatedRequest(HttpMethod.Post, TestEndpoint, _testData);
    }

    [Fact]
    public async Task PostDirectNoAuthAsync_ShouldReturnObject_WithoutAuthentication()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_testData, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.PostDirectNoAuthAsync<TestModel>(TestEndpoint, _testData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testData.Name, result.Name);
        VerifyUnauthenticatedRequest(HttpMethod.Post, TestEndpoint, _testData);
    }

    #endregion

    #region PUT Tests

    [Fact]
    public async Task PutStringAsync_ShouldReturnString_WhenSuccessful()
    {
        // Arrange
        var expectedResponse = "Put success";
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiService.PutStringAsync(TestEndpoint, _testData);

        // Assert
        Assert.Equal(expectedResponse, result);
        VerifyAuthenticatedRequest(HttpMethod.Put, TestEndpoint, _testData);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnApiResponse_WhenSuccessful()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_successApiResponse, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.PutAsync<TestModel>(TestEndpoint, _testData);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        VerifyAuthenticatedRequest(HttpMethod.Put, TestEndpoint, _testData);
    }

    [Fact]
    public async Task PutDirectAsync_ShouldReturnObject_WhenSuccessful()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_testData, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.PutDirectAsync<TestModel>(TestEndpoint, _testData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testData.Name, result.Name);
        VerifyAuthenticatedRequest(HttpMethod.Put, TestEndpoint, _testData);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task DeleteStringAsync_ShouldReturnString_WhenSuccessful()
    {
        // Arrange
        var expectedResponse = "Delete success";
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiService.DeleteStringAsync(TestEndpoint);

        // Assert
        Assert.Equal(expectedResponse, result);
        VerifyAuthenticatedRequest(HttpMethod.Delete, TestEndpoint);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnApiResponse_WhenSuccessful()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_successApiResponse, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.DeleteAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.True(result.Success);
        VerifyAuthenticatedRequest(HttpMethod.Delete, TestEndpoint);
    }

    [Fact]
    public async Task DeleteDirectAsync_ShouldReturnObject_WhenSuccessful()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_testData, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.DeleteDirectAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testData.Name, result.Name);
        VerifyAuthenticatedRequest(HttpMethod.Delete, TestEndpoint);
    }

    [Fact]
    public async Task DeleteNoAuthAsync_ShouldReturnApiResponse_WithoutAuthentication()
    {
        // Arrange
        var jsonResponse = JsonSerializer.Serialize(_successApiResponse, GetJsonOptions());
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _apiService.DeleteNoAuthAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.True(result.Success);
        VerifyUnauthenticatedRequest(HttpMethod.Delete, TestEndpoint);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetAsync_ShouldHandleNetworkError_Gracefully()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _apiService.GetAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Error de conexión", result.Message);
    }

    [Fact]
    public async Task PostAsync_ShouldHandleNetworkError_Gracefully()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _apiService.PostAsync<TestModel>(TestEndpoint, _testData);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Error de conexión", result.Message);
    }

    [Fact]
    public async Task GetDirectAsync_ShouldReturnNull_OnNetworkError()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _apiService.GetDirectAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Case Insensitive JSON Tests

    [Fact]
    public async Task GetDirectAsync_ShouldDeserialize_CaseInsensitiveJson()
    {
        // Arrange
        var caseInsensitiveJson = """{"ID":1,"name":"Test","EMAIL":"test@test.com"}""";
        SetupHttpResponse(HttpStatusCode.OK, caseInsensitiveJson);

        // Act
        var result = await _apiService.GetDirectAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.Equal("test@test.com", result.Email);
    }

    [Fact]
    public async Task GetAsync_ShouldDeserialize_CaseInsensitiveApiResponse()
    {
        // Arrange
        var caseInsensitiveJson = """
        {
            "success": true,
            "message": "Success",
            "data": {"ID":1,"name":"Test","EMAIL":"test@test.com"}
        }
        """;
        SetupHttpResponse(HttpStatusCode.OK, caseInsensitiveJson);

        // Act
        var result = await _apiService.GetAsync<TestModel>(TestEndpoint);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Success", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.Id);
        Assert.Equal("Test", result.Data.Name);
        Assert.Equal("test@test.com", result.Data.Email);
    }

    #endregion

    #region ApiResponse Processing Tests

    [Fact]
    public async Task ProcessResponseAsync_ShouldExecuteOnSuccess_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);
        bool onSuccessCalled = false;
        bool onErrorCalled = false;

        // Act
        await _apiService.ProcessResponseAsync(
            response,
            async data => 
            {
                onSuccessCalled = true;
                Assert.Equal(_testData.Name, data.Name);
            },
            async error => { onErrorCalled = true; }
        );

        // Assert
        Assert.True(onSuccessCalled);
        Assert.False(onErrorCalled);
    }

    [Fact]
    public async Task ProcessResponseAsync_ShouldExecuteOnError_WhenResponseHasError()
    {
        // Arrange
        var response = ApiResponse<TestModel>.ErrorResponse("Test error");
        bool onSuccessCalled = false;
        bool onErrorCalled = false;

        // Act
        await _apiService.ProcessResponseAsync(
            response,
            async data => { onSuccessCalled = true; },
            async error => 
            {
                onErrorCalled = true;
                Assert.Equal("Test error", error.Message);
            }
        );

        // Assert
        Assert.False(onSuccessCalled);
        Assert.True(onErrorCalled);
    }

    [Fact]
    public void ProcessResponse_ShouldExecuteOnSuccess_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);
        bool onSuccessCalled = false;

        // Act
        _apiService.ProcessResponse(
            response,
            data => 
            {
                onSuccessCalled = true;
                Assert.Equal(_testData.Name, data.Name);
            }
        );

        // Assert
        Assert.True(onSuccessCalled);
    }

    [Fact]
    public void GetDataOrDefault_ShouldReturnData_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);

        // Act
        var result = _apiService.GetDataOrDefault(response);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testData.Name, result.Name);
    }

    [Fact]
    public void GetDataOrDefault_ShouldReturnDefault_WhenResponseHasError()
    {
        // Arrange
        var response = ApiResponse<TestModel>.ErrorResponse("Error");
        var defaultValue = new TestModel { Name = "Default" };

        // Act
        var result = _apiService.GetDataOrDefault(response, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GetDataOrThrow_ShouldReturnData_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);

        // Act
        var result = _apiService.GetDataOrThrow(response);

        // Assert
        Assert.Equal(_testData.Name, result.Name);
    }

    [Fact]
    public void GetDataOrThrow_ShouldThrowException_WhenResponseHasError()
    {
        // Arrange
        var response = ApiResponse<TestModel>.ErrorResponse("Test error");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _apiService.GetDataOrThrow(response));
        Assert.Contains("Test error", exception.Message);
    }

    [Fact]
    public void TransformResponse_ShouldTransformData_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);

        // Act
        var result = _apiService.TransformResponse(response, data => data.Name.ToUpper());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(_testData.Name.ToUpper(), result.Data);
    }

    [Fact]
    public void TransformResponse_ShouldReturnError_WhenOriginalResponseHasError()
    {
        // Arrange
        var response = ApiResponse<TestModel>.ErrorResponse("Original error");

        // Act
        var result = _apiService.TransformResponse(response, data => data.Name);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Original error", result.Message);
    }

    [Fact]
    public async Task TransformResponseAsync_ShouldTransformData_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);

        // Act
        var result = await _apiService.TransformResponseAsync(response, 
            async data => await Task.FromResult(data.Name.ToUpper()));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(_testData.Name.ToUpper(), result.Data);
    }

    [Fact]
    public void CombineResponses_ShouldCombineSuccessfulResponses()
    {
        // Arrange
        var response1 = ApiResponse<TestModel>.SuccessResponse(_testData);
        var response2 = ApiResponse<TestModel>.SuccessResponse(new TestModel { Name = "Test2" });

        // Act
        var result = _apiService.CombineResponses(response1, response2);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data?.Count);
    }

    [Fact]
    public void CombineResponses_ShouldReturnError_WhenAllResponsesFail()
    {
        // Arrange
        var response1 = ApiResponse<TestModel>.ErrorResponse("Error 1");
        var response2 = ApiResponse<TestModel>.ErrorResponse("Error 2");

        // Act
        var result = _apiService.CombineResponses(response1, response2);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Todas las operaciones fallaron", result.Message);
    }

    [Fact]
    public void GetErrorMessages_ShouldReturnEmptyString_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);

        // Act
        var result = _apiService.GetErrorMessages(response);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetErrorMessages_ShouldReturnErrorMessages_WhenResponseHasErrors()
    {
        // Arrange
        var response = ApiResponse<TestModel>.ErrorResponse("Main error", 
            new List<string> { "Error 1", "Error 2" });

        // Act
        var result = _apiService.GetErrorMessages(response);

        // Assert
        Assert.Contains("Main error", result);
        Assert.Contains("Error 1", result);
        Assert.Contains("Error 2", result);
    }

    [Fact]
    public void IsSuccessWithData_ShouldReturnTrue_WhenResponseIsSuccessfulWithData()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);

        // Act
        var result = _apiService.IsSuccessWithData(response);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSuccessWithData_ShouldReturnFalse_WhenResponseHasError()
    {
        // Arrange
        var response = ApiResponse<TestModel>.ErrorResponse("Error");

        // Act
        var result = _apiService.IsSuccessWithData(response);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void OnSuccess_ShouldExecuteAction_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);
        bool actionExecuted = false;

        // Act
        var result = _apiService.OnSuccess(response, data => actionExecuted = true);

        // Assert
        Assert.True(actionExecuted);
        Assert.Same(response, result);
    }

    [Fact]
    public void OnError_ShouldExecuteAction_WhenResponseHasError()
    {
        // Arrange
        var response = ApiResponse<TestModel>.ErrorResponse("Error");
        bool actionExecuted = false;

        // Act
        var result = _apiService.OnError(response, error => actionExecuted = true);

        // Assert
        Assert.True(actionExecuted);
        Assert.Same(response, result);
    }

    [Fact]
    public async Task OnSuccessAsync_ShouldExecuteAction_WhenResponseIsSuccessful()
    {
        // Arrange
        var response = ApiResponse<TestModel>.SuccessResponse(_testData);
        bool actionExecuted = false;

        // Act
        var result = await _apiService.OnSuccessAsync(response, async data => 
        {
            actionExecuted = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.True(actionExecuted);
        Assert.Same(response, result);
    }

    [Fact]
    public async Task OnErrorAsync_ShouldExecuteAction_WhenResponseHasError()
    {
        // Arrange
        var response = ApiResponse<TestModel>.ErrorResponse("Error");
        bool actionExecuted = false;

        // Act
        var result = await _apiService.OnErrorAsync(response, async error => 
        {
            actionExecuted = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.True(actionExecuted);
        Assert.Same(response, result);
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyAuthenticatedRequest(HttpMethod method, string endpoint, object? data = null)
    {
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == method &&
                    req.RequestUri!.PathAndQuery == endpoint &&
                    req.Headers.Any(h => h.Key == "Authorization" && h.Value.Contains(TestToken)) &&
                    (data == null || req.Content != null)),
                ItExpr.IsAny<CancellationToken>());
    }

    private void VerifyUnauthenticatedRequest(HttpMethod method, string endpoint, object? data = null)
    {
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == method &&
                    req.RequestUri!.PathAndQuery == endpoint &&
                    !req.Headers.Any(h => h.Key == "Authorization") &&
                    (data == null || req.Content != null)),
                ItExpr.IsAny<CancellationToken>());
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #endregion
}