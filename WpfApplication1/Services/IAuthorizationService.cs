using System;

namespace WpfApplication1.Services
{
    /// <summary>
    /// Authorization request data transfer object.
    /// </summary>
    public class AuthorizationRequestData
    {
        public int AccountId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string UserFirstName { get; set; }
        public string AuthRequestCode { get; set; }
        public string AuthCode { get; set; }
    }

    /// <summary>
    /// Result of authorization service operations.
    /// </summary>
    public class AuthorizationResult
    {
        public bool Success { get; set; }
        public int AccountId { get; set; }
        public string AuthCode { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public string ServiceUsed { get; set; } // "WCF" or "gRPC"
    }

    /// <summary>
    /// Interface for authorization service operations.
    /// Abstracts WCF and gRPC implementations.
    /// </summary>
    public interface IAuthorizationService : IDisposable
    {
        /// <summary>
        /// Register an authorization request.
        /// </summary>
        /// <param name="request">The authorization request data</param>
        /// <returns>Result containing account ID if successful</returns>
        AuthorizationResult TryToRegisterAuthRequest(AuthorizationRequestData request);

        /// <summary>
        /// Get the authorization code for a paid account.
        /// </summary>
        /// <param name="accountId">The account ID</param>
        /// <param name="authRequestCode">The authorization request code</param>
        /// <returns>Result containing auth code if found</returns>
        AuthorizationResult GetAuthCode(int accountId, string authRequestCode);

        /// <summary>
        /// Check if the service is available.
        /// </summary>
        /// <returns>True if service is reachable</returns>
        bool IsAvailable();
    }
}
