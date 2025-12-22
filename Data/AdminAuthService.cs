using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VocabularyTestApp.Data
{
    public class AdminAuthService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AdminAuthService>? _logger;
        public bool IsAuthenticated { get; private set; }

        public AdminAuthService(IConfiguration config, ILogger<AdminAuthService>? logger = null)
        {
            _config = config;
            _logger = logger;
        }

        public bool Login(string username, string password)
        {
            var cfgUser = (_config["Admin:Username"] ?? "admin").Trim();
            var cfgPass = (_config["Admin:Password"] ?? "CHANGE_ME").Trim();
            var inputUser = (username ?? "").Trim();
            var inputPass = (password ?? "").Trim();
            _logger?.LogInformation("Admin login attempt: inputUser='{user}', configuredUser='{cfgUser}', passwordConfigured={configured}",
                inputUser, cfgUser, IsPasswordConfigured());
            if (string.Equals(inputUser, cfgUser, StringComparison.OrdinalIgnoreCase) && inputPass == cfgPass)
            {
                IsAuthenticated = true;
                return true;
            }
            IsAuthenticated = false;
            return false;
        }

        public bool IsPasswordConfigured()
        {
            var cfgPass = (_config["Admin:Password"] ?? "CHANGE_ME").Trim();
            return !string.IsNullOrEmpty(cfgPass) && !string.Equals(cfgPass, "CHANGE_ME", StringComparison.Ordinal);
        }

        public void Logout()
        {
            IsAuthenticated = false;
        }
    }
}
