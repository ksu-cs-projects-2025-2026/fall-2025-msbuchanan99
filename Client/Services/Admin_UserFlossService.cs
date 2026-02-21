using Client.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client.Services
{
    public class Admin_UserFlossState
    {
        private List<Admin_FlossModel> _floss = new();
        public IReadOnlyList<Admin_FlossModel> Floss => _floss;
        public int? LoadedForUserId { get; private set; }

        public bool IsLoading { get; private set; }
        public string? LastError { get; private set; }
        public event Action? Changed;

        
        public int? SelectedFlossId { get; private set; }
        public Admin_FlossModel? Selected => SelectedFlossId is int id ?
            _floss.FirstOrDefault(f => f.Id == id) : null;
        public Admin_FlossModel? Draft { get; private set; }

        public void BeginLoad()
        {
            IsLoading = true;
            LastError = null;
            Changed?.Invoke();
        }

        public void SetError(string error)
        {
            LastError = error;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void Clear()
        {
            _floss.Clear();
            LoadedForUserId = null;
            IsLoading = false;
            LastError = null;
            SelectedFlossId = null;
            Draft = null;
            Changed?.Invoke();
        }

        private static Admin_FlossModel Clone(Admin_FlossModel f) => new()
        {
            Id = f.Id,
            Name = f.Name,
            Number = f.Number,
            HexColor = f.HexColor,
            CreatedOn = f.CreatedOn,
            LastModified = f.LastModified
        };

        private void Update(Admin_FlossModel floss)
        {
            var idx = _floss.FindIndex(f => f.Id == floss.Id);
            if (idx >= 0) _floss[idx] = floss;
        }

        public void SetFlossList(int userId, IEnumerable<Admin_FlossModel>? floss)
        {
            LoadedForUserId = userId;
            _floss = floss.ToList() ?? new();
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetSelectedId(int id)
        {
            SelectedFlossId = id;
            LastError = null;
            Changed?.Invoke();
        }

        public bool BeginEdit(int id)
        {
            var current = _floss.First(f => f.Id == id);
            if (current is null) return false;

            Draft = Clone(current);
            LastError = null;

            Changed?.Invoke();
            return true;
        }
        public void CancelEdit()
        {
            Draft = null;
            LastError = null;
            Changed?.Invoke();
        }
        public void ApplyEdit(Admin_FlossModel floss)
        {
            Update(floss);
            Draft = null;
            LastError = null;
            Changed?.Invoke();
        }
    }
    public class Admin_UserFlossService
    {
        private readonly Admin_UserFlossState _state;
        private readonly HttpClient _http;
        public Admin_UserFlossService(Admin_UserFlossState state, HttpClient http)
        {
            _state = state;
            _http = http;
        }

        public async Task<Result> LoadForUserAsync(int userId, bool forceReload = false)
        {
            _state.BeginLoad();

            if (!forceReload && _state.Floss.Count > 0 && _state.LoadedForUserId == userId) return Result.Success();

            var response = await _http.GetAsync($"api/users/admin/{userId}/floss");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var floss = await response.Content.ReadFromJsonAsync<List<Admin_FlossModel>>();
            _state.SetFlossList(userId, floss);
            return Result.Success();
        }

        public bool BeginEdit(int id) => _state.BeginEdit(id);
        public void CancelEdit() => _state.CancelEdit();
        public async Task<Result> SaveEditAsync()
        {
            var draft = _state.Draft;
            if(draft is null)
            {
                _state.SetError("Nothing to save");
                return Result.Fail();
            }

            var userId = _state.LoadedForUserId;
            var flossId = _state.SelectedFlossId;

            var response = await _http.PutAsJsonAsync($"api/users/admin/{userId}/floss/{flossId}", draft.Amount);
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
