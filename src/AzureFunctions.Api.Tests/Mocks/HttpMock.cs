
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Internal;
using Moq;
using Microsoft.AspNetCore.Http;

namespace AzureFunctions.Api.Tests.Mocks
{
    public class HttpMock
    {
        public static HttpRequest HttpRequestSetup(Dictionary<String, StringValues> header, Dictionary<String, StringValues> query, string body)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            reqMock.Setup(req => req.Headers).Returns(new HeaderDictionary(header));
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            reqMock.Setup(req => req.Body).Returns(stream);
            return reqMock.Object;
        }
    }
}