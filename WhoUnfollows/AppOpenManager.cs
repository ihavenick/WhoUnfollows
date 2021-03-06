using Android.App;
using Android.Content;
using Android.Gms.Ads;
using Android.Gms.Ads.AppOpen;
using Java.Lang;

namespace WhoUnfollows
{
    public class AppOpenManager : AppOpenAd.AppOpenAdLoadCallback
    {
        public Activity activity;
        public Context Context;

        public AppOpenManager(Activity act, Context ctx)
        {
            activity = act;
            Context = ctx;
        }

        public void Fetch()
        {
            var adRequest = new AdRequest.Builder().Build();
            AppOpenAd.Load(Context,"ca-app-pub-9927527797473679/6279632383",adRequest,AppOpenAd.AppOpenAdOrientationPortrait,this);
        }

        public void Show(AppOpenAd appOpenAd)
        {
            appOpenAd.Show(activity, new FullScreenContentCallback());
        }

        public override void OnAppOpenAdLoaded(AppOpenAd appOpenAd)
        {
            Show(appOpenAd);
        }
    }
}