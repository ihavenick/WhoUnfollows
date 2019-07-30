using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace WhoUnfollows
{
    public class TableItem
    {
        public string kullaniciAdi { get; set; }
        public string AdiSoyadi { get; set; }
        public long userId { get; set; }
        public Bitmap Resim { get; set; }

    }
}