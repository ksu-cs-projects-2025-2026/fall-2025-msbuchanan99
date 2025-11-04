using Client.Models;
using System.Collections.Generic;
using System.Net.Http.Json;
namespace Client.Services
{
    public sealed class ProjectState
    {
        // userId -> (projectId -> ProjectModel)
        private readonly Dictionary<int, Dictionary<int, ProjectModel>> _byUser = new();
        public event Action? Changed;

        public IReadOnlyList<ProjectModel> GetForUser(int userId)
        => _byUser.TryGetValue(userId, out var map)
           ? map.Values.OrderBy(p => p.Name).ToList()
           : Array.Empty<ProjectModel>();
        public void SetForUser(int userId, IEnumerable<ProjectModel> projects)
        {
            _byUser[userId] = projects.ToDictionary(p => p.Id, p => p);
            Changed?.Invoke();
        }

        // Add or update one project in the correct user's bucket
        public void UpsertForUser(ProjectModel project)
        {
            if (!_byUser.TryGetValue(project.UserId, out var map))
            {
                map = new Dictionary<int, ProjectModel>();
                _byUser[project.UserId] = map;
            }
            map[project.Id] = project;
            Changed?.Invoke();
        }

        // Remove a single project from a user's bucket
        public void RemoveForUser(int userId, int projectId)
        {
            if (_byUser.TryGetValue(userId, out var map) && map.Remove(projectId))
            {
                if (map.Count == 0) _byUser.Remove(userId); // optional cleanup
                Changed?.Invoke();
            }
        }

        public void ClearUser(int userId)
        {
            if (_byUser.Remove(userId))
                Changed?.Invoke();
        }

        public void ClearAll()
        {
            if (_byUser.Count == 0) return;
            _byUser.Clear();
            Changed?.Invoke();
        }
    }
    public class ProjectService
    {
        private readonly ProjectState _state;
        private readonly HttpClient _http;
        public ProjectService(ProjectState state, HttpClient http)
        {
            _state = state;
            _http = http;
        }

        #region Admin



        #endregion

        #region User
        public async Task<Result<IReadOnlyList<ProjectModel>>> LoadForUserAsync(int userId, bool forceFresh = false)
        {
            if (!forceFresh)
            {
                var cached = _state.GetForUser(userId);
                if (cached.Count > 0) return Result<IReadOnlyList<ProjectModel>>.Success(cached);
            }

            //get fresh list of projects
            var response = await _http.GetAsync($"api/users/{userId}/projects");
            if (response.IsSuccessStatusCode)
            {
                var projects = await response.Content.ReadFromJsonAsync<List<ProjectModel>>() 
                    ?? new List<ProjectModel>();
                _state.SetForUser(userId, projects);
                return Result<IReadOnlyList<ProjectModel>>.Success(projects);
            }
            else
            {
                return Result<IReadOnlyList<ProjectModel>>.Fail(await response.Content.ReadAsStringAsync())
            }
        }

        #endregion
    }
}
