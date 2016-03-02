using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.DTO.Server;
using Common.Exceptions;
using Common.Tools;
using Moq;
using NUnit.Framework;

namespace Common.Tests
{
    [TestFixture]
    public class HttpClientToolboxTests
    {
        private HttpClientToolbox _toolbox;
        private Mock<IHttpClient> _clientMock;

        [SetUp]
        public void SetUp()
        {
            _clientMock = new Mock<IHttpClient>(MockBehavior.Strict);
            _clientMock.Setup(c => c.Dispose());
            _clientMock.SetupProperty(c => c.BaseAddress);

            // HttpRequestHeaders constructor has internal access..
            var httpClient = new HttpClient();
            _clientMock.SetupGet(c => c.DefaultRequestHeaders).Returns(httpClient.DefaultRequestHeaders);

            _toolbox = new HttpClientToolbox(_clientMock.Object);
        }

        #region Constructor
        [Test]
        public void HttpClientToolbox_NoArgumentsConstructor_No_Exception()
        {
            // Act
            var testDelegate = new TestDelegate(() => new HttpClientToolbox());

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void HttpClientToolbox_Uri_Constructor_No_Exception()
        {
            // Act
            var testDelegate = new TestDelegate(() => new HttpClientToolbox(new Uri("http://localhost:13752/")));

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void HttpClientToolbox_Uri_AuthenticationHeader_No_Exception()
        {
            // Act
            var testDelegate = new TestDelegate(() => new HttpClientToolbox(new Uri("http://localhost:13752/"), new AuthenticationHeaderValue("someScheme")));

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void HttpClientToolbox_String_Constructor_No_Exception()
        {
            // Act
            var testDelegate = new TestDelegate(() => new HttpClientToolbox("http://localhost:13752/"));

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void HttpClientToolbox_String_AuthenticationHeader_No_Exception()
        {
            // Act
            var testDelegate = new TestDelegate(() => new HttpClientToolbox("http://localhost:13752/", new AuthenticationHeaderValue("someScheme")));

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }
        #endregion
        #region Dispose
        [Test]
        public void Dispose_No_Errors()
        {
            // Arrange
            _clientMock.Setup(c => c.Dispose()).Verifiable();

            // Act
            using (_toolbox) { }

            // Assert
            _clientMock.Verify(c => c.Dispose(), Times.Once);
        }
        #endregion
        #region AuthenticationHeader
        [Test]
        public void AuthenticationHeader_Get()
        {
            // Act
            var testDelegate = new TestDelegate(() => { });

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void AuthenticationHeader_Set()
        {
            // Arrange
            var header = new AuthenticationHeaderValue("someScheme");

            // Act
            _toolbox.AuthenticationHeader = header;

            // Assert
            Assert.AreSame(header, _toolbox.AuthenticationHeader);
        }
        #endregion
        #region SetBaseAddress(string)

        [Test]
        public void SetBaseAddress_string()
        {
            // Act
            var testDelegate = new TestDelegate(() => _toolbox.SetBaseAddress("http://localhost:13768/"));

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }
        #endregion
        #region Create
        [Test]
        public void Create_OK_Does_Not_Throw()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create("", new object()));

            // Assert
            Assert.DoesNotThrow(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_NotFound_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create("", new object()));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_Unauthorized_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create("", new object()));

            // Assert
            Assert.Throws<UnauthorizedException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_Locked_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create("", new object()));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_NotExecutable_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.PreconditionFailed));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create("", new object()));

            // Assert
            Assert.Throws<NotExecutableException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_UnknownError_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Internal Server Error")});

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create("", new object()));

            // Assert
            Assert.Throws<Exception>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_HostNotFound_Throws_HttpRequestException()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create("", new object()));

            // Assert
            Assert.Throws<HttpRequestException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }
        #endregion
        #region Create<T>

        [Test]
        public async Task Create_T_OK_Returns_T()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"RolesOnWorkflows\":{\"healthcare\":[\"Admin\"]}}", Encoding.UTF8, "application/json")                    
                });


            // Act
            var result = await _toolbox.Create<object, RolesOnWorkflowsDto>("", new object());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Admin", result.RolesOnWorkflows["healthcare"].First());
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_T_NotFound_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create<object, object>("", new object()));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_T_Unauthorized_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create<object, object>("", new object()));

            // Assert
            Assert.Throws<UnauthorizedException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_T_Locked_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create<object, object>("", new object()));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_T_NotExecutable_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.PreconditionFailed));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create<object, object>("", new object()));

            // Assert
            Assert.Throws<NotExecutableException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_T_UnknownError_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Internal Server Error") });

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create<object, object>("", new object()));

            // Assert
            Assert.Throws<Exception>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Create_T_HostNotFound_Throws_HttpRequestException()
        {
            // Arrange
            _clientMock.Setup(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Create<object, object>("", new object()));

            // Assert
            Assert.Throws<HttpRequestException>(testDelegate);
            _clientMock.Verify(c => c.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }
        #endregion
        #region Read
        [Test]
        public async Task Read_T_OK_Returns_T()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"RolesOnWorkflows\":{\"healthcare\":[\"Admin\"]}}", Encoding.UTF8, "application/json")
                });


            // Act
            var result = await _toolbox.Read<RolesOnWorkflowsDto>("");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Admin", result.RolesOnWorkflows["healthcare"].First());
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Read_T_NotFound_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Read<object>(""));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Read_T_Unauthorized_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Read<object>(""));

            // Assert
            Assert.Throws<UnauthorizedException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Read_T_Locked_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Read<object>(""));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Read_T_NotExecutable_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.PreconditionFailed));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Read<object>(""));

            // Assert
            Assert.Throws<NotExecutableException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Read_T_UnknownError_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Internal Server Error") });

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Read<object>(""));

            // Assert
            Assert.Throws<Exception>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Read_T_HostNotFound_Throws_HttpRequestException()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Read<object>(""));

            // Assert
            Assert.Throws<HttpRequestException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }
        #endregion
        #region ReadList
        [Test]
        public async Task ReadList_T_OK_Returns_ListOfT()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[{\"RolesOnWorkflows\":{\"healthcare\":[\"Admin\"]}}]", Encoding.UTF8, "application/json")
                });


            // Act
            var result = await _toolbox.ReadList<RolesOnWorkflowsDto>("");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Admin", result.First().RolesOnWorkflows["healthcare"].First());
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ReadList_T_NotFound_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.ReadList<object>(""));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ReadList_T_Unauthorized_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.ReadList<object>(""));

            // Assert
            Assert.Throws<UnauthorizedException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ReadList_T_Locked_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.ReadList<object>(""));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ReadList_T_NotExecutable_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.PreconditionFailed));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.ReadList<object>(""));

            // Assert
            Assert.Throws<NotExecutableException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ReadList_T_UnknownError_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Internal Server Error") });

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.ReadList<object>(""));

            // Assert
            Assert.Throws<Exception>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ReadList_T_HostNotFound_Throws_HttpRequestException()
        {
            // Arrange
            _clientMock.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.ReadList<object>(""));

            // Assert
            Assert.Throws<HttpRequestException>(testDelegate);
            _clientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }
        #endregion
        #region Update
        [Test]
        public void Update_OK_Does_Not_Throw()
        {
            // Arrange
            _clientMock.Setup(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Update("", new object()));

            // Assert
            Assert.DoesNotThrow(testDelegate);
            _clientMock.Verify(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Update_NotFound_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Update("", new object()));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
            _clientMock.Verify(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Update_Unauthorized_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Update("", new object()));

            // Assert
            Assert.Throws<UnauthorizedException>(testDelegate);
            _clientMock.Verify(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Update_Locked_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Update("", new object()));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
            _clientMock.Verify(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Update_NotExecutable_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.PreconditionFailed));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Update("", new object()));

            // Assert
            Assert.Throws<NotExecutableException>(testDelegate);
            _clientMock.Verify(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Update_UnknownError_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Internal Server Error") });

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Update("", new object()));

            // Assert
            Assert.Throws<Exception>(testDelegate);
            _clientMock.Verify(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void Update_HostNotFound_Throws_HttpRequestException()
        {
            // Arrange
            _clientMock.Setup(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Update("", new object()));

            // Assert
            Assert.Throws<HttpRequestException>(testDelegate);
            _clientMock.Verify(c => c.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }
        #endregion
        #region Delete
        [Test]
        public void Delete_OK_Does_Not_Throw()
        {
            // Arrange
            _clientMock.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Delete(""));

            // Assert
            Assert.DoesNotThrow(testDelegate);
            _clientMock.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Delete_NotFound_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Delete(""));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
            _clientMock.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Delete_Unauthorized_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Delete(""));

            // Assert
            Assert.Throws<UnauthorizedException>(testDelegate);
            _clientMock.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Delete_Locked_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Delete(""));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
            _clientMock.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Delete_NotExecutable_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.PreconditionFailed));

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Delete(""));

            // Assert
            Assert.Throws<NotExecutableException>(testDelegate);
            _clientMock.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Delete_UnknownError_Throws()
        {
            // Arrange
            _clientMock.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Internal Server Error") });

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Delete(""));

            // Assert
            Assert.Throws<Exception>(testDelegate);
            _clientMock.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Delete_HostNotFound_Throws_HttpRequestException()
        {
            // Arrange
            _clientMock.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _toolbox.Delete(""));

            // Assert
            Assert.Throws<HttpRequestException>(testDelegate);
            _clientMock.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Once);
        }
        #endregion
    }
}
