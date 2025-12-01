using Client.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client.Services
{
    public class Admin_ProjectFlossState
    {
        private List<Admin_FlossModel> _floss = new();
        public IReadOnlyList<Admin_FlossModel> Floss => _floss;
        public int? LoadedForProjectId { get; private set; }

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
            LoadedForProjectId = null;
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
            Amount = f.Amount,
            Strands = f.Strands,
            CreatedOn = f.CreatedOn,
            LastModified = f.LastModified
        };

        private void Upsert(Admin_FlossModel floss)
        {
            var idx = _floss.FindIndex(f => f.Id == floss.Id);
            if (idx >= 0) _floss[idx] = floss;
            else _floss.Add(floss);
        }

        public void SetFlossList(int projectId, IEnumerable<Admin_FlossModel>? floss)
        {
            LoadedForProjectId = projectId;
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
            IsLoading = true;

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
            IsLoading = false;
            Changed?.Invoke();
        }
        public void ApplyEdit(Admin_FlossModel floss)
        {
            Upsert(floss);
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void BeginCreate()
        {
            IsLoading = true;
            Draft = new()
            {
                Id = 0,
                Name = null,
                Number = null,
                HexColor = null,
                Amount = 0,
                Strands = 0
            };
            LastError = null;
            Changed?.Invoke();
        }

        public void CancelCreate()
        {
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
        public void ApplyCreate(Admin_FlossModel floss)
        {
            Upsert(floss);
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public bool BeginDelete(int id)
        {
            IsLoading = true;

            var current = _floss.First(f => f.Id == id);
            if (current is null) return false;

            LastError = null;
            Changed?.Invoke();
            return true;
        }

        public void CancelDelete()
        {
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void ApplyDelete(int id)
        {
            _floss.RemoveAll(f => f.Id == id);
            Draft = null;
            SelectedFlossId = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
    }
    public class Admin_ProjectFlossService
    {
        private readonly Admin_ProjectFlossState _state;
        private readonly HttpClient _http;
        public Admin_ProjectFlossService(Admin_ProjectFlossState state, HttpClient http)
        {
            _state = state;
            _http = http;
        }

        public async Task<Result> LoadForUserAsync(int projectId, bool forceReload = false)
        {
            _state.BeginLoad();

            if (!forceReload && _state.Floss.Count > 0 && _state.LoadedForProjectId == projectId) return Result.Success();

            var response = await _http.GetAsync($"api/projects/admin/{projectId}/floss");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var floss = await response.Content.ReadFromJsonAsync<List<Admin_FlossModel>>();
            _state.SetFlossList(projectId, floss);
            return Result.Success();
        }

        public bool BeginEdit(int id) => _state.BeginEdit(id);
        public void CancelEdit() => _state.CancelEdit();
        public async Task<Result> SaveEditAsync()
        {
            var draft = _state.Draft;
            if (draft is null)
            {
                _state.SetError("Nothing to save");
                return Result.Fail();
            }

            var projectId = _state.LoadedForProjectId;
            var flossId = _state.SelectedFlossId;

            var response = await _http.PutAsJsonAsync($"api/projects/admin/{projectId}/floss/{flossId}", draft);
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyEdit(draft);
            return Result.Success();
        }

        public void BeginCreate() => _state.BeginCreate();
        public void CancelEdit() => _state.CancelEdit();
        public async Task<Result> SaveCreateAsync()
        {
            var draft = _state.Draft;
            if(draft is null)
            {
                _state.SetError("Nothing to submit.");
                return Result.Fail();
            }
            var projectId = _state.LoadedForProjectId;
            var response = await _http.PostAsJsonAsync($"api/projects/admin/{projectId}/floss", draft);
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyCreate(draft);
            return Result.Success();
        }

        public bool BeginDelete(int id) => _state.BeginDelete(id);
        public void CancelDelete() => _state.CancelDelete();
        public async Task<Result> DeleteAsync(int id)
        {
            var projectId = _state.LoadedForProjectId;
            var response = await _http.DeleteAsync($"api/project/admin/{projectId}/floss/{id}");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyDelete(id);
            return Result.Success();
        }

        private async Task SetErrorFromResponse(HttpResponseMessage response)
        {
            var message = await response.Content.ReadAsStringAsync();
            _state.SetError(message);
        }
    }
}
