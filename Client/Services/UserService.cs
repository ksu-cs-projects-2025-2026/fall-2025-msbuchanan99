using Client.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace Client.Services
{
    public class UserState
    {
        public event Action? Changed;
        public UserModel? CurrentUser { get; private set; }
        public List<UserFlossModel> UserFloss { get; private set }
        public bool IsLoggedIn => CurrentUser is not null;

        public void Set(UserModel? user)
        {
            CurrentUser = user;
            Changed?.Invoke();
        }
    }
    public sealed class UserService
    {
        private readonly HttpClient _http;
        private readonly UserState _userState;
        private readonly NavigationManager _nav;

        public UserService(HttpClient http, UserState state, NavigationManager nav)
        {
            _http = http;
            _userState = state;
            _nav = nav;
        }

        #region general use
        public async Task<string?> LoginAsync(UserModel? user)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", user);
            if (response.IsSuccessStatusCode)
            {
                var me = await _http.GetFromJsonAsync<UserModel>("api/auth/me");
                _userState.Set(me);
                _nav.NavigateTo("/", forceLoad: true);
                return null;
            }
            else return await response.Content.ReadAsStringAsync();
        }

        public async Task LogoutAsync()
        {
            var response = await _http.PostAsync("api/auth/logout", null);
            response.EnsureSuccessStatusCode();
            _userState.Set(null);
            _nav.NavigateTo("/", replace: true);
        }

        public async Task UserLoadedAsync()
        {
            if (_userState.CurrentUser is not null) return;
            var response = await _http.GetAsync("api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserModel>();
                _userState.Set(user);
            }
            else
            {
                _userState.Set(null);
            }
        }

        #endregion

        #region Admin



        #endregion

        #region User



        #endregion
    }
}
