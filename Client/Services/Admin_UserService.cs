using Client.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client.Services
{
    public class Admin_UserState
    {
        private List<Admin_UserModel> _users = new();
        public IReadOnlyList<Admin_UserModel> Users => _users;
        public bool ListLoading { get; private set; }

        public int? SelectedUserId { get; private set; }
        public Admin_UserModel? Selected => SelectedUserId is int id ?
            _users.FirstOrDefault(u => u.Id == id) : null;

        public Admin_UserModel? Draft { get; private set; }

        public bool IndividualLoading { get; private set; }

        public string? LastError { get; private set; }
        public event Action? Changed;

        public void BeginLoad()
        {
            ListLoading = true;
            Changed?.Invoke();
        }

        public void SetUserList(IEnumerable<Admin_UserModel> users)
        {
            _users = users?.ToList() ?? new();
            SelectedUserId = null;
            LastError = null;
            ListLoading = false;
            Draft = null;
            Changed?.Invoke();
        }

        public void BeginIndividualLoad()
        {
            IndividualLoading = true;
            Changed?.Invoke();
        }

        public void SetUser(int id)
        {
            SelectedUserId = id;
            LastError = null;
            IndividualLoading = false;
            Changed?.Invoke();
        }

        public void SetError(string error)
        {
            LastError = error;
            ListLoading = false;
            IndividualLoading = false;
            Changed?.Invoke();
        }
        public void Clear()
        {
            _users.Clear();
            SelectedUserId = null;
            LastError = null;
            ListLoading = false;
            IndividualLoading = false;
            Draft = null;
            Changed?.Invoke();
        }

        public Admin_UserModel CloneUser(Admin_UserModel user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CreatedOn = user.CreatedOn,
            LastModified = user.LastModified
        };

        public void Upsert(Admin_UserModel user)
        {
            var idx = _users.FindIndex(u => u.Id == user.Id);
            if (idx >= 0) _users[idx] = user;
            else _users.Add(user);
        }

        public void Remove(int id)
        {
            _users.RemoveAll(u => u.Id == id);
            if(SelectedUserId == id)
            {
                SelectedUserId = null;
                Draft = null;
            }
            Changed?.Invoke();
        }

        //Editing
        public bool BeginEdit(int id)
        {
            IndividualLoading = true;

            var current = _users.First(u => u.Id == id);
            if (current is null) return false;

            Draft = CloneUser(current);
            LastError = null;

            Changed?.Invoke();
            return true;
        }
        public void CancelEdit()
        {
            Draft = null;
            LastError = null;
            IndividualLoading = false;
            Changed?.Invoke();
        }
        public void ApplyEdit(Admin_UserModel user)
        {
            Upsert(user);
            Draft = null;
            LastError = null;
            IndividualLoading = false;
            Changed?.Invoke();
        }

        //Create
        public void BeginCreate()
        {
            IndividualLoading = true;
            SelectedUserId = null;
            LastError = null;
            Draft = new()
            {
                Id = 0,
                Username = null,
                Role = null
            };
            Changed?.Invoke();
        }
        public void CancelCreate()
        {
            Draft = null;
            LastError = null;
            IndividualLoading = false;
            Changed?.Invoke();
        }
        public void ApplyCreate(Admin_UserModel user)
        {
            Upsert(user);
            Draft = null;
            LastError = null;
            IndividualLoading = false;
            Changed?.Invoke();
        }
        
        //Delete
        public bool BeginDelete(int id)
        {
            IndividualLoading = true;

            var current = _users.First(u => u.Id == id);
            if (current is null) return false;

            SelectedUserId = id;
            LastError = null;

            Changed?.Invoke();
            return true;
        }
        public void CancelDelete()
        {
            SelectedUserId = null;
            LastError = null;
            IndividualLoading = false;
            Changed?.Invoke();
        }
        public void ApplyDelete(int id)
        {
            Remove(id);

            SelectedUserId = null;
            LastError = null;

            Changed?.Invoke();
        }
    }
    public class Admin_UserService
    {
        private readonly Admin_UserState _state;
        private readonly HttpClient _http;
        public Admin_UserService(Admin_UserState state, HttpClient http)
        {
            _state = state;
            _http = http;
        }

        public async Task<Result> LoadUserList(bool forceRefresh = false)
        {
            if (!forceRefresh && _state.Users.Count >= 1)
            {
                return Result.Success();
            }

            _state.BeginLoad();

            var response = await _http.GetAsync("api/users/admin");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var users = await response.Content.ReadFromJsonAsync<IEnumerable<Admin_UserModel>>();
            _state.SetUserList(users);

            return Result.Success();
        }

        public async Task<Result> LoadSingleUser(int id, bool forceRefresh = false)
        {
            if(!forceRefresh && _state.SelectedUserId is not null) return Result.Success();

            _state.BeginIndividualLoad();
            _state.SetUser(id);

            return Result.Success();
        }

        public bool BeginEdit(int id) => _state.BeginEdit(id);
        public void CancelEdit() => _state.CancelEdit();
        public async Task<Result> SaveEditAsync()
        {
            var draft = _state.Draft;
            if (draft is null)
            {
                _state.SetError("Nothing to save.");
                return Result.Fail();
            }

            var response = await _http.PutAsJsonAsync($"api/users/admin/{draft.Id}", draft);
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

            var response = await _http.PostAsJsonAsync("api/users/admin", draft);
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
            var response = await _http.DeleteAsync($"api/users/admin/{id}");
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
