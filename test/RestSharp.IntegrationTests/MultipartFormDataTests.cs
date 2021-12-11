﻿using System.Net;
using RestSharp.Tests.Shared.Fixtures;

namespace RestSharp.IntegrationTests;

public class MultipartFormDataTests : IDisposable {
    public MultipartFormDataTests() {
        _server = SimpleServer.Create(RequestHandler.Handle);
        _client = new RestClient(_server.Url);
    }

    public void Dispose() => _server.Dispose();

    const string LineBreak = "\r\n";

    const string Expected = "--{0}" +
        LineBreak +
        "Content-Disposition: form-data; name=\"foo\"" +
        LineBreak +
        LineBreak +
        "bar" +
        LineBreak +
        "--{0}" +
        LineBreak +
        "Content-Disposition: form-data; name=\"a name with spaces\"" +
        LineBreak +
        LineBreak +
        "somedata" +
        LineBreak +
        "--{0}--" +
        LineBreak;

    const string ExpectedFileAndBodyRequestContent = "--{0}" +
        LineBreak +
        "Content-Type: application/json" +
        LineBreak +
        "Content-Disposition: form-data; name=\"controlName\"" +
        LineBreak +
        LineBreak +
        "test" +
        LineBreak +
        "--{0}" +
        LineBreak +
        "Content-Disposition: form-data; name=\"fileName\"; filename=\"TestFile.txt\"" +
        LineBreak +
        "Content-Type: application/octet-stream" +
        LineBreak +
        LineBreak +
        "This is a test file for RestSharp." +
        LineBreak +
        "--{0}--" +
        LineBreak;

    const string ExpectedDefaultMultipartContentType = "multipart/form-data; boundary={0}";

    const string ExpectedCustomMultipartContentType = "multipart/vnd.resteasy+form-data; boundary={0}";

    readonly SimpleServer _server;
    readonly RestClient   _client;

    static class RequestHandler {
        public static string CapturedContentType { get; set; }

        public static void Handle(HttpListenerContext context) {
            CapturedContentType = context.Request.ContentType;
            Handlers.Echo(context);
        }
    }

    static void AddParameters(IRestRequest request) {
        request.AddParameter("foo", "bar");
        request.AddParameter("a name with spaces", "somedata");
    }

    [Fact]
    public async Task AlwaysMultipartFormData_WithParameter_Execute() {
        var request = new RestRequest("?json_route=/posts") {
            AlwaysMultipartFormData = true,
            Method                  = Method.Post
        };

        request.AddParameter("title", "test", ParameterType.RequestBody);

        var response = await _client.ExecuteAsync(request);

        Assert.Null(response.ErrorException);
    }

    [Fact]
    public async Task MultipartFormData() {
        var request = new RestRequest("/", Method.Post) {
            AlwaysMultipartFormData = true
        };

        AddParameters(request);

        string? boundary = null;
        request.OnBeforeRequest += http => boundary = http.FormBoundary;

        var response = await _client.ExecuteAsync(request);

        var expected = string.Format(Expected, boundary);

        Assert.Equal(expected, response.Content);
    }

    [Fact]
    public async Task MultipartFormData_HasDefaultContentType() {
        var request = new RestRequest("/", Method.Post);

        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TestFile.txt");
        request.AddFile("fileName", path);

        request.AddParameter("controlName", "test", "application/json", ParameterType.RequestBody);

        string? boundary = null;
        request.OnBeforeRequest += http => boundary = http.FormBoundary;

        var response = await _client.ExecuteAsync(request);

        var expectedFileAndBodyRequestContent   = string.Format(ExpectedFileAndBodyRequestContent, boundary);
        var expectedDefaultMultipartContentType = string.Format(ExpectedDefaultMultipartContentType, boundary);

        Assert.Equal(expectedFileAndBodyRequestContent, response.Content);
        Assert.Equal(expectedDefaultMultipartContentType, RequestHandler.CapturedContentType);
    }

    [Fact]
    public async Task MultipartFormData_WithCustomContentType() {
        var request = new RestRequest("/", Method.Post);

        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TestFile.txt");

        const string customContentType = "multipart/vnd.resteasy+form-data";
        request.AddHeader("Content-Type", customContentType);
        request.AddFile("fileName", path);
        request.AddParameter("controlName", "test", "application/json", ParameterType.RequestBody);

        string? boundary = null;
        request.OnBeforeRequest += http => boundary = http.FormBoundary;

        var response = await _client.ExecuteAsync(request);

        var expectedFileAndBodyRequestContent  = string.Format(ExpectedFileAndBodyRequestContent, boundary);
        var expectedCustomMultipartContentType = string.Format(ExpectedCustomMultipartContentType, boundary);

        Assert.Equal(expectedFileAndBodyRequestContent, response.Content);
        Assert.Equal(expectedCustomMultipartContentType, RequestHandler.CapturedContentType);
    }

    [Fact]
    public async Task MultipartFormData_WithParameterAndFile_Async() {
        var request = new RestRequest("/", Method.Post) {
            AlwaysMultipartFormData = true
        };

        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TestFile.txt");
        request.AddFile("fileName", path);

        request.AddParameter("controlName", "test", "application/json", ParameterType.RequestBody);

        string? boundary = null;
        request.OnBeforeRequest += http => boundary = http.FormBoundary;

        var response = await _client.ExecuteAsync(request);

        var expectedFileAndBodyRequestContent = string.Format(ExpectedFileAndBodyRequestContent, boundary);

        Assert.Equal(expectedFileAndBodyRequestContent, response.Content);
    }

    [Fact]
    public async Task MultipartFormDataAsync() {
        var request = new RestRequest("/", Method.Post) { AlwaysMultipartFormData = true };

        AddParameters(request);

        string? boundary = null;

        var expected = string.Format(Expected, boundary);

        request.OnBeforeRequest += http => boundary = http.FormBoundary;

        var response = await _client.ExecuteAsync(request);
        Assert.Equal(expected, response.Content);
    }
}