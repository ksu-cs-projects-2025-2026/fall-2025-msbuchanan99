using Client.Models;
using System.Net.Http.Json;

namespace Client.Services
{
    public class FlossState
    {
        private List<FlossModel> _floss = new();
        public IReadOnlyList<FlossModel> Floss => _floss;
        public Dictionary<int, string> FlossDictionary => _floss.ToDictionary(f => (int)f.Id!, f => $"{f.Number} - {f.Name}");

        public int? LoadedForUser { get; private set; }
        private List<UserFlossModel> _userFloss = new();
        public IReadOnlyList<UserFlossModel>? UserFlossFiltered => _userFloss
            .Where(uf => (uf.Name.Contains(SearchText) || uf.Number.Contains(SearchText)) &&
                         ((uf.Amount > 0) == ShowOwned || (uf.Amount == 0) == ShowUnowned))
            .ToList();
        private string SearchText { get; set; } = "";
        private bool ShowOwned { get; set; } = true;
        private bool ShowUnowned { get; set; } = false;

        public bool IsLoading { get; private set; }
        public string? LastError { get; private set; }
        public event Action? Changed;

        public void BeginLoad()
        {
            IsLoading = true;
            Changed?.Invoke();
        }

        public void SetFloss(IEnumerable<FlossModel> floss)
        {
            _floss = floss.ToList() ?? new();
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetUserFloss(int userId, IEnumerable<UserFlossModel> userFloss)
        {
            LoadedForUser = userId;
            _userFloss = userFloss.ToList() ?? new();
            LastError = null;
            Changed?.Invoke();
        }

        public void SetFilter(string search, bool own, bool unown)
        {
            SearchText = search;
            ShowOwned = own;
            ShowUnowned = unown;
            LastError = null;
            Changed?.Invoke();
        }

        public void Clear()
        {
            _floss.Clear();
            _userFloss.Clear();
            LoadedForUser = null;
            LastError = null;
            Changed?.Invoke();
        }

        public void SetError(string error)
        {
            LastError = error;
            IsLoading = false;
            Changed?.Invoke();
        }
    }
    public class FlossService
    {
        private readonly HttpClient _http;
        private readonly FlossState _state;
        public FlossService(HttpClient http, FlossState state)
        {
            _http = http;
            _state = state;
        }

        public async Task<Result> LoadFlosses(bool forceRefresh = false)
        {
            if (!forceRefresh && _state.Floss.Count > 0) return Result.Success();

            var response = await _http.GetAsync("api/floss");
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                _state.SetError(msg);
                return Result.Fail();
            }

            var flosses = await response.Content.ReadFromJsonAsync<IEnumerable<FlossModel>>();
            _state.SetFloss(flosses!);
            return Result.Success();
        }

        public async Task<Result> LoadUserFlosses(int userId)
        {
            var response = await _http.GetAsync($"api/users/{userId}/floss");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var userFloss = await response.Content.ReadFromJsonAsync<List<UserFlossModel>>();
            _state.SetUserFloss(userId, userFloss);
            return Result.Success();
        }

        public void FilterUserFlosses(string searchText, OwnershipMode owned)
        {
            bool own = true;
            bool unown = false;
            if (owned == OwnershipMode.Both) unown = true;
            else if(owned == OwnershipMode.Unowned) own = false;
            _state.SetFilter(searchText, own, unown);
        }

        public async Task<Result> EditFloss(int userId, int? selectedId, int? amount)
        {
            if(selectedId == null || amount == null)
            {
                _state.SetError("Amount cannot be null.");
                return Result.Fail();
            }

            var response = await _http.PutAsJsonAsync($"api/users/{userId}/floss/{selectedId}", amount);
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }
            return Result.Success();
        }

        private async Task SetErrorFromResponse(HttpResponseMessage response)
        {
            var message = await response.Content.ReadAsStringAsync();
            _state.SetError(message);
        }
    }
}
