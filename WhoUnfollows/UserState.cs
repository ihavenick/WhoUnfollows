using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;
using Microsoft.AspNetCore.Components;

namespace WhoUnfollows
{
    public class UserState
    {
        public IInstaApi InstaApi { get; private set; }
        public InstaCurrentUser InstaUser { get; private set; }
        
        public string Avatar { get; private set; }
        
        public int FollowersCount { get; private set; }
        
        public List<InstaUserShort> NotFollowers { get; private set; }
        
        public int FollowingCount { get; private set; }
        
        public int NotFollowingCount { get; private set; }
        
        public bool isLoggedIn { get; private set; }

        public UserState()
        {
            isLoggedIn = false;
        }

        public async Task<bool> Login(string username, string password)
        {
            var user = new UserSessionData()
            {
                UserName = username,
                Password = password
            };
            
            InstaApi = InstaApiBuilder.CreateBuilder()
                
                    .SetUser(user)
                    .Build();
            
            var loggedIn = await InstaApi.LoginAsync();

            if (!loggedIn.Succeeded) return false;
            
            isLoggedIn = true;
            StateChanged?.Invoke();
            var _InstaUser = await InstaApi.GetCurrentUserAsync();
            InstaUser = _InstaUser.Value;
            Avatar = _InstaUser.Value.ProfilePicture;
            var followers = await InstaApi.GetCurrentUserFollowersAsync(PaginationParameters.Empty);
            var followings = await InstaApi.GetUserFollowingAsync(username, PaginationParameters.Empty);
            
            var nofollowers = followings.Value.Except(followers.Value).ToList();
            FollowersCount = followers.Value.Count;
            FollowingCount = followings.Value.Count;
            NotFollowers = nofollowers;
            NotFollowingCount = nofollowers.Count; 
            return true;


        }
        
        public void Logout()
        {
            isLoggedIn = false;
            InstaApi.LogoutAsync();
            InstaUser = null;
            FollowersCount = 0;
            FollowingCount = 0;
            NotFollowingCount = 0;
            Avatar = String.Empty;
            
            StateChanged?.Invoke();
        }
        

        public event Action StateChanged;
    }
}