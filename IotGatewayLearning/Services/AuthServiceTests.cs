using IotGatewayLearning.Application.DTOs;
using IotGatewayLearning.Application.Services;
using IotGatewayLearning.Common.Exceptions;
using IotGatewayLearning.Domain.Entities;
using IotGatewayLearning.Infrastructure.Repositories;
using Moq;
using System.Linq.Expressions;
using Xunit;
using Microsoft.Extensions.Logging;

namespace IotGatewayLearning.Tests.Services
{
    
    public class AuthServiceTests
    {
        [Fact]          // xUnit 告诉 Visual Studio:这是一个可以运行的测试方法
        public async Task LoginAsync_UsernameEmpty_ShouldThrowArgumentException()
        {
            // ==============================
            // 1. Arrange：准备
            // ==============================
            //new Mock<IUnitOfWork>(): Moq，帮我临时造一个假的 IUnitOfWork
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var tokenServiceMock = new Mock<ITokenService>();

            var loggerMock = new Mock<ILogger<AuthService>>();

            var authService = new AuthService(unitOfWorkMock.Object, tokenServiceMock.Object, loggerMock.Object);

            var request =
                new LoginRequestDto
                {
                    Username = "",
                    Password = "123456"
                };


            // ==============================
            // 2. Act + Assert：执行并验证
            // ==============================

            var exception = await Assert.ThrowsAsync<UnauthorizedAppException>(
                    () => authService.LoginAsync(request));


            // ==============================
            // 3. Assert：继续验证错误信息
            // ==============================

            Assert.Equal("用户名或密码错误", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_PasswordEmpty_ShouldThrowArgumentException()
        {
            // ==============================
            // 1. Arrange：准备测试数据
            // ==============================

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var tokenServiceMock = new Mock<ITokenService>();
            var loggerMock = new Mock<ILogger<AuthService>>();

            var authService = new AuthService(unitOfWorkMock.Object, tokenServiceMock.Object, loggerMock.Object);

            var request = new LoginRequestDto
            {
                Username = "admin",
                Password = ""
            };


            // ==============================
            // 2. Act + Assert：执行，并判断异常类型
            // ==============================
            var exception = await Assert.ThrowsAsync<UnauthorizedAppException>(() => authService.LoginAsync(request));


            // ==============================
            // 3. Assert：检查异常信息
            // ==============================
            Assert.Equal("用户名或密码错误", exception.Message);
        }


        [Fact]
        public async Task LoginAsync_UserNotFound_ShouldThrowUnauthorizedAccessException()
        {
            // ==============================
            // 1. Arrange：准备
            // ==============================

            // 假的 UnitOfWork
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            // 假的用户仓储
            var userRepositoryMock = new Mock<IRepository<User>>();

            var loggerMock = new Mock<ILogger<AuthService>>();

            // 假的 TokenService
            var tokenServiceMock = new Mock<ITokenService>();

            // 告诉假的 UnitOfWork： .Setup(u => u.Users)
            // 如果别人访问 Users，就把假的 userRepository 给他: .Returns(userRepositoryMock.Object)
            unitOfWorkMock.Setup(u => u.Users).Returns(userRepositoryMock.Object);

            // 告诉假的用户仓储：
            // 如果有人调用 FirstOrDefaultAsync 查询用户
            // 就返回 null，模拟“数据库找不到用户”
            userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);

            // 创建真正要测试的 AuthService
            var authService = new AuthService(unitOfWorkMock.Object, tokenServiceMock.Object, loggerMock.Object);

            // 用户名、密码都填写
            var request = new LoginRequestDto
            {
                Username = "admin",
                Password = "123456"
            };



            // ==============================
            // 2. Act + Assert
            // ==============================

            var exception = await Assert.ThrowsAsync<UnauthorizedAppException>(() => authService.LoginAsync(request));


            // ==============================
            // 3. Assert：检查错误信息
            // ==============================

            Assert.Equal("用户名或密码错误", exception.Message);
        }
    }
}
