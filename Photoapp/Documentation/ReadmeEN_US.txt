
To initialize your app put in your App.cs this
Obs: if using App.xaml delete and create a class;) do not be lazy!

public class App : BootStrapper{
    public static new App Current {get;set;}
    
    public App(){
       NavigationServiceFactory(new NavigationPage(new YourPage()));
    }
    
}
============================================


If you are using mvvm all your viewmodels will inherit from ViewModelBase (FormsExtensions.Mvvm)

To add a navigation create an AppViewModelBase that extends from ViewModelBase (FormsExtensions.Mvvm) and
Add this

public INavigationService NavigationService { get { return App.Current.NavigationService; } }

navigate between pages in your viewmodel

NavigationService.NavigateAsync(new YourPage(),parametro,bool animado);
NavigationService.NavigateModalAsync(new YourPage(),parametro,bool animated);
NavigationService.GoBack(bool animated);
NavigationService.ModalGoBack(bool animated);
NavigationService.ClearHistory(bool animated);


In your viewmodelbase you will have the following overwritten methods

       public virtual async Task OnNavigatedFromAsync() 
        {
          //... Loads the viewmodel when it returns from a page
        }

        public virtual async Task OnNavigatedToAsync(object parameter, NavigationMode mode) 
        {
           object parameter Retrieves the last parameter in the navigationservice
           
           LoadData((MyObject)parameter);   //... Loads the viewmodel          
        
        }

        public virtual async Task OnNavigatingToAsync(object parameter, NavigationMode mode)
        {
        // Carry on how much
        }
==============================================================  
Styles

   public class DefaultAppStyleKit : StyleKit
    {
        public DefaultAppStyleKit()
        {
            PrimaryColor = Color.FromHex("#9C27B0");
            PrimaryDarkColor = Color.FromHex("#7B1FA2");
            PrimaryLightColor = Color.FromHex("#E1BEE7");
            SecondaryColor = Color.FromHex("#9C27B0");
            SecondaryDarkColor = Color.FromHex("#7B1FA2");
            AccentColor = Color.FromHex("#FF4081");
            PrimaryTextColor = Color.FromHex("#212121");
            SecondaryTextColor = Color.FromHex("#ffffff");
            DividerColor = Color.FromHex("#B6B6B6");
            WindowColor = PrimaryDarkColor;
        }
    }
    
    
In your App.cs put this
    
    public App(){
      StyleKit = new DefaultAppStyleKit();
     ...
     
     acces stylekit
     
     var label = new Label(){
        TextColor = App.Current.StyleKit.PrimaryColor
     }
    
===============================================================================
new controls

FloatingActionButtonView somente android
CheckBox android ios
RadioButton android ios -> boa pratica use junto com o BindableRadioGroup
BindableRadioGroup android ios
ImageButton android ios
LabeledEntry android, ios embreve
HyperLinkLabel android ios
BottomTabbedPage android (ios ja é por padrão)
ContentPageBase android ios
TabbedPageBase android ios
TapViewBehavior android ios
ImageGalleryVew android ios
BindableToolbarItem android ios


========================================================================
helpers 
LocationHelper android ios

=========================================================================
extensions 
convert  list,ienumerable etc.. mylist.ToObservableCollection(); android ios


Attention to some controls may not work right on iOs