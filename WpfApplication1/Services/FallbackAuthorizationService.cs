using System;
using WpfApplication1.Properties;

namespace WpfApplication1.Services
{
    /// <summary>
    /// Fallback authorization service that tries WCF first, then gRPC if WCF fails.
    /// Provides resilience when one service is unavailable.
    /// </summary>
    public class FallbackAuthorizationService : IAuthorizationService
    {
        private readonly Lazy<WcfAuthorizationService> _wcfService;
        private readonly Lazy<GrpcAuthorizationService> _grpcService;
        private bool _disposed;
        private string _lastServiceUsed;

        /// <summary>
        /// Gets the name of the last service that was successfully used.
        /// </summary>
        public string LastServiceUsed => _lastServiceUsed;

        /// <summary>
        /// Creates a new fallback authorization service.
        /// Uses settings for gRPC server address.
        /// </summary>
        public FallbackAuthorizationService()
        {
            _wcfService = new Lazy<WcfAuthorizationService>(() => new WcfAuthorizationService());

            // Get gRPC address from settings or use default
            string grpcAddress = GetGrpcServerAddress();
            _grpcService = new Lazy<GrpcAuthorizationService>(() => new GrpcAuthorizationService(grpcAddress));
        }

        /// <summary>
        /// Creates a new fallback authorization service with explicit gRPC address.
        /// </summary>
        /// <param name="grpcServerAddress">The gRPC server address</param>
        public FallbackAuthorizationService(string grpcServerAddress)
        {
            _wcfService = new Lazy<WcfAuthorizationService>(() => new WcfAuthorizationService());
            _grpcService = new Lazy<GrpcAuthorizationService>(() => new GrpcAuthorizationService(grpcServerAddress));
        }

        public AuthorizationResult TryToRegisterAuthRequest(AuthorizationRequestData request)
        {
            // Try WCF first
            try
            {
                var result = _wcfService.Value.TryToRegisterAuthRequest(request);
                if (result.Success || !IsConnectionError(result.ErrorCode))
                {
                    _lastServiceUsed = "WCF";
                    return result;
                }
            }
            catch (Exception)
            {
                // WCF failed, fall through to gRPC
            }

            // Fall back to gRPC
            try
            {
                var result = _grpcService.Value.TryToRegisterAuthRequest(request);
                _lastServiceUsed = "gRPC";
                result.Message = $"[gRPC fallback] {result.Message}";
                return result;
            }
            catch (Exception ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = $"Both WCF and gRPC services failed. Last error: {ex.Message}",
                    ErrorCode = "ALL_SERVICES_FAILED",
                    ServiceUsed = "None"
                };
            }
        }

        public AuthorizationResult GetAuthCode(int accountId, string authRequestCode)
        {
            // Try WCF first
            try
            {
                var result = _wcfService.Value.GetAuthCode(accountId, authRequestCode);
                if (result.Success || !IsConnectionError(result.ErrorCode))
                {
                    _lastServiceUsed = "WCF";
                    return result;
                }
            }
            catch (Exception)
            {
                // WCF failed, fall through to gRPC
            }

            // Fall back to gRPC
            try
            {
                var result = _grpcService.Value.GetAuthCode(accountId, authRequestCode);
                _lastServiceUsed = "gRPC";
                result.Message = $"[gRPC fallback] {result.Message}";
                return result;
            }
            catch (Exception ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = $"Both WCF and gRPC services failed. Last error: {ex.Message}",
                    ErrorCode = "ALL_SERVICES_FAILED",
                    ServiceUsed = "None"
                };
            }
        }

        public bool IsAvailable()
        {
            // Check if either service is available
            try
            {
                if (_wcfService.Value.IsAvailable())
                    return true;
            }
            catch { }

            try
            {
                if (_grpcService.Value.IsAvailable())
                    return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Determines if the error code indicates a connection/communication error
        /// that should trigger fallback to gRPC.
        /// </summary>
        private bool IsConnectionError(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode))
                return false;

            return errorCode == "TIMEOUT" ||
                   errorCode == "COMMUNICATION" ||
                   errorCode == "Unavailable" ||
                   errorCode == "DeadlineExceeded";
        }

        private string GetGrpcServerAddress()
        {
            // Try to get from settings, fall back to default
            try
            {
                var address = Settings.Default.GrpcServerAddress;
                if (!string.IsNullOrEmpty(address))
                    return address;
            }
            catch { }

            // Default gRPC server address (same host as WCF, but gRPC endpoint)
            return "https://vizdr.somee.com";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_wcfService.IsValueCreated)
                    _wcfService.Value.Dispose();
                if (_grpcService.IsValueCreated)
                    _grpcService.Value.Dispose();
                _disposed = true;
            }
        }
    }
}
