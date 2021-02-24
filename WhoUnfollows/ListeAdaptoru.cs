using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using InstagramApiSharp.API;
using Uri = Android.Net.Uri;

namespace WhoUnfollows
{
    public class ListeAdaptoru : BaseAdapter<TableItem>
    {
        private readonly Activity context;
        private readonly IInstaApi gelen;
        private readonly List<TableItem> items;


        public ListeAdaptoru(Activity context, List<TableItem> items, IInstaApi instaApi)
        {
            gelen = instaApi ?? throw new ArgumentNullException(nameof(instaApi));
            this.context = context;
            this.items = items;
        }

        public override TableItem this[int position] => items[position];

        public override int Count => items.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];
            var view = convertView;
            if (view == null) // no view to re-use, create new
                view = context.LayoutInflater.Inflate(Resource.Layout.takipciler, null);
            view.FindViewById<TextView>(Resource.Id.Text1).Text = item.kullaniciAdi;
            view.FindViewById<TextView>(Resource.Id.Text2).Text = item.AdiSoyadi;
            view.FindViewById<ImageView>(Resource.Id.Image).SetImageBitmap(item.Resim);
            view.FindViewById<ImageView>(Resource.Id.Image).Tag = position;
            view.FindViewById<ImageView>(Resource.Id.Image).Click += profilac;


            var b = view.FindViewById<Button>(Resource.Id.Buttonn);

            b.Tag = position;
            b.Click += deneme;

            return view;
        }

        private void profilac(object sender, EventArgs e)
        {
            var resim = sender as ImageView;
            var itemid = (int) resim.Tag;

            var item = items[itemid];
            // Android.Widget.Toast.MakeText(this, item.kullaniciAdi, Android.Widget.ToastLength.Short).Show();
            var uri = Uri.Parse("http://instagram.com/" + item.kullaniciAdi);
            var intent = new Intent(Intent.ActionView, uri);
            context.StartActivity(intent);
            // StartActivity(intent);
        }

        private void deneme(object sender, EventArgs e)
        {
            var button = sender as Button;
            var iteminidsi = (int) button.Tag;
            var item = items[iteminidsi];
            var cevap = gelen.UserProcessor.UnFollowUserAsync(item.userId);
            cevap.RunSynchronously();
            if (cevap.Result.Succeeded)
            {
                button.Text = "Çıkıldı";
                button.Enabled = false;
            }
            
        }
    }
}