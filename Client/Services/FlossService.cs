using Client.Models;
using System.Net.Http.Json;

namespace Client.Services
{
    public class FlossState
    {
        private List<FlossModel> _floss = new();
        public IReadOnlyList<FlossModel> Floss => _floss;
        public Dictionary<int, string> FlossDictionary => _floss.ToDictionary(f => (int)f.Id!, f => $"{f.Number} - {f.Name}");

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

        public void Clear()
        {
            _floss.Clear();
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
    }
}
