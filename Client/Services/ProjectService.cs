using Client.Models;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
namespace Client.Services
{
    public class ProjectState
    {
        //All projects for current user
        private List<ProjectModel> _projects = new();
        public IReadOnlyList<ProjectModel> Projects => _projects;
        public int? LoadedForUserWithId { get; private set; }


        //Currently selected projects and attributes
        public int? SelectedProjectId { get; private set; }
        public ProjectModel? Selected => SelectedProjectId is int id ? 
            _projects.FirstOrDefault(p => p.Id == id) : null;
        public ProjectModel? Draft { get; private set;  }

        public List<FlossInProjectModel> _floss = new();
        public IReadOnlyList<FlossInProjectModel> Floss => _floss;
        public string? PdfDataUrl { get; private set; }

        //Utilities
        public bool IsLoading { get; private set; }
        public string? LastError { get; private set; }

        public event Action? Changed;

        /// <summary>
        /// Marks the state as loading
        /// </summary>
        public void BeginLoad()
        {
            IsLoading = true;
            Changed?.Invoke();
        }

        /// <summary>
        /// Sets the list of projects that belongs to userId
        /// </summary>
        /// <param name="userId">the user that owns the projects</param>
        /// <param name="projects">List of projects</param>
        public void SetProjects(int userId, IEnumerable<ProjectModel> projects)
        {
            _projects = projects?.ToList() ?? new();
            LoadedForUserWithId = userId;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetSelectedProjectId(int projectId)
        {
            SelectedProjectId = projectId;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetFloss(IEnumerable<FlossInProjectModel> floss)
        {
            _floss = floss?.ToList() ?? new();
            IsLoading = false;
            Changed?.Invoke();
        }

        public void SetPDF(string? dataUrl)
        {
            PdfDataUrl = dataUrl;
            Changed?.Invoke();
        }

        #region cache utilities

        /// <summary>
        /// Sets the most recent error
        /// </summary>
        /// <param name="error"></param>
        public void SetError(string error)
        {
            IsLoading = false;
            LastError = error;
            Changed?.Invoke();
        }

        /// <summary>
        /// Clears all properties
        /// </summary>
        public void Clear()
        {
            _projects.Clear();
            LoadedForUserWithId = null;
            SelectedProjectId = null;
            Draft = null;
            _floss.Clear();
            PdfDataUrl = null;
            IsLoading = false;
            LastError = null;
            Changed?.Invoke();
        }

        /// <summary>
        /// Update or insert the given project into _projects
        /// </summary>
        /// <param name="project">The project being updated or inserted</param>
        public void UpdateInsert(ProjectModel project)
        {
            var idx = _projects.FindIndex(p => p.Id == project.Id);
            if(idx >= 0) _projects[idx] = project;
            else _projects.Add(project);
        }

        public void Remove(int id)
        {
            _projects.RemoveAll(p => p.Id == id);
            if(SelectedProjectId == id)
            {
                SelectedProjectId = null;
                Draft = null;
            }
            Changed?.Invoke();
        }

        private static ProjectModel Clone(ProjectModel p) => new()
        {
            Id = p.Id,
            UserId = p.UserId,
            Name = p.Name,
            IsCompleted = p.IsCompleted,
            CompletionDate = p.CompletionDate,
            KeyPage = p.KeyPage,
            Aida = p.Aida
        };

        #endregion
        #region Single Project Setters

        /// <summary>
        /// Looks through current list of projects to find a project with the given id.
        /// Sets SelectedProject to given id and returns true if a project is found.
        /// </summary>
        /// <param name="id">Id of a project to search for</param>
        /// <returns>If the project was found</returns>
        public bool Select(int id)
        {
            if(_projects.Any(p => p.Id == id))
            {
                SelectedProjectId = id;
                Draft = null;
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Start the edit by cloning the current selected project to Draft
        /// </summary>
        /// <param name="id">Id of the project to edit</param>
        /// <returns>If a project of the id exists</returns>
        public bool BeginUpdate(int id)
        {
            var current = _projects.FirstOrDefault(p => p.Id == id);
            if (current == null) return false;

            SelectedProjectId = id;
            Draft = Clone(current);

            IsLoading = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Cancels the edit by discarding the draft.
        /// </summary>
        public void CancelUpdate()
        {
            Draft = null;
            IsLoading = false;
            Changed?.Invoke();
        }


        public void ApplyEdit(ProjectModel updated)
        {
            UpdateInsert(updated);
            Draft = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        
        public void BeginCreate(int userId)
        {
            SelectedProjectId = null;
            Draft = new()
            {
                Id = 0,
                UserId = userId,
                Name = null,
                IsCompleted = false,
                CompletionDate = null,
                KeyPage = null,
                Aida = null
            };
            IsLoading = true;
            Changed?.Invoke();
        }


        public void ApplyCreate(ProjectModel created)
        {
            UpdateInsert(created);
            Draft = null;
            SelectedProjectId = created.Id;
            IsLoading = false;
            Changed?.Invoke();
        }

        #endregion
    }


    public class ProjectService
    {
        private readonly ProjectState _state;
        private readonly UserState _userState;
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private IJSObjectReference? _module;
        public ProjectService(ProjectState state, UserState userState, HttpClient http, IJSRuntime js)
        {
            _state = state;
            _userState = userState;
            _http = http;
            _js = js;
        }

        #region User
        
        public async Task<Result> LoadForCurrentUserAsync(bool forceRefresh = false)
        {
            if (_userState.User is null) 
            {
                _state.Clear();
                _state.SetError("Not signed in");
                return Result.NotAuthorized();
            }
            var userId = _userState.User.Id;

            //Check for force refresh or for if refresh is needed
            if(!forceRefresh && _state.LoadedForUserWithId == userId)
            {
                return Result.Success();
            }

            //Need to reload...
            _state.BeginLoad();

            var response = await _http.GetAsync($"api/users/{userId}/projects");
            if (response.IsSuccessStatusCode)
            {
                var projects = await response.Content.ReadFromJsonAsync<List<ProjectModel>>() 
                    ?? new List<ProjectModel>();
                _state.SetProjects(userId, projects);
                return Result.Success();
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _state.SetError(msg);
                return Result.Fail();
            }
        }

        public async Task<Result> LoadCurrentProject(int id, bool forceRefresh = false)
        {
            if (_userState.User is null) return Result.NotAuthorized();

            //If refresh is not forced and SelectedProjectId is id
            //Check if the cached list of projects has a project with the given id
            //If it does, return success. If not, get the project with the given id
            if(!forceRefresh && _state.SelectedProjectId == id)
            {
                var cached = _state.Projects.FirstOrDefault(p => p.Id == id);
                if (cached != null) return Result.Success();
            }

            var response = await _http.GetAsync($"api/projects/{id}/details");
            if (response.IsSuccessStatusCode)
            {
                var item = await response.Content.ReadFromJsonAsync<ProjectModel>();
                if (item is null)
                {
                    _state.SetError("Project not found");
                    return Result.Fail();
                }
                _state.SetSelectedProjectId(id);
                _state.UpdateInsert(item);

                var pdfResult = await LoadPDF(id);
                if (!pdfResult.Ok)
                {
                    _state.Clear();
                    _state.SetError($"Error occurred in loading PDF: {pdfResult.Error}");
                    return Result.Fail();
                }
                else
                {
                    _state.SetPDF(pdfResult.Value);
                }

                return Result.Success();
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _state.SetError(msg);
                return Result.Fail();
            }
        }

        private async Task<Result<string?>> LoadPDF(int projectId)
        {
            var response = await _http.GetAsync($"api/projects/{projectId}/Pattern");
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var base64 = Convert.ToBase64String(bytes);
                var dataUrl = $"data:application/pdf;base64,{base64}";
                return Result<string?>.Success(dataUrl);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return Result<string?>.Fail(error);
            }
        }

        public async Task<Result> LoadCurrentProjectFloss(bool forceRefresh = false)
        {
            if (_userState.User is null) return Result.NotAuthorized();
            if (_state.Selected is null)
            {
                _state.SetError("Selected project is null. Call LoadCurrentProject.");
                return Result.Fail();
            }

            if(!forceRefresh)
            {
                var cachedFlossMatchesProject = _state.Floss.All(f => f.ProjectId == _state.SelectedProjectId);
                if(cachedFlossMatchesProject) return Result.Success();
            }

            var response = await _http.GetAsync($"api/projects/{_state.SelectedProjectId}/floss");
            if (response.IsSuccessStatusCode)
            {
                var flosses = await response.Content.ReadFromJsonAsync<IEnumerable<FlossInProjectModel>>();
                if(flosses is null)
                {
                    _state.SetError("Returned IEnumerable is null");
                    return Result.Fail();
                }
                _state.SetFloss(flosses);
                return Result.Success();
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _state.SetError(msg);
                return Result.Fail();
            }
        }

        public async Task<Result> DeleteAsync(int id)
        {
            if (_userState.User is null) return Result.NotAuthorized();

            var response = await _http.DeleteAsync($"api/projects/{id}");
            if (response.IsSuccessStatusCode)
            {
                _state.Remove(id);
                return Result.Success();
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _state.SetError(msg);
                return Result.Fail();
            }
        }

        public bool BeginUpdate(int id) => _state.BeginUpdate(id);
        public void CancelUpdate() => _state.CancelUpdate();
        public async Task<Result> SaveUpdateAsync()
        {
            if (_userState.User is null) return Result.NotAuthorized();
            int userId = _userState.User.Id;

            var draft = _state.Draft;
            if (draft is null)
            {
                _state.SetError("Nothing to save.");
                return Result.Fail();
            }

            var response = await _http.PutAsJsonAsync($"api/projects/{draft.Id}/edit", draft);
            if (response.IsSuccessStatusCode)
            {
                _state.ApplyEdit(draft);
                return Result.Success();
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _state.SetError(msg);
                return Result.Fail();
            }
        }


        public void BeginCreate(int userId) => _state.BeginCreate(userId);
        public async Task<Result> CreateAsync()
        {
            if(_userState.User is null) return Result.NotAuthorized();
            var userId = _userState.User.Id;

            var draft = _state.Draft;
            if (draft is null)
            {
                _state.SetError("No draft to submit.");
                return Result.Fail();
            }

            draft.UserId = userId;

            var response = await _http.PostAsJsonAsync($"api/projects/create", draft);
            if (response.IsSuccessStatusCode)
            {
                var project = await response.Content.ReadFromJsonAsync<ProjectModel>();
                if (project is null)
                {
                    _state.SetError("A problem happened in reading the JSON.");
                    return Result.Fail();
                }
                _state.ApplyCreate(project);
                return Result.Success();
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _state.SetError(msg);
                return Result.Fail();
            }
        }

        public async Task<Result> DownloadFile(int id)
        {
            var project = _state.Projects.FirstOrDefault(p => p.Id == id);
            if (project is null)
            {
                _state.SetError("Project with that id isn't loaded.");
                return Result.Fail();
            }

            var response = await _http.GetAsync($"api/projects/{id}/download");
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                _state.SetError(msg);
                return Result.Fail();
            }

            var fileName = TryGetFileName(response.Content.Headers)
                       ?? $"{project.Name.Replace(" ", "")}.pdf";

            var contentType = response.Content.Headers.ContentType?.ToString()
                              ?? "application/pdf";

            var bytes = await response.Content.ReadAsByteArrayAsync();

            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/downloads.js");
            await _module.InvokeVoidAsync("saveFile", bytes, fileName, contentType);
            return Result.Success();
        }

        private static string? TryGetFileName(HttpContentHeaders headers)
        {
            var cd = headers.ContentDisposition;
            if (cd?.FileNameStar is not null) return cd.FileNameStar.Trim('"');
            if (cd?.FileName is not null) return cd.FileName.Trim('"');
            return null;
        }
        #endregion
    }
}
