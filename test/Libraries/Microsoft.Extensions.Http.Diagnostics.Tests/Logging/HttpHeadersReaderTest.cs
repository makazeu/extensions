﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Http.Logging.Internal;
using Microsoft.Extensions.Http.Logging.Test.Internal;
using Microsoft.Extensions.Telemetry.Internal;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Logging.Test;

public class HttpHeadersReaderTest
{
    [Fact]
    public void HttpHeadersReader_WhenEmptyHeaders_DoesNothing()
    {
        using var httpRequest = new HttpRequestMessage();
        using var httpResponse = new HttpResponseMessage();
        var options = new LoggingOptions();

        var headersReader = new HttpHeadersReader(options.ToOptionsMonitor(), Mock.Of<IHttpHeadersRedactor>());
        var buffer = new List<KeyValuePair<string, string>>();

        headersReader.ReadRequestHeaders(httpRequest, buffer);
        buffer.Should().BeEmpty();

        headersReader.ReadResponseHeaders(httpResponse, buffer);
        buffer.Should().BeEmpty();
    }

    [Fact]
    public void HttpHeadersReader_WhenHeadersProvided_ReadsThem()
    {
        const string Redacted = "REDACTED";
        using var httpRequest = new HttpRequestMessage();
        using var httpResponse = new HttpResponseMessage();

        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), FakeTaxonomy.PrivateData))
            .Returns(Redacted);
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), FakeTaxonomy.PublicData))
            .Returns<IEnumerable<string>, DataClassification>((x, _) => string.Join(",", x));

        var options = new LoggingOptions
        {
            RequestHeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { "Header1", FakeTaxonomy.PrivateData },
                { "Header2", FakeTaxonomy.PrivateData }
            },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { "Header3", FakeTaxonomy.PublicData },
                { "Header4", FakeTaxonomy.PublicData },
                { "hEaDeR7", FakeTaxonomy.PrivateData } // This one is to test case-insensitivity
            }
        };

        var headersReader = new HttpHeadersReader(options.ToOptionsMonitor(), mockHeadersRedactor.Object);
        var buffer = new List<KeyValuePair<string, string>>();

        headersReader.ReadRequestHeaders(httpRequest, buffer);
        buffer.Should().BeEmpty();

        headersReader.ReadResponseHeaders(httpResponse, buffer);
        buffer.Should().BeEmpty();

        httpRequest.Headers.Add("Header1", "Value.1");
        httpRequest.Headers.Add("Header2", "Value.2");
        httpResponse.Headers.Add("Header3", "Value.3");
        httpResponse.Headers.Add("Header4", "Value.4");
        httpRequest.Headers.Add("Header5", string.Empty);
        httpResponse.Headers.Add("Header6", string.Empty);
        httpResponse.Headers.Add("Header7", "Value.7");

        var requestBuffer = new List<KeyValuePair<string, string>>();
        var responseBuffer = new List<KeyValuePair<string, string>>();
        var expectedRequest = new[]
        {
            new KeyValuePair<string, string>("Header1", Redacted),
            new KeyValuePair<string, string>("Header2", Redacted)
        };

        var expectedResponse = new[]
        {
            new KeyValuePair<string, string>("Header3", "Value.3"),
            new KeyValuePair<string, string>("Header4", "Value.4"),
            new KeyValuePair<string, string>("hEaDeR7", Redacted)
        };

        headersReader.ReadRequestHeaders(httpRequest, requestBuffer);
        headersReader.ReadResponseHeaders(httpResponse, responseBuffer);

        requestBuffer.Should().BeEquivalentTo(expectedRequest);
        responseBuffer.Should().BeEquivalentTo(expectedResponse);
    }

    [Theory]
    [CombinatorialData]
    public void HttpHeadersReader_WhenProvided_ReadsContentHeaders(bool logContentHeaders)
    {
        var mockHeadersRedactor = new Mock<IHttpHeadersRedactor>();
        mockHeadersRedactor.Setup(r => r.Redact(It.IsAny<IEnumerable<string>>(), FakeTaxonomy.PublicData))
            .Returns<IEnumerable<string>, DataClassification>((x, _) => string.Join(",", x));

        var options = new LoggingOptions
        {
            RequestHeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { "Header1", FakeTaxonomy.PublicData },
                { "Content-Header1", FakeTaxonomy.PublicData },
                { "Content-Type", FakeTaxonomy.PublicData }
            },
            ResponseHeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { "Header3", FakeTaxonomy.PublicData },
                { "Content-Header2", FakeTaxonomy.PublicData },
                { "Content-Length", FakeTaxonomy.PublicData }
            },
            LogContentHeaders = logContentHeaders
        };

        var headersReader = new HttpHeadersReader(options.ToOptionsMonitor(), mockHeadersRedactor.Object);

        using var requestContent = new StringContent(string.Empty);
        requestContent.Headers.ContentType = new(MediaTypeNames.Application.Soap);
        requestContent.Headers.ContentLength = 42;
        requestContent.Headers.Add("Content-Header1", "Content.1");

        using var httpRequest = new HttpRequestMessage { Content = requestContent };
        httpRequest.Headers.Add("Header1", "Value.1");
        httpRequest.Headers.Add("Header2", "Value.2");

        using var responseContent = new StringContent(string.Empty);
        responseContent.Headers.ContentType = new(MediaTypeNames.Text.Html);
        responseContent.Headers.ContentLength = 24;
        responseContent.Headers.Add("Content-Header2", "Content.2");

        using var httpResponse = new HttpResponseMessage { Content = responseContent };
        httpResponse.Headers.Add("Header3", "Value.3");
        httpResponse.Headers.Add("Header4", "Value.4");

        List<KeyValuePair<string, string>> expectedRequest = [new KeyValuePair<string, string>("Header1", "Value.1")];
        List<KeyValuePair<string, string>> expectedResponse = [new KeyValuePair<string, string>("Header3", "Value.3")];
        if (logContentHeaders)
        {
            expectedRequest.Add(new KeyValuePair<string, string>("Content-Header1", "Content.1"));
            expectedRequest.Add(new KeyValuePair<string, string>("Content-Type", MediaTypeNames.Application.Soap));

            expectedResponse.Add(new KeyValuePair<string, string>("Content-Header2", "Content.2"));
            expectedResponse.Add(new KeyValuePair<string, string>("Content-Length", "24"));
        }

        List<KeyValuePair<string, string>> requestBuffer = [];
        headersReader.ReadRequestHeaders(httpRequest, requestBuffer);
        requestBuffer.Should().BeEquivalentTo(expectedRequest);

        List<KeyValuePair<string, string>> responseBuffer = [];
        headersReader.ReadResponseHeaders(httpResponse, responseBuffer);
        responseBuffer.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public void HttpHeadersReader_WhenBufferIsNull_DoesNothing()
    {
        var options = new LoggingOptions();

        var headersReader = new HttpHeadersReader(options.ToOptionsMonitor(), Mock.Of<IHttpHeadersRedactor>());
        List<KeyValuePair<string, string>>? responseBuffer = null;

        using var httpRequest = new HttpRequestMessage();
        using var httpResponse = new HttpResponseMessage();

        headersReader.ReadResponseHeaders(httpResponse, responseBuffer);
        responseBuffer.Should().BeNull();

        headersReader.ReadRequestHeaders(httpRequest, responseBuffer);
        responseBuffer.Should().BeNull();
    }
}
