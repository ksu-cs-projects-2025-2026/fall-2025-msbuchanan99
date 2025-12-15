using Client.Models;
using System.Net.Http.Json;

namespace Client.Services
{
    public class UserFlossState
    {
        private List<UserFlossModel> _floss = new();
        public IReadOnlyList<UserFlossModel> Floss => _floss;
        public int? LoadedForUserId { get; private set; }

        public int? SelectedFlossId { get; private set; }
        public UserFlossModel? Selected => SelectedFlossId is int id ? _floss.FirstOrDefault(f => f.Id == id) : null;
        public UserFlossModel? Draft { get; private set; }

        public bool IsLoading { get; private set; }
        public string? LastError { get; private set; }
        public event Action? Changed;

        public void BeginLoad()
        {
            IsLoading = true;
            Changed?.Invoke();
        }

        public void SetFloss(int userId, IEnumerable<UserFlossModel> floss)
        {
            _floss = floss?.ToList() ?? new();
            LoadedForUserId = userId;
            LastError = null;
            SelectedFlossId = null;
            Draft = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetSelectedFlossId(int id)
        {
            SelectedFlossId = id;
            LastError = null;
            Changed?.Invoke();
        }

        public void SetError(string error)
        {
            IsLoading = false;
            LastError = null;
            Changed?.Invoke();
        }

        public void Clear()
        {
            _floss.Clear();
            LoadedForUserId = null;
            SelectedFlossId = null;
            Draft = null;
            LastError = null;
            IsLoading = false; 
            Changed?.Invoke();
        }

        public void Upsert(UserFlossModel floss)
        {
            var idx = _floss.FindIndex(p => p.Id == floss.Id);
            if (idx >= 0) _floss[idx] = floss;
        }

        public void Remove(int id)
        {
            _floss.RemoveAll(f => f.Id == id);
            if(SelectedFlossId == id)
            {
                SelectedFlossId = null;
                Draft = null;
            }
            Changed?.Invoke();
        }

        private static UserFlossModel Clone(UserFlossModel f) => new()
        {
            Id = f.Id,
            Name = f.Name,
            Number = f.Number,
            HexColor = f.HexColor,
            Amount = f.Amount
        };

        public bool BeginEdit(int id)
        {
            IsLoading = true;

            SelectedFlossId = id;
            if(Selected is null)
            {
                SelectedFlossId = null;
                return false;
            }

            Draft = Clone(Selected);
            LastError = null;

            Changed?.Invoke();
            return true;
        }
        public void CancelEdit()
        {
            SelectedFlossId = null;
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
        public void ApplyEdit(UserFlossModel floss)
        {
            Upsert(floss);
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
    }
    public class UserFlossService
    {
        private readonly UserFlossState _state;
        private readonly HttpClient _http;
        public UserFlossService(UserFlossState state, HttpClient http)
        {
            _state = state;
            _http = http;
        }

        public async Task<Result> LoadFloss(int userId, bool forceRefresh = false)
        {
            if(!forceRefresh && _state.LoadedForUserId == userId)
            {
                return Result.Success();
            }

            _state.BeginLoad();

            var response = await _http.GetAsync($"api/users/{userId}/projects");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var floss = await response.Content.ReadFromJsonAsync<IEnumerable<UserFlossModel>>();
            _state.SetFloss(userId, floss);

            return Result.Success();
        }

        public async Task<Result> SetFloss(int id, bool forceRefresh = false)
        {
            if(!forceRefresh && _state.SelectedFlossId == id) return Result.Success();

            _state.SetSelectedFlossId(id);

            return Result.Success();
        }

        public bool BeginEdt(int id) => _state.BeginEdit(id);
        public void CancelEdit() => _state.CancelEdit();
        public async Task<Result> ApplyEditAsync()
        {
            var draft = _state.Draft;
            if(draft is null)
            {
                _state.SetError("Nothing to save");
                return Result.Fail();
            }

            var userId = _state.LoadedForUserId;
            var flossId = _state.SelectedFlossId;

            var response = await _http.PutAsJsonAsync($"api/users/{userId}/floss/{flossId}", draft.Amount);
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyEdit(draft);
            return Result.Success();
        }

        private async Task SetErrorFromResponse(HttpResponseMessage response)
        {
            var message = await response.Content.ReadAsStringAsync();
            _state.SetError(message);
        }
    }
}
