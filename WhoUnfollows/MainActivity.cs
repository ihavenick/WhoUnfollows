using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using Plugin.Permissions;
using SQLitePCL;
using Xamarin.Essentials;
using Environment = System.Environment;
using Path = System.IO.Path;
using Permission = Plugin.Permissions.Abstractions.Permission;
using PermissionStatus = Plugin.Permissions.Abstractions.PermissionStatus;
using Uri = Android.Net.Uri;

namespace WhoUnfollows
{
    [Activity(Label = "WhoUnfollows", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustResize,
        ScreenOrientation = ScreenOrientation.Portrait,
        Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        private static readonly string dosyayolu = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        private readonly List<TableItem> hayranItem = new List<TableItem>();
        private readonly string stateFile = Path.Combine(dosyayolu, "state.bin");
        private readonly List<TableItem> tableItems = new List<TableItem>();

        private IInstaApi _instaApi;
        private IInstaApi _instaApi2;
        private int imageIndex = 0;
        private RelativeLayout rAnaSayfa;
        private RelativeLayout rHakkinda;
        private RelativeLayout rHayran;
        private RelativeLayout rTakipci;

        private EditText txtEmail;
        private EditText txtPassword;
        private UserSessionData userSession;
        private ProgressBar yuklemeBar;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetTheme(Resource.Style.MyTheme);
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

            MobileAds.Initialize(this, "ca-app-pub-9927527797473679~9358311233");

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
                var status = CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();

                if (status.Result != PermissionStatus.Granted)
                {
                    if (CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Storage).Result)

                        Toast.MakeText(Application.Context, Resources.GetText(Resource.String.fileperrmisson), ToastLength.Long)
                            .Show();

                    status = CrossPermissions.Current.RequestPermissionAsync<StoragePermission>();
                }

                // load session file if exists
                if (File.Exists(stateFile))
                {
                    using (var fs = File.OpenRead(stateFile))
                    {
                        _instaApi2.LoadStateDataFromStream(fs);
                        if (_instaApi2.IsUserAuthenticated)
                        {
                            var result2 = Task.Run(async () => await _instaApi2.UserProcessor.GetUserFollowingAsync(
                                _instaApi2.GetLoggedUser().LoggedInUser.UserName,
                                PaginationParameters.MaxPagesToLoad(1))).Result;
                            var following = result2.Value;

                            if (following == null)
                            {
                                _instaApi2.LogoutAsync();
                                File.Delete(stateFile);
                                throw new InvalidOperationException("Oturum süreniz dolmuş");
                            }

                            girisYapti(button, _instaApi2);
                        }
                    }
                }
            }
            catch (Exception er)
            {
                HataGoster(er.Message);
            }


            button.Click += butonTiklandiAsync;
        }

        private void logoutAsync(object sender, EventArgs e)
        {
            if (_instaApi != null)
                _instaApi.LogoutAsync();
            if (_instaApi2 != null)
                _instaApi2.LogoutAsync();

            File.Delete(stateFile);

            ActionBar.NavigationMode = ActionBarNavigationMode.Standard;
            ActionBar.RemoveAllTabs();

            var activity = new Intent(this, typeof(MainActivity));


            StartActivity(activity);

            Toast.MakeText(Application.Context, Resources.GetText(Resource.String.logout), ToastLength.Short).Show();
        }

        private async void butonTiklandiAsync(object sender, EventArgs e)
        {
            var button = sender as Button;

            var status = await CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();

            if (status != PermissionStatus.Granted)
            {
                if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Storage))
                    
                    Toast.MakeText(Application.Context, Resources.GetText(Resource.String.fileperrmisson), ToastLength.Long).Show();

                status = await CrossPermissions.Current.RequestPermissionAsync<StoragePermission>();
            }
            
            yuklemeBar.Visibility = ViewStates.Visible;

            if (txtEmail.Text.Length <= 0 || txtPassword.Text.Length <= 0)
            {
                HataGoster(Resources.GetText(Resource.String.passwordempty));
                yuklemeBar.Visibility = ViewStates.Invisible;
                return;
            }
            
            userSession = new UserSessionData
            {
                UserName = txtEmail.Text,
                Password = txtPassword.Text
            };
            
            _instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSession)
                .UseLogger(new DebugLogger(LogLevel.Exceptions))
                .Build();


            if (!_instaApi.IsUserAuthenticated)
            {
                var logInResult = await _instaApi.LoginAsync();
                if (!logInResult.Succeeded)
                {
                    Toast.MakeText(Application.Context, logInResult.Info.Message, ToastLength.Long).Show();
                    HataGoster(logInResult.Info.Message);
                    yuklemeBar.Visibility = ViewStates.Invisible;
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

        private void HataGoster(string govde)
        {
            var dlgAlert = new AlertDialog.Builder(this);
            dlgAlert.SetTitle(Resources.GetText(Resource.String.error));
            dlgAlert.SetMessage(govde);

            dlgAlert.SetPositiveButton("OK", delegate { dlgAlert.Dispose(); });
            dlgAlert.Show();
        }

        private async Task girisYapti(Button button, IInstaApi instaApi)
        {
            SetContentView(Resource.Layout.Menu);
            var progress = new ProgressDialog(this);


            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            ActionBar.Show();

            var adview = FindViewById<AdView>(Resource.Id.adView);
            
            //adview.AdUnitId = "ca-app-pub-9927527797473679/5706022590";

            var adRequest = new AdRequest.Builder().Build();
            adview.LoadAd(adRequest);

            rAnaSayfa = FindViewById<RelativeLayout>(Resource.Id.AnaSayfa);
            rTakipci = FindViewById<RelativeLayout>(Resource.Id.takipcilerSayfasi);
            rHayran = FindViewById<RelativeLayout>(Resource.Id.hayranlarSayfasi);
            rHakkinda = FindViewById<RelativeLayout>(Resource.Id.hakkindaSayfasi);
            var yukleme = FindViewById<RelativeLayout>(Resource.Id.Yukleme);


            var ata = FindViewById<TextView>(Resource.Id.ata);
            var ramazan = FindViewById<TextView>(Resource.Id.ramazan);
            var sukulit = FindViewById<ImageView>(Resource.Id.sukulitlogo);
            sukulit.Click += SukulitLogo;

            ata.Click += Ata_Click;
            ramazan.Click += Ramazan_Click;

            var tab = ActionBar.NewTab();
            tab.SetText("Bilgi");
            tab.SetIcon(Resource.Mipmap.Icon);
            tab.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = ViewStates.Visible;
                rTakipci.Visibility = ViewStates.Invisible;
                rHayran.Visibility = ViewStates.Invisible;
                rHakkinda.Visibility = ViewStates.Invisible;
            };
            ActionBar.AddTab(tab);


            var tab2 = ActionBar.NewTab();
            tab2.SetText(Resources.GetText(Resource.String.takipetmeyenler));
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
            tab3.SetText(Resources.GetText(Resource.String.hayranlar));
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
            tab4.SetText(Resources.GetText(Resource.String.hakkinda));
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

            try
            {
                var takipcii = FindViewById<TextView>(Resource.Id.textView1);
                var takipciler = FindViewById<TextView>(Resource.Id.takipciler);
                var takipedilenler = FindViewById<TextView>(Resource.Id.takipedilenler);
                var kullaniciAdi = FindViewById<TextView>(Resource.Id.kullaniciAdi);


                rAnaSayfa.Visibility = ViewStates.Invisible;


                yukleme.Visibility = ViewStates.Visible;

                var result = await instaApi.UserProcessor.GetUserFollowersAsync(
                    instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(100));
                var followers = result.Value;


                var result2 = await instaApi.UserProcessor.GetUserFollowingAsync(
                    instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(100));
                var following = result2.Value;

                if (following == null || followers == null)
                    refresh_clickAsync(button, new EventArgs());


                var takipetmeyenler = following.Except(followers).ToList();
                var hayranlar = followers.Except(following).ToList();

                takipcii.Text = Resources.GetText(Resource.String.textView11) + takipetmeyenler.Count;
                takipciler.Text = Resources.GetText(Resource.String.takipcilerr) + followers.Count;
                takipedilenler.Text = Resources.GetText(Resource.String.takipedilenlerr) + following.Count;


                foreach (var item in takipetmeyenler)
                    tableItems.Add(new TableItem
                    {
                        kullaniciAdi = item.UserName,
                        AdiSoyadi = item.FullName,
                        Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                        userId = item.Pk
                    });

                foreach (var item in hayranlar)
                    hayranItem.Add(new TableItem
                    {
                        kullaniciAdi = item.UserName,
                        AdiSoyadi = item.FullName,
                        Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                        userId = item.Pk
                    });


                rAnaSayfa.Visibility = ViewStates.Visible;

                yukleme.Visibility = ViewStates.Invisible;


                kullaniciAdi.Text = instaApi.GetLoggedUser().LoggedInUser.UserName;
                rAnaSayfa.Visibility = ViewStates.Visible;

                yukleme.Visibility = ViewStates.Invisible;
            }
            catch (Exception ex)
            {
                if (ex.Message != "The remote server returned an error: (410) Gone.")
                {
                    HataGoster(ex.ToString());
                }
            }

            var listView = FindViewById<ListView>(Resource.Id.listView1);


            listView.Adapter = new ListeAdaptoru(this, tableItems, instaApi, progress);


            var listView2 = FindViewById<ListView>(Resource.Id.listView2);


            listView2.Adapter = new ListeAdaptoru2(this, hayranItem, instaApi);

            rAnaSayfa.Visibility = ViewStates.Visible;

            yukleme.Visibility = ViewStates.Invisible;
        }

        private void SukulitLogo(object sender, EventArgs e)
        {
            var uri = Uri.Parse("https://sukulit-apps.github.io/");
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void Ramazan_Click(object sender, EventArgs e)
        {
            var uri = Uri.Parse("http://instagram.com/ramazankabadayi");
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void Ata_Click(object sender, EventArgs e)
        {
            var uri = Uri.Parse("http://instagram.com/atacetin_");
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private async void refresh_clickAsync(object sender, EventArgs e)
        {
            var anaekran = FindViewById<RelativeLayout>(Resource.Id.AnaSayfa);
            anaekran.Visibility = ViewStates.Invisible;

            var yukleme = FindViewById<RelativeLayout>(Resource.Id.Yukleme);
            yukleme.Visibility = ViewStates.Visible;
            try
            {
                ActionBar.Hide();

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
                    instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(100));
                var followers = result.Value;

                var result2 = await instaApi.UserProcessor.GetUserFollowingAsync(
                    instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(100));
                var following = result2.Value;

                if (followers == null || following == null)
                    logoutAsync(sender, e);

                var takipetmeyenler = following.Except(followers).ToList();
                var hayranlar = followers.Except(following).ToList();


                takipci.Text = Resources.GetText(Resource.String.textView11) + takipetmeyenler.Count;
                takipciler.Text = Resources.GetText(Resource.String.takipcilerr) + followers.Count;
                takipedilenler.Text = Resources.GetText(Resource.String.takipedilenlerr) + following.Count;
                kullaniciAdi.Text = instaApi.GetLoggedUser().LoggedInUser.UserName;

                foreach (var item in takipetmeyenler)
                    tableItems.Add(new TableItem
                    {
                        kullaniciAdi = item.UserName,
                        AdiSoyadi = item.FullName,
                        Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                        userId = item.Pk
                    });

                foreach (var item in hayranlar)
                    hayranItem.Add(new TableItem
                    {
                        kullaniciAdi = item.UserName,
                        AdiSoyadi = item.FullName,
                        Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                        userId = item.Pk
                    });

                yukleme.Visibility = ViewStates.Invisible;
                anaekran.Visibility = ViewStates.Visible;
                ActionBar.Show();
            }
            catch (Exception exception)
            {
                if (exception.Message == "The remote server returned an error: (410) Gone.")
                {
                    yukleme.Visibility = ViewStates.Invisible;
                    anaekran.Visibility = ViewStates.Visible;
                    ActionBar.Show();
                }
                else
                {
                    HataGoster(exception.Message);
                    yukleme.Visibility = ViewStates.Invisible;
                    anaekran.Visibility = ViewStates.Visible;
                    ActionBar.Show();
                    
                }
            }
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