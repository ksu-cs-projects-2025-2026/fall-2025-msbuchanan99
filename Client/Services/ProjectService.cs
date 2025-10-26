using Client.Models;
namespace Client.Services
{
    public sealed class ProjectState
    {
        public event Action? Changed;
        public List<ProjectModel> Projects { get; private set; }
        public List<ProjectFlossModel> ProjectFloss {  get; private set; } 
        public void Set(List<ProjectModel> projects)
        {
            Projects = projects;
        }
    }
    public class ProjectService
    {
        private readonly ProjectState _state;
        private readonly UserState _user;
        private readonly HttpClient _http;
        public ProjectService(ProjectState state, UserState user, HttpClient http)
        {
            _state = state;
            _user = user;
            _http = http;
        }

        #region Admin



        #endregion

        #region User



        #endregion
    }
}
