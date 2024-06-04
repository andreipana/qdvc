using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace qdvc.Tests.TestInfrastructure
{
    internal class TestRepository
    {
        private Dictionary<string, string> _storage = [];

        public HttpClient CreateClient()
        {

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Put,
                        "https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/*")
                    .Respond(c =>
                    {
                        _storage[c.RequestUri!.ToString()] = c.Content!.ReadAsStringAsync().Result;
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK
                        };
                    });
            mockHttp.When(HttpMethod.Head,
                    "https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/*")
                .Respond(c =>
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = _storage.ContainsKey(c.RequestUri!.ToString()) ? HttpStatusCode.OK : HttpStatusCode.NotFound
                    };
                });
            mockHttp.When(HttpMethod.Get,
                    "https://artifactory.hexagon.com/artifactory/gsurv-generic-release-local/sprout/testdata/files/md5/*")
                .Respond(c =>
                {
                    if (_storage.TryGetValue(c.RequestUri!.ToString(), out string? value))
                    {
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(value)
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.NotFound
                        };
                    }
                });

            return new HttpClient(mockHttp);
        }
    }
}
