using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;

namespace WhoUnfollows
{
    public class UserState
    {
        private readonly HttpClient http;
        public UserState(HttpClient http)
        {
            isLoggedIn = false;
        }

        public IInstaApi _ınstaApi { get; private set; }
        public InstaCurrentUser InstaUser { get; private set; }

        public string Avatar { get; set; }

        private static readonly string dosyayolu = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        private readonly string stateFile = Path.Combine(dosyayolu, "state.bin");
        
        public int FollowersCount { get; private set; }

        public List<InstaUserShort> NotFollowers { get; private set; }

        public int FollowingCount { get; private set; }

        public int NotFollowingCount { get; private set; }

        public bool isLoggedIn { get; private set; }
        
        public bool twoFactor { get; private set; }

        public async Task<bool> tryToLoginAsync()
        {
            if (File.Exists(stateFile))
            {
                await using var fs = File.OpenRead(stateFile);
                _ınstaApi =  InstaApiBuilder.CreateBuilder()
                    .SetUser(UserSessionData.Empty)
                    .Build();
                    
                await _ınstaApi.LoadStateDataFromStreamAsync(fs);
                if (!_ınstaApi.IsUserAuthenticated) return false;
                
                var result2 = await _ınstaApi.UserProcessor.GetCurrentUserAsync();

                if (!result2.Succeeded)
                {
                    await _ınstaApi.LogoutAsync();
                    File.Delete(stateFile);
                       
                    return false;
                }
                
                InstaUser = result2.Value;
                Avatar = result2.Value.ProfilePicture;
                var followers = await _ınstaApi.UserProcessor.GetCurrentUserFollowersAsync(PaginationParameters.Empty);
                var followings = await _ınstaApi.UserProcessor.GetUserFollowingAsync(InstaUser.UserName, PaginationParameters.Empty);

                var nofollowers = followings.Value.Except(followers.Value).ToList();
                FollowersCount = followers.Value.Count;
                FollowingCount = followings.Value.Count;
                NotFollowers = nofollowers;
                NotFollowingCount = nofollowers.Count;
                isLoggedIn = true;
                StateChanged?.Invoke();
                return true;

            }

            return false;
        }

      
        public async Task<string> TwoFactorLogin(string username,string password,string twoFactorCode)
        {
            if (username.Length<=0 && password.Length<=0 && twoFactorCode.Length<=0)
            {
                Console.WriteLine("BBoş geldi meeku");
            }
            
            var user = new UserSessionData
            {
                UserName = username,
                Password = password
            };

            _ınstaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(user)
                .UseHttpClient(http)
                .SetRequestDelay(RequestDelay.FromSeconds(0,1))
                .Build();
            
            await _ınstaApi.LoginAsync();
            
            var twoFactorLogin = await _ınstaApi.TwoFactorLoginAsync(twoFactorCode);
          
            
            if (!twoFactorLogin.Succeeded)
                return twoFactorLogin.Info.Message;
            
            await SaveSession();
            
            var _InstaUser = await _ınstaApi.UserProcessor.GetCurrentUserAsync();
            if (!_InstaUser.Succeeded)
            {
                return _InstaUser.Info.Message;
            }
            
            
            InstaUser = _InstaUser.Value;
            Avatar = _InstaUser.Value.ProfilePicture;
            
            var followers = await _ınstaApi.UserProcessor.GetCurrentUserFollowersAsync(PaginationParameters.Empty);
            var followings = await _ınstaApi.UserProcessor.GetUserFollowingAsync(username, PaginationParameters.Empty);

            var nofollowers = followings.Value.Except(followers.Value).ToList();
            FollowersCount = followers.Value.Count;
            FollowingCount = followings.Value.Count;
            NotFollowers = nofollowers;
            NotFollowingCount = nofollowers.Count;

            isLoggedIn = true;
            
            StateChanged?.Invoke();

            return "true";
        }

        private async Task SaveSession()
        {
            var state = await _ınstaApi.GetStateDataAsStreamAsync();
            await using var fileStream = File.Create(stateFile);
            state.Seek(0, SeekOrigin.Begin);
            await state.CopyToAsync(fileStream);
        }


        public async Task<string> Login(string username, string password)
        {
            
           
                var user = new UserSessionData
                {
                    UserName = username,
                    Password = password
                };

                _ınstaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(user)
                    .UseHttpClient(http)
                    .SetRequestDelay(RequestDelay.FromSeconds(0,1))
                    .Build();
                        
                var loggedIn = await _ınstaApi.LoginAsync();

                if (!loggedIn.Succeeded)
                {
                    if (loggedIn.Value == InstaLoginResult.TwoFactorRequired)
                    {
                        twoFactor = true;
                        //StateChanged?.Invoke();
                        return "twofactor";
                    }

                    return loggedIn.Info.Message;
                }
            
            
            
            var _InstaUser = await _ınstaApi.GetCurrentUserAsync();
            InstaUser = _InstaUser.Value;
            Avatar = _InstaUser.Value.ProfilePicture;
            var followers = await _ınstaApi.UserProcessor.GetCurrentUserFollowersAsync(PaginationParameters.Empty);
            var followings = await _ınstaApi.UserProcessor.GetUserFollowingAsync(username, PaginationParameters.Empty);

            var nofollowers = followings.Value.Except(followers.Value).ToList();
            FollowersCount = followers.Value.Count;
            FollowingCount = followings.Value.Count;
            NotFollowers = nofollowers;
            NotFollowingCount = nofollowers.Count;

            await SaveSession();

            isLoggedIn = true;
            StateChanged?.Invoke();
            
            return "true";
        }
        

        public void Logout()
        {
            isLoggedIn = false;
            _ınstaApi.LogoutAsync();
            File.Delete(stateFile);
            InstaUser = null;
            FollowersCount = 0;
            FollowingCount = 0;
            NotFollowingCount = 0;
            Avatar = string.Empty;

            StateChanged?.Invoke();
        }


        public event Action StateChanged;
    }
}