using System;
using System.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using SSKA.Grpc;

namespace WpfApplication1.Services
{
    /// <summary>
    /// gRPC implementation of the authorization service.
    /// Uses SSKA.Grpc.Client for communication.
    /// </summary>
    public class GrpcAuthorizationService : IAuthorizationService
    {
        private readonly GrpcChannel _channel;
        private readonly AuthorizationService.AuthorizationServiceClient _client;
        private readonly string _serverAddress;
        private bool _disposed;

        /// <summary>
        /// Creates a new gRPC authorization service client.
        /// </summary>
        /// <param name="serverAddress">The gRPC server address (e.g., "https://vizdr.somee.com")</param>
        public GrpcAuthorizationService(string serverAddress)
        {
            _serverAddress = serverAddress;

            // Use gRPC-Web handler for HTTP/1.1 compatibility (required for most hosting)
            var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());

            _channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });

            _client = new AuthorizationService.AuthorizationServiceClient(_channel);
        }

        public AuthorizationResult TryToRegisterAuthRequest(AuthorizationRequestData request)
        {
            try
            {
                var grpcRequest = new AuthorizationRequest
                {
                    AccountId = request.AccountId,
                    UserEmail = request.UserEmail ?? string.Empty,
                    UserName = request.UserName ?? string.Empty,
                    UserFirstName = request.UserFirstName ?? string.Empty,
                    AuthRequestCode = request.AuthRequestCode ?? string.Empty
                };

                var response = _client.TryToRegisterAuthRequest(grpcRequest);

                return new AuthorizationResult
                {
                    Success = response.Success,
                    AccountId = response.AccountId,
                    Message = response.Message,
                    ErrorCode = response.ErrorCode,
                    ServiceUsed = "gRPC"
                };
            }
            catch (RpcException ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = $"gRPC error: {ex.Status.Detail}",
                    ErrorCode = ex.Status.StatusCode.ToString(),
                    ServiceUsed = "gRPC"
                };
            }
            catch (Exception ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = "Unknown error: " + ex.Message,
                    ErrorCode = "UNKNOWN",
                    ServiceUsed = "gRPC"
                };
            }
        }

        public AuthorizationResult GetAuthCode(int accountId, string authRequestCode)
        {
            try
            {
                var request = new GetAuthCodeRequest
                {
                    AccountId = accountId,
                    AuthRequestCode = authRequestCode ?? string.Empty
                };

                var response = _client.GetAuthCode(request);

                return new AuthorizationResult
                {
                    Success = response.Success,
                    AuthCode = response.AuthCode,
                    Message = response.Message,
                    ErrorCode = response.ErrorCode,
                    ServiceUsed = "gRPC"
                };
            }
            catch (RpcException ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = $"gRPC error: {ex.Status.Detail}",
                    ErrorCode = ex.Status.StatusCode.ToString(),
                    ServiceUsed = "gRPC"
                };
            }
            catch (Exception ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = "Unknown error: " + ex.Message,
                    ErrorCode = "UNKNOWN",
                    ServiceUsed = "gRPC"
                };
            }
        }

        public bool IsAvailable()
        {
            try
            {
                var response = _client.CheckHealth(new HealthCheckRequest());
                return response.IsHealthy;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _channel?.Dispose();
                _disposed = true;
            }
        }
    }
}
