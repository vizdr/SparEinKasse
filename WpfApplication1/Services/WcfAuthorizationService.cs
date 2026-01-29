using System;
using System.ServiceModel;
using WpfApplication1.ServiceRefServARCode;

namespace WpfApplication1.Services
{
    /// <summary>
    /// WCF implementation of the authorization service.
    /// Wraps the existing ServARCodeClient.
    /// </summary>
    public class WcfAuthorizationService : IAuthorizationService
    {
        private ServARCodeClient _client;
        private bool _disposed;

        public WcfAuthorizationService()
        {
            _client = new ServARCodeClient();
        }

        public AuthorizationResult TryToRegisterAuthRequest(AuthorizationRequestData request)
        {
            try
            {
                var wcfRequest = new AuthorizationRequest
                {
                    AccountId = request.AccountId,
                    AuthCode = request.AuthCode,
                    AuthRequestCode = request.AuthRequestCode,
                    UserEmail = request.UserEmail,
                    UserName = request.UserName,
                    UserFirstName = request.UserFirstName
                };

                int resultedAccId = _client.TryToRegisterAuthRequest(wcfRequest);

                return new AuthorizationResult
                {
                    Success = resultedAccId > 0,
                    AccountId = resultedAccId,
                    Message = resultedAccId > 0 ? "Registration successful" : "Registration failed",
                    ServiceUsed = "WCF"
                };
            }
            catch (TimeoutException ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = "Timeout: " + ex.Message,
                    ErrorCode = "TIMEOUT",
                    ServiceUsed = "WCF"
                };
            }
            catch (FaultException<AuthorizationRequestFault> ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = ex.Detail.FaultMessage,
                    ErrorCode = "FAULT",
                    ServiceUsed = "WCF"
                };
            }
            catch (CommunicationException ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = "Communication error: " + ex.Message,
                    ErrorCode = "COMMUNICATION",
                    ServiceUsed = "WCF"
                };
            }
            catch (Exception ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = "Unknown error: " + ex.Message,
                    ErrorCode = "UNKNOWN",
                    ServiceUsed = "WCF"
                };
            }
        }

        public AuthorizationResult GetAuthCode(int accountId, string authRequestCode)
        {
            try
            {
                string authCode = _client.GetAuthCode(accountId, authRequestCode);

                return new AuthorizationResult
                {
                    Success = !string.IsNullOrEmpty(authCode),
                    AuthCode = authCode,
                    Message = !string.IsNullOrEmpty(authCode) ? "Auth code retrieved" : "Auth code not found",
                    ServiceUsed = "WCF"
                };
            }
            catch (TimeoutException ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = "Timeout: " + ex.Message,
                    ErrorCode = "TIMEOUT",
                    ServiceUsed = "WCF"
                };
            }
            catch (FaultException<AuthorizationRequestFault> ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = ex.Detail.FaultMessage,
                    ErrorCode = "FAULT",
                    ServiceUsed = "WCF"
                };
            }
            catch (CommunicationException ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = "Communication error: " + ex.Message,
                    ErrorCode = "COMMUNICATION",
                    ServiceUsed = "WCF"
                };
            }
            catch (Exception ex)
            {
                return new AuthorizationResult
                {
                    Success = false,
                    Message = "Unknown error: " + ex.Message,
                    ErrorCode = "UNKNOWN",
                    ServiceUsed = "WCF"
                };
            }
        }

        public bool IsAvailable()
        {
            try
            {
                // Try a simple operation to check connectivity
                // The WCF client state check
                if (_client.State == CommunicationState.Faulted)
                {
                    _client.Abort();
                    _client = new ServARCodeClient();
                }
                return _client.State == CommunicationState.Created ||
                       _client.State == CommunicationState.Opened;
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
                try
                {
                    if (_client.State == CommunicationState.Faulted)
                    {
                        _client.Abort();
                    }
                    else
                    {
                        _client.Close();
                    }
                }
                catch
                {
                    _client.Abort();
                }
                _disposed = true;
            }
        }
    }
}
