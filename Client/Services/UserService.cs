using Client.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace Client.Services
{
    public class UserState
    {
        public event Action? Changed;
        public UserModel? User { get; private set; }
        public bool IsLoggedIn => User is not null;

        public void Set(UserModel? user)
        {
            User = user;
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

        public UserModel? CurrentUser => _userState.User;
        public bool IsLoggedIn => _userState.IsLoggedIn;

        public event Action? UserChanged
        {
            add { _userState.Changed += value; }
            remove { _userState.Changed -= value; }
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
            if (_userState.User is not null) return;
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

        public async Task<string?> DeleteAsync()
        {
            if (CurrentUser is null) return "Can't Delete a Nulled User.";

            var deleteResponse = await _http.DeleteAsync($"api/users/delete/{CurrentUser.Id}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                await LogoutAsync();
                return null;
            }
            else
            {
                return "Something went wrong. Can't delete User. \n" + deleteResponse.Content;
            }
        }

        #endregion

        #region Admin



        #endregion

        #region User

        public async Task<string?> DeleteAccountAsync()
        {
            var response = await _http.DeleteAsync($"api/users/{CurrentUser!.Id}");
            if (response.IsSuccessStatusCode)
            {
                _userState.Set(null);
                _nav.NavigateTo("/logout");
                return null;
            }
            return "Something went wrong with deleting the account.\n" + response.Content;
        }

        public async Task<string?> UpdateUserAsync(UserModel? newUser)
        {
            if (CurrentUser is null) return "Can't update null User.";

            Dictionary<string, string> user = new();

            //Check username
            if(!(string.IsNullOrEmpty(newUser.Username) || newUser.Username == CurrentUser.Username)) 
                user.Add("Username", newUser.Username);

            //Ensure passwords match
            if(newUser.Password != newUser.ConfirmPassword) return "Passwords must match.";
            else if(!string.IsNullOrEmpty(newUser.Password)) user.Add("Password", newUser.Password);

            if (user.Count > 0)
            {
                var response = await _http.PutAsJsonAsync($"api/users/{CurrentUser!.Id}", user);
                if (response.IsSuccessStatusCode)
                {
                    var responseUser = await response.Content.ReadFromJsonAsync<UserModel>();
                    if (responseUser != null)
                    {
                        _userState.Set(responseUser);
                    }
                    return null;
                }
                else
                {
                    return "Failed to Update.\n" + await response.Content.ReadAsStringAsync();
                }
            }
            else return null;
        }

        public async Task<string?> CreateUserAsync(UserModel? user)
        {
            if (user is null) return "User is null.";

            if (string.IsNullOrWhiteSpace(user.Username)) return "Username cannot be empty.";
            if (string.IsNullOrEmpty(user.Password)) return "Password cannot be empty.";
            if (user.Password != user.ConfirmPassword) return "Passwords must match.";

            var response = await _http.PostAsJsonAsync("api/users/create", new
            {
                Username = user.Username,
                HashPassword = user.Password
            });

            if (response.IsSuccessStatusCode)
            {
                //Navigate to login page for user to log in
                _nav.NavigateTo("/login");
                return null;
            }
            else
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        #endregion
    }
}
