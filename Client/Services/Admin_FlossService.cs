using Client.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client.Services
{
    public class Admin_FlossState
    {
        // Properties
        private List<Admin_FlossModel> _floss = new();
        public IReadOnlyList<Admin_FlossModel> Floss => _floss;

        public int? SelectedFlossId { get; private set; }
        public Admin_FlossModel? Selected => SelectedFlossId is int id ? 
            _floss.FirstOrDefault(f => f.Id == id) : null;
        public Admin_FlossModel? Draft { get; private set; }
        
        //Utilities
        public bool IsLoading { get; private set; }
        public string? LastError { get; private set; }
        public event Action? Changed;

        public void BeginLoad()
        {
            IsLoading = true;
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
            SelectedFlossId = null;
            LastError = null;
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

        private void UpdateInsert(Admin_FlossModel floss)
        {
            var idx = _floss.FindIndex(f => f.Id == floss.Id);
            if (idx >= 0) _floss[idx] = floss;
            else _floss.Add(floss);
        }

        public void Remove(int id)
        {
            _floss.RemoveAll(p => p.Id == id);
            if (SelectedFlossId == id)
            {
                SelectedFlossId = null;
                Draft = null;
            }
            Changed?.Invoke();
        }

        public void SetFlossList(IEnumerable<Admin_FlossModel>? floss)
        {
            _floss = floss.ToList() ?? new();
            SelectedFlossId = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetSelectedId(int id)
        {
            SelectedFlossId = id;
            LastError = null;
            IsLoading = false;
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
            UpdateInsert(floss);
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void BeginCreate()
        {
            IsLoading = true;
            SelectedFlossId = null;
            Draft = new()
            {
                Id = 0,
                Name = null,
                Number = null,
                HexColor = null
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

        public void ApplyCreate(Admin_FlossModel created)
        {
            UpdateInsert(created);
            Draft = null;
            SelectedFlossId = created.Id;
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

    public class Admin_FlossService
    {
        private readonly Admin_FlossState _state;
        private readonly HttpClient _http;
        public Admin_FlossService(Admin_FlossState state, HttpClient http)
        {
            _state = state;
            _http = http;
        }


        public async Task<Result> LoadFlossList(bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                return Result.Success();
            }

            _state.BeginLoad();
            _state.Clear();

            var response = await _http.GetAsync("api/floss/admin");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var flosses = await response.Content.ReadFromJsonAsync<IEnumerable<Admin_FlossModel>>();
            _state.SetFlossList(flosses);

            return Result.Success();
        }

        public async Task<Result> LoadSingleFloss(int id, bool forceRefresh = false)
        {
            if (!forceRefresh && _state.SelectedFlossId is not null) return Result.Success();
            
            _state.BeginLoad();
            _state.SetSelectedId(id);

            return Result.Success();
        }

        public bool BeginEdit(int id) => _state.BeginEdit(id);
        public void CancelEdit() => _state.CancelEdit();
        public async Task<Result> SaveEditAsync()
        {
            var draft = _state.Draft;
            if(draft is null)
            {
                _state.SetError("Nothing to save.");
                return Result.Fail();
            }

            var response = await _http.PutAsJsonAsync($"api/floss/admin/{draft.Id}", draft);
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyEdit(draft);
            return Result.Success();
        }

        public void BeginCreate() => _state.BeginCreate();
        public void CancelCreate() => _state.CancelCreate();
        public async Task<Result> SaveCreateAsync()
        {
            var draft = _state.Draft;
            if(draft is null)
            {
                _state.SetError("Nothing to submit.");
                return Result.Fail();
            }

            var response = await _http.PostAsJsonAsync("api/floss/admin", draft);
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
            var response = await _http.DeleteAsync($"api/floss/admin/{id}");
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
