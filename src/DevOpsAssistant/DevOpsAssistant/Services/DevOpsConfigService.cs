using Blazored.LocalStorage;

namespace DevOpsAssistant.Services
{
    public class DevOpsConfigService
    {
        private const string StorageKey = "devops-config";
        private readonly ILocalStorageService _localStorage;
        private DevOpsConfig _config = new();

        public DevOpsConfigService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public DevOpsConfig Config => _config;

        public async Task LoadAsync()
        {
            var config = await _localStorage.GetItemAsync<DevOpsConfig>(StorageKey);
            if (config != null)
            {
                _config = config;
            }
        }

        public async Task SaveAsync(DevOpsConfig config)
        {
            _config = config;
            await _localStorage.SetItemAsync(StorageKey, config);
        }
    }
}
