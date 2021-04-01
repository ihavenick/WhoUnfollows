using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public UserState()
        {
            isLoggedIn = false;
        }

        private IInstaApi InstaApi;
        public InstaCurrentUser InstaUser { get; private set; }

        public string Avatar { get; private set; }

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
                InstaApi =  InstaApiBuilder.CreateBuilder()
                    .SetUser(UserSessionData.Empty)
                    .Build();
                    
                await InstaApi.LoadStateDataFromStreamAsync(fs);
                if (!InstaApi.IsUserAuthenticated) return false;
                var result2 = await InstaApi.GetCurrentUserAsync();
                        
                if (!result2.Succeeded)
                {
                    await InstaApi.LogoutAsync();
                    File.Delete(stateFile);
                       
                    return false;
                }
                
                
                    
                InstaUser = result2.Value;
                Avatar = result2.Value.ProfilePicture;
                var followers = await InstaApi.UserProcessor.GetCurrentUserFollowersAsync(PaginationParameters.Empty);
                var followings = await InstaApi.UserProcessor.GetUserFollowingAsync(InstaUser.UserName, PaginationParameters.Empty);

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

        public async Task<bool> TwoFactorLogin(string twoFactorCode)
        {
            var twoFactorLogin = await InstaApi.TwoFactorLoginAsync(twoFactorCode);
            
            if (!twoFactorLogin.Succeeded) return false;

            var test = await InstaApi.UserProcessor.GetCurrentUserAsync();
            InstaUser = test.Value;
            Avatar = test.Value.ProfilePicture;
            var followers = await InstaApi.UserProcessor.GetCurrentUserFollowersAsync(PaginationParameters.Empty);
            var followings = await InstaApi.UserProcessor.GetUserFollowingAsync(InstaUser.UserName, PaginationParameters.Empty);

            var nofollowers = followings.Value.Except(followers.Value).ToList();
            FollowersCount = followers.Value.Count;
            FollowingCount = followings.Value.Count;
            NotFollowers = nofollowers;
            NotFollowingCount = nofollowers.Count;
            isLoggedIn = true;
            StateChanged?.Invoke();
            
            
            
            await SaveSession();
            return true;

        }

        private async Task SaveSession()
        {
            var state = await InstaApi.GetStateDataAsStreamAsync();
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

                InstaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(user)
                    .SetRequestDelay(RequestDelay.FromSeconds(0,1))
                    .Build();
                        
                var loggedIn = await InstaApi.LoginAsync();

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
            
            
            
            var _InstaUser = await InstaApi.GetCurrentUserAsync();
            InstaUser = _InstaUser.Value;
            Avatar = _InstaUser.Value.ProfilePicture;
            var followers = await InstaApi.UserProcessor.GetCurrentUserFollowersAsync(PaginationParameters.Empty);
            var followings = await InstaApi.UserProcessor.GetUserFollowingAsync(username, PaginationParameters.Empty);

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
            InstaApi.LogoutAsync();
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