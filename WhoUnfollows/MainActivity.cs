using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using InstagramApiSharp;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace WhoUnfollows
{
    [Activity(Label = "WhoUnfollows", MainLauncher = true, WindowSoftInputMode = Android.Views.SoftInput.AdjustResize, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        EditText txtEmail;
        EditText txtPassword;

        InstagramApiSharp.API.IInstaApi _instaApi;
        InstagramApiSharp.API.IInstaApi _instaApi2;
        static string dosyayolu = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        string stateFile = System.IO.Path.Combine(dosyayolu, "state.bin");
        UserSessionData userSession;
        List<TableItem> tableItems = new List<TableItem>();
        List<TableItem> hayranItem = new List<TableItem>();

        ApplicationDbContext db;

        ProgressBar yuklemeBar;


        public override void OnContentChanged()
        {
            base.OnContentChanged();

        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ActionBar.Hide();
            ActionBar.SetDisplayShowTitleEnabled(false);
            ActionBar.SetDisplayShowHomeEnabled(false);

            SetContentView(Resource.Layout.Main);


            // Get our button from the layout resource,
            // and attach an event to it

            Button button = FindViewById<Button>(Resource.Id.myButton);

            yuklemeBar = FindViewById<ProgressBar>(Resource.Id.progressBar1);

            txtEmail = FindViewById<EditText>(Resource.Id.tbEmail);
            txtPassword = FindViewById<EditText>(Resource.Id.tbPassword);






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


                        if (_instaApi2.IsUserAuthenticated)
                        {
                            girisYapti(button, _instaApi2);

                        }

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


            foreach (var item in db.TakipEtmeyenler)
            {
                db.TakipEtmeyenler.Remove(item);
            }
            db.SaveChangesAsync();

            File.Delete(stateFile);

            ActionBar.NavigationMode = ActionBarNavigationMode.Standard;
            ActionBar.RemoveAllTabs();

            var activity = new Intent(this, typeof(MainActivity));


            StartActivity(activity);

            //SetContentView(Resource.Layout.Main);
            Toast.MakeText(Application.Context, "Çıkış Yapıldı!", ToastLength.Short).Show();

        }

        private async void butonTiklandiAsync(object sender, EventArgs e)
        {



            yuklemeBar.Visibility = Android.Views.ViewStates.Visible;





            userSession = new UserSessionData
            {
                UserName = txtEmail.Text,
                Password = txtPassword.Text
            };

            var button = sender as Button;

            _instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSession)
                .UseLogger(new DebugLogger(LogLevel.Exceptions)) // use logger for requests and debug messages
                .Build();


            if (!_instaApi.IsUserAuthenticated)
            {
                // login
                Console.WriteLine($"Logging in as {userSession.UserName}");
                //delay.Disable();
                var logInResult = await _instaApi.LoginAsync();
                //delay.Enable();
                if (!logInResult.Succeeded)
                {
                    Console.WriteLine($"Unable to login: {logInResult.Info.Message}");
                    button.Text = $"giris basarisiz";
                }

                await girisYapti(button, _instaApi);

                // save session in file
                var state = _instaApi.GetStateDataAsStream();
                // in .net core or uwp apps don't use GetStateDataAsStream.
                // use this one:
                // var state = _instaApi.GetStateDataAsString();
                // this returns you session as json string.
                using (var fileStream = File.Create(stateFile))
                {
                    state.Seek(0, SeekOrigin.Begin);
                    state.CopyTo(fileStream);


                }


            }



            //button.Text = $"deneme";

        }

        private async System.Threading.Tasks.Task girisYapti(Button button, InstagramApiSharp.API.IInstaApi instaApi)
        {
            SetContentView(Resource.Layout.Menu);



            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            ActionBar.Show();


            RelativeLayout rAnaSayfa = FindViewById<RelativeLayout>(Resource.Id.AnaSayfa);
            RelativeLayout rTakipci = FindViewById<RelativeLayout>(Resource.Id.takipcilerSayfasi);
            RelativeLayout rHayran = FindViewById<RelativeLayout>(Resource.Id.hayranlarSayfasi);
            RelativeLayout rHakkinda = FindViewById<RelativeLayout>(Resource.Id.hakkindaSayfasi);


            ActionBar.Tab tab = ActionBar.NewTab();
            tab.SetText("Bilgi");
            //tab.SetIcon(Resource.Drawable.tab1_icon);
            tab.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = Android.Views.ViewStates.Visible;
                rTakipci.Visibility = Android.Views.ViewStates.Invisible;
                rHayran.Visibility = Android.Views.ViewStates.Invisible;
                rHakkinda.Visibility = Android.Views.ViewStates.Invisible;
            };
            ActionBar.AddTab(tab);


            ActionBar.Tab tab2 = ActionBar.NewTab();
            tab2.SetText("Takip Etmeyenler");
            //tab.SetIcon(Resource.Drawable.tab1_icon);
            tab2.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = Android.Views.ViewStates.Invisible;
                rTakipci.Visibility = Android.Views.ViewStates.Visible;
                rHayran.Visibility = Android.Views.ViewStates.Invisible;
                rHakkinda.Visibility = Android.Views.ViewStates.Invisible;
            };
            ActionBar.AddTab(tab2);



            ActionBar.Tab tab3 = ActionBar.NewTab();
            tab3.SetText("Hayranlar");
            //tab.SetIcon(Resource.Drawable.tab1_icon);
            tab3.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = Android.Views.ViewStates.Invisible;
                rTakipci.Visibility = Android.Views.ViewStates.Invisible;
                rHayran.Visibility = Android.Views.ViewStates.Visible;
                rHakkinda.Visibility = Android.Views.ViewStates.Invisible;
            };
            ActionBar.AddTab(tab3);


            ActionBar.Tab tab4 = ActionBar.NewTab();
            tab4.SetText("Hakkında");
            //tab.SetIcon(Resource.Drawable.tab1_icon);
            tab4.TabSelected += (sender2, args) =>
            {
                rAnaSayfa.Visibility = Android.Views.ViewStates.Invisible;
                rTakipci.Visibility = Android.Views.ViewStates.Invisible;
                rHayran.Visibility = Android.Views.ViewStates.Invisible;
                rHakkinda.Visibility = Android.Views.ViewStates.Visible;
            };
            ActionBar.AddTab(tab4);



            var url = instaApi.GetLoggedUser().LoggedInUser.ProfilePicture;

            ImageButton logout = FindViewById<ImageButton>(Resource.Id.logOut);
            logout.Click += logoutAsync;


            ImageButton refresh = FindViewById<ImageButton>(Resource.Id.reflesh);
            refresh.Click += refresh_clickAsync;


            ImageView imageView = FindViewById<ImageView>(Resource.Id.imageView1);
            imageView.SetImageBitmap(GetBitmapFromUrl(url));


            TextView takipci = FindViewById<TextView>(Resource.Id.textView1);



            var dbFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            var fileName = "takipci.db";
            var dbFullPath = System.IO.Path.Combine(dbFolder, fileName);
            db = new ApplicationDbContext(dbFullPath);
            try
            {

                await db.Database.MigrateAsync(); //We need to ensure the latest Migration was added. This is different than EnsureDatabaseCreated.


                if (await db.TakipEtmeyenler.CountAsync() < 1)
                {
                    var result = await instaApi.UserProcessor.GetUserFollowersAsync(instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
                    var followers = result.Value;
                    var anyDuplicate = followers.GroupBy(x => x.Pk).Any(g => g.Count() > 1);
                    button.Text = $"{ followers.Count} takipci";

                    TextView takipcii = FindViewById<TextView>(Resource.Id.textView1);
                    TextView takipciler = FindViewById<TextView>(Resource.Id.takipciler);
                    TextView takipedilenler = FindViewById<TextView>(Resource.Id.takipedilenler);
                    TextView kullaniciAdi = FindViewById<TextView>(Resource.Id.kullaniciAdi);

                    var result2 = await instaApi.UserProcessor.GetUserFollowingAsync(instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
                    var following = result2.Value;


                    // var takipetmeyenler = followers.Except(following).ToList();
                    var takipetmeyenler = following.Except(followers).ToList();
                    var hayranlar = followers.Except(following).ToList();

                    takipcii.Text = "Takip Etmeyen: " + takipetmeyenler.Count.ToString();
                    takipciler.Text = "Takipçi: " + followers.Count.ToString();
                    takipedilenler.Text = "Takip: " + following.Count.ToString();
                    kullaniciAdi.Text = instaApi.GetLoggedUser().LoggedInUser.UserName;

                    //List<string> adlar = new List<string>();


                    foreach (var item in takipetmeyenler)
                    {
                        takipci.Text += item.UserName;
                        // adlar.Add(item.UserName);

                        tableItems.Add(new TableItem()
                        {
                            kullaniciAdi = item.UserName,
                            AdiSoyadi = item.FullName,
                            Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                            userId = item.Pk
                        });
                        await db.TakipEtmeyenler.AddAsync(item);
                    }

                    foreach (var item in hayranlar)
                    {
                        hayranItem.Add(new TableItem()
                        {
                            kullaniciAdi = item.UserName,
                            AdiSoyadi = item.FullName,
                            Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                            userId = item.Pk


                        });
                    }


                    //foreach (var item in followers)
                    //{
                    //   await db.Takipciler.AddAsync(item);
                    //}
                    //foreach (var item in following)
                    //{
                    //   await db.TakipEdilenler.AddAsync(item);
                    //}


                    await db.SaveChangesAsync();
                }
                else
                {

                    //button.Text = $"{ db.Takipciler.ToList().Count} takipci";



                    foreach (var item in db.TakipEtmeyenler)
                    {
                        takipci.Text += item.UserName;
                        // adlar.Add(item.UserName);

                        tableItems.Add(new TableItem()
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
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }









            ListView listView = FindViewById<ListView>(Resource.Id.listView1);






            listView.Adapter = new ListeAdaptoru(this, tableItems, instaApi);
            //listView.ItemClick += listeItemineTiklandi;




            ListView listView2 = FindViewById<ListView>(Resource.Id.listView2);






            listView2.Adapter = new ListeAdaptoru2(this, hayranItem, instaApi);
            // var ListAdapter = new ArrayAdapter<string>(this, Resource.Layout.takipciler, adlar);
            //listView.Adapter = ListAdapter;


        }

        private async void refresh_clickAsync(object sender, EventArgs e)
        {
            ActionBar.Hide();
            RelativeLayout anaekran = FindViewById<RelativeLayout>(Resource.Id.AnaSayfa);
            anaekran.Visibility = Android.Views.ViewStates.Invisible;

            RelativeLayout yukleme = FindViewById<RelativeLayout>(Resource.Id.Yukleme);
            yukleme.Visibility = Android.Views.ViewStates.Visible;






            tableItems.Clear();


            InstagramApiSharp.API.IInstaApi instaApi;

            if (_instaApi != null)
                instaApi = _instaApi;
            else
                instaApi = _instaApi2;

            TextView takipci = FindViewById<TextView>(Resource.Id.textView1);
            TextView takipciler = FindViewById<TextView>(Resource.Id.takipciler);
            TextView takipedilenler = FindViewById<TextView>(Resource.Id.takipedilenler);
            TextView kullaniciAdi = FindViewById<TextView>(Resource.Id.kullaniciAdi);

            var result = await instaApi.UserProcessor.GetUserFollowersAsync(instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
            var followers = result.Value;
            var anyDuplicate = followers.GroupBy(x => x.Pk).Any(g => g.Count() > 1);
            //button.Text = $"{ followers.Count} takipci";


            var result2 = await instaApi.UserProcessor.GetUserFollowingAsync(instaApi.GetLoggedUser().LoggedInUser.UserName, PaginationParameters.MaxPagesToLoad(5));
            var following = result2.Value;


            // var takipetmeyenler = followers.Except(following).ToList();
            var takipetmeyenler = following.Except(followers).ToList();
            var hayranlar = followers.Except(following).ToList();
            //List<string> adlar = new List<string>();


            foreach (var item in db.TakipEtmeyenler)
            {
                db.TakipEtmeyenler.Remove(item);
            }
            await db.SaveChangesAsync();

            takipci.Text = "Takip Etmeyen: " + takipetmeyenler.Count.ToString();
            takipciler.Text = "Takipçi: " + followers.Count.ToString();
            takipedilenler.Text = "Takip: " + following.Count.ToString();
            kullaniciAdi.Text = instaApi.GetLoggedUser().LoggedInUser.UserName;

            foreach (var item in takipetmeyenler)
            {

                // adlar.Add(item.UserName);

                tableItems.Add(new TableItem()
                {
                    kullaniciAdi = item.UserName,
                    AdiSoyadi = item.FullName,
                    Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                    userId = item.Pk
                });
                if (!db.TakipEtmeyenler.Contains(item))
                {
                    await db.TakipEtmeyenler.AddAsync(item);
                }
            }
            foreach (var item in hayranlar)
            {
                hayranItem.Add(new TableItem()
                {
                    kullaniciAdi = item.UserName,
                    AdiSoyadi = item.FullName,
                    Resim = GetBitmapFromUrl(item.ProfilePicUrl),
                    userId = item.Pk


                });
            }



            await db.SaveChangesAsync();
            yukleme.Visibility = Android.Views.ViewStates.Invisible;
            anaekran.Visibility = Android.Views.ViewStates.Visible;
            ActionBar.Show();
        }

        //void listeItemineTiklandi(object sender, AdapterView.ItemClickEventArgs e)
        //{
        //    var listView = sender as ListView;
        //    var t = tableItems[e.Position];
        //    Android.Widget.Toast.MakeText(this, t.kullaniciAdi, Android.Widget.ToastLength.Short).Show();
        //    var uri = Android.Net.Uri.Parse("http://instagram.com/" + t.kullaniciAdi);
        //    var intent = new Intent(Intent.ActionView, uri);
        //    StartActivity(intent);
        //}





        public Bitmap GetBitmapFromUrl(string url)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] bytes = webClient.DownloadData(url);

                if (bytes != null && bytes.Length > 0)
                {
                    return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
                }
            }
            return null;
        }


    }
}

