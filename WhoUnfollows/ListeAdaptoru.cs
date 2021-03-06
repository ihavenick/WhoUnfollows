﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ProgressDialog progress;

        public ListeAdaptoru(Activity context, List<TableItem> items, IInstaApi instaApi,ProgressDialog pd)
        {
            gelen = instaApi ?? throw new ArgumentNullException(nameof(instaApi));
            this.context = context;
            this.items = items;

            this.progress = pd;

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
            var view =  context.LayoutInflater.Inflate(Resource.Layout.takipciler, null);
            view.FindViewById<TextView>(Resource.Id.Text1).Text = item.kullaniciAdi;
            view.FindViewById<TextView>(Resource.Id.Text2).Text = item.AdiSoyadi;
            view.FindViewById<ImageView>(Resource.Id.Image).SetImageBitmap(item.Resim);
            view.FindViewById<ImageView>(Resource.Id.Image).Tag = item.kullaniciAdi;
            view.FindViewById<ImageView>(Resource.Id.Image).Click += profilac;

            view.FindViewById<Button>(Resource.Id.Buttonn).Tag = item.userId;
            view.FindViewById<Button>(Resource.Id.Buttonn).Click +=  deneme;
            
           

            return view;
        }

        private void profilac(object sender, EventArgs e)
        {
            var resim = sender as ImageView;
            var kullaniciadi =  resim.Tag;

            
            // Android.Widget.Toast.MakeText(this, item.kullaniciAdi, Android.Widget.ToastLength.Short).Show();
            var uri = Uri.Parse("http://instagram.com/" + kullaniciadi);
            var intent = new Intent(Intent.ActionView, uri);
            context.StartActivity(intent);
            // StartActivity(intent);
        }

        private void deneme(object sender, EventArgs e)
        {
           
            progress.SetTitle("Takipten cıkılıyor");
            progress.SetMessage("Lütfen bekleyin...");
            progress.SetCancelable(false); // disable dismiss by tapping outside of the dialog
            progress.Show();

            
          
            var button = sender as Button;

            var kullaniciid = (long) button.Tag;
            var cevap = Task.Run(async () => await gelen.UserProcessor.UnFollowUserAsync(kullaniciid)).Result;
            
            if (!cevap.Succeeded) return;

            button.Click -= deneme;
            
            Toast.MakeText(context, "Takipten cıkılıyor", ToastLength.Short).Show();  
            
            items.Remove(items.SingleOrDefault(x => x.userId == kullaniciid));
            
            NotifyDataSetChanged();
            
            progress.Dismiss();
        }
    }
}