using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gestures;
using Android.Gms.Ads;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Tabs.AppCompat.App;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using Microsoft.EntityFrameworkCore;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using SQLitePCL;
using Xamarin.Essentials;
using Debug = System.Diagnostics.Debug;
using Environment = System.Environment;
using Path = System.IO.Path;
using PermissionStatus = Plugin.Permissions.Abstractions.PermissionStatus;


namespace WhoUnfollows
{
    [Activity(Label = "WhoUnfollows", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustResize, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait,
        Icon = "@mipmap/icon")]
    public class MainActivity : Activity,
        GestureDetector.IOnGestureListener, View.IOnTouchListener
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
        private GestureDetector gestureDetector;  
        private ProgressBar yuklemeBar;
        private RelativeLayout rAnaSayfa;
        private RelativeLayout rTakipci;
        private RelativeLayout rHayran;
        private RelativeLayout rHakkinda;
        private int imageIndex = 0;

        private readonly int SWIPE_MIN_DISTANCE = 120;  
        private static int SWIPE_MAX_OFF_PATH = 250;  
        private static int SWIPE_THRESHOLD_VELOCITY = 200;  


       

         
        public bool OnDown(MotionEvent e) {  
            Toast.MakeText(this, "On Down", ToastLength.Short).Show();  
            return true;  
        }  

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY) {  
            bool result = false;  
            try {  
                float diffY = e2.GetY() - e1.GetY();  
                float diffX = e2.GetX() - e1.GetX();  
                if (Math.Abs(diffX) > Math.Abs(diffY)) {  
                    if (Math.Abs(diffX) > SWIPE_THRESHOLD_VELOCITY && Math.Abs(velocityX) > SWIPE_THRESHOLD_VELOCITY) {  
                        if (diffX > 0) {  
                            //onSwipeRight();    
                            if (imageIndex > 0) {  
                                imageIndex--;  
                            }  
                            //txtGestureView.Text = "Swiped Right";  
                            rAnaSayfa.Visibility = ViewStates.Invisible;
                            rTakipci.Visibility = ViewStates.Visible;
                            Toast.MakeText(this, "On Touch", ToastLength.Short).Show();  
                            rHayran.Visibility = ViewStates.Invisible;
                            rHakkinda.Visibility = ViewStates.Invisible;
                        } else {  
                            if (imageIndex < 28) {  
                                imageIndex++;  
                            }  
                            //onSwipeLeft();    
                            //txtGestureView.Text = "Swiped Left";  Toast.MakeText(this, "On Touch", ToastLength.Short).Show();  
                            Toast.MakeText(this, "On Touch", ToastLength.Short).Show();  
                        }  
                        result = true;  
                    }  
                } else  
                if (Math.Abs(diffY) > SWIPE_THRESHOLD_VELOCITY && Math.Abs(velocityY) > SWIPE_THRESHOLD_VELOCITY) {  
                    if (diffY > 0) {  
                        //onSwipeBottom();    
                        //txtGestureView.Text = "Swiped Bottom";  Toast.MakeText(this, "On Touch", ToastLength.Short).Show();  
                        Toast.MakeText(this, "On Touch", ToastLength.Short).Show();  
                    } else {  
                        //onSwipeTop();    
                       // txtGestureView.Text = "Swiped Top";  Toast.MakeText(this, "On Touch", ToastLength.Short).Show();  
                       Toast.MakeText(this, "On Touch", ToastLength.Short).Show();  
                    }  
                    result = true;  
                }  
            } catch (Exception exception) {  
                Console.WriteLine(exception.Message);  
            }  
            return result;  
        }  
        public void OnLongPress(MotionEvent e) {  
            Toast.MakeText(this, "On Long Press", ToastLength.Short).Show();  
        }  
        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY) {  
            Toast.MakeText(this, "On Scroll", ToastLength.Short).Show();  
            return true;  
        }  
        public void OnShowPress(MotionEvent e) {  
            Toast.MakeText(this, "On Show Press", ToastLength.Short).Show();  
        }  
        public bool OnSingleTapUp(MotionEvent e) {  
            Toast.MakeText(this, "On Single Tab Up", ToastLength.Short).Show();  
            return true;  
        }  


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


            gestureDetector = new GestureDetector(this,this);
            var button = FindViewById<ImageButton>(Resource.Id.myButton);

            yuklemeBar = FindViewById<ProgressBar>(Resource.Id.progressBar1);

            txtEmail = FindViewById<EditText>(Resource.Id.tbEmail);
            txtPassword = FindViewById<EditText>(Resource.Id.tbPassword);

            CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();

            MobileAds.Initialize(this);

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
                    if ( CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Storage).Result)
                        
                        Toast.MakeText(Application.Context, "Dosya saklama izni vermen gerekiyo", ToastLength.Long).Show();

                    status =  CrossPermissions.Current.RequestPermissionAsync<StoragePermission>();
                }
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
                var dlgAlert = new AlertDialog.Builder(this);
                dlgAlert.SetTitle("hata");
                dlgAlert.SetMessage(er.Message);

                dlgAlert.SetPositiveButton("OK", delegate { dlgAlert.Dispose(); });
                dlgAlert.Show();
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
            var button = sender as ImageButton;

            var status = await CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();

            if (status != PermissionStatus.Granted)
            {
                if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Storage))
                    //button.Text = "Dosya saklama izni vermen gerekiyo";
                    Toast.MakeText(Application.Context, "Dosya saklama izni vermen gerekiyo", ToastLength.Long).Show();

                status = await CrossPermissions.Current.RequestPermissionAsync<StoragePermission>();
            }


            yuklemeBar.Visibility = ViewStates.Visible;

            if (txtEmail.Text == null && txtPassword.Text == null)
            {
                var dlgAlert = new AlertDialog.Builder(this);
                dlgAlert.SetTitle("hata");
                dlgAlert.SetMessage("Şifre ve kullanıcı adını boş bırakma");

                dlgAlert.SetPositiveButton("OK", delegate { dlgAlert.Dispose(); });
                dlgAlert.Show();
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
                Console.WriteLine($"Logging in as {userSession.UserName}");
                var logInResult = await _instaApi.LoginAsync();
                if (!logInResult.Succeeded)
                {
                    Console.WriteLine($"Unable to login: {logInResult.Info.Message}");
                    //button.Text = logInResult.Info.Message;
                    Toast.MakeText(Application.Context, logInResult.Info.Message, ToastLength.Long).Show();
                    var dlgAlert = new AlertDialog.Builder(this);
                    dlgAlert.SetTitle(logInResult.Info.Message);
                    dlgAlert.SetMessage(logInResult.Info.Message);

                    dlgAlert.SetPositiveButton("OK", delegate { dlgAlert.Dispose(); });
                    dlgAlert.Show();
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

        private async Task girisYapti(ImageButton button, IInstaApi instaApi)
        {
            SetContentView(Resource.Layout.Menu);


            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            ActionBar.Show();

            var adview = FindViewById<AdView>(Resource.Id.adView);

            //Test device request.
            //var adRequest = new AdRequest.Builder().AddTestDevice("33BE2250B43518CCDA7DE426D04EE231").Build();
            //adview.LoadAd(adRequest);
            
            rAnaSayfa = FindViewById<RelativeLayout>(Resource.Id.AnaSayfa); 
            rTakipci = FindViewById<RelativeLayout>(Resource.Id.takipcilerSayfasi);
            rHayran = FindViewById<RelativeLayout>(Resource.Id.hayranlarSayfasi);
            rHakkinda = FindViewById<RelativeLayout>(Resource.Id.hakkindaSayfasi);
            var yukleme = FindViewById<RelativeLayout>(Resource.Id.Yukleme);
            

            var ata = FindViewById<TextView>(Resource.Id.ata);
            var ramazan = FindViewById<TextView>(Resource.Id.ramazan);

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


            var dbFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var fileName = "takipci.db";
            var dbFullPath = Path.Combine(dbFolder, fileName);
            db = new ApplicationDbContext(dbFullPath);
            try
            {
                await db.Database
                    .MigrateAsync();

                var takipcii = FindViewById<TextView>(Resource.Id.textView1);
                var takipciler = FindViewById<TextView>(Resource.Id.takipciler);
                var takipedilenler = FindViewById<TextView>(Resource.Id.takipedilenler);
                var kullaniciAdi = FindViewById<TextView>(Resource.Id.kullaniciAdi);
                
                

                if (db.TakipEtmeyenler.Count() < 1)
                {
                    
                    rAnaSayfa.Visibility = ViewStates.Invisible;

                    
                    yukleme.Visibility = ViewStates.Visible;

                    var result = await instaApi.UserProcessor.GetUserFollowersAsync(
                        instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
                    var followers = result.Value;


                    var result2 = await instaApi.UserProcessor.GetUserFollowingAsync(
                        instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
                    var following = result2.Value;


                    var takipetmeyenler = following.Except(followers).ToList();
                    var hayranlar = followers.Except(following).ToList();

                    takipcii.Text = "Takip Etmeyen: " + takipetmeyenler.Count;
                    takipciler.Text = "Takipçi: " + followers.Count;
                    takipedilenler.Text = "Takip: " + following.Count;


                    foreach (var item in takipetmeyenler)
                    {
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
                    rAnaSayfa.Visibility = ViewStates.Visible;

                    yukleme.Visibility = ViewStates.Invisible;
                }
                else
                {
                    foreach (var item in db.TakipEtmeyenler)
                        tableItems.Add(new TableItem
                        {
                            kullaniciAdi = item.UserName,
                            AdiSoyadi = item.FullName,
                            Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                            userId = item.Pk
                        });
                    takipcii.Text = "Takip Etmeyen: " + db.TakipEtmeyenler.Count();
                }

                kullaniciAdi.Text = instaApi.GetLoggedUser().LoggedInUser.UserName;
                rAnaSayfa.Visibility = ViewStates.Visible;

                yukleme.Visibility = ViewStates.Invisible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }


            var listView = FindViewById<ListView>(Resource.Id.listView1);


            listView.Adapter = new ListeAdaptoru(this, tableItems, instaApi);


            var listView2 = FindViewById<ListView>(Resource.Id.listView2);


            listView2.Adapter = new ListeAdaptoru2(this, hayranItem, instaApi);
            rAnaSayfa.Visibility = ViewStates.Visible;

            yukleme.Visibility = ViewStates.Invisible;
        }

         void Ramazan_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse("http://instagram.com/ramazankabadayi");
            var intent = new Intent(Intent.ActionView, uri);
            this.StartActivity(intent);
        }

        private void Ata_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse("http://instagram.com/atacetin_");
            var intent = new Intent(Intent.ActionView, uri);
            this.StartActivity(intent);
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
                    instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
                var followers = result.Value;

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
                    var dlgAlert = new AlertDialog.Builder(this);
                    dlgAlert.SetTitle("hata");
                    dlgAlert.SetMessage(exception.Message);

                    dlgAlert.SetPositiveButton("OK", delegate
                    {
                        dlgAlert.Dispose();

                        yukleme.Visibility = ViewStates.Invisible;
                        anaekran.Visibility = ViewStates.Visible;
                        ActionBar.Show();
                    });
                    dlgAlert.Show();
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

        public bool OnTouch(View? v, MotionEvent? e)
        {
            return true;
        }
    }
}