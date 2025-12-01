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
        public UserModel? Draft { get; private set; }
        public bool IsLoggedIn => User is not null;

        public string? LastError { get; private set; }
        public bool IsLoading { get; private set; }

        public void BeginLoad()
        {
            IsLoading = true;
            Changed?.Invoke();
        }
        public void Set(UserModel? user)
        {
            User = user;
            LastError = null;
            IsLoading = false;
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
            User = null;
            LastError = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        private static UserModel Clone(UserModel user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Password = user.Password,
            ConfirmPassword = user.ConfirmPassword,
            Role = user.Role
        };
        private static UserModel CloneWithoutPassword(UserModel user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Password = null,
            ConfirmPassword = null,
            Role = user.Role
        };

        public bool BeginUpdate()
        {
            if (User == null) return false;
            Draft = CloneWithoutPassword(User);

            Changed?.Invoke();
            return true;
        }

        public void CancelUpdate()
        {
            Draft = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void ApplyUpdate(UserModel user)
        {
            User = user;
            Draft = null;
            IsLoading = false;
            Changed?.Invoke();
        }

        public void BeginCreate()
        {
            Draft = new()
            {
                Id = 0,
                Username = null,
                Password = null,
                ConfirmPassword = null,
                Role = "Anonymous"
            };
            IsLoading = true;
            Changed?.Invoke();
        }

        public void ApplyCreate(UserModel user)
        {
            User = user;
            Draft = null;
            IsLoading = false;
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
        public async Task<Result> LoginAsync(UserModel? user)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", user);
            if (response.IsSuccessStatusCode)
            {
                var me = await _http.GetFromJsonAsync<UserModel>("api/auth/me");
                _userState.Set(me);
                _nav.NavigateTo("/", forceLoad: true);
                return Result.Success();
            }
            else
            {
                _userState.SetError(await response.Content.ReadAsStringAsync());
                return Result.Fail();
            }
        }

        public async Task LogoutAsync()
        {
            var response = await _http.PostAsync("api/auth/logout", null);
            response.EnsureSuccessStatusCode();
            _userState.Clear();
            _nav.NavigateTo("/", replace: true);
        }

        public async Task UserLoadedAsync()
        {
            if (_userState.User is not null) return;
            _userState.BeginLoad();
            var response = await _http.GetAsync("api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserModel>();

                _userState.Set(user);
            }
            else
            {
                _userState.Clear();
            }
        }

        public async Task<Result> DeleteAsync()
        {
            if (_userState.User is null) return Result.NotAuthorized();

            var deleteResponse = await _http.DeleteAsync($"api/users/delete/{_userState.User.Id}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                await LogoutAsync();
                return Result.Success();
            }
            else
            {
                _userState.SetError("Something went wrong. Can't delete User. \n" + await deleteResponse.Content.ReadAsStringAsync());
                return Result.Fail();
            }
        }

        #endregion

        #region Admin



        #endregion

        #region User

        public async Task<Result> DeleteAccountAsync()
        {
            var response = await _http.DeleteAsync($"api/users/{_userState.User!.Id}");
            if (response.IsSuccessStatusCode)
            {
                _userState.Clear();
                _nav.NavigateTo("/logout");
                return Result.Success();
            }
            _userState.SetError("Something went wrong with deleting the account.\n" + await response.Content.ReadAsStringAsync());
            return Result.Fail();
        }


        public bool BeginUpdate() => _userState.BeginUpdate();
        public void CancelUpdate() => _userState.CancelUpdate();
        public async Task<Result> UpdateUserAsync()
        {
            if (_userState.User is null) return Result.NotAuthorized();
            int userId = _userState.User.Id;

            var draft = _userState.Draft;
            if(draft is null)
            {
                _userState.SetError("Nothing to save.");
                return Result.Fail();
            }

            //Check username
            if (string.IsNullOrEmpty(draft.Username))
            {
                _userState.SetError("Username can't be empty");
                return Result.Fail();
            }

            //Ensure passwords match
            if (draft.Password != draft.ConfirmPassword)
            {
                _userState.SetError("Passwords must match.");
                return Result.Fail();
            }

            if (string.IsNullOrEmpty(draft.Password)) draft.Password = null;

            Dictionary<string, string?> dict = new();
            dict.Add("Username", draft.Username);
            dict.Add("Password", draft.Password);

            var response = await _http.PutAsJsonAsync($"api/users/{draft.Id}", dict);
            if (response.IsSuccessStatusCode)
            {
                var responseUser = await response.Content.ReadFromJsonAsync<UserModel>();
                if (responseUser != null)
                {
                    _userState.ApplyUpdate(responseUser);
                }
                return Result.Success();
            }
            else
            {
                _userState.SetError("Failed to Update.\n" + await response.Content.ReadAsStringAsync());
                return Result.Fail();
            }
        }

        public void BeginCreate() => _userState.BeginCreate();
        public async Task<Result> CreateUserAsync()
        {
            var draft = _userState.Draft;
            string? error = null;
            if (draft is null) error = "User is null.";
            if (string.IsNullOrWhiteSpace(draft.Username)) error = "Username cannot be empty.";
            if (string.IsNullOrEmpty(draft.Password)) error = "Password cannot be empty.";
            if (draft.Password != draft.ConfirmPassword) error = "Passwords must match.";

            if(error is not null)
            {
                _userState.SetError(error);
                return Result.Fail();
            }

            var response = await _http.PostAsJsonAsync("api/users/create", new
            {
                Username = draft.Username,
                HashPassword = draft.Password
            });

            if (response.IsSuccessStatusCode)
            {
                _userState.ApplyCreate(draft);
                //Navigate to login page for user to log in
                _nav.NavigateTo("/login");
                return Result.Success();
            }
            else
            {
                _userState.SetError(await response.Content.ReadAsStringAsync());
                return Result.Fail();
            }
        }

        #endregion
    }
}
