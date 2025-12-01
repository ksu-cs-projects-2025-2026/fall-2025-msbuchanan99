using Client.Models;
using System.Net.Http.Json;

namespace Client.Services
{
    public class Admin_ProjectState
    {
        private List<Admin_ProjectModel> _projects = new();
        public IReadOnlyList<Admin_ProjectModel> Projects => _projects;
        public int CurrentUserId { get; private set; }

        public int? SelectedProjectId { get; private set; }
        public Admin_ProjectModel? Selected => SelectedProjectId is int id ? _projects.FirstOrDefault(p => p.Id == id) : null;
        public Admin_ProjectModel? Draft { get; private set; }
        public bool IsLoading { get; private set; }
        public string? LastError { get; private set; }
        public event Action? Changed;

        public void BeginLoad()
        {
            IsLoading = true;
            Changed?.Invoke();
        }

        public void SetProjectList(int userId, IEnumerable<Admin_ProjectModel> projects)
        {
            _projects = projects?.ToList() ?? new();
            CurrentUserId = userId;
            SelectedProjectId = null;
            LastError = null;
            Draft = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetProject(int id)
        {
            SelectedProjectId = id;
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
            _projects.Clear();
            SelectedProjectId = null;
            LastError = null;
            Draft = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public Admin_ProjectModel Clone(Admin_ProjectModel project) => new()
        {
            Id = project.Id,
            Name = project.Name,
            UserId = project.UserId,
            FileName = project.FileName,
            IsCompleted = project.IsCompleted,
            CompletionDate = project.CompletionDate,
            KeyPage = project.KeyPage,
            Aida = project.Aida,
            CreatedOn = project.CreatedOn,
            LastModified = project.LastModified
        };

        public void Upsert(Admin_ProjectModel project)
        {
            var idx = _projects.FindIndex(p => p.Id == project.Id);
            if(idx >= 0) _projects[idx] = project;
            else _projects.Add(project);
        }

        public void Remove(int id)
        {
            _projects.RemoveAll(u => u.Id == id);
            if (SelectedProjectId == id)
            {
                SelectedProjectId = null;
                Draft = null;
            }
            Changed?.Invoke();
        }

        //Creating
        public void BeginCreate(int userId)
        {
            IsLoading = true;
            SelectedProjectId = null;
            LastError = null;
            Draft = new()
            {
                Id = 0,
                Name = null,
                UserId = userId,
                FileName = null,
                IsCompleted = false,
                CompletionDate = null,
                KeyPage = null,
                Aida = 0,
                CreatedOn = null,
                LastModified = null
            };
            Changed?.Invoke();
        }
        public void CancelCreate()
        {
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
        public void ApplyCreate(Admin_ProjectModel project)
        {
            Upsert(project);
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        //Editing
        public bool BeginEdit(int id)
        {
            IsLoading = true;

            var current = _projects.First(p => p.Id == id);
            if (current is null) return false;

            Draft = Clone(current);
            SelectedProjectId = id;
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
        public void ApplyEdit(Admin_ProjectModel project)
        {
            Upsert(project);
            Draft = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        //Deleting
        public bool BeginDelete(int id)
        {
            IsLoading = true;
            var current = _projects.First(p => p.Id == id);
            if (current is null) return false;
            SelectedProjectId = id;
            LastError = null;

            Changed?.Invoke();
            return true;
        }
        public void CancelDelete()
        {
            SelectedProjectId = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
        public void ApplyDelete(int id)
        {
            Remove(id);
            SelectedProjectId = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }
    }
    public class Admin_ProjectService
    {
        private readonly Admin_ProjectState _state;
        private readonly HttpClient _http;
        public Admin_ProjectService(Admin_ProjectState state, HttpClient http)
        {
            _state = state;
            _http = http;
        }

        public async Task<Result> LoadProjectList(int userId, bool forceRefresh = false)
        {
            if(!forceRefresh && _state.CurrentUserId == userId)
            {
                return Result.Success();
            }

            _state.BeginLoad();

            var response = await _http.GetAsync($"api/users/admin/{userId}/projects");
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            var projects = await response.Content.ReadFromJsonAsync<IEnumerable<Admin_ProjectModel>>();
            _state.SetProjectList(userId, projects);

            return Result.Success();
        }

        public async Task<Result> SetProject(int id, bool forceRefresh = false)
        {
            if (!forceRefresh && _state.SelectedProjectId == id) return Result.Success();

            _state.SetProject(id);
            return Result.Success();
        }

        public void BeginCreate(int userId) => _state.BeginCreate(userId);
        public void CancelCreate() => _state.CancelCreate();
        public async Task<Result> ApplyCreateAsync()
        {
            var draft = _state.Draft;
            if (draft is null)
            {
                _state.SetError("Nothing to save");
                return Result.Fail();
            }

            var result = await _http.PutAsJsonAsync($"api/projects/admin", draft);
            if (!result.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(result);
                return Result.Fail();
            }

            _state.ApplyCreate(draft);
            return Result.Success();
        }

        public bool BeginEdit(int id) => _state.BeginEdit(id);
        public void CancelEdit() => _state.CancelEdit();
        public async Task<Result> ApplyEditAsync()
        {
            var draft = _state.Draft;
            if(draft is null)
            {
                _state.SetError("Nothing to submit.");
                return Result.Fail();
            }

            var response = await _http.PostAsJsonAsync($"api/projects/admin/{draft.Id}", draft);
            if (!response.IsSuccessStatusCode)
            {
                await SetErrorFromResponse(response);
                return Result.Fail();
            }

            _state.ApplyEdit(draft);
            return Result.Success();
        }

        public bool BeginDelete(int id) => _state.BeginDelete(id);
        public void CancelDelete() => _state.CancelDelete();
        public async Task<Result> ApplyDelete(int id)
        {
            var response = await _http.DeleteAsync($"api/projects/admin/{id}");
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
