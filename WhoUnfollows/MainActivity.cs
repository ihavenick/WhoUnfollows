using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using Microsoft.EntityFrameworkCore;
using Plugin.Permissions;
using SQLitePCL;
using Xamarin.Essentials;
using Debug = System.Diagnostics.Debug;
using Environment = System.Environment;
using Path = System.IO.Path;

namespace WhoUnfollows
{
    [Activity(Label = "WhoUnfollows", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustResize,
        Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        private static readonly string dosyayolu = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        private readonly List<TableItem> hayranItem = new List<TableItem>();
        private readonly string stateFile = Path.Combine(dosyayolu, "state.bin");
        private readonly List<TableItem> tableItems = new List<TableItem>();

        private IInstaApi _instaApi;
        private IInstaApi _instaApi2;

        private ApplicationDbContext db;
        private EditText txtEmail;
        private EditText txtPassword;
        private UserSessionData userSession;

        private ProgressBar yuklemeBar;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ActionBar.Hide();
            ActionBar.SetDisplayShowTitleEnabled(false);
            ActionBar.SetDisplayShowHomeEnabled(false);

            SetContentView(Resource.Layout.Main);

            Platform.Init(this, savedInstanceState);
            Batteries.Init();

            var button = FindViewById<Button>(Resource.Id.myButton);

            yuklemeBar = FindViewById<ProgressBar>(Resource.Id.progressBar1);

            txtEmail = FindViewById<EditText>(Resource.Id.tbEmail);
            txtPassword = FindViewById<EditText>(Resource.Id.tbPassword);


            CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();


            userSession = new UserSessionData
            {
                UserName = "",
                Password = ""
            };


            var delay = RequestDelay.FromSeconds(2, 2);
            // create new InstaApi instance using Builder
            _instaApi2 = InstaApiBuilder.CreateBuilder()
                .UseLogger(new DebugLogger(LogLevel.Exceptions)) // use logger for requests and debug messages
                .Build();

            try
            {
                // load session file if exists
                if (File.Exists(stateFile))
                {
                    Console.WriteLine("Loading state from file");
                    using (var fs = File.OpenRead(stateFile))
                    {
                        _instaApi2.LoadStateDataFromStream(fs);


#pragma warning disable 4014
                        if (_instaApi2.IsUserAuthenticated) girisYapti(button, _instaApi2);
#pragma warning restore 4014
                    }
                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er);
            }


            button.Click += butonTiklandiAsync;
        }

        private void logoutAsync(object sender, EventArgs e)
        {
            if (_instaApi != null)
                _instaApi.LogoutAsync();
            if (_instaApi2 != null)
                _instaApi2.LogoutAsync();


            foreach (var item in db.TakipEtmeyenler) db.TakipEtmeyenler.Remove(item);
            db.SaveChangesAsync();

            File.Delete(stateFile);

            ActionBar.NavigationMode = ActionBarNavigationMode.Standard;
            ActionBar.RemoveAllTabs();

            var activity = new Intent(this, typeof(MainActivity));


            StartActivity(activity);

            Toast.MakeText(Application.Context, "Çıkış Yapıldı!", ToastLength.Short).Show();
        }

        private async void butonTiklandiAsync(object sender, EventArgs e)
        {
            yuklemeBar.Visibility = ViewStates.Visible;


            userSession = new UserSessionData
            {
                UserName = txtEmail.Text,
                Password = txtPassword.Text
            };

            var button = sender as Button;

            _instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSession)
                .UseLogger(new DebugLogger(LogLevel.Exceptions))
                .Build();


            if (!_instaApi.IsUserAuthenticated)
            {
                Console.WriteLine($"Logging in as {userSession.UserName}");
                var logInResult = await _instaApi.LoginAsync();
                if (!logInResult.Succeeded)
                {
                    Console.WriteLine($"Unable to login: {logInResult.Info.Message}");
                    button.Text = logInResult.Info.Message;
                    Toast.MakeText(Application.Context, logInResult.Info.Exception.Message, ToastLength.Long).Show();
                    return;
                }

                await girisYapti(button, _instaApi);
                var state = _instaApi.GetStateDataAsStream();
                using (var fileStream = File.Create(stateFile))
                {
                    state.Seek(0, SeekOrigin.Begin);
                    state.CopyTo(fileStream);
                }
            }
        }

        private async Task girisYapti(Button button, IInstaApi instaApi)
        {
            SetContentView(Resource.Layout.Menu);


            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            ActionBar.Show();


            var rAnaSayfa = FindViewById<RelativeLayout>(Resource.Id.AnaSayfa);
            var rTakipci = FindViewById<RelativeLayout>(Resource.Id.takipcilerSayfasi);
            var rHayran = FindViewById<RelativeLayout>(Resource.Id.hayranlarSayfasi);
            var rHakkinda = FindViewById<RelativeLayout>(Resource.Id.hakkindaSayfasi);


            var tab = ActionBar.NewTab();
            tab.SetText("Bilgi");
            //tab.SetIcon(Resource.Drawable.tab1_icon);
            tab.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = ViewStates.Visible;
                rTakipci.Visibility = ViewStates.Invisible;
                rHayran.Visibility = ViewStates.Invisible;
                rHakkinda.Visibility = ViewStates.Invisible;
            };
            ActionBar.AddTab(tab);


            var tab2 = ActionBar.NewTab();
            tab2.SetText("Takip Etmeyenler");
            //tab.SetIcon(Resource.Drawable.tab1_icon);
            tab2.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = ViewStates.Invisible;
                rTakipci.Visibility = ViewStates.Visible;
                rHayran.Visibility = ViewStates.Invisible;
                rHakkinda.Visibility = ViewStates.Invisible;
            };
            ActionBar.AddTab(tab2);


            var tab3 = ActionBar.NewTab();
            tab3.SetText("Hayranlar");
            //tab.SetIcon(Resource.Drawable.tab1_icon);
            tab3.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = ViewStates.Invisible;
                rTakipci.Visibility = ViewStates.Invisible;
                rHayran.Visibility = ViewStates.Visible;
                rHakkinda.Visibility = ViewStates.Invisible;
            };
            ActionBar.AddTab(tab3);


            var tab4 = ActionBar.NewTab();
            tab4.SetText("Hakkında");
            //tab.SetIcon(Resource.Drawable.tab1_icon);
            tab4.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = ViewStates.Invisible;
                rTakipci.Visibility = ViewStates.Invisible;
                rHayran.Visibility = ViewStates.Invisible;
                rHakkinda.Visibility = ViewStates.Visible;
            };
            ActionBar.AddTab(tab4);


            var url = instaApi.GetLoggedUser().LoggedInUser.ProfilePicture;

            var logout = FindViewById<ImageButton>(Resource.Id.logOut);
            logout.Click += logoutAsync;


            var refresh = FindViewById<ImageButton>(Resource.Id.reflesh);
            refresh.Click += refresh_clickAsync;


            var imageView = FindViewById<ImageView>(Resource.Id.imageView1);
            imageView.SetImageBitmap(GetBitmapFromUrl(url));


            var takipci = FindViewById<TextView>(Resource.Id.textView1);


            var dbFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var fileName = "takipci.db";
            var dbFullPath = Path.Combine(dbFolder, fileName);
            db = new ApplicationDbContext(dbFullPath);
            try
            {
                await db.Database
                    .MigrateAsync(); 


                if (db.TakipEtmeyenler.Count() < 1)
                {
                    var result = await instaApi.UserProcessor.GetUserFollowersAsync(
                        instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
                    var followers = result.Value;
                    var anyDuplicate = followers.GroupBy(x => x.Pk).Any(g => g.Count() > 1);
                    button.Text = $"{followers.Count} takipci";

                    var takipcii = FindViewById<TextView>(Resource.Id.textView1);
                    var takipciler = FindViewById<TextView>(Resource.Id.takipciler);
                    var takipedilenler = FindViewById<TextView>(Resource.Id.takipedilenler);
                    var kullaniciAdi = FindViewById<TextView>(Resource.Id.kullaniciAdi);

                    var result2 = await instaApi.UserProcessor.GetUserFollowingAsync(
                        instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
                    var following = result2.Value;


                    var takipetmeyenler = following.Except(followers).ToList();
                    var hayranlar = followers.Except(following).ToList();

                    takipcii.Text = "Takip Etmeyen: " + takipetmeyenler.Count;
                    takipciler.Text = "Takipçi: " + followers.Count;
                    takipedilenler.Text = "Takip: " + following.Count;
                    kullaniciAdi.Text = instaApi.GetLoggedUser().LoggedInUser.UserName;

               


                    foreach (var item in takipetmeyenler)
                    {
                        takipci.Text += item.UserName;
                        

                        tableItems.Add(new TableItem
                        {
                            kullaniciAdi = item.UserName,
                            AdiSoyadi = item.FullName,
                            Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                            userId = item.Pk
                        });
                        await db.TakipEtmeyenler.AddAsync(item);
                    }

                    foreach (var item in hayranlar)
                        hayranItem.Add(new TableItem
                        {
                            kullaniciAdi = item.UserName,
                            AdiSoyadi = item.FullName,
                            Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                            userId = item.Pk
                        });


                    

                    await db.SaveChangesAsync();
                }
                else
                {
                    foreach (var item in db.TakipEtmeyenler)
                    {
                        takipci.Text += item.UserName;
                        // adlar.Add(item.UserName);

                        tableItems.Add(new TableItem
                        {
                            kullaniciAdi = item.UserName,
                            AdiSoyadi = item.FullName,
                            Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                            userId = item.Pk
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }


            var listView = FindViewById<ListView>(Resource.Id.listView1);


            listView.Adapter = new ListeAdaptoru(this, tableItems, instaApi);


            var listView2 = FindViewById<ListView>(Resource.Id.listView2);


            listView2.Adapter = new ListeAdaptoru2(this, hayranItem, instaApi);
        }

        private async void refresh_clickAsync(object sender, EventArgs e)
        {
            ActionBar.Hide();
            var anaekran = FindViewById<RelativeLayout>(Resource.Id.AnaSayfa);
            anaekran.Visibility = ViewStates.Invisible;

            var yukleme = FindViewById<RelativeLayout>(Resource.Id.Yukleme);
            yukleme.Visibility = ViewStates.Visible;


            tableItems.Clear();


            IInstaApi instaApi;

            if (_instaApi != null)
                instaApi = _instaApi;
            else
                instaApi = _instaApi2;

            var takipci = FindViewById<TextView>(Resource.Id.textView1);
            var takipciler = FindViewById<TextView>(Resource.Id.takipciler);
            var takipedilenler = FindViewById<TextView>(Resource.Id.takipedilenler);
            var kullaniciAdi = FindViewById<TextView>(Resource.Id.kullaniciAdi);

            var result = await instaApi.UserProcessor.GetUserFollowersAsync(
                instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
            var followers = result.Value;
            var anyDuplicate = followers.GroupBy(x => x.Pk).Any(g => g.Count() > 1);


            var result2 = await instaApi.UserProcessor.GetUserFollowingAsync(
                instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
            var following = result2.Value;

            var takipetmeyenler = following.Except(followers).ToList();
            var hayranlar = followers.Except(following).ToList();


            foreach (var item in db.TakipEtmeyenler) db.TakipEtmeyenler.Remove(item);
            await db.SaveChangesAsync();

            takipci.Text = "Takip Etmeyen: " + takipetmeyenler.Count;
            takipciler.Text = "Takipçi: " + followers.Count;
            takipedilenler.Text = "Takip: " + following.Count;
            kullaniciAdi.Text = instaApi.GetLoggedUser().LoggedInUser.UserName;

            foreach (var item in takipetmeyenler)
            {

                tableItems.Add(new TableItem
                {
                    kullaniciAdi = item.UserName,
                    AdiSoyadi = item.FullName,
                    Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                    userId = item.Pk
                });
                if (!db.TakipEtmeyenler.Contains(item)) await db.TakipEtmeyenler.AddAsync(item);
            }

            foreach (var item in hayranlar)
                hayranItem.Add(new TableItem
                {
                    kullaniciAdi = item.UserName,
                    AdiSoyadi = item.FullName,
                    Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                    userId = item.Pk
                });


            await db.SaveChangesAsync();
            yukleme.Visibility = ViewStates.Invisible;
            anaekran.Visibility = ViewStates.Visible;
            ActionBar.Show();
        }

        public Bitmap GetBitmapFromUrl(string url)
        {
            using (var webClient = new WebClient())
            {
                var bytes = webClient.DownloadData(url);

                if (bytes != null && bytes.Length > 0) return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
            }

            return null;
        }
    }
}