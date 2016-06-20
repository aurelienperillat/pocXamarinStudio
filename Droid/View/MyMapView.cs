using System;

using Android.App;
using Android.Content;
using Android.Widget;
using MvvmCross.Droid.Views;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using static Android.Gms.Maps.GoogleMap;
using Android.Support.V4.Content;
using Android;
using Android.Content.PM;
using Android.Locations;
using PocAlim.ViewModels;
using PocAlim.Services;
using Android.Gms.Common;
using Android.Views;
using Android.Net;

namespace PocAlim.Droid.View
{

    /**Classe de cr�ation de la map
     * et ajout des markers**/
    [Activity(Label = "Map", Theme = "@style/MyTheme.NoTitle")]
    public class MyMapView : MvxActivity, IOnMapReadyCallback, Android.Gms.Maps.GoogleMap.IOnMyLocationButtonClickListener
    {

		public static readonly int InstallGooglePlayServicesId = 1000;
		private bool _isGooglePlayServicesInstalled;

        private GoogleMap _gMap;
        private Marker _marker;
        LocationManager _locationManager;


        //Specification du ViewModel
        public new FillingListOfMyPOIViewModel ViewModel
        {
            get { return (FillingListOfMyPOIViewModel)base.ViewModel; }
            set { base.ViewModel = value; }
        }
        //Une fois le ViewModel charg� on genere la vue
        protected override void OnViewModelSet()
        {
            base.OnViewModelSet();

			//On v�rifie si les Google Play Services sont dispo
			_isGooglePlayServicesInstalled = TestIfGooglePlayServicesIsInstalled();
			// Si OUI, on charge le Layout de mani�re classique
			if (_isGooglePlayServicesInstalled) {
				SetContentView (Resource.Layout.View_Map);

				//On v�rifie la connexion internet
				bool test = isNetworkConnected();
				if (!test) {
					Toast.MakeText(this, "La connexion internet est necessaire", ToastLength.Short).Show();
				}


				if (_gMap == null) {
					FragmentManager.FindFragmentById<MapFragment> (Resource.Id.map).GetMapAsync (this);
				}
					
			}
			//sinon on cache notre view pour laisser la proposition
			//d'installation de google play services
			else
			{
				SetContentView(Resource.Layout.View_Map);
				if (_gMap == null)
					FragmentManager.FindFragmentById<MapFragment> (Resource.Id.map).GetMapAsync (this);
				FrameLayout myLayout = (FrameLayout)FindViewById(Resource.Id.myLayout);
				myLayout.Visibility = ViewStates.Invisible;
			}
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            _gMap = googleMap;

			//Autorisation et positionnement du boutton zoom
            _gMap.UiSettings.ZoomControlsEnabled = true;

            //Verification des permissions  de localisation
            checkLocationPermission();

            //Listener sur click d'un marker
            _gMap.MarkerClick += MapOnMarkerClick;

            //Listener sur click de la map
            _gMap.MapClick += MapOnMapClick;

            //Position de d�part de la camera
            moveCameraStart();

            //parcours de la liste de markers du ViewModel
            //et ajout des markers � la map
            addMarkers();

            _gMap.SetInfoWindowAdapter(new CustomMarkerPopupAdapter(LayoutInflater));
        }

		//verification de l'�tat de la connexion
		private Boolean isNetworkConnected()
		{
			ConnectivityManager cm = (ConnectivityManager)GetSystemService(Context.ConnectivityService);

			return cm.ActiveNetworkInfo != null;
		}

        //Verification de l'autorisation de localisation
        public void checkLocationPermission()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation)
                == (int)Permission.Granted)
            {
                //Affichage du Bouton de localisation google
                _gMap.MyLocationEnabled = true;
                _gMap.SetOnMyLocationButtonClickListener(this);
            }
            else
            {
                Toast.MakeText(this, "Location Permissions are required !", ToastLength.Short).Show();
            }

        }

        //Listener du bouton de localisation google
        bool Android.Gms.Maps.GoogleMap.IOnMyLocationButtonClickListener.OnMyLocationButtonClick()
        {
            bool _isGpsEnable = false;
            _isGpsEnable=checkGPS();

            if (_isGpsEnable)
            {
                //le gps est activ�
                //return false zoom sur la localisation
                return false;
            }
            else
            {
                Toast.MakeText(this, String.Format("Veuillez Activer le GPS"), ToastLength.Short).Show();
                return true;
            }
        }

        //v�rification de l'activation du GPS
        public bool checkGPS()
        {
            _locationManager = GetSystemService(Context.LocationService) as LocationManager;

            string provider = LocationManager.GpsProvider;

            if (_locationManager.IsProviderEnabled(provider))
            {
                return true;
            }
            return false;
        }
      

        private void MapOnMapClick(object sender, GoogleMap.MapClickEventArgs mapClickEventArgs)
        {
           // Toast.MakeText(this, String.Format("You clicked on the MAP"), ToastLength.Short).Show();

        }

        private void MapOnMarkerClick(object sender, GoogleMap.MarkerClickEventArgs markerClickEventArgs)
        {
            markerClickEventArgs.Handled = true;
            Marker marker = markerClickEventArgs.Marker;

            //zoom avec animation sur le marker
            //cliqu� avec un d�callage pour
            //laisser de la place � la infowindow
             animateCameraOnMarker(marker);
            
            //affichage des infos
            marker.ShowInfoWindow();
        }

        //zoom avec animation sur le marker
        //cliqu� avec un d�callage pour
        //laisser de la place � la infowindow
        public void animateCameraOnMarker(Marker marker)
        {
            double _latToZoom = marker.Position.Latitude;
            double _lngToZoom = marker.Position.Longitude;

            _gMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(_latToZoom,_lngToZoom), _gMap.CameraPosition.Zoom));
        }

        //Position de d�part de la camera
        public void moveCameraStart()
        {
                LatLng location = new LatLng(48.828808,2.261146);
                CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
                builder.Target(location);
                builder.Zoom(14);
                CameraPosition cameraPosition = builder.Build();
                CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);

            if (_gMap != null)
            {
                _gMap.MoveCamera(cameraUpdate);
            }
           
        }
        //parcours de la liste de markers du ViewModel
        //et ajout des markers � la map
        public void addMarkers()
        {
			try{
				
			foreach (MyPOI marker in ViewModel.MarkerListFiltre)
                {
                    var option = new MarkerOptions();
                    option.SetPosition(new LatLng(marker.Coord.Lat, marker.Coord.Lng));
                    option.SetTitle(marker.Nom);

					//le poi poss�de au moins deux type
					if (marker.Type.Contains (","))
					option.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Mipmap.marker_generique));
					
				//le poi ne poss�de qu'un type
				    else if (marker.Type.Contains("Restauration Collective"))
					option.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Mipmap.marker_restauration_collective));
					else if (marker.Type.Contains("Alimentation Generale"))
					option.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Mipmap.marker_alimentation_generale));
					else if (marker.Type.Contains("Supermarches Hypermarches"))
					option.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Mipmap.marker_supermarches_hypermarches));
					else if (marker.Type.Contains("Charcuteries"))
					option.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Mipmap.marker_charcuteries));

					if (_gMap != null) {
						_marker = _gMap.AddMarker (option);
					}
				}
			}
			catch (FormatException)
			{
				
			}catch (OverflowException)
			{
			}

        }

		private bool TestIfGooglePlayServicesIsInstalled()
		{
			int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
			if (queryResult == ConnectionResult.Success)
			{
				return true;
			}
			return false;
		}

		public override void OnBackPressed()
		{
		}

		protected override void OnResume()
		{
			base.OnResume();

			Toast.MakeText(this, "onresume 1", ToastLength.Short).Show();
			_isGooglePlayServicesInstalled = TestIfGooglePlayServicesIsInstalled();
			Toast.MakeText(this, "onresume 2", ToastLength.Short).Show();

			if (_isGooglePlayServicesInstalled)
			{
				FrameLayout myLayout = (FrameLayout)FindViewById(Resource.Id.myLayout);
				if (myLayout.Visibility == ViewStates.Invisible)
					myLayout.Visibility = ViewStates.Visible;
				Toast.MakeText(this, "onresume 3", ToastLength.Short).Show();

			}
			Toast.MakeText(this, "onresume 4", ToastLength.Short).Show();

		}
    }

}